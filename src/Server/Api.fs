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

    libraryToggleTag = fun (entryId, tagId) -> async {
        try
            // Check if tag is already associated
            let! currentTags = Persistence.getTagsForEntry entryId
            if List.contains tagId currentTags then
                do! Persistence.removeTagFromEntry entryId tagId
            else
                do! Persistence.addTagToEntry entryId tagId
            let! entry = Persistence.getLibraryEntryById entryId
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Entry not found after update"
        with
        | ex -> return Error $"Failed to toggle tag: {ex.Message}"
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
    // Tags Operations
    // =====================================

    tagsGetAll = fun () -> Persistence.getAllTags()

    tagsGetById = fun id -> async {
        let! tag = Persistence.getTagById (TagId id)
        match tag with
        | Some t -> return Ok t
        | None -> return Error "Tag not found"
    }

    tagsCreate = fun request -> async {
        try
            let! tag = Persistence.insertTag request
            return Ok tag
        with
        | ex -> return Error $"Failed to create tag: {ex.Message}"
    }

    tagsUpdate = fun request -> async {
        try
            do! Persistence.updateTag request
            let! tag = Persistence.getTagById request.Id
            match tag with
            | Some t -> return Ok t
            | None -> return Error "Tag not found after update"
        with
        | ex -> return Error $"Failed to update tag: {ex.Message}"
    }

    tagsDelete = fun id -> async {
        try
            do! Persistence.deleteTag (TagId id)
            return Ok ()
        with
        | ex -> return Error $"Failed to delete tag: {ex.Message}"
    }

    tagsGetTaggedEntries = fun tagId -> async {
        let! entryIds = Persistence.getEntriesWithTag (TagId tagId)
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

    sessionsToggleTag = fun (sessionId, tagId) -> async {
        try
            // Check if tag is already associated
            let! currentTags = Persistence.getTagsForSession sessionId
            if List.contains tagId currentTags then
                do! Persistence.removeTagFromSession sessionId tagId
            else
                do! Persistence.addTagToSession sessionId tagId
            let! session = Persistence.getSessionById sessionId
            match session with
            | Some s -> return Ok s
            | None -> return Error "Session not found after update"
        with
        | ex -> return Error $"Failed to toggle tag: {ex.Message}"
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
    // TMDB Operations
    // =====================================

    tmdbSearchMovies = fun query -> TmdbClient.searchMovies query

    tmdbSearchSeries = fun query -> TmdbClient.searchSeries query

    tmdbSearchAll = fun query -> TmdbClient.searchAll query

    tmdbGetMovieDetails = fun tmdbId -> TmdbClient.getMovieDetails tmdbId

    tmdbGetSeriesDetails = fun tmdbId -> TmdbClient.getSeriesDetails tmdbId

    tmdbGetSeasonDetails = fun (tmdbId, seasonNumber) ->
        TmdbClient.getSeasonDetails tmdbId seasonNumber

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
