module TraktImport

open System
open Shared.Domain

// =====================================
// Trakt Import Service
// =====================================
// Handles the process of importing data from Trakt.tv
// into the Cinemarco library

// =====================================
// Import State
// =====================================

type private ImportState = {
    InProgress: bool
    CurrentItem: string option
    Completed: int
    Total: int
    Errors: string list
    CancellationRequested: bool
}

let private importState = ref {
    InProgress = false
    CurrentItem = None
    Completed = 0
    Total = 0
    Errors = []
    CancellationRequested = false
}

/// Get the current import status
let getStatus () : ImportStatus =
    let state = !importState
    {
        InProgress = state.InProgress
        CurrentItem = state.CurrentItem
        Completed = state.Completed
        Total = state.Total
        Errors = state.Errors
    }

/// Request cancellation of the import
let requestCancellation () =
    importState := { !importState with CancellationRequested = true }

/// Reset import state
let private resetState () =
    importState := {
        InProgress = false
        CurrentItem = None
        Completed = 0
        Total = 0
        Errors = []
        CancellationRequested = false
    }

let private updateProgress item completed =
    importState := { !importState with CurrentItem = Some item; Completed = completed }

let private addError error =
    let state = !importState
    importState := { state with Errors = state.Errors @ [error] }

let private setTotal total =
    importState := { !importState with Total = total }

let private isCancelled () = (!importState).CancellationRequested

// =====================================
// Import Preview
// =====================================

/// Get a preview of what will be imported from Trakt
let getImportPreview (options: TraktImportOptions) : Async<Result<TraktImportPreview, string>> = async {
    let mutable movieItems: TraktHistoryItem list = []
    let mutable seriesItems: TraktHistoryItem list = []

    // Get watched movies if requested
    let! moviesError =
        if options.ImportWatchedMovies then
            async {
                let! moviesResult = TraktClient.getWatchedMovies()
                match moviesResult with
                | Ok movies ->
                    movieItems <- movies
                    return None
                | Error err -> return Some $"Failed to fetch watched movies: {err}"
            }
        else async { return None }

    match moviesError with
    | Some err -> return Error err
    | None ->

    // Get watched series if requested
    let! seriesError =
        if options.ImportWatchedSeries then
            async {
                let! seriesResult = TraktClient.getWatchedShows()
                match seriesResult with
                | Ok series ->
                    seriesItems <- series
                    return None
                | Error err -> return Some $"Failed to fetch watched series: {err}"
            }
        else async { return None }

    match seriesError with
    | Some err -> return Error err
    | None ->

    // Get watchlist items if requested
    let! watchlistError =
        if options.ImportWatchlist then
            async {
                let! watchlistResult = TraktClient.getWatchlist()
                match watchlistResult with
                | Ok items ->
                    let watchlistMovies = items |> List.filter (fun i -> i.MediaType = Movie)
                    let watchlistSeries = items |> List.filter (fun i -> i.MediaType = Series)
                    movieItems <- movieItems @ watchlistMovies
                    seriesItems <- seriesItems @ watchlistSeries
                    return None
                | Error err -> return Some $"Failed to fetch watchlist: {err}"
            }
        else async { return None }

    match watchlistError with
    | Some err -> return Error err
    | None ->

    // Deduplicate by TMDB ID
    let uniqueMovies = movieItems |> List.distinctBy (fun m -> m.TmdbId)
    let uniqueSeries = seriesItems |> List.distinctBy (fun s -> s.TmdbId)

    // Check which items are already in the library
    let! alreadyInLibraryMovies = async {
        let! results =
            uniqueMovies
            |> List.map (fun m -> async {
                let! existing = Persistence.isMovieInLibrary (TmdbMovieId m.TmdbId)
                return m.TmdbId, existing.IsSome
            })
            |> Async.Sequential
        return results |> Array.toList |> List.filter snd |> List.map fst |> Set.ofList
    }

    let! alreadyInLibrarySeries = async {
        let! results =
            uniqueSeries
            |> List.map (fun s -> async {
                let! existing = Persistence.isSeriesInLibrary (TmdbSeriesId s.TmdbId)
                return s.TmdbId, existing.IsSome
            })
            |> Async.Sequential
        return results |> Array.toList |> List.filter snd |> List.map fst |> Set.ofList
    }

    // Convert to TmdbSearchResult for preview display
    let movieResults: TmdbSearchResult list =
        uniqueMovies
        |> List.map (fun m -> {
            TmdbId = m.TmdbId
            MediaType = Movie
            Title = m.Title
            ReleaseDate = None
            PosterPath = None
            Overview = None
            VoteAverage = None
        })

    let seriesResults: TmdbSearchResult list =
        uniqueSeries
        |> List.map (fun s -> {
            TmdbId = s.TmdbId
            MediaType = Series
            Title = s.Title
            ReleaseDate = None
            PosterPath = None
            Overview = None
            VoteAverage = None
        })

    let totalItems = uniqueMovies.Length + uniqueSeries.Length
    let alreadyInLibrary = alreadyInLibraryMovies.Count + alreadyInLibrarySeries.Count
    let newItems = totalItems - alreadyInLibrary

    return Ok {
        Movies = movieResults
        Series = seriesResults
        TotalItems = totalItems
        AlreadyInLibrary = alreadyInLibrary
        NewItems = newItems
    }
}

// =====================================
// Import Execution
// =====================================

/// Import a single movie from Trakt
let private importMovie (item: TraktHistoryItem) (rating: int option) : Async<Result<unit, string>> = async {
    let tmdbId = TmdbMovieId item.TmdbId

    // Check if already in library
    let! existing = Persistence.isMovieInLibrary tmdbId
    match existing with
    | Some entryId ->
        // Movie already exists - add a new watch session if we have a watched date
        // but only if no session exists for that date already
        match item.WatchedAt with
        | Some watchedDate ->
            let! sessionExists = Persistence.movieWatchSessionExistsForDate entryId watchedDate
            if not sessionExists then
                let request: CreateMovieWatchSessionRequest = {
                    EntryId = entryId
                    WatchedDate = watchedDate
                    Friends = []
                    Name = Some "Imported from Trakt"
                }
                let! _ = Persistence.insertMovieWatchSession request
                // Mark the movie as watched
                do! Persistence.markMovieWatched entryId (Some watchedDate)
        | None -> ()

        // Update rating if we have one and the entry doesn't have a rating yet
        match rating with
        | Some r ->
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e when e.PersonalRating.IsNone ->
                let mappedRating = TraktClient.mapTraktRating r
                do! Persistence.updatePersonalRating entryId (Some mappedRating)
            | _ -> ()
        | None -> ()

        return Ok ()
    | None ->
        // New movie - add to library
        let request: AddMovieRequest = {
            TmdbId = tmdbId
            WhyAdded = Some {
                RecommendedBy = None
                RecommendedByName = None
                Source = Some "Trakt Import"
                Context = None
                DateRecommended = None
            }
            InitialFriends = []
        }

        // Fetch movie details from TMDB
        let! detailsResult = TmdbClient.getMovieDetails tmdbId
        match detailsResult with
        | Error err -> return Error $"Failed to fetch movie details for {item.Title}: {err}"
        | Ok details ->
            // Cache poster and backdrop images
            do! ImageCache.downloadPoster details.PosterPath
            do! ImageCache.downloadBackdrop details.BackdropPath

            let! entryResult = Persistence.insertLibraryEntryForMovie details request
            match entryResult with
            | Error err -> return Error err
            | Ok entry ->
                // Create a watch session if we have a watched date
                match item.WatchedAt with
                | Some watchedDate ->
                    let sessionRequest: CreateMovieWatchSessionRequest = {
                        EntryId = entry.Id
                        WatchedDate = watchedDate
                        Friends = []
                        Name = Some "Imported from Trakt"
                    }
                    let! _ = Persistence.insertMovieWatchSession sessionRequest
                    // Also mark the movie as watched (updates watch_status to Completed)
                    do! Persistence.markMovieWatched entry.Id (Some watchedDate)
                | None -> ()

                // Set rating if we have one
                match rating with
                | Some r ->
                    let mappedRating = TraktClient.mapTraktRating r
                    do! Persistence.updatePersonalRating entry.Id (Some mappedRating)
                | None -> ()

                return Ok ()
}

/// Simple episode import - uses Trakt watched dates directly (for incremental sync)
/// Does NOT apply binge-detection or air date substitution
let private importEpisodeWatchDataSimple (entryId: EntryId) (seriesId: SeriesId) (watchedEpisodes: TraktWatchedEpisode list) : Async<unit> = async {
    let! defaultSession = Persistence.getDefaultSession entryId
    match defaultSession with
    | None ->
        printfn "[TraktSync] ERROR: No default session for entry %d - episodes will not be synced!" (EntryId.value entryId)
    | Some session ->
        for ep in watchedEpisodes do
            match ep.WatchedAt with
            | None ->
                printfn "[TraktSync] Warning: Episode S%02dE%02d has no watched date, skipping" ep.SeasonNumber ep.EpisodeNumber
            | Some _ ->
                do! Persistence.insertEpisodeProgressWithDate
                        session.Id
                        entryId
                        seriesId
                        ep.SeasonNumber
                        ep.EpisodeNumber
                        ep.WatchedAt
        // Update the watch status based on imported episode progress
        do! Persistence.updateSeriesWatchStatusFromProgress entryId
}

/// Import episode watch data for an existing series entry (FULL IMPORT)
/// If more than 4 episodes were watched on the same day, uses original air dates instead
let private importEpisodeWatchData (entryId: EntryId) (seriesId: SeriesId) (watchedEpisodes: TraktWatchedEpisode list) : Async<unit> = async {
    printfn "[TraktImport] importEpisodeWatchData called with %d episodes for seriesId %A" watchedEpisodes.Length (SeriesId.value seriesId)

    // Get the default session for this entry
    let! defaultSession = Persistence.getDefaultSession entryId
    match defaultSession with
    | None ->
        printfn "[TraktImport] ERROR: No default session found for entry %A" (EntryId.value entryId)
    | Some session ->
        printfn "[TraktImport] Found session %A" (SessionId.value session.Id)

        // Group episodes by watched date (day only) to detect binge-watching
        let episodesByDate =
            watchedEpisodes
            |> List.groupBy (fun ep ->
                ep.WatchedAt |> Option.map (fun d -> d.Date))

        // Debug: show grouping
        for (date, eps) in episodesByDate do
            printfn "[TraktImport] Date group: %A -> %d episodes" date eps.Length

        // Check if any date has more than 4 episodes (binge-watching threshold)
        let hasBingeDay =
            episodesByDate
            |> List.exists (fun (date, eps) -> date.IsSome && eps.Length > 4)

        printfn "[TraktImport] hasBingeDay: %b" hasBingeDay

        // If binge-watching detected, fetch air dates from database
        let! airDates =
            if hasBingeDay then
                printfn "[TraktImport] Fetching air dates for seriesId %d..." (SeriesId.value seriesId)
                Persistence.getEpisodeAirDates seriesId
            else
                async { return Map.empty }

        printfn "[TraktImport] Fetched %d air dates from database" airDates.Count

        // Debug: show first few air dates if any
        if airDates.Count > 0 then
            let sample = airDates |> Map.toSeq |> Seq.truncate 3 |> Seq.toList
            for ((s, e), d) in sample do
                printfn "[TraktImport] Sample air date: S%02dE%02d -> %A" s e d

        for ep in watchedEpisodes do
            // Determine the watch date to use
            let watchDate =
                match ep.WatchedAt with
                | Some watchedAt ->
                    // Check if this episode was part of a binge day (>4 episodes on same day)
                    let sameDayEpisodes =
                        watchedEpisodes
                        |> List.filter (fun e ->
                            match e.WatchedAt with
                            | Some d -> d.Date = watchedAt.Date
                            | None -> false)

                    if sameDayEpisodes.Length > 4 then
                        // Use air date if available, otherwise fall back to watched date
                        match Map.tryFind (ep.SeasonNumber, ep.EpisodeNumber) airDates with
                        | Some airDate ->
                            printfn "[TraktImport] Using air date %A instead of watched date %A for S%02dE%02d (binge day: %d episodes)"
                                airDate watchedAt ep.SeasonNumber ep.EpisodeNumber sameDayEpisodes.Length
                            Some airDate
                        | None -> Some watchedAt  // No air date available, keep original
                    else
                        Some watchedAt  // Not a binge day, keep original
                | None -> None

            do! Persistence.insertEpisodeProgressWithDate
                    session.Id
                    entryId
                    seriesId
                    ep.SeasonNumber
                    ep.EpisodeNumber
                    watchDate
        // Update the watch status based on imported episode progress
        do! Persistence.updateSeriesWatchStatusFromProgress entryId
}

/// Import a single series from Trakt (with episode-level watch data)
let private importSeriesWithEpisodes (item: TraktWatchedSeries) (rating: int option) : Async<Result<unit, string>> = async {
    let tmdbId = TmdbSeriesId item.TmdbId
    printfn "[TraktImport] >>> importSeriesWithEpisodes START: %s (TMDB: %d)" item.Title item.TmdbId

    try
        // Check if already in library
        let! existing = Persistence.isSeriesInLibrary tmdbId
        printfn "[TraktImport] isSeriesInLibrary result: %A" existing
        
        match existing with
        | Some entryId ->
            printfn "[TraktImport] Series already exists with entryId: %d" (EntryId.value entryId)
            // Series already exists - import episode watch data (no binge-detection for already tracked series)
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e ->
                match e.Media with
                | LibrarySeries series ->
                    // First: ensure we have episode air dates in DB (fetch from TMDB if needed)
                    let seasonNumbers =
                        item.WatchedEpisodes
                        |> List.map (fun ep -> ep.SeasonNumber)
                        |> List.distinct

                    // Check if we already have episodes for these seasons
                    let! existingAirDates = Persistence.getEpisodeAirDates series.Id
                    let missingSeasons =
                        seasonNumbers
                        |> List.filter (fun seasonNum ->
                            // Check if any episode from this season has an air date
                            not (existingAirDates |> Map.exists (fun (s, _) _ -> s = seasonNum)))

                    if not (List.isEmpty missingSeasons) then
                        printfn "[TraktImport] Fetching %d missing seasons from TMDB for air dates..." missingSeasons.Length
                        for seasonNum in missingSeasons do
                            let! seasonResult = TmdbClient.getSeasonDetails tmdbId seasonNum
                            match seasonResult with
                            | Ok seasonDetails ->
                                do! Persistence.saveSeasonEpisodes series.Id seasonDetails
                                // Cache season poster and episode stills
                                do! ImageCache.cacheSeasonImages seasonDetails.PosterPath (seasonDetails.Episodes |> List.map (fun e -> e.StillPath))
                                printfn "[TraktImport] Saved season %d with %d episodes" seasonNum seasonDetails.Episodes.Length
                            | Error err ->
                                printfn "[TraktImport] Warning: Failed to fetch season %d: %s" seasonNum err

                    // Use simple import (no binge-detection) for already tracked series
                    do! importEpisodeWatchDataSimple entryId series.Id item.WatchedEpisodes
                | _ ->
                    printfn "[TraktImport] WARNING: Entry media is not LibrarySeries!"

                // Update rating if we have one and the entry doesn't have a rating yet
                match rating with
                | Some r when e.PersonalRating.IsNone ->
                    let mappedRating = TraktClient.mapTraktRating r
                    do! Persistence.updatePersonalRating entryId (Some mappedRating)
                | _ -> ()
            | None -> 
                printfn "[TraktImport] WARNING: Could not fetch entry by id %d" (EntryId.value entryId)

            printfn "[TraktImport] <<< importSeriesWithEpisodes END (existing): %s" item.Title
            return Ok ()
        | None ->
            printfn "[TraktImport] Series not in library, creating new entry..."
            // New series - add to library
            let request: AddSeriesRequest = {
                TmdbId = tmdbId
                WhyAdded = Some {
                    RecommendedBy = None
                    RecommendedByName = None
                    Source = Some "Trakt Import"
                    Context = None
                    DateRecommended = None
                }
                InitialFriends = []
            }

            // Fetch series details from TMDB
            printfn "[TraktImport] Fetching series details from TMDB..."
            let! detailsResult = TmdbClient.getSeriesDetails tmdbId
            match detailsResult with
            | Error err -> 
                printfn "[TraktImport] <<< FAILED to fetch TMDB details: %s" err
                return Error $"Failed to fetch series details for {item.Title}: {err}"
            | Ok details ->
                printfn "[TraktImport] Got TMDB details: %s" details.Name
                // Cache poster and backdrop images
                do! ImageCache.downloadPoster details.PosterPath
                do! ImageCache.downloadBackdrop details.BackdropPath

                printfn "[TraktImport] Inserting library entry..."
                let! entryResult = Persistence.insertLibraryEntryForSeries details request
                match entryResult with
                | Error err -> 
                    printfn "[TraktImport] <<< FAILED to insert library entry: %s" err
                    return Error err
                | Ok entry ->
                    printfn "[TraktImport] Created library entry with id: %d" (EntryId.value entry.Id)
                    match entry.Media with
                    | LibrarySeries series ->
                        // First: fetch and save episode data from TMDB so we have air dates for binge-detection
                        // Get unique season numbers from watched episodes
                        let seasonNumbers =
                            item.WatchedEpisodes
                            |> List.map (fun ep -> ep.SeasonNumber)
                            |> List.distinct

                        printfn "[TraktImport] Fetching %d seasons from TMDB for air dates..." seasonNumbers.Length

                        for seasonNum in seasonNumbers do
                            let! seasonResult = TmdbClient.getSeasonDetails tmdbId seasonNum
                            match seasonResult with
                            | Ok seasonDetails ->
                                do! Persistence.saveSeasonEpisodes series.Id seasonDetails
                                // Cache season poster and episode stills
                                do! ImageCache.cacheSeasonImages seasonDetails.PosterPath (seasonDetails.Episodes |> List.map (fun e -> e.StillPath))
                                printfn "[TraktImport] Saved season %d with %d episodes" seasonNum seasonDetails.Episodes.Length
                            | Error err ->
                                printfn "[TraktImport] Warning: Failed to fetch season %d: %s" seasonNum err

                        // Now import episode watch data (can use air dates from DB)
                        printfn "[TraktImport] Importing episode watch data..."
                        do! importEpisodeWatchData entry.Id series.Id item.WatchedEpisodes
                        printfn "[TraktImport] Episode watch data imported"
                    | _ -> 
                        printfn "[TraktImport] WARNING: Created entry media is not LibrarySeries!"

                    // Set rating if we have one
                    match rating with
                    | Some r ->
                        let mappedRating = TraktClient.mapTraktRating r
                        do! Persistence.updatePersonalRating entry.Id (Some mappedRating)
                    | None -> ()

                    printfn "[TraktImport] <<< importSeriesWithEpisodes END (new): %s" item.Title
                    return Ok ()
    with
    | ex ->
        printfn "[TraktImport] <<< EXCEPTION in importSeriesWithEpisodes for %s: %s" item.Title ex.Message
        printfn "[TraktImport] Stack trace: %s" ex.StackTrace
        return Error $"Exception importing {item.Title}: {ex.Message}"
}

/// Import episode watch data for an EXISTING series (incremental sync only)
/// Uses simple import (no binge-detection), only for series already in library
let private importEpisodesForExistingSeries (entryId: EntryId) (watchedEpisodes: TraktWatchedEpisode list) : Async<int> = async {
    let! entry = Persistence.getLibraryEntryById entryId
    match entry with
    | Some e ->
        match e.Media with
        | LibrarySeries series ->
            // Get or create default session
            let! defaultSession = Persistence.getDefaultSession entryId
            let! session = 
                match defaultSession with
                | Some s -> async { return s }
                | None ->
                    printfn "[TraktSync] Creating default session for series: %s" series.Name
                    Persistence.createDefaultSession entryId series.Id
            
            do! importEpisodeWatchDataSimple entryId series.Id watchedEpisodes
            return watchedEpisodes.Length
        | _ -> 
            printfn "[TraktSync] Warning: Entry %d is not a series" (EntryId.value entryId)
            return 0
    | None -> 
        printfn "[TraktSync] Warning: Entry %d not found" (EntryId.value entryId)
        return 0
}

/// Import a NEW series from Trakt (for incremental sync)
/// Uses simple episode import (no binge-detection) - uses Trakt watched dates directly
let private importSeriesWithEpisodesSimple (item: TraktWatchedSeries) : Async<Result<unit, string>> = async {
    let tmdbId = TmdbSeriesId item.TmdbId

    // Add series to library
    let request: AddSeriesRequest = {
        TmdbId = tmdbId
        WhyAdded = Some {
            RecommendedBy = None
            RecommendedByName = None
            Source = Some "Trakt Sync"
            Context = None
            DateRecommended = None
        }
        InitialFriends = []
    }

    let! detailsResult = TmdbClient.getSeriesDetails tmdbId
    match detailsResult with
    | Error err -> return Error $"Failed to fetch series details for {item.Title}: {err}"
    | Ok details ->
        do! ImageCache.downloadPoster details.PosterPath
        do! ImageCache.downloadBackdrop details.BackdropPath

        let! entryResult = Persistence.insertLibraryEntryForSeries details request
        match entryResult with
        | Error err -> return Error err
        | Ok entry ->
            // Import episodes using SIMPLE import (no binge-detection)
            match entry.Media with
            | LibrarySeries series ->
                do! importEpisodeWatchDataSimple entry.Id series.Id item.WatchedEpisodes
            | _ -> ()

            return Ok ()
}

/// Add a movie from watchlist to library (without watch session)
let private addWatchlistMovie (item: TraktHistoryItem) : Async<Result<unit, string>> = async {
    let tmdbId = TmdbMovieId item.TmdbId

    // Check if already in library
    let! existing = Persistence.isMovieInLibrary tmdbId
    match existing with
    | Some _ -> return Ok () // Already in library
    | None ->
        // New movie - add to library without marking as watched
        let request: AddMovieRequest = {
            TmdbId = tmdbId
            WhyAdded = Some {
                RecommendedBy = None
                RecommendedByName = None
                Source = Some "Trakt Watchlist"
                Context = None
                DateRecommended = None
            }
            InitialFriends = []
        }

        let! detailsResult = TmdbClient.getMovieDetails tmdbId
        match detailsResult with
        | Error err -> return Error $"Failed to fetch movie details for {item.Title}: {err}"
        | Ok details ->
            do! ImageCache.downloadPoster details.PosterPath
            do! ImageCache.downloadBackdrop details.BackdropPath

            let! entryResult = Persistence.insertLibraryEntryForMovie details request
            match entryResult with
            | Error err -> return Error err
            | Ok _ -> return Ok ()
}

/// Add a series from watchlist to library (without watch session)
let private addWatchlistSeries (item: TraktHistoryItem) : Async<Result<unit, string>> = async {
    let tmdbId = TmdbSeriesId item.TmdbId

    // Check if already in library
    let! existing = Persistence.isSeriesInLibrary tmdbId
    match existing with
    | Some _ -> return Ok () // Already in library
    | None ->
        // New series - add to library
        let request: AddSeriesRequest = {
            TmdbId = tmdbId
            WhyAdded = Some {
                RecommendedBy = None
                RecommendedByName = None
                Source = Some "Trakt Watchlist"
                Context = None
                DateRecommended = None
            }
            InitialFriends = []
        }

        let! detailsResult = TmdbClient.getSeriesDetails tmdbId
        match detailsResult with
        | Error err -> return Error $"Failed to fetch series details for {item.Title}: {err}"
        | Ok details ->
            do! ImageCache.downloadPoster details.PosterPath
            do! ImageCache.downloadBackdrop details.BackdropPath

            let! entryResult = Persistence.insertLibraryEntryForSeries details request
            match entryResult with
            | Error err -> return Error err
            | Ok _ -> return Ok ()
}

/// Start the import process
let startImport (options: TraktImportOptions) : Async<Result<unit, string>> = async {
    printfn "[TraktImport] startImport called with options: Movies=%b, Series=%b, Watchlist=%b, Ratings=%b"
        options.ImportWatchedMovies options.ImportWatchedSeries options.ImportWatchlist options.ImportRatings

    // Check if already running
    if (!importState).InProgress then
        printfn "[TraktImport] Import already in progress, returning error"
        return Error "An import is already in progress"
    else
        printfn "[TraktImport] Starting new import..."
        // Reset and start
        resetState()
        importState := { !importState with InProgress = true }

        try
            // Gather items to import
            let mutable movieItems: TraktHistoryItem list = []
            let mutable seriesItems: TraktWatchedSeries list = []

            if options.ImportWatchedMovies then
                let! moviesResult = TraktClient.getWatchedMovies()
                match moviesResult with
                | Ok movies -> movieItems <- movies
                | Error err ->
                    addError $"Failed to fetch watched movies: {err}"

            if options.ImportWatchedSeries then
                printfn "[TraktImport] Fetching watched series from Trakt..."
                let! seriesResult = TraktClient.getWatchedShowsWithEpisodes()
                match seriesResult with
                | Ok series ->
                    printfn "[TraktImport] Fetched %d watched series from Trakt" series.Length
                    for s in series do
                        printfn "[TraktImport] Series: %s (TMDB: %d) - %d episodes" s.Title s.TmdbId s.WatchedEpisodes.Length
                    seriesItems <- series
                | Error err ->
                    printfn "[TraktImport] ERROR fetching watched series: %s" err
                    addError $"Failed to fetch watched series: {err}"

            if options.ImportWatchlist then
                let! watchlistResult = TraktClient.getWatchlist()
                match watchlistResult with
                | Ok items ->
                    let watchlistMovies = items |> List.filter (fun i -> i.MediaType = Movie)
                    // Convert watchlist series to TraktWatchedSeries (no episode data)
                    let watchlistSeries =
                        items
                        |> List.filter (fun i -> i.MediaType = Series)
                        |> List.map (fun i -> {
                            TmdbId = i.TmdbId
                            Title = i.Title
                            LastWatchedAt = i.WatchedAt
                            WatchedEpisodes = []
                            TraktRating = i.TraktRating
                        })
                    movieItems <- movieItems @ watchlistMovies
                    seriesItems <- seriesItems @ watchlistSeries
                | Error err ->
                    addError $"Failed to fetch watchlist: {err}"

            // Deduplicate
            let uniqueMovies = movieItems |> List.distinctBy (fun m -> m.TmdbId)
            let uniqueSeries = seriesItems |> List.distinctBy (fun s -> s.TmdbId)

            // Get ratings if requested
            let! ratings = async {
                if options.ImportRatings then
                    let! ratingsResult = TraktClient.getRatings()
                    match ratingsResult with
                    | Ok r -> return r
                    | Error err ->
                        addError $"Failed to fetch ratings: {err}"
                        return Map.empty
                else
                    return Map.empty
            }

            let totalItems = uniqueMovies.Length + uniqueSeries.Length
            setTotal totalItems

            let mutable completed = 0

            // Import movies
            for movie in uniqueMovies do
                if isCancelled() then ()
                else
                    updateProgress movie.Title completed
                    let rating = ratings |> Map.tryFind (movie.TmdbId, Movie)
                    let! result = importMovie movie rating
                    match result with
                    | Error err -> addError err
                    | Ok () -> ()
                    completed <- completed + 1

            // Import series (with episode-level watch data)
            printfn "[TraktImport] Starting import of %d unique series..." uniqueSeries.Length
            for series in uniqueSeries do
                if isCancelled() then ()
                else
                    updateProgress series.Title completed
                    printfn "[TraktImport] Importing series: %s (TMDB: %d, %d episodes)" series.Title series.TmdbId series.WatchedEpisodes.Length
                    let rating = ratings |> Map.tryFind (series.TmdbId, Series)
                    let! result = importSeriesWithEpisodes series rating
                    match result with
                    | Error err ->
                        printfn "[TraktImport] ERROR importing %s: %s" series.Title err
                        addError err
                    | Ok () ->
                        printfn "[TraktImport] Successfully imported: %s" series.Title
                    completed <- completed + 1

            importState := { !importState with InProgress = false; Completed = completed; CurrentItem = None }
            return Ok ()
        with
        | ex ->
            importState := { !importState with InProgress = false }
            return Error $"Import failed: {ex.Message}"
}

// =====================================
// Incremental Sync (Lightweight - Home Screen)
// =====================================

/// Internal: Core sync logic from a specific date
/// Used by both incrementalSync and resyncSince
let private syncFromDate (effectiveDate: DateTime) : Async<Result<TraktSyncResult, string>> = async {
    try
        let mutable newMovieWatches = 0
        let mutable newEpisodeWatches = 0
        let mutable newWatchlistItems = 0
        let mutable errors: string list = []

        // 1. Sync watched movies since date
        let! moviesResult = TraktClient.getWatchedMoviesSince effectiveDate
        match moviesResult with
        | Error err -> errors <- errors @ [$"Failed to fetch movies: {err}"]
        | Ok movies ->
            printfn "[TraktSync] Fetched %d movie watches" movies.Length

            for movie in movies do
                let tmdbId = TmdbMovieId movie.TmdbId
                let! existsInLibrary = Persistence.isMovieInLibrary tmdbId

                match existsInLibrary with
                | Some entryId ->
                    match movie.WatchedAt with
                    | Some watchedDate ->
                        let! sessionExists = Persistence.movieWatchSessionExistsForDate entryId watchedDate
                        if not sessionExists then
                            printfn "[TraktSync] Adding watch session: %s (%A)" movie.Title watchedDate
                            let request: CreateMovieWatchSessionRequest = {
                                EntryId = entryId
                                WatchedDate = watchedDate
                                Friends = []
                                Name = Some "Synced from Trakt"
                            }
                            let! _ = Persistence.insertMovieWatchSession request
                            do! Persistence.markMovieWatched entryId (Some watchedDate)
                            newMovieWatches <- newMovieWatches + 1
                    | None -> ()
                | None ->
                    printfn "[TraktSync] Adding new movie: %s (watchedAt: %A)" movie.Title movie.WatchedAt
                    let! result = importMovie movie None
                    match result with
                    | Ok () -> newMovieWatches <- newMovieWatches + 1
                    | Error err -> errors <- errors @ [err]

        // 2. Sync watched episodes since date
        let! seriesResult = TraktClient.getWatchedShowsWithEpisodesSince effectiveDate
        match seriesResult with
        | Error err -> errors <- errors @ [$"Failed to fetch series: {err}"]
        | Ok seriesList ->
            let totalEpisodes = seriesList |> List.sumBy (fun s -> s.WatchedEpisodes.Length)
            printfn "[TraktSync] Fetched %d series with %d total episodes" seriesList.Length totalEpisodes
            for series in seriesList do
                let tmdbId = TmdbSeriesId series.TmdbId
                let! existsInLibrary = Persistence.isSeriesInLibrary tmdbId

                match existsInLibrary with
                | Some entryId ->
                    if not (List.isEmpty series.WatchedEpisodes) then
                        let! syncedCount = importEpisodesForExistingSeries entryId series.WatchedEpisodes
                        newEpisodeWatches <- newEpisodeWatches + syncedCount
                | None ->
                    if not (List.isEmpty series.WatchedEpisodes) then
                        printfn "[TraktSync] Adding new series: %s (%d episodes)" series.Title series.WatchedEpisodes.Length
                        let! result = importSeriesWithEpisodesSimple series
                        match result with
                        | Ok () -> newEpisodeWatches <- newEpisodeWatches + series.WatchedEpisodes.Length
                        | Error err -> errors <- errors @ [err]

        // 3. Sync watchlist
        let! watchlistResult = TraktClient.getWatchlist()
        match watchlistResult with
        | Error err -> errors <- errors @ [$"Failed to fetch watchlist: {err}"]
        | Ok watchlistItems ->
            for item in watchlistItems do
                match item.MediaType with
                | Movie ->
                    let! result = addWatchlistMovie item
                    match result with
                    | Ok () ->
                        let tmdbId = TmdbMovieId item.TmdbId
                        let! exists = Persistence.isMovieInLibrary tmdbId
                        if exists.IsSome then newWatchlistItems <- newWatchlistItems + 1
                    | Error err -> errors <- errors @ [err]
                | Series ->
                    let! result = addWatchlistSeries item
                    match result with
                    | Ok () ->
                        let tmdbId = TmdbSeriesId item.TmdbId
                        let! exists = Persistence.isSeriesInLibrary tmdbId
                        if exists.IsSome then newWatchlistItems <- newWatchlistItems + 1
                    | Error err -> errors <- errors @ [err]

        // Update last sync time
        do! TraktClient.updateLastSyncTime()

        return Ok {
            NewMovieWatches = newMovieWatches
            NewEpisodeWatches = newEpisodeWatches
            UpdatedItems = newWatchlistItems
            Errors = errors
        }
    with
    | ex -> return Error $"Sync failed: {ex.Message}"
}

/// Perform an incremental sync - lightweight, non-destructive
/// Uses the most recent watch date from DB as starting point
let incrementalSync () : Async<Result<TraktSyncResult, string>> = async {
    let! isAuth = TraktClient.isAuthenticatedAsync()
    if not isAuth then
        return Error "Not authenticated with Trakt"
    else

    let! lastWatchDate = Persistence.getLastKnownWatchDate()

    match lastWatchDate with
    | None ->
        printfn "[TraktSync] No watch history found - skipping incremental sync. Use full import."
        return Ok { NewMovieWatches = 0; NewEpisodeWatches = 0; UpdatedItems = 0; Errors = [] }
    | Some lastWatch ->

    let effectiveLastSync =
        let utcDate =
            if lastWatch.Kind = DateTimeKind.Unspecified then
                DateTime.SpecifyKind(lastWatch, DateTimeKind.Utc)
            else
                lastWatch.ToUniversalTime()
        utcDate.AddHours(-1.0)

    printfn "[TraktSync] Incremental sync from: %A (effective: %A UTC)" lastWatch effectiveLastSync
    return! syncFromDate effectiveLastSync
}

/// Get current sync status
let getSyncStatus () : Async<TraktSyncStatus> = async {
    let! isAuth = TraktClient.isAuthenticatedAsync()
    let! settings = Persistence.getTraktSettings()
    return {
        IsAuthenticated = isAuth
        LastSyncAt = settings.LastSyncAt
        AutoSyncEnabled = settings.AutoSyncEnabled
    }
}

/// Resync from a specific date - for filling gaps in watch history
/// Reuses incrementalSync logic but with an explicit start date
let resyncSince (sinceDate: DateTime) : Async<Result<TraktSyncResult, string>> = async {
    let! isAuth = TraktClient.isAuthenticatedAsync()
    if not isAuth then
        return Error "Not authenticated with Trakt"
    else

    // Normalize to UTC and apply 1-hour buffer (same as incrementalSync)
    let effectiveSinceDate =
        let utcDate =
            if sinceDate.Kind = DateTimeKind.Unspecified then
                DateTime.SpecifyKind(sinceDate, DateTimeKind.Utc)
            else
                sinceDate.ToUniversalTime()
        utcDate.AddHours(-1.0)

    printfn "[TraktResync] Starting resync from: %A (effective: %A UTC)" sinceDate effectiveSinceDate

    // Call the internal sync logic with explicit date
    return! syncFromDate effectiveSinceDate
}
