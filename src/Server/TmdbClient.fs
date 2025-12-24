module TmdbClient

open System
open System.Net
open System.Net.Http
open System.Threading
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Shared.Domain

// =====================================
// TMDB API Client
// =====================================
// Handles all HTTP communication with The Movie Database API
// with caching, rate limiting, and error handling

// =====================================
// Configuration
// =====================================

/// TMDB API base URL
let private baseUrl = "https://api.themoviedb.org/3"

/// TMDB image base URL (w500 is a good quality/size balance)
let imageBaseUrl = "https://image.tmdb.org/t/p"

/// Get the TMDB API key from environment variable
let private getApiKey () =
    match Environment.GetEnvironmentVariable("TMDB_API_KEY") with
    | null | "" -> None
    | key -> Some key

/// Cache duration for different types of requests (in hours)
module CacheDuration =
    let search = 1           // Search results: 1 hour
    let movieDetails = 24    // Movie details: 24 hours
    let seriesDetails = 24   // Series details: 24 hours
    let seasonDetails = 24   // Season details: 24 hours
    let personDetails = 168  // Person details: 1 week
    let filmography = 168    // Filmography: 1 week
    let credits = 24         // Credits: 24 hours
    let collection = 168     // Collections: 1 week
    let trending = 1         // Trending: 1 hour

// =====================================
// HTTP Client Setup
// =====================================

/// Shared HTTP client instance (reuse for connection pooling)
let private httpClient =
    let handler = new HttpClientHandler()
    let client = new HttpClient(handler)
    client.DefaultRequestHeaders.Add("Accept", "application/json")
    client.Timeout <- TimeSpan.FromSeconds(30.0)
    client

/// Rate limiting: tracks the last request time
let private lastRequestTime = ref DateTime.MinValue
let private requestLock = obj()

/// Minimum time between requests (TMDB allows ~40 requests per 10 seconds)
let private minRequestInterval = TimeSpan.FromMilliseconds(250.0)

/// Wait if needed to respect rate limiting
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

/// Make a GET request to TMDB API with caching
let private makeRequest (endpoint: string) (cacheKey: string) (cacheDuration: int) : Async<Result<string, string>> = async {
    match getApiKey() with
    | None ->
        return Error "TMDB_API_KEY environment variable is not set"
    | Some apiKey ->
        // Check cache first
        let! cached = Persistence.getCachedTmdbResponse cacheKey
        match cached with
        | Some json ->
            return Ok json
        | None ->
            // Make the request
            waitForRateLimit()

            let separator = if endpoint.Contains("?") then "&" else "?"
            let url = $"{baseUrl}{endpoint}{separator}api_key={apiKey}"

            try
                let! response = httpClient.GetAsync(url) |> Async.AwaitTask
                let! content = response.Content.ReadAsStringAsync() |> Async.AwaitTask

                match response.StatusCode with
                | HttpStatusCode.OK ->
                    // Cache the response
                    do! Persistence.setCachedTmdbResponse cacheKey content cacheDuration
                    return Ok content

                | HttpStatusCode.TooManyRequests ->
                    // Rate limited - wait and retry once
                    do! Async.Sleep 1000
                    waitForRateLimit()
                    let! retryResponse = httpClient.GetAsync(url) |> Async.AwaitTask
                    let! retryContent = retryResponse.Content.ReadAsStringAsync() |> Async.AwaitTask
                    if retryResponse.IsSuccessStatusCode then
                        do! Persistence.setCachedTmdbResponse cacheKey retryContent cacheDuration
                        return Ok retryContent
                    else
                        return Error $"TMDB API rate limited: {retryResponse.StatusCode}"

                | HttpStatusCode.NotFound ->
                    return Error "Not found on TMDB"

                | _ ->
                    return Error $"TMDB API error: {response.StatusCode} - {content}"
            with
            | ex -> return Error $"Network error: {ex.Message}"
}

// =====================================
// JSON Parsing Helpers
// =====================================

let private parseDateTime (s: obj) : DateTime option =
    match s with
    | null -> None
    | :? string as str when String.IsNullOrEmpty(str) -> None
    | :? string as str ->
        match DateTime.TryParse(str) with
        | true, dt -> Some dt
        | false, _ -> None
    | :? JToken as token ->
        match token.Type with
        | JTokenType.Null -> None
        | JTokenType.String ->
            let str = string token
            if String.IsNullOrEmpty(str) then None
            else
                match DateTime.TryParse(str) with
                | true, dt -> Some dt
                | false, _ -> None
        | _ -> None
    | _ -> None

let private parseFloat (token: JToken) : float option =
    match token with
    | null -> None
    | t when t.Type = JTokenType.Null -> None
    | t -> Some (float t)

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

let private parseStringList (token: JToken) : string list =
    match token with
    | null -> []
    | t when t.Type = JTokenType.Null -> []
    | t when t.Type = JTokenType.Array ->
        t |> Seq.map (fun x -> string x) |> Seq.toList
    | _ -> []

// =====================================
// Response Parsers
// =====================================

/// Parse a search result from JSON
let private parseSearchResult (json: JToken) (mediaType: MediaType) : TmdbSearchResult =
    let title =
        match mediaType with
        | Movie -> parseString json.["title"] |> Option.defaultValue ""
        | Series -> parseString json.["name"] |> Option.defaultValue ""

    let releaseDate =
        match mediaType with
        | Movie -> parseDateTime json.["release_date"]
        | Series -> parseDateTime json.["first_air_date"]

    {
        TmdbId = int json.["id"]
        MediaType = mediaType
        Title = title
        ReleaseDate = releaseDate
        PosterPath = parseString json.["poster_path"]
        Overview = parseString json.["overview"]
        VoteAverage = parseFloat json.["vote_average"]
    }

/// Parse a multi-search result (can be movie or series)
let private parseMultiSearchResult (json: JToken) : TmdbSearchResult option =
    let mediaTypeStr = parseString json.["media_type"]
    match mediaTypeStr with
    | Some "movie" -> Some (parseSearchResult json Movie)
    | Some "tv" -> Some (parseSearchResult json Series)
    | _ -> None // Skip people and other types

/// Parse a cast member from JSON
let private parseCastMember (json: JToken) : TmdbCastMember =
    {
        TmdbPersonId = TmdbPersonId (int json.["id"])
        Name = parseString json.["name"] |> Option.defaultValue ""
        Character = parseString json.["character"]
        ProfilePath = parseString json.["profile_path"]
        Order = parseInt json.["order"] |> Option.defaultValue 999
    }

/// Parse a crew member from JSON
let private parseCrewMember (json: JToken) : TmdbCrewMember =
    {
        TmdbPersonId = TmdbPersonId (int json.["id"])
        Name = parseString json.["name"] |> Option.defaultValue ""
        Department = parseString json.["department"] |> Option.defaultValue ""
        Job = parseString json.["job"] |> Option.defaultValue ""
        ProfilePath = parseString json.["profile_path"]
    }

/// Parse genres from TMDB format
let private parseGenres (json: JToken) : string list =
    match json with
    | null -> []
    | t when t.Type = JTokenType.Null -> []
    | t when t.Type = JTokenType.Array ->
        t |> Seq.choose (fun g -> parseString g.["name"]) |> Seq.toList
    | _ -> []

/// Parse movie details from JSON
let private parseMovieDetails (json: JObject) : TmdbMovieDetails =
    let credits = json.["credits"]
    let cast =
        match credits with
        | null -> []
        | c -> c.["cast"] |> Seq.map parseCastMember |> Seq.toList
    let crew =
        match credits with
        | null -> []
        | c -> c.["crew"] |> Seq.map parseCrewMember |> Seq.toList

    {
        TmdbId = TmdbMovieId (int json.["id"])
        Title = parseString json.["title"] |> Option.defaultValue ""
        OriginalTitle = parseString json.["original_title"]
        Overview = parseString json.["overview"]
        ReleaseDate = parseDateTime json.["release_date"]
        RuntimeMinutes = parseInt json.["runtime"]
        PosterPath = parseString json.["poster_path"]
        BackdropPath = parseString json.["backdrop_path"]
        Genres = parseGenres json.["genres"]
        OriginalLanguage = parseString json.["original_language"]
        VoteAverage = parseFloat json.["vote_average"]
        VoteCount = parseInt json.["vote_count"]
        Tagline = parseString json.["tagline"]
        ImdbId = parseString json.["imdb_id"]
        Cast = cast
        Crew = crew
    }

/// Parse a season summary from JSON
let private parseSeasonSummary (json: JToken) : TmdbSeasonSummary =
    {
        SeasonNumber = parseInt json.["season_number"] |> Option.defaultValue 0
        Name = parseString json.["name"]
        Overview = parseString json.["overview"]
        PosterPath = parseString json.["poster_path"]
        AirDate = parseDateTime json.["air_date"]
        EpisodeCount = parseInt json.["episode_count"] |> Option.defaultValue 0
    }

/// Parse series details from JSON
let private parseSeriesDetails (json: JObject) : TmdbSeriesDetails =
    let credits = json.["credits"]
    let cast =
        match credits with
        | null -> []
        | c when c.["cast"] <> null -> c.["cast"] |> Seq.map parseCastMember |> Seq.toList
        | _ -> []
    let crew =
        match credits with
        | null -> []
        | c when c.["crew"] <> null -> c.["crew"] |> Seq.map parseCrewMember |> Seq.toList
        | _ -> []

    let seasons =
        match json.["seasons"] with
        | null -> []
        | s -> s |> Seq.map parseSeasonSummary |> Seq.toList

    let episodeRuntime =
        match json.["episode_run_time"] with
        | null -> None
        | t when t.Type = JTokenType.Array && t.HasValues ->
            Some (int (t.First))
        | _ -> None

    {
        TmdbId = TmdbSeriesId (int json.["id"])
        Name = parseString json.["name"] |> Option.defaultValue ""
        OriginalName = parseString json.["original_name"]
        Overview = parseString json.["overview"]
        FirstAirDate = parseDateTime json.["first_air_date"]
        LastAirDate = parseDateTime json.["last_air_date"]
        PosterPath = parseString json.["poster_path"]
        BackdropPath = parseString json.["backdrop_path"]
        Genres = parseGenres json.["genres"]
        OriginalLanguage = parseString json.["original_language"]
        VoteAverage = parseFloat json.["vote_average"]
        VoteCount = parseInt json.["vote_count"]
        Status = parseString json.["status"] |> Option.defaultValue "Unknown"
        NumberOfSeasons = parseInt json.["number_of_seasons"] |> Option.defaultValue 0
        NumberOfEpisodes = parseInt json.["number_of_episodes"] |> Option.defaultValue 0
        EpisodeRunTimeMinutes = episodeRuntime
        Seasons = seasons
        Cast = cast
        Crew = crew
    }

/// Parse an episode summary from JSON
let private parseEpisodeSummary (json: JToken) : TmdbEpisodeSummary =
    {
        EpisodeNumber = parseInt json.["episode_number"] |> Option.defaultValue 0
        Name = parseString json.["name"] |> Option.defaultValue ""
        Overview = parseString json.["overview"]
        AirDate = parseDateTime json.["air_date"]
        RuntimeMinutes = parseInt json.["runtime"]
        StillPath = parseString json.["still_path"]
    }

/// Parse season details from JSON
let private parseSeasonDetails (seriesId: TmdbSeriesId) (json: JObject) : TmdbSeasonDetails =
    let episodes =
        match json.["episodes"] with
        | null -> []
        | e -> e |> Seq.map parseEpisodeSummary |> Seq.toList

    {
        TmdbSeriesId = seriesId
        SeasonNumber = parseInt json.["season_number"] |> Option.defaultValue 0
        Name = parseString json.["name"]
        Overview = parseString json.["overview"]
        PosterPath = parseString json.["poster_path"]
        AirDate = parseDateTime json.["air_date"]
        Episodes = episodes
    }

/// Parse person details from JSON
let private parsePersonDetails (json: JObject) : TmdbPersonDetails =
    {
        TmdbPersonId = TmdbPersonId (int json.["id"])
        Name = parseString json.["name"] |> Option.defaultValue ""
        ProfilePath = parseString json.["profile_path"]
        KnownForDepartment = parseString json.["known_for_department"]
        Birthday = parseDateTime json.["birthday"]
        Deathday = parseDateTime json.["deathday"]
        PlaceOfBirth = parseString json.["place_of_birth"]
        Biography = parseString json.["biography"]
    }

/// Convert a crew job to ContributorRole
let private jobToRole (job: string) (department: string) : ContributorRole =
    match job.ToLowerInvariant() with
    | "director" -> Director
    | "writer" | "screenplay" | "story" -> Writer
    | "director of photography" | "cinematographer" -> Cinematographer
    | "original music composer" | "composer" | "music" -> Composer
    | "producer" -> Producer
    | "executive producer" -> ExecutiveProducer
    | "creator" -> CreatedBy
    | _ -> Other department

/// Parse a filmography work from JSON
let private parseFilmographyWork (json: JToken) (isActor: bool) : TmdbWork =
    let mediaTypeStr = parseString json.["media_type"]
    let mediaType =
        match mediaTypeStr with
        | Some "movie" -> Movie
        | Some "tv" -> Series
        | _ -> Movie // Default to movie

    let title =
        match mediaType with
        | Movie -> parseString json.["title"] |> Option.defaultValue ""
        | Series -> parseString json.["name"] |> Option.defaultValue ""

    let releaseDate =
        match mediaType with
        | Movie -> parseDateTime json.["release_date"]
        | Series -> parseDateTime json.["first_air_date"]

    let role =
        if isActor then
            Actor (parseString json.["character"])
        else
            let job = parseString json.["job"] |> Option.defaultValue ""
            let dept = parseString json.["department"] |> Option.defaultValue ""
            jobToRole job dept

    {
        TmdbId = int json.["id"]
        MediaType = mediaType
        Title = title
        ReleaseDate = releaseDate
        PosterPath = parseString json.["poster_path"]
        Role = role
    }

/// Parse filmography from JSON
let private parseFilmography (personId: TmdbPersonId) (json: JObject) : TmdbFilmography =
    let castCredits =
        match json.["cast"] with
        | null -> []
        | c -> c |> Seq.map (fun j -> parseFilmographyWork j true) |> Seq.toList

    let crewCredits =
        match json.["crew"] with
        | null -> []
        | c -> c |> Seq.map (fun j -> parseFilmographyWork j false) |> Seq.toList

    {
        PersonId = personId
        CastCredits = castCredits
        CrewCredits = crewCredits
    }

/// Parse credits from JSON
let private parseCredits (json: JObject) : TmdbCredits =
    let cast =
        match json.["cast"] with
        | null -> []
        | c -> c |> Seq.map parseCastMember |> Seq.toList

    let crew =
        match json.["crew"] with
        | null -> []
        | c -> c |> Seq.map parseCrewMember |> Seq.toList

    { Cast = cast; Crew = crew }

/// Parse a collection from JSON
let private parseCollection (json: JObject) : TmdbCollection =
    let parts =
        match json.["parts"] with
        | null -> []
        | p -> p |> Seq.map (fun j -> parseSearchResult j Movie) |> Seq.toList

    {
        TmdbCollectionId = int json.["id"]
        Name = parseString json.["name"] |> Option.defaultValue ""
        Overview = parseString json.["overview"]
        PosterPath = parseString json.["poster_path"]
        BackdropPath = parseString json.["backdrop_path"]
        Parts = parts
    }

// =====================================
// Public API Functions
// =====================================

/// Health check - verify TMDB API is accessible and API key is valid
let healthCheck () : Async<Result<string, string>> = async {
    match getApiKey() with
    | None -> return Error "TMDB_API_KEY environment variable is not set"
    | Some _ ->
        try
            // Use configuration endpoint as a lightweight health check
            let! result = makeRequest "/configuration" "health:config" 1
            match result with
            | Ok _ -> return Ok "Connected"
            | Error err -> return Error err
        with
        | ex -> return Error $"Connection failed: {ex.Message}"
}

/// Search for movies
let searchMovies (query: string) : Async<TmdbSearchResult list> = async {
    if String.IsNullOrWhiteSpace(query) then
        return []
    else
        let encodedQuery = Uri.EscapeDataString(query)
        let cacheKey = $"search:movie:{query.ToLowerInvariant()}"
        let! result = makeRequest $"/search/movie?query={encodedQuery}&include_adult=false" cacheKey CacheDuration.search

        match result with
        | Ok json ->
            let parsed = JObject.Parse(json)
            let results = parsed.["results"]
            return results |> Seq.map (fun j -> parseSearchResult j Movie) |> Seq.toList
        | Error _ ->
            return []
}

/// Search for series (TV shows)
let searchSeries (query: string) : Async<TmdbSearchResult list> = async {
    if String.IsNullOrWhiteSpace(query) then
        return []
    else
        let encodedQuery = Uri.EscapeDataString(query)
        let cacheKey = $"search:tv:{query.ToLowerInvariant()}"
        let! result = makeRequest $"/search/tv?query={encodedQuery}&include_adult=false" cacheKey CacheDuration.search

        match result with
        | Ok json ->
            let parsed = JObject.Parse(json)
            let results = parsed.["results"]
            return results |> Seq.map (fun j -> parseSearchResult j Series) |> Seq.toList
        | Error _ ->
            return []
}

/// Search for both movies and series
let searchAll (query: string) : Async<TmdbSearchResult list> = async {
    if String.IsNullOrWhiteSpace(query) then
        return []
    else
        let encodedQuery = Uri.EscapeDataString(query)
        let cacheKey = $"search:multi:{query.ToLowerInvariant()}"
        let! result = makeRequest $"/search/multi?query={encodedQuery}&include_adult=false" cacheKey CacheDuration.search

        match result with
        | Ok json ->
            let parsed = JObject.Parse(json)
            let results = parsed.["results"]
            return results |> Seq.choose parseMultiSearchResult |> Seq.toList
        | Error _ ->
            return []
}

/// Find movie or series by IMDB ID (e.g., "tt1883367")
let findByImdbId (imdbId: string) : Async<TmdbSearchResult option> = async {
    if String.IsNullOrWhiteSpace(imdbId) then
        return None
    else
        let cacheKey = $"find:imdb:{imdbId.ToLowerInvariant()}"
        let! result = makeRequest $"/find/{imdbId}?external_source=imdb_id" cacheKey CacheDuration.search

        match result with
        | Ok json ->
            try
                let parsed = JObject.Parse(json)
                // Check movie_results first
                let movieResults = parsed.["movie_results"]
                if movieResults <> null && movieResults.HasValues then
                    let movie = movieResults.[0]
                    return Some (parseSearchResult movie Movie)
                else
                    // Check tv_results
                    let tvResults = parsed.["tv_results"]
                    if tvResults <> null && tvResults.HasValues then
                        let tv = tvResults.[0]
                        return Some (parseSearchResult tv Series)
                    else
                        return None
            with _ ->
                return None
        | Error _ ->
            return None
}

/// Get full movie details
let getMovieDetails (TmdbMovieId movieId) : Async<Result<TmdbMovieDetails, string>> = async {
    let cacheKey = $"movie:{movieId}"
    let! result = makeRequest $"/movie/{movieId}?append_to_response=credits" cacheKey CacheDuration.movieDetails

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseMovieDetails parsed)
        with ex ->
            return Error $"Failed to parse movie details: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get full series details
let getSeriesDetails (TmdbSeriesId seriesId) : Async<Result<TmdbSeriesDetails, string>> = async {
    let cacheKey = $"tv:{seriesId}"
    let! result = makeRequest $"/tv/{seriesId}?append_to_response=credits" cacheKey CacheDuration.seriesDetails

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseSeriesDetails parsed)
        with ex ->
            return Error $"Failed to parse series details: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get season details with episodes
let getSeasonDetails (TmdbSeriesId seriesId) (seasonNumber: int) : Async<Result<TmdbSeasonDetails, string>> = async {
    let cacheKey = $"tv:{seriesId}:season:{seasonNumber}"
    let! result = makeRequest $"/tv/{seriesId}/season/{seasonNumber}" cacheKey CacheDuration.seasonDetails

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseSeasonDetails (TmdbSeriesId seriesId) parsed)
        with ex ->
            return Error $"Failed to parse season details: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get person details
let getPersonDetails (TmdbPersonId personId) : Async<Result<TmdbPersonDetails, string>> = async {
    let cacheKey = $"person:{personId}"
    let! result = makeRequest $"/person/{personId}" cacheKey CacheDuration.personDetails

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parsePersonDetails parsed)
        with ex ->
            return Error $"Failed to parse person details: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get person's filmography (combined credits)
let getPersonFilmography (TmdbPersonId personId) : Async<Result<TmdbFilmography, string>> = async {
    let cacheKey = $"person:{personId}:credits"
    let! result = makeRequest $"/person/{personId}/combined_credits" cacheKey CacheDuration.filmography

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseFilmography (TmdbPersonId personId) parsed)
        with ex ->
            return Error $"Failed to parse filmography: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get movie credits (cast and crew)
let getMovieCredits (TmdbMovieId movieId) : Async<Result<TmdbCredits, string>> = async {
    let cacheKey = $"movie:{movieId}:credits"
    let! result = makeRequest $"/movie/{movieId}/credits" cacheKey CacheDuration.credits

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseCredits parsed)
        with ex ->
            return Error $"Failed to parse credits: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get series credits (cast and crew)
let getSeriesCredits (TmdbSeriesId seriesId) : Async<Result<TmdbCredits, string>> = async {
    let cacheKey = $"tv:{seriesId}:credits"
    let! result = makeRequest $"/tv/{seriesId}/credits" cacheKey CacheDuration.credits

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseCredits parsed)
        with ex ->
            return Error $"Failed to parse credits: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get TMDB collection (e.g., Marvel collection)
let getTmdbCollection (collectionId: int) : Async<Result<TmdbCollection, string>> = async {
    let cacheKey = $"collection:{collectionId}"
    let! result = makeRequest $"/collection/{collectionId}" cacheKey CacheDuration.collection

    match result with
    | Ok json ->
        try
            let parsed = JObject.Parse(json)
            return Ok (parseCollection parsed)
        with ex ->
            return Error $"Failed to parse collection: {ex.Message}"
    | Error err ->
        return Error err
}

/// Get trending movies (this week)
let getTrendingMovies () : Async<TmdbSearchResult list> = async {
    let cacheKey = "trending:movie"
    let! result = makeRequest "/trending/movie/week" cacheKey CacheDuration.trending

    match result with
    | Ok json ->
        let parsed = JObject.Parse(json)
        let results = parsed.["results"]
        return results |> Seq.map (fun j -> parseSearchResult j Movie) |> Seq.toList
    | Error _ ->
        return []
}

/// Get trending series (this week)
let getTrendingSeries () : Async<TmdbSearchResult list> = async {
    let cacheKey = "trending:tv"
    let! result = makeRequest "/trending/tv/week" cacheKey CacheDuration.trending

    match result with
    | Ok json ->
        let parsed = JObject.Parse(json)
        let results = parsed.["results"]
        return results |> Seq.map (fun j -> parseSearchResult j Series) |> Seq.toList
    | Error _ ->
        return []
}

// =====================================
// Image URL Helpers
// =====================================

/// Get the full URL for a poster image
let getPosterUrl (size: string) (path: string option) : string option =
    path |> Option.map (fun p -> $"{imageBaseUrl}/{size}{p}")

/// Get the full URL for a backdrop image
let getBackdropUrl (size: string) (path: string option) : string option =
    path |> Option.map (fun p -> $"{imageBaseUrl}/{size}{p}")

/// Get the full URL for a profile image
let getProfileUrl (size: string) (path: string option) : string option =
    path |> Option.map (fun p -> $"{imageBaseUrl}/{size}{p}")

/// Common image sizes
module ImageSize =
    // Poster sizes
    let posterSmall = "w185"
    let posterMedium = "w342"
    let posterLarge = "w500"
    let posterOriginal = "original"

    // Backdrop sizes
    let backdropSmall = "w300"
    let backdropMedium = "w780"
    let backdropLarge = "w1280"
    let backdropOriginal = "original"

    // Profile sizes
    let profileSmall = "w45"
    let profileMedium = "w185"
    let profileLarge = "h632"
    let profileOriginal = "original"
