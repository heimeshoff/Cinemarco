module TraktClient

open System
open System.Net
open System.Net.Http
open System.Threading
open System.Text
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Shared.Domain

// =====================================
// Trakt.tv API Client
// =====================================
// Handles OAuth authentication and data import from Trakt.tv
// Trakt uses TMDB IDs, making the mapping straightforward

// =====================================
// Configuration
// =====================================

/// Trakt API base URL
let private baseUrl = "https://api.trakt.tv"

/// Get Trakt client ID from environment variable
let private getClientId () =
    match Environment.GetEnvironmentVariable("TRAKT_CLIENT_ID") with
    | null | "" -> None
    | id -> Some id

/// Get Trakt client secret from environment variable
let private getClientSecret () =
    match Environment.GetEnvironmentVariable("TRAKT_CLIENT_SECRET") with
    | null | "" -> None
    | secret -> Some secret

/// Get the redirect URI for OAuth
let getRedirectUri () =
    // For local development, use a local callback
    // In production, this should be configured via environment variable
    match Environment.GetEnvironmentVariable("TRAKT_REDIRECT_URI") with
    | null | "" -> "urn:ietf:wg:oauth:2.0:oob"  // Out-of-band for manual entry
    | uri -> uri

// =====================================
// Token Storage (persisted to database)
// =====================================

type private TokenInfo = {
    AccessToken: string
    RefreshToken: string
    ExpiresAt: DateTime
}

// In-memory cache of tokens (loaded from DB on startup)
let private tokenCache = ref (None: TokenInfo option)
let private cacheLoaded = ref false

/// Load tokens from database into cache
let private loadTokensFromDb () = async {
    if not !cacheLoaded then
        let! settings = Persistence.getTraktSettings()
        match settings.AccessToken, settings.RefreshToken, settings.ExpiresAt with
        | Some access, Some refresh, Some expires ->
            tokenCache := Some { AccessToken = access; RefreshToken = refresh; ExpiresAt = expires }
        | _ ->
            tokenCache := None
        cacheLoaded := true
}

/// Check if we have a valid access token
let isAuthenticated () =
    // Synchronously check cache (will be populated on first API call)
    match !tokenCache with
    | Some t -> t.ExpiresAt > DateTime.UtcNow
    | None -> false

/// Check authentication async (loads from DB if needed)
let isAuthenticatedAsync () = async {
    do! loadTokensFromDb()
    return isAuthenticated()
}

/// Get the current access token if valid
let private getAccessToken () =
    match !tokenCache with
    | Some t when t.ExpiresAt > DateTime.UtcNow -> Some t.AccessToken
    | _ -> None

/// Get access token async (loads from DB if needed)
let private getAccessTokenAsync () = async {
    do! loadTokensFromDb()
    return getAccessToken()
}

/// Store a new token (to both cache and database)
let private storeToken accessToken refreshToken expiresIn = async {
    let expiresAt = DateTime.UtcNow.AddSeconds(float expiresIn - 60.0) // 1 minute buffer
    tokenCache := Some {
        AccessToken = accessToken
        RefreshToken = refreshToken
        ExpiresAt = expiresAt
    }
    cacheLoaded := true
    do! Persistence.saveTraktTokens accessToken refreshToken expiresAt
}

/// Clear stored tokens (from both cache and database)
let clearTokens () = async {
    tokenCache := None
    do! Persistence.clearTraktTokens()
}

/// Get the last sync timestamp
let getLastSyncTime () = async {
    let! settings = Persistence.getTraktSettings()
    return settings.LastSyncAt
}

/// Update the last sync timestamp
let updateLastSyncTime () = Persistence.updateTraktLastSync()

// =====================================
// HTTP Client Setup
// =====================================

/// Shared HTTP client instance
let private httpClient =
    let handler = new HttpClientHandler()
    let client = new HttpClient(handler)
    client.DefaultRequestHeaders.Add("Accept", "application/json")
    client.DefaultRequestHeaders.Add("trakt-api-version", "2")
    client.DefaultRequestHeaders.Add("User-Agent", "Cinemarco/1.0 (Personal Cinema Tracker)")
    client.Timeout <- TimeSpan.FromSeconds(30.0)
    client

/// Rate limiting: Trakt allows 1000 calls per 5 minutes
let private lastRequestTime = ref DateTime.MinValue
let private requestLock = obj()
let private minRequestInterval = TimeSpan.FromMilliseconds(50.0)

let private waitForRateLimit () =
    lock requestLock (fun () ->
        let now = DateTime.UtcNow
        let elapsed = now - !lastRequestTime
        if elapsed < minRequestInterval then
            let waitTime = minRequestInterval - elapsed
            Thread.Sleep(int waitTime.TotalMilliseconds)
        lastRequestTime := DateTime.UtcNow
    )

// =====================================
// HTTP Request Helpers
// =====================================

/// Make an authenticated GET request to Trakt API
let private makeAuthenticatedRequest (endpoint: string) : Async<Result<string, string>> = async {
    // Load tokens from DB if not already loaded
    let! accessTokenOpt = getAccessTokenAsync()
    match getClientId(), accessTokenOpt with
    | None, _ ->
        return Error "TRAKT_CLIENT_ID environment variable is not set"
    | _, None ->
        return Error "Not authenticated with Trakt. Please connect your account first."
    | Some clientId, Some accessToken ->
        waitForRateLimit()

        let url = $"{baseUrl}{endpoint}"
        use request = new HttpRequestMessage(HttpMethod.Get, url)
        request.Headers.Add("trakt-api-key", clientId)
        request.Headers.Add("Authorization", $"Bearer {accessToken}")

        try
            let! response = httpClient.SendAsync(request) |> Async.AwaitTask
            let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask

            match response.StatusCode with
            | HttpStatusCode.OK ->
                return Ok content
            | HttpStatusCode.Unauthorized ->
                return Error "Trakt authentication expired. Please reconnect your account."
            | HttpStatusCode.TooManyRequests ->
                // Rate limited - wait and retry once
                do! Async.Sleep 2000
                let! retryResponse = httpClient.SendAsync(request) |> Async.AwaitTask
                let! retryContent = retryResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                if retryResponse.IsSuccessStatusCode then
                    return Ok retryContent
                else
                    return Error $"Trakt API rate limited"
            | _ ->
                return Error $"Trakt API error: {response.StatusCode} - {content}"
        with
        | ex -> return Error $"Network error: {ex.Message}"
}

/// Make a POST request to Trakt API (for OAuth)
let private makePostRequest (endpoint: string) (body: obj) : Async<Result<string, string>> = async {
    let url = $"{baseUrl}{endpoint}"
    let json = JsonConvert.SerializeObject(body)
    use content = new StringContent(json, Encoding.UTF8, "application/json")

    // Add Content-Type header explicitly
    content.Headers.ContentType <- System.Net.Http.Headers.MediaTypeHeaderValue("application/json")

    try
        use request = new HttpRequestMessage(HttpMethod.Post, url)
        request.Content <- content

        // Add trakt-api-key header for OAuth endpoints
        match getClientId() with
        | Some clientId -> request.Headers.Add("trakt-api-key", clientId)
        | None -> ()

        let! response = httpClient.SendAsync(request) |> Async.AwaitTask
        let! responseContent = response.Content.ReadAsStringAsync() |> Async.AwaitTask

        if response.IsSuccessStatusCode then
            return Ok responseContent
        else
            return Error $"Trakt API error: {response.StatusCode} - {responseContent}"
    with
    | ex -> return Error $"Network error: {ex.Message}"
}

// =====================================
// JSON Parsing Helpers
// =====================================

let private parseDateTime (token: JToken) : DateTime option =
    match token with
    | null -> None
    | t when t.Type = JTokenType.Null -> None
    | t ->
        let str = string t
        // Try parsing with invariant culture first (handles ISO 8601 and common formats)
        match DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None) with
        | true, dt -> Some dt
        | false, _ ->
            // Try specific formats that Trakt might return
            let formats = [|
                "MM/dd/yyyy HH:mm:ss"
                "yyyy-MM-ddTHH:mm:ss.fffZ"
                "yyyy-MM-ddTHH:mm:ssZ"
                "yyyy-MM-dd HH:mm:ss"
            |]
            match DateTime.TryParseExact(str, formats, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None) with
            | true, dt -> Some dt
            | false, _ -> None

let private parseInt (token: JToken) : int option =
    match token with
    | null -> None
    | t when t.Type = JTokenType.Null -> None
    | t -> Some (int t)

let private parseString (token: JToken) : string option =
    match token with
    | null -> None
    | t when t.Type = JTokenType.Null -> None
    | t ->
        let s = string t
        if String.IsNullOrEmpty(s) then None else Some s

// =====================================
// OAuth Flow
// =====================================

/// Generate the OAuth authorization URL
let getAuthUrl () : Result<TraktAuthUrl, string> =
    match getClientId() with
    | None -> Error "TRAKT_CLIENT_ID environment variable is not set"
    | Some clientId ->
        let state = Guid.NewGuid().ToString("N")
        let redirectUri = Uri.EscapeDataString(getRedirectUri())
        let url = $"https://trakt.tv/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUri}&state={state}"
        Ok { Url = url; State = state }

/// Exchange the OAuth code for access tokens
let exchangeCode (code: string) (state: string) : Async<Result<unit, string>> = async {
    match getClientId(), getClientSecret() with
    | None, _ -> return Error "TRAKT_CLIENT_ID environment variable is not set"
    | _, None -> return Error "TRAKT_CLIENT_SECRET environment variable is not set"
    | Some clientId, Some clientSecret ->
        let body = {|
            code = code
            client_id = clientId
            client_secret = clientSecret
            redirect_uri = getRedirectUri()
            grant_type = "authorization_code"
        |}

        let! result = makePostRequest "/oauth/token" body
        match result with
        | Error err -> return Error err
        | Ok json ->
            try
                let parsed = JObject.Parse(json)
                let accessToken = string parsed.["access_token"]
                let refreshToken = string parsed.["refresh_token"]
                let expiresIn = int parsed.["expires_in"]

                do! storeToken accessToken refreshToken expiresIn
                return Ok ()
            with
            | ex -> return Error $"Failed to parse token response: {ex.Message}"
}

// =====================================
// Watch History Parsing
// =====================================

/// Parse a movie from Trakt history
let private parseHistoryMovie (json: JToken) : TraktHistoryItem option =
    let movie = json.["movie"]
    if isNull movie then None
    else
        let ids = movie.["ids"]
        let tmdbId = parseInt ids.["tmdb"]
        match tmdbId with
        | None -> None  // Skip items without TMDB ID
        | Some id ->
            Some {
                TmdbId = id
                MediaType = Movie
                Title = parseString movie.["title"] |> Option.defaultValue ""
                WatchedAt = parseDateTime json.["watched_at"]
                TraktRating = None  // Ratings come from a separate endpoint
            }

/// Parse a show (series) from Trakt history
let private parseHistoryShow (json: JToken) : TraktHistoryItem option =
    let show = json.["show"]
    if isNull show then None
    else
        let ids = show.["ids"]
        let tmdbId = parseInt ids.["tmdb"]
        match tmdbId with
        | None -> None  // Skip items without TMDB ID
        | Some id ->
            Some {
                TmdbId = id
                MediaType = Series
                Title = parseString show.["title"] |> Option.defaultValue ""
                WatchedAt = parseDateTime json.["watched_at"]
                TraktRating = None
            }

/// Parse a rating item
let private parseRatingItem (json: JToken) : (int * MediaType * int) option =
    // Returns (tmdb_id, media_type, rating)
    let rating = parseInt json.["rating"]
    let mediaType = parseString json.["type"]

    match rating, mediaType with
    | Some r, Some "movie" ->
        let movie = json.["movie"]
        let ids = movie.["ids"]
        let tmdbId = parseInt ids.["tmdb"]
        tmdbId |> Option.map (fun id -> (id, Movie, r))
    | Some r, Some "show" ->
        let show = json.["show"]
        let ids = show.["ids"]
        let tmdbId = parseInt ids.["tmdb"]
        tmdbId |> Option.map (fun id -> (id, Series, r))
    | _ -> None

// =====================================
// Public API Functions
// =====================================

/// Get watched movies from Trakt history
let getWatchedMovies () : Async<Result<TraktHistoryItem list, string>> = async {
    // Use sync/history for full watch history including dates
    let! result = makeAuthenticatedRequest "/sync/history/movies?limit=10000"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let items =
                parsed
                |> Seq.choose parseHistoryMovie
                |> Seq.toList
            return Ok items
        with
        | ex -> return Error $"Failed to parse watched movies: {ex.Message}"
}

/// Get watched movies from Trakt history since a specific date (for incremental sync)
let getWatchedMoviesSince (since: DateTime) : Async<Result<TraktHistoryItem list, string>> = async {
    let startAt = since.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    let! result = makeAuthenticatedRequest $"/sync/history/movies?start_at={startAt}&limit=10000"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let items =
                parsed
                |> Seq.choose parseHistoryMovie
                |> Seq.toList
            return Ok items
        with
        | ex -> return Error $"Failed to parse watched movies: {ex.Message}"
}

/// Get watched shows from Trakt history (basic info only)
let getWatchedShows () : Async<Result<TraktHistoryItem list, string>> = async {
    // Use sync/watched for shows (gives all watched episodes grouped by show)
    let! result = makeAuthenticatedRequest "/sync/watched/shows"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let items =
                parsed
                |> Seq.choose (fun showJson ->
                    let show = showJson.["show"]
                    if isNull show then None
                    else
                        let ids = show.["ids"]
                        let tmdbId = parseInt ids.["tmdb"]
                        let lastWatched = parseDateTime showJson.["last_watched_at"]
                        tmdbId |> Option.map (fun id ->
                            {
                                TmdbId = id
                                MediaType = Series
                                Title = parseString show.["title"] |> Option.defaultValue ""
                                WatchedAt = lastWatched
                                TraktRating = None
                            }))
                |> Seq.toList
            return Ok items
        with
        | ex -> return Error $"Failed to parse watched shows: {ex.Message}"
}

/// Get watched shows with episode-level detail from Trakt
/// Uses /sync/history/shows for accurate episode watch dates
let getWatchedShowsWithEpisodes () : Async<Result<TraktWatchedSeries list, string>> = async {
    // Use history endpoint to get episode-level watch dates
    let! result = makeAuthenticatedRequest "/sync/history/shows?limit=10000"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)

            // Group by show TMDB ID and collect episodes
            let allEpisodes =
                parsed
                |> Seq.choose (fun historyItem ->
                    let show = historyItem.["show"]
                    let episode = historyItem.["episode"]
                    if isNull show || isNull episode then None
                    else
                        let showIds = show.["ids"]
                        let tmdbId = parseInt showIds.["tmdb"]
                        let title = parseString show.["title"] |> Option.defaultValue ""
                        let watchedAt = parseDateTime historyItem.["watched_at"]
                        let seasonNum = parseInt episode.["season"] |> Option.defaultValue 0
                        let episodeNum = parseInt episode.["number"]

                        match tmdbId, episodeNum with
                        | Some tid, Some epNum ->
                            Some (tid, title, seasonNum, epNum, watchedAt)
                        | _ -> None)
                |> Seq.toList

            let showGroups =
                allEpisodes
                |> List.groupBy (fun (tid, _, _, _, _) -> tid)
                |> List.map (fun (tmdbId, episodes) ->
                    let episodesList = episodes
                    let title = episodesList |> List.tryHead |> Option.map (fun (_, t, _, _, _) -> t) |> Option.defaultValue ""
                    let lastWatched = episodesList |> List.choose (fun (_, _, _, _, w) -> w) |> List.sortDescending |> List.tryHead

                    // Deduplicate episodes, keeping the earliest watch date per episode
                    let watchedEpisodes =
                        episodesList
                        |> List.groupBy (fun (_, _, s, e, _) -> (s, e))
                        |> List.map (fun ((s, e), watches) ->
                            // Keep the earliest watch date for this episode
                            let earliestWatch = watches |> List.choose (fun (_, _, _, _, w) -> w) |> List.sort |> List.tryHead
                            {
                                SeasonNumber = s
                                EpisodeNumber = e
                                WatchedAt = earliestWatch
                            })

                    {
                        TmdbId = tmdbId
                        Title = title
                        LastWatchedAt = lastWatched
                        WatchedEpisodes = watchedEpisodes
                        TraktRating = None
                    })

            return Ok showGroups
        with
        | ex -> return Error $"Failed to parse watched shows: {ex.Message}"
}

/// Get watched shows with episode-level detail since a specific date (for incremental sync)
/// Does NOT deduplicate - returns all episode watches as-is
let getWatchedShowsWithEpisodesSince (since: DateTime) : Async<Result<TraktWatchedSeries list, string>> = async {
    let startAt = since.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    let! result = makeAuthenticatedRequest $"/sync/history/shows?start_at={startAt}&limit=10000"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)

            // Group by show TMDB ID and collect episodes (no deduplication for incremental)
            let allEpisodes =
                parsed
                |> Seq.choose (fun historyItem ->
                    let show = historyItem.["show"]
                    let episode = historyItem.["episode"]
                    if isNull show || isNull episode then None
                    else
                        let showIds = show.["ids"]
                        let tmdbId = parseInt showIds.["tmdb"]
                        let title = parseString show.["title"] |> Option.defaultValue ""
                        let watchedAt = parseDateTime historyItem.["watched_at"]
                        let seasonNum = parseInt episode.["season"] |> Option.defaultValue 0
                        let episodeNum = parseInt episode.["number"]

                        match tmdbId, episodeNum with
                        | Some tid, Some epNum ->
                            Some (tid, title, seasonNum, epNum, watchedAt)
                        | _ -> None)
                |> Seq.toList

            let showGroups =
                allEpisodes
                |> List.groupBy (fun (tid, _, _, _, _) -> tid)
                |> List.map (fun (tmdbId, episodes) ->
                    let episodesList = episodes
                    let title = episodesList |> List.tryHead |> Option.map (fun (_, t, _, _, _) -> t) |> Option.defaultValue ""
                    let lastWatched = episodesList |> List.choose (fun (_, _, _, _, w) -> w) |> List.sortDescending |> List.tryHead

                    // For incremental sync, keep all watches without deduplication
                    let watchedEpisodes =
                        episodesList
                        |> List.map (fun (_, _, s, e, w) ->
                            {
                                SeasonNumber = s
                                EpisodeNumber = e
                                WatchedAt = w
                            })

                    {
                        TmdbId = tmdbId
                        Title = title
                        LastWatchedAt = lastWatched
                        WatchedEpisodes = watchedEpisodes
                        TraktRating = None
                    })

            return Ok showGroups
        with
        | ex -> return Error $"Failed to parse watched shows: {ex.Message}"
}

/// Debug: Get raw history for a specific show (by Trakt show ID or TMDB ID)
let debugGetShowHistory (tmdbId: int) : Async<Result<string, string>> = async {
    // Get all history and filter for the specific show
    let! result = makeAuthenticatedRequest "/sync/history/shows?limit=10000"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let showEntries =
                parsed
                |> Seq.filter (fun item ->
                    let show = item.["show"]
                    if isNull show then false
                    else
                        let ids = show.["ids"]
                        let showTmdbId = parseInt ids.["tmdb"]
                        showTmdbId = Some tmdbId)
                |> Seq.truncate 20  // Limit to first 20 entries
                |> Seq.map (fun item -> item.ToString())
                |> String.concat "\n---\n"
            return Ok showEntries
        with
        | ex -> return Error $"Failed to get show history: {ex.Message}"
}

/// Get user ratings from Trakt
let getRatings () : Async<Result<Map<int * MediaType, int>, string>> = async {
    let! result = makeAuthenticatedRequest "/sync/ratings"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let ratings =
                parsed
                |> Seq.choose parseRatingItem
                |> Seq.map (fun (tmdbId, mediaType, rating) -> ((tmdbId, mediaType), rating))
                |> Map.ofSeq
            return Ok ratings
        with
        | ex -> return Error $"Failed to parse ratings: {ex.Message}"
}

/// Get watchlist items from Trakt
let getWatchlist () : Async<Result<TraktHistoryItem list, string>> = async {
    let! result = makeAuthenticatedRequest "/sync/watchlist"
    match result with
    | Error err -> return Error err
    | Ok json ->
        try
            let parsed = JArray.Parse(json)
            let items =
                parsed
                |> Seq.choose (fun item ->
                    let itemType = parseString item.["type"]
                    match itemType with
                    | Some "movie" -> parseHistoryMovie item
                    | Some "show" -> parseHistoryShow item
                    | _ -> None)
                |> Seq.toList
            return Ok items
        with
        | ex -> return Error $"Failed to parse watchlist: {ex.Message}"
}

/// Map Trakt rating (1-10) to PersonalRating
let mapTraktRating (traktRating: int) : PersonalRating =
    // Trakt: 1-10, Cinemarco: 1-5 (Waste, Meh, Decent, Entertaining, Outstanding)
    match traktRating with
    | r when r >= 9 -> Outstanding      // 9-10 -> Outstanding
    | r when r >= 7 -> Entertaining     // 7-8 -> Entertaining
    | r when r >= 5 -> Decent           // 5-6 -> Decent
    | r when r >= 3 -> Meh              // 3-4 -> Meh
    | _ -> Waste                        // 1-2 -> Waste
