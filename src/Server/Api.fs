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
    // Friends Operations
    // =====================================

    friendsGetAll = fun () -> Persistence.getAllFriends()

    friendsCreate = fun request -> async {
        try
            let! friend = Persistence.insertFriend request
            return Ok friend
        with
        | ex -> return Error $"Failed to create friend: {ex.Message}"
    }

    // =====================================
    // Tags Operations
    // =====================================

    tagsGetAll = fun () -> Persistence.getAllTags()

    tagsCreate = fun request -> async {
        try
            let! tag = Persistence.insertTag request
            return Ok tag
        with
        | ex -> return Error $"Failed to create tag: {ex.Message}"
    }

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
