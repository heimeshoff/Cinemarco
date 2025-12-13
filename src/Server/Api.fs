module Api

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Giraffe
open Microsoft.AspNetCore.Http
open Shared.Api
open Shared.Domain
open System

// =====================================
// Main API Implementation
// =====================================

let cinemarcoApi : ICinemarcoApi = {
    healthCheck = fun () -> async {
        return {
            Status = "healthy"
            Version = "0.1.0"
            Timestamp = DateTime.UtcNow
        }
    }

    // =====================================
    // Library Operations
    // =====================================

    libraryAddMovie = fun request -> async {
        // Fetch movie details from TMDB
        let! movieDetailsResult = TmdbClient.getMovieDetails request.TmdbId
        match movieDetailsResult with
        | Error err -> return Error $"Failed to fetch movie details: {err}"
        | Ok details ->
            // Cache poster and backdrop images
            do! ImageCache.downloadPoster details.PosterPath
            do! ImageCache.downloadBackdrop details.BackdropPath
            return! Persistence.insertLibraryEntryForMovie details request
    }

    libraryAddSeries = fun request -> async {
        // Fetch series details from TMDB
        let! seriesDetailsResult = TmdbClient.getSeriesDetails request.TmdbId
        match seriesDetailsResult with
        | Error err -> return Error $"Failed to fetch series details: {err}"
        | Ok details ->
            // Cache poster and backdrop images
            do! ImageCache.downloadPoster details.PosterPath
            do! ImageCache.downloadBackdrop details.BackdropPath
            return! Persistence.insertLibraryEntryForSeries details request
    }

    libraryGetAll = fun () -> Persistence.getAllLibraryEntries()

    libraryGetById = fun entryId -> async {
        let! entry = Persistence.getLibraryEntryById entryId
        match entry with
        | Some e -> return Ok e
        | None -> return Error "Entry not found"
    }

    libraryGetBySlug = fun (slug, isMovieFilter) -> async {
        let! entries = Persistence.getAllLibraryEntries()
        // Parse the slug to handle duplicates
        let (baseSlug, suffixIndex) = Slug.parseSlugWithSuffix slug
        let index = suffixIndex |> Option.defaultValue 0

        // Filter by media type if specified
        let typeFilteredEntries =
            match isMovieFilter with
            | Some true -> entries |> List.filter (fun e -> match e.Media with LibraryMovie _ -> true | _ -> false)
            | Some false -> entries |> List.filter (fun e -> match e.Media with LibrarySeries _ -> true | _ -> false)
            | None -> entries

        // Match entries - try exact match first, then prefix match for backwards compatibility
        // This handles both "hamilton-2020" (full slug) and "hamilton" (title-only slug)
        let matchingEntries =
            typeFilteredEntries
            |> List.filter (fun entry ->
                let entrySlug =
                    match entry.Media with
                    | LibraryMovie m -> Slug.forMovie m.Title m.ReleaseDate
                    | LibrarySeries s -> Slug.forSeries s.Name s.FirstAirDate
                let titleOnlySlug =
                    match entry.Media with
                    | LibraryMovie m -> Slug.generate m.Title
                    | LibrarySeries s -> Slug.generate s.Name
                // Match if: exact match, title-only matches, or entry slug starts with search slug
                Slug.matches baseSlug entrySlug ||
                Slug.matches baseSlug titleOnlySlug ||
                entrySlug.StartsWith(baseSlug + "-", StringComparison.OrdinalIgnoreCase))
            |> List.sortBy (fun e -> EntryId.value e.Id)

        // Get the entry at the specified index
        let entryOpt =
            if index < List.length matchingEntries then
                Some (List.item index matchingEntries)
            else
                None
        match entryOpt with
        | Some e -> return Ok e
        | None -> return Error $"Entry not found for slug: {slug}"
    }

    libraryIsMovieInLibrary = fun tmdbId -> Persistence.isMovieInLibrary tmdbId

    libraryIsSeriesInLibrary = fun tmdbId -> Persistence.isSeriesInLibrary tmdbId

    libraryDeleteEntry = fun entryId -> async {
        do! Persistence.deleteLibraryEntry entryId
        return Ok ()
    }

    // =====================================
    // Watch Status Operations
    // =====================================

    libraryMarkMovieWatched = fun (entryId, watchedDate) -> async {
        try
            do! Persistence.markMovieWatched entryId watchedDate
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to mark movie as watched: {ex.Message}"
    }

    libraryMarkMovieUnwatched = fun entryId -> async {
        try
            do! Persistence.markMovieUnwatched entryId
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to mark movie as unwatched: {ex.Message}"
    }

    libraryUpdateEpisodeProgress = fun (entryId, seasonNumber, episodeNumber, watched) -> async {
        try
            match! Persistence.getSeriesInfoForEntry entryId with
            | None -> return Error "Entry is not a series"
            | Some (seriesId, _) ->
                do! Persistence.updateEpisodeProgress entryId seriesId seasonNumber episodeNumber watched
                do! Persistence.updateSeriesWatchStatusFromProgress entryId
                return Ok ()
        with
        | ex -> return Error $"Failed to update episode progress: {ex.Message}"
    }

    sessionsUpdateEpisodeWatchedDate = fun (sessionId, seasonNumber, episodeNumber, watchedDate) -> async {
        try
            do! Persistence.updateSessionEpisodeWatchedDate sessionId seasonNumber episodeNumber watchedDate
            return Ok ()
        with
        | ex -> return Error $"Failed to update episode watched date: {ex.Message}"
    }

    libraryMarkSeasonWatched = fun (entryId, seasonNumber) -> async {
        try
            match! Persistence.getSeriesInfoForEntry entryId with
            | None -> return Error "Entry is not a series"
            | Some (seriesId, _) ->
                // Get season details from TMDB to find episode count
                let! entry = Persistence.getLibraryEntryById entryId
                match entry with
                | None -> return Error "Entry not found"
                | Some e ->
                    match e.Media with
                    | LibrarySeries series ->
                        let! seasonResult = TmdbClient.getSeasonDetails series.TmdbId seasonNumber
                        match seasonResult with
                        | Error err -> return Error $"Failed to get season details: {err}"
                        | Ok season ->
                            // Save episode metadata to DB so names show in timeline
                            do! Persistence.saveSeasonEpisodes series.Id season
                            let episodeCount = season.Episodes.Length
                            do! Persistence.markSeasonWatched entryId seriesId seasonNumber episodeCount
                            do! Persistence.updateSeriesWatchStatusFromProgress entryId
                            return Ok ()
                    | LibraryMovie _ -> return Error "Entry is not a series"
        with
        | ex -> return Error $"Failed to mark season as watched: {ex.Message}"
    }

    libraryMarkSeriesCompleted = fun entryId -> async {
        try
            do! Persistence.updateWatchStatus entryId Completed (Some DateTime.UtcNow)
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to mark series as completed: {ex.Message}"
    }

    libraryAbandonEntry = fun (entryId, request) -> async {
        try
            let abandonedAt =
                match request.AbandonedAtSeason, request.AbandonedAtEpisode with
                | Some s, Some e -> Some { CurrentSeason = Some s; CurrentEpisode = Some e; LastWatchedDate = None }
                | Some s, None -> Some { CurrentSeason = Some s; CurrentEpisode = None; LastWatchedDate = None }
                | None, Some e -> Some { CurrentSeason = None; CurrentEpisode = Some e; LastWatchedDate = None }
                | None, None -> None

            let abandonedInfo : AbandonedInfo = {
                AbandonedAt = abandonedAt
                Reason = request.Reason
                AbandonedDate = Some DateTime.UtcNow
            }
            do! Persistence.updateWatchStatus entryId (Abandoned abandonedInfo) None
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to abandon entry: {ex.Message}"
    }

    libraryResumeEntry = fun entryId -> async {
        try
            // Get current entry to check previous progress
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | None -> return Error "Entry not found"
            | Some e ->
                match e.WatchStatus with
                | Abandoned info ->
                    // Resume to InProgress if there was progress, otherwise NotStarted
                    let newStatus =
                        match info.AbandonedAt with
                        | Some progress -> InProgress progress
                        | None -> NotStarted
                    do! Persistence.updateWatchStatus entryId newStatus None
                    let! updatedEntry = Persistence.getLibraryEntryById entryId
                    match updatedEntry with
                    | Some updated -> return Ok updated
                    | None -> return Error "Entry not found after update"
                | _ ->
                    // Entry is not abandoned, just return it
                    return Ok e
        with
        | ex -> return Error $"Failed to resume entry: {ex.Message}"
    }

    libraryGetEpisodeProgress = fun entryId -> Persistence.getEpisodeProgress entryId

    // =====================================
    // Entry Update Operations
    // =====================================

    librarySetRating = fun (entryId, rating) -> async {
        try
            do! Persistence.updatePersonalRating entryId rating
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to set rating: {ex.Message}"
    }

    libraryToggleFavorite = fun entryId -> async {
        try
            let! _ = Persistence.toggleFavorite entryId
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to toggle favorite: {ex.Message}"
    }

    libraryUpdateNotes = fun (entryId, notes) -> async {
        try
            do! Persistence.updateNotes entryId notes
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to update notes: {ex.Message}"
    }

    libraryToggleFriend = fun (entryId, friendId) -> async {
        try
            // Check if friend is already associated
            let! currentFriends = Persistence.getFriendsForEntry entryId
            if List.contains friendId currentFriends then
                do! Persistence.removeFriendFromEntry entryId friendId
            else
                do! Persistence.addFriendToEntry entryId friendId
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to toggle friend: {ex.Message}"
    }

    // =====================================
    // Friends Operations
    // =====================================

    friendsGetAll = fun () -> Persistence.getAllFriends()

    friendsGetById = fun id -> async {
        let! friend = Persistence.getFriendById (FriendId id)
        match friend with
        | Some f -> return Ok f
        | None -> return Error "Friend not found"
    }

    friendsGetBySlug = fun slug -> async {
        let! friends = Persistence.getAllFriends()
        // Parse the slug to handle duplicates
        let (baseSlug, suffixIndex) = Slug.parseSlugWithSuffix slug
        let index = suffixIndex |> Option.defaultValue 0
        // Find all friends matching the base slug, sorted by ID
        let matchingFriends =
            friends
            |> List.filter (fun f -> Slug.matches baseSlug (Slug.forFriend f.Name))
            |> List.sortBy (fun f -> FriendId.value f.Id)
        // Get the friend at the specified index
        let friendOpt =
            if index < List.length matchingFriends then
                Some (List.item index matchingFriends)
            else
                None
        match friendOpt with
        | Some f -> return Ok f
        | None -> return Error $"Friend not found for slug: {slug}"
    }

    friendsCreate = fun request -> async {
        try
            let! friend = Persistence.insertFriend request
            return Ok friend
        with
        | ex -> return Error $"Failed to create friend: {ex.Message}"
    }

    friendsUpdate = fun request -> async {
        try
            do! Persistence.updateFriend request
            let! friend = Persistence.getFriendById request.Id
            match friend with
            | Some f -> return Ok f
            | None -> return Error "Friend not found after update"
        with
        | ex -> return Error $"Failed to update friend: {ex.Message}"
    }

    friendsDelete = fun id -> async {
        try
            do! Persistence.deleteFriend (FriendId id)
            return Ok ()
        with
        | ex -> return Error $"Failed to delete friend: {ex.Message}"
    }

    friendsGetWatchedWith = fun friendId -> async {
        let! entryIds = Persistence.getEntriesWatchedWithFriend (FriendId friendId)
        let! entries =
            entryIds
            |> List.map (fun eid -> Persistence.getLibraryEntryById (EntryId eid))
            |> Async.Sequential
        return entries |> Array.choose id |> Array.toList
    }

    // =====================================
    // Watch Session Operations
    // =====================================

    sessionsGetForEntry = fun entryId -> Persistence.getSessionsForEntry entryId

    sessionsGetById = fun sessionId -> async {
        let! session = Persistence.getSessionById sessionId
        match session with
        | None -> return Error "Session not found"
        | Some s ->
            // Get the entry for total episode count
            let! entry = Persistence.getLibraryEntryById s.EntryId
            match entry with
            | None -> return Error "Entry not found for session"
            | Some e ->
                // Get episode progress for this session
                let! progress = Persistence.getSessionEpisodeProgress sessionId
                let watchedCount = progress |> List.filter (fun p -> p.IsWatched) |> List.length
                let totalEpisodes =
                    match e.Media with
                    | LibrarySeries series -> series.NumberOfEpisodes
                    | LibraryMovie _ -> 0

                let completionPct =
                    if totalEpisodes > 0 then
                        float watchedCount / float totalEpisodes * 100.0
                    else 0.0

                return Ok {
                    Session = s
                    Entry = e
                    EpisodeProgress = progress
                    TotalEpisodes = totalEpisodes
                    WatchedEpisodes = watchedCount
                    CompletionPercentage = completionPct
                }
    }

    sessionsGetBySlug = fun slug -> async {
        // Sessions don't have standalone names, so we need to find by series slug + session index
        // Format: {series-slug}-session-{n}
        let! entries = Persistence.getAllLibraryEntries()
        // Try to find a matching session by iterating through entries and their sessions
        let! allSessions =
            entries
            |> List.choose (fun e ->
                match e.Media with
                | LibrarySeries _ -> Some e.Id
                | _ -> None)
            |> List.map Persistence.getSessionsForEntry
            |> Async.Sequential
        let flatSessions = allSessions |> Array.toList |> List.concat

        let matchingSession =
            flatSessions
            |> List.indexed
            |> List.tryPick (fun (_, session) ->
                // Find the entry for this session to get the series name
                entries
                |> List.tryFind (fun e -> e.Id = session.EntryId)
                |> Option.bind (fun entry ->
                    match entry.Media with
                    | LibrarySeries s ->
                        let sessionSlug = Slug.forSession s.Name (SessionId.value session.Id)
                        if Slug.matches slug sessionSlug then Some session else None
                    | _ -> None))

        match matchingSession with
        | None -> return Error $"Session not found for slug: {slug}"
        | Some s ->
            let! entry = Persistence.getLibraryEntryById s.EntryId
            match entry with
            | None -> return Error "Entry not found for session"
            | Some e ->
                let! progress = Persistence.getSessionEpisodeProgress s.Id
                let watchedCount = progress |> List.filter (fun p -> p.IsWatched) |> List.length
                let totalEpisodes =
                    match e.Media with
                    | LibrarySeries series -> series.NumberOfEpisodes
                    | LibraryMovie _ -> 0
                let completionPct =
                    if totalEpisodes > 0 then float watchedCount / float totalEpisodes * 100.0 else 0.0
                return Ok {
                    Session = s
                    Entry = e
                    EpisodeProgress = progress
                    TotalEpisodes = totalEpisodes
                    WatchedEpisodes = watchedCount
                    CompletionPercentage = completionPct
                }
    }

    sessionsCreate = fun request -> async {
        try
            // Verify entry exists and is a series
            let! entry = Persistence.getLibraryEntryById request.EntryId
            match entry with
            | None -> return Error "Entry not found"
            | Some e ->
                match e.Media with
                | LibraryMovie _ -> return Error "Sessions can only be created for series"
                | LibrarySeries _ ->
                    let! session = Persistence.insertWatchSession request
                    return Ok session
        with
        | ex -> return Error $"Failed to create session: {ex.Message}"
    }

    sessionsUpdate = fun request -> async {
        try
            do! Persistence.updateWatchSession request
            let! session = Persistence.getSessionById request.Id
            match session with
            | Some s -> return Ok s
            | None -> return Error "Session not found after update"
        with
        | ex -> return Error $"Failed to update session: {ex.Message}"
    }

    sessionsDelete = fun sessionId -> async {
        try
            do! Persistence.deleteWatchSession sessionId
            return Ok ()
        with
        | ex -> return Error $"Failed to delete session: {ex.Message}"
    }

    sessionsToggleFriend = fun (sessionId, friendId) -> async {
        try
            // Check if friend is already associated
            let! currentFriends = Persistence.getFriendsForSession sessionId
            if List.contains friendId currentFriends then
                do! Persistence.removeFriendFromSession sessionId friendId
            else
                do! Persistence.addFriendToSession sessionId friendId
            let! session = Persistence.getSessionById sessionId
            match session with
            | Some s -> return Ok s
            | None -> return Error "Session not found after update"
        with
        | ex -> return Error $"Failed to toggle friend: {ex.Message}"
    }

    sessionsUpdateEpisodeProgress = fun (sessionId, seasonNumber, episodeNumber, watched) -> async {
        try
            let! session = Persistence.getSessionById sessionId
            match session with
            | None -> return Error "Session not found"
            | Some s ->
                match! Persistence.getSeriesInfoForEntry s.EntryId with
                | None -> return Error "Entry is not a series"
                | Some (seriesId, _) ->
                    do! Persistence.updateSessionEpisodeProgress sessionId s.EntryId seriesId seasonNumber episodeNumber watched
                    let! progress = Persistence.getSessionEpisodeProgress sessionId
                    return Ok progress
        with
        | ex -> return Error $"Failed to update episode progress: {ex.Message}"
    }

    sessionsMarkSeasonWatched = fun (sessionId, seasonNumber) -> async {
        try
            let! session = Persistence.getSessionById sessionId
            match session with
            | None -> return Error "Session not found"
            | Some s ->
                let! entry = Persistence.getLibraryEntryById s.EntryId
                match entry with
                | None -> return Error "Entry not found"
                | Some e ->
                    match e.Media with
                    | LibraryMovie _ -> return Error "Entry is not a series"
                    | LibrarySeries series ->
                        // Get season details from TMDB to find episode count
                        let! seasonResult = TmdbClient.getSeasonDetails series.TmdbId seasonNumber
                        match seasonResult with
                        | Error err -> return Error $"Failed to get season details: {err}"
                        | Ok season ->
                            // Save episode metadata to DB so names show in timeline
                            do! Persistence.saveSeasonEpisodes series.Id season
                            let episodeCount = season.Episodes.Length
                            match! Persistence.getSeriesInfoForEntry s.EntryId with
                            | None -> return Error "Series info not found"
                            | Some (seriesId, _) ->
                                do! Persistence.markSessionSeasonWatched sessionId s.EntryId seriesId seasonNumber episodeCount
                                let! progress = Persistence.getSessionEpisodeProgress sessionId
                                return Ok progress
        with
        | ex -> return Error $"Failed to mark season as watched: {ex.Message}"
    }

    sessionsGetProgress = fun sessionId -> Persistence.getSessionEpisodeProgress sessionId

    // =====================================
    // Movie Watch Session Operations
    // =====================================

    movieSessionsGetForEntry = fun entryId -> Persistence.getMovieWatchSessionsForEntry entryId

    movieSessionsCreate = fun request -> async {
        try
            // Verify entry exists and is a movie
            let! entry = Persistence.getLibraryEntryById request.EntryId
            match entry with
            | None -> return Error "Entry not found"
            | Some e ->
                match e.Media with
                | LibrarySeries _ -> return Error "Watch sessions are for movies, use watch logs for series"
                | LibraryMovie _ ->
                    let! session = Persistence.insertMovieWatchSession request
                    return Ok session
        with
        | ex -> return Error $"Failed to create movie watch session: {ex.Message}"
    }

    movieSessionsDelete = fun sessionId -> async {
        try
            do! Persistence.deleteMovieWatchSession sessionId
            return Ok ()
        with
        | ex -> return Error $"Failed to delete movie watch session: {ex.Message}"
    }

    movieSessionsUpdateDate = fun request -> async {
        try
            let! result = Persistence.updateMovieWatchSessionDate request
            match result with
            | Some session -> return Ok session
            | None -> return Error "Watch session not found"
        with
        | ex -> return Error $"Failed to update watch session date: {ex.Message}"
    }

    movieSessionsUpdate = fun request -> async {
        try
            let! result = Persistence.updateMovieWatchSession request
            match result with
            | Some session -> return Ok session
            | None -> return Error "Watch session not found"
        with
        | ex -> return Error $"Failed to update watch session: {ex.Message}"
    }

    // =====================================
    // Collection Operations
    // =====================================

    collectionsGetAll = fun () -> Persistence.getAllCollections()

    collectionsGetById = fun collectionId -> async {
        let! result = Persistence.getCollectionWithItems collectionId
        match result with
        | Some cwi -> return Ok cwi
        | None -> return Error "Collection not found"
    }

    collectionsGetBySlug = fun slug -> async {
        let! collections = Persistence.getAllCollections()
        // Parse the slug to handle duplicates (e.g., "my-collection_1")
        let (baseSlug, suffixIndex) = Slug.parseSlugWithSuffix slug
        let index = suffixIndex |> Option.defaultValue 0
        // Find all collections matching the base slug, sorted by ID
        let matchingCollections =
            collections
            |> List.filter (fun c -> Slug.matches baseSlug (Slug.forCollection c.Name))
            |> List.sortBy (fun c -> CollectionId.value c.Id)
        // Get the collection at the specified index
        let collectionOpt =
            if index < List.length matchingCollections then
                Some (List.item index matchingCollections)
            else
                None
        match collectionOpt with
        | None -> return Error $"Collection not found for slug: {slug}"
        | Some c ->
            let! result = Persistence.getCollectionWithItems c.Id
            match result with
            | Some cwi -> return Ok cwi
            | None -> return Error "Collection not found"
    }

    collectionsCreate = fun request -> async {
        try
            let! collection = Persistence.insertCollection request
            return Ok collection
        with
        | ex -> return Error $"Failed to create collection: {ex.Message}"
    }

    collectionsUpdate = fun request -> async {
        try
            do! Persistence.updateCollection request
            let! collection = Persistence.getCollectionById request.Id
            match collection with
            | Some c -> return Ok c
            | None -> return Error "Collection not found after update"
        with
        | ex -> return Error $"Failed to update collection: {ex.Message}"
    }

    collectionsDelete = fun collectionId -> async {
        try
            do! Persistence.deleteCollection collectionId
            return Ok ()
        with
        | ex -> return Error $"Failed to delete collection: {ex.Message}"
    }

    collectionsAddItem = fun (collectionId, itemRef, notes) -> async {
        try
            // Verify the referenced item exists
            let! isValid = async {
                match itemRef with
                | LibraryEntryRef entryId ->
                    let! entry = Persistence.getLibraryEntryById entryId
                    return entry.IsSome
                | SeasonRef (seriesId, _) ->
                    let! series = Persistence.getSeriesById seriesId
                    return series.IsSome
                | EpisodeRef (seriesId, _, _) ->
                    let! series = Persistence.getSeriesById seriesId
                    return series.IsSome
            }
            if not isValid then
                return Error "Referenced item not found"
            else
                do! Persistence.addItemToCollection collectionId itemRef notes
                let! result = Persistence.getCollectionWithItems collectionId
                match result with
                | Some cwi -> return Ok cwi
                | None -> return Error "Collection not found after update"
        with
        | ex -> return Error $"Failed to add item to collection: {ex.Message}"
    }

    collectionsRemoveItem = fun (collectionId, itemRef) -> async {
        try
            do! Persistence.removeItemFromCollection collectionId itemRef
            let! result = Persistence.getCollectionWithItems collectionId
            match result with
            | Some cwi -> return Ok cwi
            | None -> return Error "Collection not found after update"
        with
        | ex -> return Error $"Failed to remove item from collection: {ex.Message}"
    }

    collectionsReorderItems = fun (collectionId, itemRefs) -> async {
        try
            do! Persistence.reorderCollectionItems collectionId itemRefs
            let! result = Persistence.getCollectionWithItems collectionId
            match result with
            | Some cwi -> return Ok cwi
            | None -> return Error "Collection not found after update"
        with
        | ex -> return Error $"Failed to reorder collection items: {ex.Message}"
    }

    collectionsGetProgress = fun collectionId -> async {
        let! result = Persistence.getCollectionProgress collectionId
        match result with
        | Some progress -> return Ok progress
        | None -> return Error "Collection not found"
    }

    collectionsGetForEntry = fun entryId -> Persistence.getCollectionsForEntry entryId

    collectionsGetForItem = fun itemRef -> Persistence.getCollectionsForItem itemRef

    // =====================================
    // TMDB Operations
    // =====================================

    tmdbHealthCheck = fun () -> TmdbClient.healthCheck()

    tmdbSearchMovies = fun query -> TmdbClient.searchMovies query

    tmdbSearchSeries = fun query -> TmdbClient.searchSeries query

    tmdbSearchAll = fun query -> TmdbClient.searchAll query

    tmdbGetMovieDetails = fun tmdbId -> TmdbClient.getMovieDetails tmdbId

    tmdbGetSeriesDetails = fun tmdbId -> TmdbClient.getSeriesDetails tmdbId

    tmdbGetSeasonDetails = fun (tmdbId, seasonNumber) -> async {
        // First check if series is in our library
        let! series = Persistence.getSeriesByTmdbId tmdbId
        match series with
        | Some s ->
            // Try to get from local database first
            let! localData = Persistence.getLocalSeasonDetails s.Id seasonNumber
            match localData with
            | Some cachedSeason ->
                // Return cached data immediately, but refresh from TMDB in background
                Async.Start (async {
                    let! result = TmdbClient.getSeasonDetails tmdbId seasonNumber
                    match result with
                    | Ok freshSeason -> do! Persistence.saveSeasonEpisodes s.Id freshSeason
                    | Error _ -> () // Ignore errors during background refresh
                })
                return Ok cachedSeason
            | None ->
                // No local data, fetch from TMDB and save
                let! result = TmdbClient.getSeasonDetails tmdbId seasonNumber
                match result with
                | Ok season ->
                    do! Persistence.saveSeasonEpisodes s.Id season
                    return Ok season
                | Error err -> return Error err
        | None ->
            // Series not in library, just fetch from TMDB without caching
            return! TmdbClient.getSeasonDetails tmdbId seasonNumber
    }

    tmdbGetPersonDetails = fun tmdbId -> TmdbClient.getPersonDetails tmdbId

    tmdbGetPersonFilmography = fun tmdbId -> TmdbClient.getPersonFilmography tmdbId

    tmdbGetMovieCredits = fun tmdbId -> TmdbClient.getMovieCredits tmdbId

    tmdbGetSeriesCredits = fun tmdbId -> TmdbClient.getSeriesCredits tmdbId

    tmdbGetCollection = fun collectionId -> TmdbClient.getTmdbCollection collectionId

    tmdbGetTrendingMovies = fun () -> TmdbClient.getTrendingMovies()

    tmdbGetTrendingSeries = fun () -> TmdbClient.getTrendingSeries()

    // =====================================
    // Cache Management Operations
    // =====================================

    cacheGetEntries = fun () -> Persistence.getAllCacheEntries()

    cacheGetStats = fun () -> Persistence.getCacheStats()

    cacheClearExpired = fun () -> Persistence.clearExpiredCacheWithStats()

    cacheClearAll = fun () -> Persistence.clearAllCache()

    // =====================================
    // Maintenance Operations
    // =====================================

    maintenanceRecalculateSeriesWatchStatus = fun () -> async {
        // Get all library entries that are series
        let! entries = Persistence.getAllLibraryEntries()
        let seriesEntries =
            entries
            |> List.filter (fun e ->
                match e.Media with
                | LibrarySeries _ -> true
                | _ -> false)

        let mutable updatedCount = 0
        for entry in seriesEntries do
            do! Persistence.updateSeriesWatchStatusFromProgress entry.Id
            updatedCount <- updatedCount + 1

        return updatedCount
    }

    // =====================================
    // Tracked Contributors Operations
    // =====================================

    contributorsGetAll = fun () -> Persistence.getAllTrackedContributors()

    contributorsGetById = fun trackedId -> async {
        let! result = Persistence.getTrackedContributorById trackedId
        match result with
        | Some tc -> return Ok tc
        | None -> return Error "Tracked contributor not found"
    }

    contributorsGetBySlug = fun slug -> async {
        let! contributors = Persistence.getAllTrackedContributors()
        // Parse the slug to handle duplicates
        let (baseSlug, suffixIndex) = Slug.parseSlugWithSuffix slug
        let index = suffixIndex |> Option.defaultValue 0
        // Find all contributors matching the base slug, sorted by TMDB ID
        let matchingContributors =
            contributors
            |> List.filter (fun c -> Slug.matches baseSlug (Slug.forContributor c.Name))
            |> List.sortBy (fun c -> TmdbPersonId.value c.TmdbPersonId)
        // Get the contributor at the specified index
        let contributorOpt =
            if index < List.length matchingContributors then
                Some (List.item index matchingContributors)
            else
                None
        match contributorOpt with
        | Some tc -> return Ok tc
        | None -> return Error $"Tracked contributor not found for slug: {slug}"
    }

    contributorsTrack = fun request -> Persistence.trackContributor request

    contributorsUntrack = fun trackedId -> Persistence.untrackContributor trackedId

    contributorsUpdateNotes = fun (trackedId, notes) ->
        Persistence.updateTrackedContributorNotes trackedId notes

    contributorsIsTracked = fun tmdbPersonId -> Persistence.isContributorTracked tmdbPersonId

    contributorsGetByTmdbId = fun tmdbPersonId -> Persistence.getTrackedContributorByTmdbId tmdbPersonId

    // =====================================
    // Time Intelligence / Stats Operations
    // =====================================

    statsGetTimeIntelligence = fun () -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! movieSessions = Persistence.getAllMovieWatchSessions()
        let! episodeWatchData = Persistence.getAllWatchedEpisodeData()

        // Helper to count watched episodes for an entry
        let countWatchedEpisodes entryId =
            Persistence.countWatchedEpisodes entryId
            |> Async.RunSynchronously

        let currentYear = DateTime.UtcNow.Year

        let lifetimeStats = Stats.calculateWatchTimeStats entries countWatchedEpisodes movieSessions episodeWatchData None
        let thisYearStats = Stats.calculateWatchTimeStats entries countWatchedEpisodes movieSessions episodeWatchData (Some currentYear)
        let backlog = Stats.calculateBacklogStats entries
        let topSeries = Stats.getTopSeriesByTime entries countWatchedEpisodes 10

        // Get top collections by watched time
        let! collections = Persistence.getAllCollections()
        let! collectionsWithProgress = async {
            let! progresses =
                collections
                |> List.map (fun c -> async {
                    let! progress = Persistence.getCollectionProgress c.Id
                    return (c, progress)
                })
                |> Async.Sequential

            return
                progresses
                |> Array.toList
                |> List.choose (fun (c, p) ->
                    p |> Option.map (fun prog -> (c, prog)))
                |> List.filter (fun (_, p) -> p.WatchedMinutes > 0)
                |> List.sortByDescending (fun (_, p) -> p.WatchedMinutes)
                |> List.truncate 5
        }

        return {
            LifetimeStats = lifetimeStats
            ThisYearStats = thisYearStats
            Backlog = backlog
            TopSeriesByTime = topSeries
            TopCollectionsByTime = collectionsWithProgress
        }
    }

    statsGetWatchTime = fun () -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! movieSessions = Persistence.getAllMovieWatchSessions()
        let! episodeWatchData = Persistence.getAllWatchedEpisodeData()
        let countWatchedEpisodes entryId =
            Persistence.countWatchedEpisodes entryId
            |> Async.RunSynchronously
        return Stats.calculateWatchTimeStats entries countWatchedEpisodes movieSessions episodeWatchData None
    }

    statsGetWatchTimeForYear = fun year -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! movieSessions = Persistence.getAllMovieWatchSessions()
        let! episodeWatchData = Persistence.getAllWatchedEpisodeData()
        let countWatchedEpisodes entryId =
            Persistence.countWatchedEpisodes entryId
            |> Async.RunSynchronously
        return Stats.calculateWatchTimeStats entries countWatchedEpisodes movieSessions episodeWatchData (Some year)
    }

    statsGetBacklog = fun () -> async {
        let! entries = Persistence.getAllLibraryEntries()
        return Stats.calculateBacklogStats entries
    }

    statsGetTopSeriesByTime = fun limit -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let countWatchedEpisodes entryId =
            Persistence.countWatchedEpisodes entryId
            |> Async.RunSynchronously
        return Stats.getTopSeriesByTime entries countWatchedEpisodes limit
    }

    // =====================================
    // Year-in-Review Operations
    // =====================================

    yearInReviewGetStats = fun year -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! movieSessions = Persistence.getAllMovieWatchSessions()
        let! episodeWatchData = Persistence.getAllWatchedEpisodeData()
        let! friends = Persistence.getAllFriends()
        let! collections = Persistence.getAllCollections()

        let countWatchedEpisodes entryId =
            Persistence.countWatchedEpisodes entryId
            |> Async.RunSynchronously

        let friendsByEntry entryId =
            Persistence.getFriendsForEntry entryId
            |> Async.RunSynchronously

        // For now, we pass empty completed collections (future: track collection completion dates)
        let completedCollections = []

        return Stats.calculateYearInReviewStats
            year
            entries
            countWatchedEpisodes
            movieSessions
            episodeWatchData
            friends
            friendsByEntry
            completedCollections
    }

    yearInReviewGetAvailableYears = fun () -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! movieSessions = Persistence.getAllMovieWatchSessions()
        let! episodeWatchData = Persistence.getAllWatchedEpisodeData()

        return Stats.getAvailableYears entries movieSessions episodeWatchData
    }

    // =====================================
    // Timeline Operations
    // =====================================

    timelineGetEntries = fun (filter, page, pageSize) -> async {
        let pageSize' = min pageSize 100 |> max 1  // Clamp between 1 and 100
        let page' = max 1 page
        return! Persistence.getTimelineEntries filter page' pageSize'
    }

    timelineGetByMonth = fun (year, month) -> async {
        return! Persistence.getTimelineEntriesByMonth year month
    }

    // =====================================
    // Trakt.tv Import Operations
    // =====================================

    traktGetAuthUrl = fun () -> async {
        return TraktClient.getAuthUrl()
    }

    traktExchangeCode = fun (code, state) -> async {
        return! TraktClient.exchangeCode code state
    }

    traktIsAuthenticated = fun () -> async {
        return! TraktClient.isAuthenticatedAsync()
    }

    traktLogout = fun () -> async {
        do! TraktClient.clearTokens()
    }

    traktGetImportPreview = fun options -> async {
        return! TraktImport.getImportPreview options
    }

    traktStartImport = fun options -> async {
        // Start import in background with error logging
        let importWithLogging = async {
            printfn "[Api] Starting import with options: Movies=%b, Series=%b, Watchlist=%b, Ratings=%b"
                options.ImportWatchedMovies options.ImportWatchedSeries options.ImportWatchlist options.ImportRatings
            try
                let! result = TraktImport.startImport options
                match result with
                | Ok () -> printfn "[Api] Import completed successfully"
                | Error err -> printfn "[Api] Import failed with error: %s" err
            with ex ->
                printfn "[Api] Import threw exception: %s" ex.Message
                printfn "[Api] Stack trace: %s" ex.StackTrace
        }
        Async.Start importWithLogging
        return Ok ()
    }

    traktGetImportStatus = fun () -> async {
        return TraktImport.getStatus()
    }

    traktCancelImport = fun () -> async {
        TraktImport.requestCancellation()
        return ()
    }

    // =====================================
    // Graph Operations
    // =====================================

    graphGetData = fun filter -> async {
        let! entries = Persistence.getAllLibraryEntries()
        let! friends = Persistence.getAllFriends()
        let! trackedContributors = Persistence.getAllTrackedContributors()
        let! collections = Persistence.getAllCollections()

        // Ensure all contributor profile images are cached
        for contributor in trackedContributors do
            do! ImageCache.ensureProfileCached contributor.ProfilePath

        // Helper to create a node key
        let getEntryKey (entry: LibraryEntry) =
            match entry.Media with
            | LibraryMovie _ -> $"movie-{EntryId.value entry.Id}"
            | LibrarySeries _ -> $"series-{EntryId.value entry.Id}"

        // Helper to create a GraphNode from an entry
        let entryToNode (entry: LibraryEntry) =
            match entry.Media with
            | LibraryMovie m -> MovieNode (entry.Id, m.Title, m.PosterPath)
            | LibrarySeries s -> SeriesNode (entry.Id, s.Name, s.PosterPath)

        // =====================================================
        // FOCUSED NODE MODE: Show 2-level neighborhood
        // =====================================================
        match filter.FocusedNode with
        | Some focusedNode ->
            let mutable nodes : GraphNode list = []
            let mutable nodeSet = Set.empty<string>
            let mutable edges : GraphEdge list = []

            // Helper to add a node if not already present
            let addNode (key: string) (node: GraphNode) =
                if not (Set.contains key nodeSet) then
                    nodes <- node :: nodes
                    nodeSet <- Set.add key nodeSet

            // Helper to add an edge
            let addEdge (source: GraphNode) (target: GraphNode) (rel: EdgeRelationship) =
                edges <- { Source = source; Target = target; Relationship = rel } :: edges

            // Find the focused node and add it
            let focusedKey, focusedGraphNode =
                match focusedNode with
                | FocusedMovie entryId ->
                    let entry = entries |> List.tryFind (fun e -> e.Id = entryId)
                    match entry with
                    | Some e ->
                        let key = $"movie-{EntryId.value entryId}"
                        key, Some (entryToNode e)
                    | None -> "", None
                | FocusedSeries entryId ->
                    let entry = entries |> List.tryFind (fun e -> e.Id = entryId)
                    match entry with
                    | Some e ->
                        let key = $"series-{EntryId.value entryId}"
                        key, Some (entryToNode e)
                    | None -> "", None
                | FocusedFriend friendId ->
                    let friend = friends |> List.tryFind (fun f -> f.Id = friendId)
                    match friend with
                    | Some f ->
                        let key = $"friend-{FriendId.value friendId}"
                        key, Some (FriendNode (f.Id, f.Name, f.AvatarUrl))
                    | None -> "", None
                | FocusedContributor contributorId ->
                    let contributor = trackedContributors |> List.tryFind (fun c ->
                        ContributorId.create (TmdbPersonId.value c.TmdbPersonId) = contributorId)
                    match contributor with
                    | Some c ->
                        let key = $"contributor-{ContributorId.value contributorId}"
                        key, Some (ContributorNode (contributorId, c.Name, c.ProfilePath, c.KnownForDepartment))
                    | None -> "", None
                | FocusedCollection collectionId ->
                    let coll = collections |> List.tryFind (fun c -> c.Id = collectionId)
                    match coll with
                    | Some c ->
                        let key = $"collection-{CollectionId.value collectionId}"
                        key, Some (CollectionNode (c.Id, c.Name, c.CoverImagePath))
                    | None -> "", None

            match focusedGraphNode with
            | None -> return { Nodes = []; Edges = [] }
            | Some centerNode ->
                addNode focusedKey centerNode

                // Level 1: Find all directly connected nodes
                let mutable level1Nodes : (string * GraphNode) list = []

                // For entries (movies/series), find connected friends, genres, collections, contributors
                match focusedNode with
                | FocusedMovie entryId | FocusedSeries entryId ->
                    let entry = entries |> List.find (fun e -> e.Id = entryId)

                    // Friends who watched with (from watch sessions + direct associations)
                    let! entryFriends = Persistence.getAllFriendsForEntry entryId
                    for friendId in entryFriends do
                        let friend = friends |> List.tryFind (fun f -> f.Id = friendId)
                        match friend with
                        | Some f ->
                            let key = $"friend-{FriendId.value friendId}"
                            let node = FriendNode (f.Id, f.Name, f.AvatarUrl)
                            addNode key node
                            level1Nodes <- (key, node) :: level1Nodes
                            addEdge centerNode node WatchedWith
                        | None -> ()

                    // Contributors (via filmography)
                    let entryTmdbId =
                        match entry.Media with
                        | LibraryMovie m -> Some (Movie, TmdbMovieId.value m.TmdbId)
                        | LibrarySeries s -> Some (Series, TmdbSeriesId.value s.TmdbId)

                    match entryTmdbId with
                    | Some (entryMediaType, tmdbId) ->
                        for contributor in trackedContributors do
                            let! filmographyResult = TmdbClient.getPersonFilmography contributor.TmdbPersonId
                            match filmographyResult with
                            | Ok filmography ->
                                let allCredits = filmography.CastCredits @ filmography.CrewCredits
                                let appearsInEntry = allCredits |> List.exists (fun c -> c.MediaType = entryMediaType && c.TmdbId = tmdbId)
                                if appearsInEntry then
                                    let cId = ContributorId.create (TmdbPersonId.value contributor.TmdbPersonId)
                                    let key = $"contributor-{ContributorId.value cId}"
                                    let node = ContributorNode (cId, contributor.Name, contributor.ProfilePath, contributor.KnownForDepartment)
                                    addNode key node
                                    level1Nodes <- (key, node) :: level1Nodes
                                    addEdge centerNode node (WorkedOn (Actor None))
                            | Error _ -> ()
                    | None -> ()

                    // Collections containing this entry
                    for coll in collections do
                        let! items = Persistence.getCollectionItems coll.Id
                        let containsEntry = items |> List.exists (fun item ->
                            match item.ItemRef with
                            | LibraryEntryRef eId -> eId = entryId
                            | _ -> false)
                        if containsEntry then
                            let key = $"collection-{CollectionId.value coll.Id}"
                            let node = CollectionNode (coll.Id, coll.Name, coll.CoverImagePath)
                            addNode key node
                            level1Nodes <- (key, node) :: level1Nodes
                            addEdge node centerNode BelongsToCollection

                | FocusedFriend friendId ->
                    // Find all entries watched with this friend (from watch sessions + direct)
                    for entry in entries do
                        let! entryFriends = Persistence.getAllFriendsForEntry entry.Id
                        if List.contains friendId entryFriends then
                            let key = getEntryKey entry
                            let node = entryToNode entry
                            addNode key node
                            level1Nodes <- (key, node) :: level1Nodes
                            addEdge node centerNode WatchedWith

                | FocusedContributor contributorId ->
                    // Find all entries this contributor appears in
                    let contributor = trackedContributors |> List.tryFind (fun c ->
                        ContributorId.create (TmdbPersonId.value c.TmdbPersonId) = contributorId)
                    match contributor with
                    | Some c ->
                        let! filmographyResult = TmdbClient.getPersonFilmography c.TmdbPersonId
                        match filmographyResult with
                        | Ok filmography ->
                            let allCredits = filmography.CastCredits @ filmography.CrewCredits
                            for entry in entries do
                                let entryTmdbId =
                                    match entry.Media with
                                    | LibraryMovie m -> Some (Movie, TmdbMovieId.value m.TmdbId)
                                    | LibrarySeries s -> Some (Series, TmdbSeriesId.value s.TmdbId)
                                match entryTmdbId with
                                | Some (mediaType, tmdbId) ->
                                    if allCredits |> List.exists (fun credit -> credit.MediaType = mediaType && credit.TmdbId = tmdbId) then
                                        let key = getEntryKey entry
                                        let node = entryToNode entry
                                        addNode key node
                                        level1Nodes <- (key, node) :: level1Nodes
                                        addEdge node centerNode (WorkedOn (Actor None))
                                | None -> ()
                        | Error _ -> ()
                    | None -> ()

                | FocusedCollection collectionId ->
                    // Find all entries in this collection
                    let! items = Persistence.getCollectionItems collectionId
                    for item in items do
                        match item.ItemRef with
                        | LibraryEntryRef entryId ->
                            let entry = entries |> List.tryFind (fun e -> e.Id = entryId)
                            match entry with
                            | Some e ->
                                let key = getEntryKey e
                                let node = entryToNode e
                                addNode key node
                                level1Nodes <- (key, node) :: level1Nodes
                                addEdge centerNode node BelongsToCollection
                            | None -> ()
                        | _ -> ()

                // =====================================================
                // Level 2: Find connections of level 1 nodes
                // =====================================================
                // Copy level1Nodes to iterate over (since it was built during level 1)
                let level1NodesCopy = level1Nodes |> List.toArray

                for (_, level1Node) in level1NodesCopy do
                    match level1Node with
                    | MovieNode (l1EntryId, _, _) | SeriesNode (l1EntryId, _, _) ->
                        let entryOpt = entries |> List.tryFind (fun e -> e.Id = l1EntryId)
                        match entryOpt with
                        | Some l1Entry ->
                            // Friends connected to this level 1 entry (from watch sessions + direct)
                            let! l1Friends = Persistence.getAllFriendsForEntry l1EntryId
                            for fId in l1Friends do
                                match friends |> List.tryFind (fun f -> f.Id = fId) with
                                | Some f ->
                                    let friendKey = $"friend-{FriendId.value fId}"
                                    let friendNode = FriendNode (f.Id, f.Name, f.AvatarUrl)
                                    addNode friendKey friendNode
                                    addEdge level1Node friendNode WatchedWith
                                | None -> ()

                            // Collections containing this level 1 entry
                            for coll in collections do
                                let! collItems = Persistence.getCollectionItems coll.Id
                                let hasEntry = collItems |> List.exists (fun item ->
                                    match item.ItemRef with
                                    | LibraryEntryRef eId -> eId = l1EntryId
                                    | _ -> false)
                                if hasEntry then
                                    let collKey = $"collection-{CollectionId.value coll.Id}"
                                    let collNode = CollectionNode (coll.Id, coll.Name, coll.CoverImagePath)
                                    addNode collKey collNode
                                    addEdge collNode level1Node BelongsToCollection

                            // Contributors in this level 1 entry (via filmography)
                            let l1TmdbId =
                                match l1Entry.Media with
                                | LibraryMovie m -> Some (Movie, TmdbMovieId.value m.TmdbId)
                                | LibrarySeries s -> Some (Series, TmdbSeriesId.value s.TmdbId)

                            match l1TmdbId with
                            | Some (l1MediaType, l1Tmdb) ->
                                for contrib in trackedContributors do
                                    let! filmResult = TmdbClient.getPersonFilmography contrib.TmdbPersonId
                                    match filmResult with
                                    | Ok filmography ->
                                        let allWorks = filmography.CastCredits @ filmography.CrewCredits
                                        if allWorks |> List.exists (fun w -> w.MediaType = l1MediaType && w.TmdbId = l1Tmdb) then
                                            let cId = ContributorId.create (TmdbPersonId.value contrib.TmdbPersonId)
                                            let contribKey = $"contributor-{ContributorId.value cId}"
                                            let contribNode = ContributorNode (cId, contrib.Name, contrib.ProfilePath, contrib.KnownForDepartment)
                                            addNode contribKey contribNode
                                            addEdge level1Node contribNode (WorkedOn (Actor None))
                                    | Error _ -> ()
                            | None -> ()
                        | None -> ()

                    | FriendNode (l1FriendId, friendName, friendAvatar) ->
                        // Find all entries watched with this level 1 friend (from watch sessions + direct)
                        for entry in entries do
                            let! watchedWith = Persistence.getAllFriendsForEntry entry.Id
                            // Check if this friend watched this entry
                            let friendWatched = watchedWith |> List.exists (fun fid -> FriendId.value fid = FriendId.value l1FriendId)
                            if friendWatched then
                                let entryKey = getEntryKey entry
                                let entryNode = entryToNode entry
                                addNode entryKey entryNode
                                // Create edge: entry was watched with this friend
                                addEdge entryNode (FriendNode (l1FriendId, friendName, friendAvatar)) WatchedWith

                    | ContributorNode (l1ContribId, contribName, contribProfile, contribKnownFor) ->
                        // Find all entries this level 1 contributor appears in
                        let contribOpt = trackedContributors |> List.tryFind (fun c ->
                            ContributorId.create (TmdbPersonId.value c.TmdbPersonId) = l1ContribId)
                        match contribOpt with
                        | Some contrib ->
                            let! filmResult = TmdbClient.getPersonFilmography contrib.TmdbPersonId
                            match filmResult with
                            | Ok filmography ->
                                let allWorks = filmography.CastCredits @ filmography.CrewCredits
                                for entry in entries do
                                    let entryTmdb =
                                        match entry.Media with
                                        | LibraryMovie m -> Some (Movie, TmdbMovieId.value m.TmdbId)
                                        | LibrarySeries s -> Some (Series, TmdbSeriesId.value s.TmdbId)
                                    match entryTmdb with
                                    | Some (mType, tId) ->
                                        if allWorks |> List.exists (fun w -> w.MediaType = mType && w.TmdbId = tId) then
                                            let entryKey = getEntryKey entry
                                            let entryNode = entryToNode entry
                                            addNode entryKey entryNode
                                            addEdge entryNode (ContributorNode (l1ContribId, contribName, contribProfile, contribKnownFor)) (WorkedOn (Actor None))
                                    | None -> ()
                            | Error _ -> ()
                        | None -> ()

                    | CollectionNode (l1CollId, collName, collCover) ->
                        // Find all entries in this level 1 collection
                        let! collItems = Persistence.getCollectionItems l1CollId
                        for item in collItems do
                            match item.ItemRef with
                            | LibraryEntryRef eId ->
                                match entries |> List.tryFind (fun e -> e.Id = eId) with
                                | Some entry ->
                                    let entryKey = getEntryKey entry
                                    let entryNode = entryToNode entry
                                    addNode entryKey entryNode
                                    addEdge (CollectionNode (l1CollId, collName, collCover)) entryNode BelongsToCollection
                                | None -> ()
                            | _ -> ()

                return { Nodes = nodes; Edges = edges }

        // =====================================================
        // NORMAL MODE (no focused node)
        // =====================================================
        | None ->

        // Apply node limits - separate limits for different node types
        let maxNodes = filter.MaxNodes |> Option.defaultValue 100
        let isSearching = filter.SearchQuery.IsSome && not (System.String.IsNullOrWhiteSpace(filter.SearchQuery.Value))

        // Search filter helper
        let matchesSearch (name: string) =
            match filter.SearchQuery with
            | None -> false  // When not searching, matchesSearch returns false (we use explicit checks)
            | Some query when System.String.IsNullOrWhiteSpace(query) -> false
            | Some query ->
                let q = query.ToLowerInvariant()
                name.ToLowerInvariant().Contains(q)

        // Build nodes
        let mutable nodes : GraphNode list = []
        let mutable nodeSet = Set.empty<string>  // Track node IDs to avoid duplicates
        let mutable nodeCount = 0

        if isSearching then
            // =====================================================
            // SEARCH MODE: Show all matching entities
            // =====================================================

            // Add matching movie/series nodes
            for entry in entries do
                if nodeCount < maxNodes then
                    match entry.Media with
                    | LibraryMovie m when matchesSearch m.Title ->
                        let nodeKey = $"movie-{EntryId.value entry.Id}"
                        if not (Set.contains nodeKey nodeSet) then
                            nodes <- MovieNode (entry.Id, m.Title, m.PosterPath) :: nodes
                            nodeSet <- Set.add nodeKey nodeSet
                            nodeCount <- nodeCount + 1
                    | LibrarySeries s when matchesSearch s.Name ->
                        let nodeKey = $"series-{EntryId.value entry.Id}"
                        if not (Set.contains nodeKey nodeSet) then
                            nodes <- SeriesNode (entry.Id, s.Name, s.PosterPath) :: nodes
                            nodeSet <- Set.add nodeKey nodeSet
                            nodeCount <- nodeCount + 1
                    | _ -> ()

            // Add matching friend nodes
            for friend in friends do
                if nodeCount < maxNodes && matchesSearch friend.Name then
                    let nodeKey = $"friend-{FriendId.value friend.Id}"
                    if not (Set.contains nodeKey nodeSet) then
                        nodes <- FriendNode (friend.Id, friend.Name, friend.AvatarUrl) :: nodes
                        nodeSet <- Set.add nodeKey nodeSet
                        nodeCount <- nodeCount + 1

            // Add matching tracked contributor nodes
            for contributor in trackedContributors do
                if nodeCount < maxNodes && matchesSearch contributor.Name then
                    let nodeKey = $"contributor-{TrackedContributorId.value contributor.Id}"
                    if not (Set.contains nodeKey nodeSet) then
                        let contributorId = ContributorId.create (TmdbPersonId.value contributor.TmdbPersonId)
                        nodes <- ContributorNode (contributorId, contributor.Name, contributor.ProfilePath, contributor.KnownForDepartment) :: nodes
                        nodeSet <- Set.add nodeKey nodeSet
                        nodeCount <- nodeCount + 1

            // Add matching collection nodes
            for collection in collections do
                if nodeCount < maxNodes && matchesSearch collection.Name then
                    let nodeKey = $"collection-{CollectionId.value collection.Id}"
                    if not (Set.contains nodeKey nodeSet) then
                        nodes <- CollectionNode (collection.Id, collection.Name, collection.CoverImagePath) :: nodes
                        nodeSet <- Set.add nodeKey nodeSet
                        nodeCount <- nodeCount + 1

        else
            // =====================================================
            // DEFAULT MODE: Only show collections, movies, and series
            // =====================================================

            // Add collection nodes first (they're usually fewer)
            for collection in collections do
                if nodeCount < maxNodes then
                    let nodeKey = $"collection-{CollectionId.value collection.Id}"
                    if not (Set.contains nodeKey nodeSet) then
                        nodes <- CollectionNode (collection.Id, collection.Name, collection.CoverImagePath) :: nodes
                        nodeSet <- Set.add nodeKey nodeSet
                        nodeCount <- nodeCount + 1

            // Add movie/series nodes
            for entry in entries do
                if nodeCount < maxNodes then
                    match entry.Media with
                    | LibraryMovie m ->
                        let nodeKey = $"movie-{EntryId.value entry.Id}"
                        if not (Set.contains nodeKey nodeSet) then
                            nodes <- MovieNode (entry.Id, m.Title, m.PosterPath) :: nodes
                            nodeSet <- Set.add nodeKey nodeSet
                            nodeCount <- nodeCount + 1
                    | LibrarySeries s ->
                        let nodeKey = $"series-{EntryId.value entry.Id}"
                        if not (Set.contains nodeKey nodeSet) then
                            nodes <- SeriesNode (entry.Id, s.Name, s.PosterPath) :: nodes
                            nodeSet <- Set.add nodeKey nodeSet
                            nodeCount <- nodeCount + 1

        // Build edges (relationships)
        let mutable edges : GraphEdge list = []

        // Get friends associated with each entry (from watch sessions + direct)
        for entry in entries do
            let! entryFriends = Persistence.getAllFriendsForEntry entry.Id
            let sourceNode =
                match entry.Media with
                | LibraryMovie m -> Some (MovieNode (entry.Id, m.Title, m.PosterPath))
                | LibrarySeries s -> Some (SeriesNode (entry.Id, s.Name, s.PosterPath))

            match sourceNode with
            | Some source ->
                // Only create edges if the source node exists in our set
                let sourceKey =
                    match entry.Media with
                    | LibraryMovie _ -> $"movie-{EntryId.value entry.Id}"
                    | LibrarySeries _ -> $"series-{EntryId.value entry.Id}"

                if Set.contains sourceKey nodeSet then
                    // Add edges to friends
                    for friendId in entryFriends do
                        let friendKey = $"friend-{FriendId.value friendId}"
                        if Set.contains friendKey nodeSet then
                            let friend = friends |> List.tryFind (fun f -> f.Id = friendId)
                            match friend with
                            | Some f ->
                                let edge = {
                                    Source = source
                                    Target = FriendNode (f.Id, f.Name, f.AvatarUrl)
                                    Relationship = WatchedWith
                                }
                                edges <- edge :: edges
                            | None -> ()
            | None -> ()

        // Add edges from collections to their entries
        for collection in collections do
            let collectionKey = $"collection-{CollectionId.value collection.Id}"
            if Set.contains collectionKey nodeSet then
                let! items = Persistence.getCollectionItems collection.Id
                for item in items do
                    match item.ItemRef with
                    | LibraryEntryRef entryId ->
                        // Check if the entry is in our node set
                        let entry = entries |> List.tryFind (fun e -> e.Id = entryId)
                        match entry with
                        | Some e ->
                            let entryKey =
                                match e.Media with
                                | LibraryMovie _ -> $"movie-{EntryId.value entryId}"
                                | LibrarySeries _ -> $"series-{EntryId.value entryId}"
                            if Set.contains entryKey nodeSet then
                                let targetNode =
                                    match e.Media with
                                    | LibraryMovie m -> MovieNode (entryId, m.Title, m.PosterPath)
                                    | LibrarySeries s -> SeriesNode (entryId, s.Name, s.PosterPath)
                                let edge = {
                                    Source = CollectionNode (collection.Id, collection.Name, collection.CoverImagePath)
                                    Target = targetNode
                                    Relationship = BelongsToCollection
                                }
                                edges <- edge :: edges
                        | None -> ()
                    | _ -> ()  // Skip season/episode refs for now

        // Add edges from tracked contributors to entries they appear in
        if trackedContributors.Length > 0 then
            for entry in entries do
                let entryKey =
                    match entry.Media with
                    | LibraryMovie _ -> $"movie-{EntryId.value entry.Id}"
                    | LibrarySeries _ -> $"series-{EntryId.value entry.Id}"

                if Set.contains entryKey nodeSet then
                    let entryTmdbId =
                        match entry.Media with
                        | LibraryMovie m -> Some (Movie, TmdbMovieId.value m.TmdbId)
                        | LibrarySeries s -> Some (Series, TmdbSeriesId.value s.TmdbId)

                    match entryTmdbId with
                    | Some (entryMediaType, entryId) ->
                        // Check each tracked contributor's filmography
                        for contributor in trackedContributors do
                            let contributorKey = $"contributor-{TrackedContributorId.value contributor.Id}"
                            if Set.contains contributorKey nodeSet then
                                // Check if this contributor appears in this entry's credits
                                let! filmographyResult = TmdbClient.getPersonFilmography contributor.TmdbPersonId
                                match filmographyResult with
                                | Ok filmography ->
                                    let allCredits = filmography.CastCredits @ filmography.CrewCredits
                                    let appearsInEntry =
                                        allCredits
                                        |> List.exists (fun credit ->
                                            credit.MediaType = entryMediaType && credit.TmdbId = entryId)

                                    if appearsInEntry then
                                        let sourceNode =
                                            match entry.Media with
                                            | LibraryMovie m -> MovieNode (entry.Id, m.Title, m.PosterPath)
                                            | LibrarySeries s -> SeriesNode (entry.Id, s.Name, s.PosterPath)
                                        let contributorId = ContributorId.create (TmdbPersonId.value contributor.TmdbPersonId)
                                        let edge = {
                                            Source = sourceNode
                                            Target = ContributorNode (contributorId, contributor.Name, contributor.ProfilePath, contributor.KnownForDepartment)
                                            Relationship = WorkedOn (Actor None)  // Default to Actor role without character info
                                        }
                                        edges <- edge :: edges
                                | Error _ -> ()  // Skip if filmography fetch fails
                    | None -> ()

        return {
            Nodes = nodes
            Edges = edges
        }
    }

    traktIncrementalSync = fun () -> async {
        return! TraktImport.incrementalSync()
    }

    traktGetSyncStatus = fun () -> async {
        return! TraktImport.getSyncStatus()
    }

    traktDebugGetShowHistory = fun tmdbId -> async {
        return! TraktClient.debugGetShowHistory tmdbId
    }
}

// =====================================
// Image Serving Endpoint
// =====================================

/// Serve a cached image
let private serveImage (imageType: string) (filename: string) : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            // Build the TMDB-style path (with leading slash)
            let tmdbPath = "/" + filename

            match ImageCache.getCachedImage imageType tmdbPath with
            | Some bytes ->
                let contentType = ImageCache.getContentType filename
                ctx.SetContentType contentType
                // Browser cache header (1 year) - this only affects browser memory cache,
                // not the permanent server-side storage. After expiry, browser re-fetches
                // from server which still serves from local disk.
                ctx.SetHttpHeader("Cache-Control", "public, max-age=31536000")
                return! ctx.WriteBytesAsync bytes
            | None ->
                // Image not cached - return 404
                ctx.SetStatusCode 404
                return! ctx.WriteTextAsync "Image not found"
        }

/// Image routes
let private imageRoutes : HttpHandler =
    choose [
        GET >=> routef "/images/posters/%s" (serveImage "posters")
        GET >=> routef "/images/backdrops/%s" (serveImage "backdrops")
        GET >=> routef "/images/profiles/%s" (serveImage "profiles")
        GET >=> routef "/images/collections/%s" (serveImage "collections")
        GET >=> routef "/images/avatars/%s" (serveImage "avatars")
    ]

// =====================================
// Main Web Application
// =====================================

let private remotingApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder (fun typeName methodName -> $"/api/{typeName}/{methodName}")
    |> Remoting.withErrorHandler (fun ex routeInfo ->
        printfn "Fable.Remoting ERROR in %s: %s" routeInfo.methodName ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        Propagate ex)
    |> Remoting.fromValue cinemarcoApi
    |> Remoting.buildHttpHandler

let webApp() =
    choose [
        imageRoutes
        remotingApi
    ]
