module Persistence

open System
open System.IO
open Microsoft.Data.Sqlite
open Dapper
open Shared.Domain

// =====================================
// Cinemarco Persistence Layer
// =====================================
// SQLite + Dapper based persistence

// =====================================
// Configuration
// =====================================

/// Data directory is configurable via DATA_DIR environment variable
let private dataDir =
    match Environment.GetEnvironmentVariable("DATA_DIR") with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "app", "cinemarco")
    | path -> path

/// Path to the SQLite database file
let private dbFile = Path.Combine(dataDir, "cinemarco.db")

/// Connection string for SQLite
let connectionString = $"Data Source={dbFile}"

/// Get the absolute path to the database file
let getDatabasePath () = dbFile

/// Ensure the data directory exists
let ensureDataDir () =
    if not (Directory.Exists dataDir) then
        Directory.CreateDirectory dataDir |> ignore
        printfn $"Created data directory: {dataDir}"

/// Get a new database connection
let getConnection () =
    let conn = new SqliteConnection(connectionString)
    conn.Open()
    // Enable foreign keys for this connection
    use cmd = new SqliteCommand("PRAGMA foreign_keys=ON", conn)
    cmd.ExecuteNonQuery() |> ignore
    conn

// =====================================
// Database Record Types (for Dapper)
// =====================================
// These flat types map directly to database rows

[<CLIMutable>]
type MovieRecord = {
    id: int
    tmdb_id: int
    title: string
    original_title: string
    overview: string
    release_date: string
    runtime_minutes: Nullable<int>
    poster_path: string
    backdrop_path: string
    genres: string
    original_language: string
    vote_average: Nullable<float>
    vote_count: Nullable<int>
    tagline: string
    imdb_id: string
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type SeriesRecord = {
    id: int
    tmdb_id: int
    name: string
    original_name: string
    overview: string
    first_air_date: string
    last_air_date: string
    poster_path: string
    backdrop_path: string
    genres: string
    original_language: string
    vote_average: Nullable<float>
    vote_count: Nullable<int>
    status: string
    number_of_seasons: int
    number_of_episodes: int
    episode_runtime_minutes: Nullable<int>
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type SeasonRecord = {
    id: int
    series_id: int
    tmdb_season_id: int
    season_number: int
    name: string
    overview: string
    poster_path: string
    air_date: string
    episode_count: int
}

[<CLIMutable>]
type SeasonEpisodeCountRecord = {
    season_number: int
    episode_count: int
}

[<CLIMutable>]
type EpisodeRecord = {
    id: int
    series_id: int
    season_id: int
    tmdb_episode_id: int
    season_number: int
    episode_number: int
    name: string
    overview: string
    air_date: string
    runtime_minutes: Nullable<int>
    still_path: string
}

[<CLIMutable>]
type FriendRecord = {
    id: int
    name: string
    nickname: string
    avatar_url: string
    notes: string
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type CollectionRecord = {
    id: int
    name: string
    description: string
    cover_image_path: string
    is_public_franchise: int
    tmdb_collection_id: Nullable<int>
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type CollectionItemRecord = {
    id: int
    collection_id: int
    item_type: string
    entry_id: Nullable<int>
    series_id: Nullable<int>
    season_number: Nullable<int>
    episode_number: Nullable<int>
    position: int
    notes: string
}

[<CLIMutable>]
type ContributorRecord = {
    id: int
    tmdb_person_id: int
    name: string
    profile_path: string
    known_for_department: string
    birthday: string
    deathday: string
    place_of_birth: string
    biography: string
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type TrackedContributorRecord = {
    id: string
    tmdb_person_id: int
    name: string
    profile_path: string
    known_for_department: string
    created_at: string
    notes: string
}

[<CLIMutable>]
type LibraryEntryRecord = {
    id: int
    media_type: string
    movie_id: Nullable<int>
    series_id: Nullable<int>
    why_recommended_by_friend_id: Nullable<int>
    why_recommended_by_name: string
    why_source: string
    why_context: string
    why_date_recommended: string
    watch_status: string
    progress_current_season: Nullable<int>
    progress_current_episode: Nullable<int>
    progress_last_watched_date: string
    abandoned_season: Nullable<int>
    abandoned_episode: Nullable<int>
    abandoned_reason: string
    abandoned_date: string
    personal_rating: Nullable<int>
    notes: string
    is_favorite: int
    date_added: string
    date_first_watched: string
    date_last_watched: string
    created_at: string
    updated_at: string
}

[<CLIMutable>]
type WatchSessionRecord = {
    id: int
    entry_id: int
    name: string
    status: string
    start_date: string
    end_date: string
    notes: string
    created_at: string
    updated_at: string
    is_default: int
}

[<CLIMutable>]
type EpisodeProgressRecord = {
    id: int
    entry_id: int
    session_id: Nullable<int>
    series_id: int
    season_number: int
    episode_number: int
    is_watched: int
    watched_date: string
}

[<CLIMutable>]
type MovieWatchSessionRecord = {
    id: int
    entry_id: int
    watched_date: string
    name: string
    created_at: string
}

[<CLIMutable>]
type CacheEntryRecord = {
    cache_key: string
    expires_at: string
    cache_value: string
}

[<CLIMutable>]
type CacheTypeCountRecord = {
    type_prefix: string
    cnt: int64
}

[<CLIMutable>]
type SeriesInfoRecord = {
    series_id: Nullable<int>
    number_of_episodes: int
}

[<CLIMutable>]
type private EpisodeAirDateRecord = {
    season_number: int64
    episode_number: int64
    air_date: string
}

// =====================================
// Helper Functions
// =====================================

let private parseDateTime (s: string) : DateTime option =
    if String.IsNullOrEmpty(s) then None
    else
        match DateTime.TryParse(s) with
        | true, dt -> Some dt
        | false, _ -> None

let private formatDateTime (dt: DateTime option) : string =
    match dt with
    | Some d -> d.ToString("o")
    | None -> null

let private nullableToOption (n: Nullable<'T>) : 'T option =
    if n.HasValue then Some n.Value else None

let private optionToNullable (o: 'T option) : Nullable<'T> =
    match o with
    | Some v -> Nullable(v)
    | None -> Nullable()

let private parseGenres (s: string) : string list =
    if String.IsNullOrEmpty(s) then []
    else
        try
            System.Text.Json.JsonSerializer.Deserialize<string list>(s)
        with _ -> []

let private formatGenres (genres: string list) : string =
    System.Text.Json.JsonSerializer.Serialize(genres)

let private parseSeriesStatus (s: string) : SeriesStatus =
    match s with
    | "Returning" -> Returning
    | "Ended" -> Ended
    | "Canceled" -> Canceled
    | "InProduction" -> InProduction
    | "Planned" -> Planned
    | _ -> Unknown

let private formatSeriesStatus (status: SeriesStatus) : string =
    match status with
    | Returning -> "Returning"
    | Ended -> "Ended"
    | Canceled -> "Canceled"
    | InProduction -> "InProduction"
    | Planned -> "Planned"
    | Unknown -> "Unknown"

// =====================================
// Movie CRUD
// =====================================

let private recordToMovie (r: MovieRecord) : Movie = {
    Id = MovieId r.id
    TmdbId = TmdbMovieId r.tmdb_id
    Title = r.title
    OriginalTitle = if String.IsNullOrEmpty(r.original_title) then None else Some r.original_title
    Overview = if String.IsNullOrEmpty(r.overview) then None else Some r.overview
    ReleaseDate = parseDateTime r.release_date
    RuntimeMinutes = nullableToOption r.runtime_minutes
    PosterPath = if String.IsNullOrEmpty(r.poster_path) then None else Some r.poster_path
    BackdropPath = if String.IsNullOrEmpty(r.backdrop_path) then None else Some r.backdrop_path
    Genres = parseGenres r.genres
    OriginalLanguage = if String.IsNullOrEmpty(r.original_language) then None else Some r.original_language
    VoteAverage = nullableToOption r.vote_average
    VoteCount = nullableToOption r.vote_count
    Tagline = if String.IsNullOrEmpty(r.tagline) then None else Some r.tagline
    ImdbId = if String.IsNullOrEmpty(r.imdb_id) then None else Some r.imdb_id
    CreatedAt = DateTime.Parse(r.created_at)
    UpdatedAt = DateTime.Parse(r.updated_at)
}

let getAllMovies () : Async<Movie list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<MovieRecord>("SELECT * FROM movies ORDER BY title")
        |> Async.AwaitTask
    return records |> Seq.map recordToMovie |> Seq.toList
}

let getMovieById (MovieId id) : Async<Movie option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<MovieRecord>(
            "SELECT * FROM movies WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToMovie record)
}

let getMovieByTmdbId (TmdbMovieId tmdbId) : Async<Movie option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<MovieRecord>(
            "SELECT * FROM movies WHERE tmdb_id = @TmdbId",
            {| TmdbId = tmdbId |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToMovie record)
}

let insertMovie (movie: Movie) : Async<int> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO movies (tmdb_id, title, original_title, overview, release_date, runtime_minutes,
                poster_path, backdrop_path, genres, original_language, vote_average, vote_count,
                tagline, imdb_id, created_at, updated_at)
            VALUES (@TmdbId, @Title, @OriginalTitle, @Overview, @ReleaseDate, @RuntimeMinutes,
                @PosterPath, @BackdropPath, @Genres, @OriginalLanguage, @VoteAverage, @VoteCount,
                @Tagline, @ImdbId, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            TmdbId = TmdbMovieId.value movie.TmdbId
            Title = movie.Title
            OriginalTitle = movie.OriginalTitle |> Option.toObj
            Overview = movie.Overview |> Option.toObj
            ReleaseDate = formatDateTime movie.ReleaseDate
            RuntimeMinutes = optionToNullable movie.RuntimeMinutes
            PosterPath = movie.PosterPath |> Option.toObj
            BackdropPath = movie.BackdropPath |> Option.toObj
            Genres = formatGenres movie.Genres
            OriginalLanguage = movie.OriginalLanguage |> Option.toObj
            VoteAverage = optionToNullable movie.VoteAverage
            VoteCount = optionToNullable movie.VoteCount
            Tagline = movie.Tagline |> Option.toObj
            ImdbId = movie.ImdbId |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return int id
}

let updateMovie (movie: Movie) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let param = {|
        Id = MovieId.value movie.Id
        TmdbId = TmdbMovieId.value movie.TmdbId
        Title = movie.Title
        OriginalTitle = Option.toObj movie.OriginalTitle
        Overview = Option.toObj movie.Overview
        ReleaseDate = formatDateTime movie.ReleaseDate
        RuntimeMinutes = optionToNullable movie.RuntimeMinutes
        PosterPath = Option.toObj movie.PosterPath
        BackdropPath = Option.toObj movie.BackdropPath
        Genres = formatGenres movie.Genres
        OriginalLanguage = Option.toObj movie.OriginalLanguage
        VoteAverage = optionToNullable movie.VoteAverage
        VoteCount = optionToNullable movie.VoteCount
        Tagline = Option.toObj movie.Tagline
        ImdbId = Option.toObj movie.ImdbId
        UpdatedAt = now
    |}
    do! conn.ExecuteAsync("""
        UPDATE movies SET
            tmdb_id = @TmdbId, title = @Title, original_title = @OriginalTitle,
            overview = @Overview, release_date = @ReleaseDate, runtime_minutes = @RuntimeMinutes,
            poster_path = @PosterPath, backdrop_path = @BackdropPath, genres = @Genres,
            original_language = @OriginalLanguage, vote_average = @VoteAverage, vote_count = @VoteCount,
            tagline = @Tagline, imdb_id = @ImdbId, updated_at = @UpdatedAt
        WHERE id = @Id
    """, param) |> Async.AwaitTask |> Async.Ignore
}

let deleteMovie (MovieId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM movies WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Series CRUD
// =====================================

let private recordToSeries (r: SeriesRecord) : Series = {
    Id = SeriesId r.id
    TmdbId = TmdbSeriesId r.tmdb_id
    Name = r.name
    OriginalName = if String.IsNullOrEmpty(r.original_name) then None else Some r.original_name
    Overview = if String.IsNullOrEmpty(r.overview) then None else Some r.overview
    FirstAirDate = parseDateTime r.first_air_date
    LastAirDate = parseDateTime r.last_air_date
    PosterPath = if String.IsNullOrEmpty(r.poster_path) then None else Some r.poster_path
    BackdropPath = if String.IsNullOrEmpty(r.backdrop_path) then None else Some r.backdrop_path
    Genres = parseGenres r.genres
    OriginalLanguage = if String.IsNullOrEmpty(r.original_language) then None else Some r.original_language
    VoteAverage = nullableToOption r.vote_average
    VoteCount = nullableToOption r.vote_count
    Status = parseSeriesStatus r.status
    NumberOfSeasons = r.number_of_seasons
    NumberOfEpisodes = r.number_of_episodes
    EpisodeRunTimeMinutes = nullableToOption r.episode_runtime_minutes
    CreatedAt = DateTime.Parse(r.created_at)
    UpdatedAt = DateTime.Parse(r.updated_at)
}

let getAllSeries () : Async<Series list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<SeriesRecord>("SELECT * FROM series ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToSeries |> Seq.toList
}

let getSeriesById (SeriesId id) : Async<Series option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<SeriesRecord>(
            "SELECT * FROM series WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToSeries record)
}

let getSeriesByTmdbId (TmdbSeriesId tmdbId) : Async<Series option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<SeriesRecord>(
            "SELECT * FROM series WHERE tmdb_id = @TmdbId",
            {| TmdbId = tmdbId |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToSeries record)
}

let insertSeries (series: Series) : Async<int> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO series (tmdb_id, name, original_name, overview, first_air_date, last_air_date,
                poster_path, backdrop_path, genres, original_language, vote_average, vote_count,
                status, number_of_seasons, number_of_episodes, episode_runtime_minutes, created_at, updated_at)
            VALUES (@TmdbId, @Name, @OriginalName, @Overview, @FirstAirDate, @LastAirDate,
                @PosterPath, @BackdropPath, @Genres, @OriginalLanguage, @VoteAverage, @VoteCount,
                @Status, @NumberOfSeasons, @NumberOfEpisodes, @EpisodeRunTimeMinutes, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            TmdbId = TmdbSeriesId.value series.TmdbId
            Name = series.Name
            OriginalName = series.OriginalName |> Option.toObj
            Overview = series.Overview |> Option.toObj
            FirstAirDate = formatDateTime series.FirstAirDate
            LastAirDate = formatDateTime series.LastAirDate
            PosterPath = series.PosterPath |> Option.toObj
            BackdropPath = series.BackdropPath |> Option.toObj
            Genres = formatGenres series.Genres
            OriginalLanguage = series.OriginalLanguage |> Option.toObj
            VoteAverage = optionToNullable series.VoteAverage
            VoteCount = optionToNullable series.VoteCount
            Status = formatSeriesStatus series.Status
            NumberOfSeasons = series.NumberOfSeasons
            NumberOfEpisodes = series.NumberOfEpisodes
            EpisodeRunTimeMinutes = optionToNullable series.EpisodeRunTimeMinutes
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return int id
}

let deleteSeries (SeriesId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM series WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Friends CRUD
// =====================================

let private recordToFriend (r: FriendRecord) : Friend = {
    Id = FriendId r.id
    Name = r.name
    Nickname = if String.IsNullOrEmpty(r.nickname) then None else Some r.nickname
    AvatarUrl = if String.IsNullOrEmpty(r.avatar_url) then None else Some r.avatar_url
    CreatedAt = DateTime.Parse(r.created_at)
}

let getAllFriends () : Async<Friend list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<FriendRecord>("SELECT * FROM friends ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToFriend |> Seq.toList
}

let getFriendById (FriendId id) : Async<Friend option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<FriendRecord>(
            "SELECT * FROM friends WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToFriend record)
}

let insertFriend (request: CreateFriendRequest) : Async<Friend> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO friends (name, nickname, created_at, updated_at)
            VALUES (@Name, @Nickname, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            Name = request.Name
            Nickname = request.Nickname |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return {
        Id = FriendId (int id)
        Name = request.Name
        Nickname = request.Nickname
        AvatarUrl = None
        CreatedAt = DateTime.UtcNow
    }
}

let updateFriend (request: UpdateFriendRequest) : Async<unit> = async {
    use conn = getConnection()
    let! existing = getFriendById request.Id
    match existing with
    | None -> ()
    | Some friend ->
        let now = DateTime.UtcNow.ToString("o")
        let name = Option.defaultValue friend.Name request.Name
        let nickname = Option.toObj (Option.orElse friend.Nickname request.Nickname)

        // Handle avatar: None = keep existing, Some "" = remove, Some data = update
        let avatarUrl =
            match request.AvatarBase64 with
            | None -> friend.AvatarUrl |> Option.toObj  // Keep existing
            | Some "" ->
                // Remove existing avatar
                friend.AvatarUrl |> Option.iter ImageCache.deleteFriendAvatar
                null
            | Some base64 ->
                // Update avatar
                friend.AvatarUrl |> Option.iter ImageCache.deleteFriendAvatar
                match ImageCache.saveFriendAvatar (FriendId.value request.Id) base64 with
                | Ok path -> path
                | Error _ -> friend.AvatarUrl |> Option.toObj

        let param = {|
            Id = FriendId.value request.Id
            Name = name
            Nickname = nickname
            AvatarUrl = avatarUrl
            UpdatedAt = now
        |}
        do! conn.ExecuteAsync("""
            UPDATE friends SET
                name = @Name,
                nickname = @Nickname,
                avatar_url = @AvatarUrl,
                updated_at = @UpdatedAt
            WHERE id = @Id
        """, param) |> Async.AwaitTask |> Async.Ignore
}

let deleteFriend (FriendId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM friends WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Collections CRUD
// =====================================

let private recordToCollection (r: CollectionRecord) : Collection = {
    Id = CollectionId r.id
    Name = r.name
    Description = if String.IsNullOrEmpty(r.description) then None else Some r.description
    CoverImagePath = if String.IsNullOrEmpty(r.cover_image_path) then None else Some r.cover_image_path
    IsPublicFranchise = r.is_public_franchise = 1
    TmdbCollectionId = nullableToOption r.tmdb_collection_id
    CreatedAt = DateTime.Parse(r.created_at)
    UpdatedAt = DateTime.Parse(r.updated_at)
}

let getAllCollections () : Async<Collection list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<CollectionRecord>("SELECT * FROM collections ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToCollection |> Seq.toList
}

let getCollectionById (CollectionId id) : Async<Collection option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<CollectionRecord>(
            "SELECT * FROM collections WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToCollection record)
}

/// Find a collection by name (case-insensitive)
let getCollectionByName (name: string) : Async<Collection option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<CollectionRecord>(
            "SELECT * FROM collections WHERE LOWER(name) = LOWER(@Name)",
            {| Name = name |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToCollection record)
}

let insertCollection (request: CreateCollectionRequest) : Async<Collection> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO collections (name, description, is_public_franchise, created_at, updated_at)
            VALUES (@Name, @Description, 0, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            Name = request.Name
            Description = request.Description |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask

    // Save logo if provided
    let! coverImagePath = async {
        match request.LogoBase64 with
        | Some base64 when not (String.IsNullOrWhiteSpace base64) ->
            match ImageCache.saveCollectionLogo (int id) base64 with
            | Ok path ->
                // Update the collection with the logo path
                do! conn.ExecuteAsync(
                    "UPDATE collections SET cover_image_path = @Path WHERE id = @Id",
                    {| Id = int id; Path = path |}) |> Async.AwaitTask |> Async.Ignore
                return Some path
            | Error _ -> return None
        | _ -> return None
    }

    return {
        Id = CollectionId (int id)
        Name = request.Name
        Description = request.Description
        CoverImagePath = coverImagePath
        IsPublicFranchise = false
        TmdbCollectionId = None
        CreatedAt = DateTime.UtcNow
        UpdatedAt = DateTime.UtcNow
    }
}

let deleteCollection (CollectionId id) : Async<unit> = async {
    use conn = getConnection()
    // Delete collection items first
    do! conn.ExecuteAsync("DELETE FROM collection_items WHERE collection_id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
    do! conn.ExecuteAsync("DELETE FROM collections WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

let updateCollection (request: UpdateCollectionRequest) : Async<unit> = async {
    use conn = getConnection()
    let! existing = getCollectionById request.Id
    match existing with
    | None -> ()
    | Some collection ->
        let now = DateTime.UtcNow.ToString("o")
        let name = Option.defaultValue collection.Name request.Name
        let description = Option.toObj (Option.orElse collection.Description request.Description)

        // Handle logo: None = keep existing, Some "" = remove, Some data = update
        let coverImagePath =
            match request.LogoBase64 with
            | None -> collection.CoverImagePath |> Option.toObj  // Keep existing
            | Some "" ->
                // Remove existing logo
                collection.CoverImagePath |> Option.iter ImageCache.deleteCollectionLogo
                null
            | Some base64 ->
                // Update logo
                collection.CoverImagePath |> Option.iter ImageCache.deleteCollectionLogo
                match ImageCache.saveCollectionLogo (CollectionId.value request.Id) base64 with
                | Ok path -> path
                | Error _ -> collection.CoverImagePath |> Option.toObj

        let param = {|
            Id = CollectionId.value request.Id
            Name = name
            Description = description
            CoverImagePath = coverImagePath
            UpdatedAt = now
        |}
        do! conn.ExecuteAsync("""
            UPDATE collections SET
                name = @Name,
                description = @Description,
                cover_image_path = @CoverImagePath,
                updated_at = @UpdatedAt
            WHERE id = @Id
        """, param) |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Collection Items CRUD
// =====================================

let private recordToCollectionItem (r: CollectionItemRecord) : CollectionItem =
    let itemRef =
        match r.item_type with
        | "season" when r.series_id.HasValue && r.season_number.HasValue ->
            SeasonRef (SeriesId r.series_id.Value, r.season_number.Value)
        | "episode" when r.series_id.HasValue && r.season_number.HasValue && r.episode_number.HasValue ->
            EpisodeRef (SeriesId r.series_id.Value, r.season_number.Value, r.episode_number.Value)
        | _ when r.entry_id.HasValue ->
            LibraryEntryRef (EntryId r.entry_id.Value)
        | _ ->
            // Fallback - shouldn't happen with valid data
            LibraryEntryRef (EntryId 0)
    {
        CollectionId = CollectionId r.collection_id
        ItemRef = itemRef
        Position = r.position
        Notes = if String.IsNullOrEmpty(r.notes) then None else Some r.notes
    }

let getCollectionItems (CollectionId collectionId) : Async<CollectionItem list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<CollectionItemRecord>(
            "SELECT * FROM collection_items WHERE collection_id = @CollectionId ORDER BY position",
            {| CollectionId = collectionId |}
        ) |> Async.AwaitTask
    return records |> Seq.map recordToCollectionItem |> Seq.toList
}

/// Check if an item already exists in a collection
let isItemInCollection (CollectionId collectionId) (itemRef: CollectionItemRef) : Async<bool> = async {
    use conn = getConnection()
    let! count =
        match itemRef with
        | LibraryEntryRef (EntryId entryId) ->
            conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM collection_items WHERE collection_id = @CollectionId AND entry_id = @EntryId",
                {| CollectionId = collectionId; EntryId = entryId |}) |> Async.AwaitTask
        | SeasonRef (SeriesId seriesId, seasonNumber) ->
            conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM collection_items WHERE collection_id = @CollectionId AND series_id = @SeriesId AND season_number = @SeasonNumber AND item_type = 'season'",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber |}) |> Async.AwaitTask
        | EpisodeRef (SeriesId seriesId, seasonNumber, episodeNumber) ->
            conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM collection_items WHERE collection_id = @CollectionId AND series_id = @SeriesId AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber AND item_type = 'episode'",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber |}) |> Async.AwaitTask
    return count > 0
}

let addItemToCollection (CollectionId collectionId) (itemRef: CollectionItemRef) (notes: string option) : Async<unit> = async {
    // Check if item already exists in collection
    let! alreadyExists = isItemInCollection (CollectionId collectionId) itemRef
    if alreadyExists then
        () // Item already in collection, skip
    else
        use conn = getConnection()
        // Get the max position for this collection
        let! maxPos = conn.ExecuteScalarAsync<Nullable<int>>("SELECT MAX(position) FROM collection_items WHERE collection_id = @CollectionId", {| CollectionId = collectionId |}) |> Async.AwaitTask
        let nextPos = if maxPos.HasValue then maxPos.Value + 1 else 0

        match itemRef with
        | LibraryEntryRef (EntryId entryId) ->
            let! _ = conn.ExecuteAsync(
                "INSERT INTO collection_items (collection_id, item_type, entry_id, position, notes) VALUES (@CollectionId, 'entry', @EntryId, @Position, @Notes)",
                {| CollectionId = collectionId; EntryId = entryId; Position = nextPos; Notes = notes |> Option.toObj |}) |> Async.AwaitTask
            ()
        | SeasonRef (SeriesId seriesId, seasonNumber) ->
            let! _ = conn.ExecuteAsync(
                "INSERT INTO collection_items (collection_id, item_type, series_id, season_number, position, notes) VALUES (@CollectionId, 'season', @SeriesId, @SeasonNumber, @Position, @Notes)",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; Position = nextPos; Notes = notes |> Option.toObj |}) |> Async.AwaitTask
            ()
        | EpisodeRef (SeriesId seriesId, seasonNumber, episodeNumber) ->
            let! _ = conn.ExecuteAsync(
                "INSERT INTO collection_items (collection_id, item_type, series_id, season_number, episode_number, position, notes) VALUES (@CollectionId, 'episode', @SeriesId, @SeasonNumber, @EpisodeNumber, @Position, @Notes)",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber; Position = nextPos; Notes = notes |> Option.toObj |}) |> Async.AwaitTask
            ()
}

let removeItemFromCollection (CollectionId collectionId) (itemRef: CollectionItemRef) : Async<unit> = async {
    use conn = getConnection()

    match itemRef with
    | LibraryEntryRef (EntryId entryId) ->
        let! _ = conn.ExecuteAsync(
            "DELETE FROM collection_items WHERE collection_id = @CollectionId AND item_type = 'entry' AND entry_id = @EntryId",
            {| CollectionId = collectionId; EntryId = entryId |}) |> Async.AwaitTask
        ()
    | SeasonRef (SeriesId seriesId, seasonNumber) ->
        let! _ = conn.ExecuteAsync(
            "DELETE FROM collection_items WHERE collection_id = @CollectionId AND item_type = 'season' AND series_id = @SeriesId AND season_number = @SeasonNumber",
            {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber |}) |> Async.AwaitTask
        ()
    | EpisodeRef (SeriesId seriesId, seasonNumber, episodeNumber) ->
        let! _ = conn.ExecuteAsync(
            "DELETE FROM collection_items WHERE collection_id = @CollectionId AND item_type = 'episode' AND series_id = @SeriesId AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber",
            {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber |}) |> Async.AwaitTask
        ()

    // Reorder remaining items to fill gaps
    let! _ = conn.ExecuteAsync(
        "UPDATE collection_items SET position = (SELECT COUNT(*) - 1 FROM collection_items AS ci2 WHERE ci2.collection_id = collection_items.collection_id AND ci2.position <= collection_items.position) WHERE collection_id = @CollectionId",
        {| CollectionId = collectionId |}) |> Async.AwaitTask
    ()
}

let reorderCollectionItems (CollectionId collectionId) (itemRefs: CollectionItemRef list) : Async<unit> = async {
    use conn = getConnection()
    // Update positions based on the order of item refs provided
    for (position, itemRef) in List.indexed itemRefs do
        match itemRef with
        | LibraryEntryRef (EntryId entryId) ->
            let! _ = conn.ExecuteAsync(
                "UPDATE collection_items SET position = @Position WHERE collection_id = @CollectionId AND item_type = 'entry' AND entry_id = @EntryId",
                {| CollectionId = collectionId; EntryId = entryId; Position = position |}) |> Async.AwaitTask
            ()
        | SeasonRef (SeriesId seriesId, seasonNumber) ->
            let! _ = conn.ExecuteAsync(
                "UPDATE collection_items SET position = @Position WHERE collection_id = @CollectionId AND item_type = 'season' AND series_id = @SeriesId AND season_number = @SeasonNumber",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; Position = position |}) |> Async.AwaitTask
            ()
        | EpisodeRef (SeriesId seriesId, seasonNumber, episodeNumber) ->
            let! _ = conn.ExecuteAsync(
                "UPDATE collection_items SET position = @Position WHERE collection_id = @CollectionId AND item_type = 'episode' AND series_id = @SeriesId AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber",
                {| CollectionId = collectionId; SeriesId = seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber; Position = position |}) |> Async.AwaitTask
            ()
}

// =====================================
// Contributors CRUD
// =====================================

let private recordToContributor (r: ContributorRecord) : Contributor = {
    Id = ContributorId r.id
    TmdbPersonId = TmdbPersonId r.tmdb_person_id
    Name = r.name
    ProfilePath = if String.IsNullOrEmpty(r.profile_path) then None else Some r.profile_path
    KnownForDepartment = if String.IsNullOrEmpty(r.known_for_department) then None else Some r.known_for_department
    Birthday = parseDateTime r.birthday
    Deathday = parseDateTime r.deathday
    PlaceOfBirth = if String.IsNullOrEmpty(r.place_of_birth) then None else Some r.place_of_birth
    Biography = if String.IsNullOrEmpty(r.biography) then None else Some r.biography
    CreatedAt = DateTime.Parse(r.created_at)
    UpdatedAt = DateTime.Parse(r.updated_at)
}

let getAllContributors () : Async<Contributor list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<ContributorRecord>("SELECT * FROM contributors ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToContributor |> Seq.toList
}

let getContributorById (ContributorId id) : Async<Contributor option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<ContributorRecord>(
            "SELECT * FROM contributors WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToContributor record)
}

let getContributorByTmdbId (TmdbPersonId tmdbId) : Async<Contributor option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<ContributorRecord>(
            "SELECT * FROM contributors WHERE tmdb_person_id = @TmdbId",
            {| TmdbId = tmdbId |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToContributor record)
}

let insertContributor (contributor: Contributor) : Async<int> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO contributors (tmdb_person_id, name, profile_path, known_for_department,
                birthday, deathday, place_of_birth, biography, created_at, updated_at)
            VALUES (@TmdbPersonId, @Name, @ProfilePath, @KnownForDepartment,
                @Birthday, @Deathday, @PlaceOfBirth, @Biography, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            TmdbPersonId = TmdbPersonId.value contributor.TmdbPersonId
            Name = contributor.Name
            ProfilePath = contributor.ProfilePath |> Option.toObj
            KnownForDepartment = contributor.KnownForDepartment |> Option.toObj
            Birthday = formatDateTime contributor.Birthday
            Deathday = formatDateTime contributor.Deathday
            PlaceOfBirth = contributor.PlaceOfBirth |> Option.toObj
            Biography = contributor.Biography |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return int id
}

let deleteContributor (ContributorId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM contributors WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Entry Friends Junction
// =====================================

let getFriendsForEntry (EntryId entryId) : Async<FriendId list> = async {
    use conn = getConnection()
    let! ids =
        conn.QueryAsync<int>(
            "SELECT friend_id FROM entry_friends WHERE entry_id = @EntryId",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask
    return ids |> Seq.map FriendId |> Seq.toList
}

/// Get all friends who watched an entry via watch sessions (movies + series)
/// This is the primary way friends are associated in Cinemarco
let getFriendsFromWatchSessions (EntryId entryId) : Async<FriendId list> = async {
    use conn = getConnection()
    let! ids =
        conn.QueryAsync<int>(
            """
            SELECT DISTINCT friend_id FROM (
                -- Movie watch session friend associations
                SELECT msf.friend_id
                FROM movie_watch_sessions mws
                INNER JOIN movie_session_friends msf ON mws.id = msf.session_id
                WHERE mws.entry_id = @EntryId
                UNION
                -- Series watch session friend associations
                SELECT sf.friend_id
                FROM watch_sessions ws
                INNER JOIN session_friends sf ON ws.id = sf.session_id
                WHERE ws.entry_id = @EntryId
            )
            """,
            {| EntryId = entryId |}
        ) |> Async.AwaitTask
    return ids |> Seq.map FriendId |> Seq.toList
}

/// Get all friends for an entry (combines entry_friends + watch session friends)
let getAllFriendsForEntry (entryId: EntryId) : Async<FriendId list> = async {
    let! directFriends = getFriendsForEntry entryId
    let! sessionFriends = getFriendsFromWatchSessions entryId
    return (directFriends @ sessionFriends) |> List.distinct
}

let addFriendToEntry (EntryId entryId) (FriendId friendId) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("""
        INSERT OR IGNORE INTO entry_friends (entry_id, friend_id) VALUES (@EntryId, @FriendId)
    """, {| EntryId = entryId; FriendId = friendId |}) |> Async.AwaitTask |> Async.Ignore
}

let removeFriendFromEntry (EntryId entryId) (FriendId friendId) : Async<unit> = async {
    use conn = getConnection()
    let param = {| EntryId = entryId; FriendId = friendId |}
    let! _ = conn.ExecuteAsync("DELETE FROM entry_friends WHERE entry_id = @EntryId AND friend_id = @FriendId", param) |> Async.AwaitTask
    return ()
}

/// Get all library entries watched with a specific friend
/// For movies: uses movie_watch_sessions (the new watch session model)
/// For series: uses session_friends (series watch sessions)
let getEntriesWatchedWithFriend (FriendId friendId) : Async<int list> = async {
    use conn = getConnection()
    let! entryIds =
        conn.QueryAsync<int>(
            """
            SELECT DISTINCT entry_id FROM (
                -- Movie watch session friend associations
                SELECT mws.entry_id
                FROM movie_watch_sessions mws
                INNER JOIN movie_session_friends msf ON mws.id = msf.session_id
                WHERE msf.friend_id = @FriendId
                UNION
                -- Series watch session friend associations
                SELECT ws.entry_id
                FROM watch_sessions ws
                INNER JOIN session_friends sf ON ws.id = sf.session_id
                WHERE sf.friend_id = @FriendId
            )
            """,
            {| FriendId = friendId |}
        ) |> Async.AwaitTask
    return entryIds |> Seq.toList
}

// =====================================
// Watch Sessions CRUD
// =====================================

let private parseSessionStatus (s: string) : SessionStatus =
    match s with
    | "Active" -> Active
    | "Paused" -> Paused
    | "SessionCompleted" -> SessionCompleted
    | _ -> Active

let private formatSessionStatus (status: SessionStatus) : string =
    match status with
    | Active -> "Active"
    | Paused -> "Paused"
    | SessionCompleted -> "SessionCompleted"

let getSessionsForEntry (EntryId entryId) : Async<WatchSession list> = async {
    use conn = getConnection()
    let param = {| EntryId = entryId |}
    let! records =
        conn.QueryAsync<WatchSessionRecord>(
            "SELECT * FROM watch_sessions WHERE entry_id = @EntryId ORDER BY created_at DESC",
            param
        ) |> Async.AwaitTask

    let mapRecord (r: WatchSessionRecord) = async {
        let friendParam = {| SessionId = r.id |}
        let! friendIds =
            conn.QueryAsync<int>(
                "SELECT friend_id FROM session_friends WHERE session_id = @SessionId",
                friendParam
            ) |> Async.AwaitTask
        return {
            Id = SessionId r.id
            EntryId = EntryId r.entry_id
            Status = parseSessionStatus r.status
            StartDate = parseDateTime r.start_date
            EndDate = parseDateTime r.end_date
            Friends = friendIds |> Seq.map FriendId |> Seq.toList
            Notes = if String.IsNullOrEmpty(r.notes) then None else Some r.notes
            CreatedAt = DateTime.Parse(r.created_at)
            IsDefault = r.is_default = 1
        }
    }

    let! sessions = records |> Seq.map mapRecord |> Async.Sequential
    return sessions |> Array.toList
}

let insertWatchSession (request: CreateSessionRequest) : Async<WatchSession> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO watch_sessions (entry_id, name, status, start_date, is_default, created_at, updated_at)
            VALUES (@EntryId, '', 'Active', @StartDate, 0, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            EntryId = EntryId.value request.EntryId
            StartDate = now
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask

    let sessionId = int id

    // Add friends
    for friendId in request.Friends do
        let friendParam = {| SessionId = sessionId; FriendId = FriendId.value friendId |}
        let! _ = conn.ExecuteAsync("INSERT INTO session_friends (session_id, friend_id) VALUES (@SessionId, @FriendId)", friendParam) |> Async.AwaitTask
        ()

    return {
        Id = SessionId sessionId
        EntryId = request.EntryId
        Status = Active
        StartDate = Some DateTime.UtcNow
        EndDate = None
        Friends = request.Friends
        Notes = None
        CreatedAt = DateTime.UtcNow
        IsDefault = false
    }
}

/// Create the default "Personal" session for a series entry
let createDefaultSession (entryId: EntryId) (seriesId: SeriesId) : Async<WatchSession> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO watch_sessions (entry_id, name, status, start_date, is_default, created_at, updated_at)
            VALUES (@EntryId, '', 'Active', @StartDate, 1, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            EntryId = EntryId.value entryId
            StartDate = now
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask

    return {
        Id = SessionId (int id)
        EntryId = entryId
        Status = Active
        StartDate = Some DateTime.UtcNow
        EndDate = None
        Friends = []
        Notes = None
        CreatedAt = DateTime.UtcNow
        IsDefault = true
    }
}

/// Get the default session for an entry
let getDefaultSession (entryId: EntryId) : Async<WatchSession option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<WatchSessionRecord>(
            "SELECT * FROM watch_sessions WHERE entry_id = @EntryId AND is_default = 1",
            {| EntryId = EntryId.value entryId |}
        ) |> Async.AwaitTask

    if isNull (box record) then return None
    else
        let! friendIds =
            conn.QueryAsync<int>(
                "SELECT friend_id FROM session_friends WHERE session_id = @SessionId",
                {| SessionId = record.id |}
            ) |> Async.AwaitTask
        return Some {
            Id = SessionId record.id
            EntryId = EntryId record.entry_id
            Status = parseSessionStatus record.status
            StartDate = parseDateTime record.start_date
            EndDate = parseDateTime record.end_date
            Friends = friendIds |> Seq.map FriendId |> Seq.toList
            Notes = if String.IsNullOrEmpty(record.notes) then None else Some record.notes
            CreatedAt = DateTime.Parse(record.created_at)
            IsDefault = record.is_default = 1
        }
}

let deleteWatchSession (SessionId id) : Async<unit> = async {
    use conn = getConnection()
    // Delete associated episode progress first
    do! conn.ExecuteAsync("DELETE FROM episode_progress WHERE session_id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
    // Delete session friends
    do! conn.ExecuteAsync("DELETE FROM session_friends WHERE session_id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
    // Delete the session
    do! conn.ExecuteAsync("DELETE FROM watch_sessions WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

/// Get a session by ID
let getSessionById (SessionId id) : Async<WatchSession option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<WatchSessionRecord>(
            "SELECT * FROM watch_sessions WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask

    if isNull (box record) then
        return None
    else
        let! friendIds =
            conn.QueryAsync<int>(
                "SELECT friend_id FROM session_friends WHERE session_id = @SessionId",
                {| SessionId = record.id |}
            ) |> Async.AwaitTask
        return Some {
            Id = SessionId record.id
            EntryId = EntryId record.entry_id
            Status = parseSessionStatus record.status
            StartDate = parseDateTime record.start_date
            EndDate = parseDateTime record.end_date
            Friends = friendIds |> Seq.map FriendId |> Seq.toList
            Notes = if String.IsNullOrEmpty(record.notes) then None else Some record.notes
            CreatedAt = DateTime.Parse(record.created_at)
            IsDefault = record.is_default = 1
        }
}

/// Update a watch session
let updateWatchSession (request: UpdateSessionRequest) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")

    // Build the update dynamically based on what fields are provided
    let! existing = getSessionById request.Id
    match existing with
    | None -> ()
    | Some session ->
        let notes = Option.toObj (Option.orElse session.Notes request.Notes)
        let status = request.Status |> Option.defaultValue session.Status

        // If marking as completed, set end date
        let endDate =
            match status with
            | SessionCompleted when session.EndDate.IsNone -> now
            | _ -> formatDateTime session.EndDate

        let param = {|
            Id = SessionId.value request.Id
            Notes = notes
            Status = formatSessionStatus status
            EndDate = endDate
            UpdatedAt = now
        |}

        do! conn.ExecuteAsync("""
            UPDATE watch_sessions SET
                notes = @Notes,
                status = @Status,
                end_date = @EndDate,
                updated_at = @UpdatedAt
            WHERE id = @Id
        """, param) |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Session Friends Junction
// =====================================

let getFriendsForSession (SessionId sessionId) : Async<FriendId list> = async {
    use conn = getConnection()
    let! ids =
        conn.QueryAsync<int>(
            "SELECT friend_id FROM session_friends WHERE session_id = @SessionId",
            {| SessionId = sessionId |}
        ) |> Async.AwaitTask
    return ids |> Seq.map FriendId |> Seq.toList
}

let addFriendToSession (SessionId sessionId) (FriendId friendId) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("""
        INSERT OR IGNORE INTO session_friends (session_id, friend_id) VALUES (@SessionId, @FriendId)
    """, {| SessionId = sessionId; FriendId = friendId |}) |> Async.AwaitTask |> Async.Ignore
}

let removeFriendFromSession (SessionId sessionId) (FriendId friendId) : Async<unit> = async {
    use conn = getConnection()
    let param = {| SessionId = sessionId; FriendId = friendId |}
    let! _ = conn.ExecuteAsync("DELETE FROM session_friends WHERE session_id = @SessionId AND friend_id = @FriendId", param) |> Async.AwaitTask
    return ()
}

// =====================================
// Movie Watch Sessions
// =====================================

/// Get friends for a movie watch session
let getMovieSessionFriends (SessionId sessionId) : Async<FriendId list> = async {
    use conn = getConnection()
    let! ids =
        conn.QueryAsync<int>(
            "SELECT friend_id FROM movie_session_friends WHERE session_id = @SessionId",
            {| SessionId = sessionId |}
        ) |> Async.AwaitTask
    return ids |> Seq.map FriendId |> Seq.toList
}

/// Get all movie watch sessions for an entry
let getMovieWatchSessionsForEntry (EntryId entryId) : Async<MovieWatchSession list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<MovieWatchSessionRecord>(
            "SELECT * FROM movie_watch_sessions WHERE entry_id = @EntryId ORDER BY watched_date DESC",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask

    let mapRecord (r: MovieWatchSessionRecord) = async {
        let! friendIds = getMovieSessionFriends (SessionId r.id)
        return {
            Id = SessionId r.id
            EntryId = EntryId r.entry_id
            WatchedDate = DateTime.Parse(r.watched_date)
            Friends = friendIds
            Name = if String.IsNullOrEmpty(r.name) then None else Some r.name
            CreatedAt = DateTime.Parse(r.created_at)
        }
    }

    let! sessions = records |> Seq.map mapRecord |> Async.Sequential
    return sessions |> Array.toList
}


/// Get a single movie watch session by ID
let getMovieWatchSessionById (SessionId sessionId) : Async<MovieWatchSession option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<MovieWatchSessionRecord>(
            "SELECT * FROM movie_watch_sessions WHERE id = @Id",
            {| Id = sessionId |}
        ) |> Async.AwaitTask

    if isNull (box record) then
        return None
    else
        let! friendIds = getMovieSessionFriends (SessionId record.id)
        return Some {
            Id = SessionId record.id
            EntryId = EntryId record.entry_id
            WatchedDate = DateTime.Parse(record.watched_date)
            Friends = friendIds
            Name = if String.IsNullOrEmpty(record.name) then None else Some record.name
            CreatedAt = DateTime.Parse(record.created_at)
        }
}

/// Get all movie watch sessions (for stats calculation)
let getAllMovieWatchSessions () : Async<MovieWatchSession list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<MovieWatchSessionRecord>(
            "SELECT * FROM movie_watch_sessions ORDER BY watched_date DESC"
        ) |> Async.AwaitTask

    let mapRecord (r: MovieWatchSessionRecord) = async {
        let! friendIds = getMovieSessionFriends (SessionId r.id)
        return {
            Id = SessionId r.id
            EntryId = EntryId r.entry_id
            WatchedDate = DateTime.Parse(r.watched_date)
            Friends = friendIds
            Name = if String.IsNullOrEmpty(r.name) then None else Some r.name
            CreatedAt = DateTime.Parse(r.created_at)
        }
    }

    let! sessions = records |> Seq.map mapRecord |> Async.Sequential
    return sessions |> Array.toList
}

/// Add a friend to a movie watch session
let addFriendToMovieSession (SessionId sessionId) (FriendId friendId) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("""
        INSERT OR IGNORE INTO movie_session_friends (session_id, friend_id) VALUES (@SessionId, @FriendId)
    """, {| SessionId = sessionId; FriendId = friendId |}) |> Async.AwaitTask |> Async.Ignore
}

/// Check if a movie watch session exists for a specific date (within same day)
let movieWatchSessionExistsForDate (entryId: EntryId) (watchedDate: DateTime) : Async<bool> = async {
    use conn = getConnection()
    let dateStr = watchedDate.Date.ToString("yyyy-MM-dd")
    let! count =
        conn.ExecuteScalarAsync<int>(
            """SELECT COUNT(*) FROM movie_watch_sessions
               WHERE entry_id = @EntryId AND date(watched_date) = date(@WatchedDate)""",
            {| EntryId = EntryId.value entryId; WatchedDate = dateStr |}
        ) |> Async.AwaitTask
    return count > 0
}

/// Get a movie watch session ID for a specific date (within same day), if it exists
let getMovieWatchSessionIdForDate (entryId: EntryId) (watchedDate: DateTime) : Async<SessionId option> = async {
    use conn = getConnection()
    let dateStr = watchedDate.Date.ToString("yyyy-MM-dd")
    let! sessionId =
        conn.QueryFirstOrDefaultAsync<Nullable<int>>(
            """SELECT id FROM movie_watch_sessions
               WHERE entry_id = @EntryId AND date(watched_date) = date(@WatchedDate)
               LIMIT 1""",
            {| EntryId = EntryId.value entryId; WatchedDate = dateStr |}
        ) |> Async.AwaitTask
    return if sessionId.HasValue then Some (SessionId sessionId.Value) else None
}

/// Create a movie watch session
let insertMovieWatchSession (request: CreateMovieWatchSessionRequest) : Async<MovieWatchSession> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let watchedDate = request.WatchedDate.ToString("o")

    let! sessionId =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO movie_watch_sessions (entry_id, watched_date, name, created_at)
            VALUES (@EntryId, @WatchedDate, @Name, @CreatedAt);
            SELECT last_insert_rowid();
        """, {|
            EntryId = EntryId.value request.EntryId
            WatchedDate = watchedDate
            Name = request.Name |> Option.toObj
            CreatedAt = now
        |}) |> Async.AwaitTask

    let sessionIdInt = int sessionId

    // Add friends to session
    for friendId in request.Friends do
        do! addFriendToMovieSession (SessionId sessionIdInt) friendId

    return {
        Id = SessionId sessionIdInt
        EntryId = request.EntryId
        WatchedDate = request.WatchedDate
        Friends = request.Friends
        Name = request.Name
        CreatedAt = DateTime.UtcNow
    }
}


/// Update a movie's DateLastWatched based on the latest watch session
/// Should be called after creating, updating, or deleting movie watch sessions
let updateMovieDateLastWatched (entryId: EntryId) : Async<unit> = async {
    use conn = getConnection()
    let entryIdVal = EntryId.value entryId
    
    // Get the max watched_date from all movie watch sessions for this entry
    let! maxWatchedDate =
        conn.ExecuteScalarAsync<string>(
            """SELECT MAX(watched_date) FROM movie_watch_sessions WHERE entry_id = @EntryId""",
            {| EntryId = entryIdVal |}
        ) |> Async.AwaitTask
    
    // Update the library entry's date_last_watched and watch_status
    match parseDateTime maxWatchedDate with
    | Some date ->
        // Has watch sessions - mark as Completed
        let dateStr = date.ToString("o")
        let! _ =
            conn.ExecuteAsync(
                """UPDATE library_entries 
                   SET date_last_watched = @DateLastWatched, 
                       watch_status = 'Completed',
                       updated_at = @UpdatedAt
                   WHERE id = @Id""",
                {| Id = entryIdVal; DateLastWatched = dateStr; UpdatedAt = DateTime.UtcNow.ToString("o") |}
            ) |> Async.AwaitTask
        ()
    | None ->
        // No watch sessions - clear date_last_watched and set status to NotStarted
        let! _ =
            conn.ExecuteAsync(
                """UPDATE library_entries 
                   SET date_last_watched = NULL, watch_status = 'NotStarted', updated_at = @UpdatedAt
                   WHERE id = @Id""",
                {| Id = entryIdVal; UpdatedAt = DateTime.UtcNow.ToString("o") |}
            ) |> Async.AwaitTask
        ()
}

/// Delete a movie watch session
let deleteMovieWatchSession (SessionId sessionId) : Async<unit> = async {
    use conn = getConnection()
    // Friends are deleted by cascade
    do! conn.ExecuteAsync("DELETE FROM movie_watch_sessions WHERE id = @Id", {| Id = sessionId |})
        |> Async.AwaitTask |> Async.Ignore
}

/// Update the date of a movie watch session
let updateMovieWatchSessionDate (request: UpdateMovieWatchSessionDateRequest) : Async<MovieWatchSession option> = async {
    use conn = getConnection()
    let sessionId = SessionId.value request.SessionId
    let newDateStr = request.NewDate.ToString("o")

    // Update the date
    let! _ =
        conn.ExecuteAsync(
            "UPDATE movie_watch_sessions SET watched_date = @WatchedDate WHERE id = @Id",
            {| Id = sessionId; WatchedDate = newDateStr |}
        ) |> Async.AwaitTask

    // Fetch the updated session
    let! record =
        conn.QuerySingleOrDefaultAsync<MovieWatchSessionRecord>(
            "SELECT * FROM movie_watch_sessions WHERE id = @Id",
            {| Id = sessionId |}
        ) |> Async.AwaitTask

    if isNull (box record) then
        return None
    else
        // Get friends for this session
        let! friendIds = getMovieSessionFriends (SessionId record.id)

        return Some {
            Id = SessionId record.id
            EntryId = EntryId record.entry_id
            WatchedDate = DateTime.Parse(record.watched_date)
            Friends = friendIds
            Name = if String.IsNullOrWhiteSpace record.name then None else Some record.name
            CreatedAt = DateTime.Parse(record.created_at)
        }
}

/// Update a movie watch session (date, friends, name)
let updateMovieWatchSession (request: UpdateMovieWatchSessionRequest) : Async<MovieWatchSession option> = async {
    use conn = getConnection()
    let sessionId = SessionId.value request.SessionId
    let watchedDateStr = request.WatchedDate.ToString("o")

    // First check the session exists and get entry_id
    let! existingRecord =
        conn.QuerySingleOrDefaultAsync<MovieWatchSessionRecord>(
            "SELECT * FROM movie_watch_sessions WHERE id = @Id",
            {| Id = sessionId |}
        ) |> Async.AwaitTask

    if isNull (box existingRecord) then
        return None
    else
        // Update the session fields
        let! _ =
            conn.ExecuteAsync(
                "UPDATE movie_watch_sessions SET watched_date = @WatchedDate, name = @Name WHERE id = @Id",
                {| Id = sessionId; WatchedDate = watchedDateStr; Name = request.Name |> Option.toObj |}
            ) |> Async.AwaitTask

        // Replace friends: delete existing, add new ones
        let! _ =
            conn.ExecuteAsync(
                "DELETE FROM movie_session_friends WHERE session_id = @SessionId",
                {| SessionId = sessionId |}
            ) |> Async.AwaitTask

        // Add new friends
        for friendId in request.Friends do
            do! addFriendToMovieSession request.SessionId friendId

        return Some {
            Id = request.SessionId
            EntryId = EntryId existingRecord.entry_id
            WatchedDate = request.WatchedDate
            Friends = request.Friends
            Name = request.Name
            CreatedAt = DateTime.Parse(existingRecord.created_at)
        }
}

// =====================================
// Session Episode Progress
// =====================================

/// Get episode progress for a specific session
let getSessionEpisodeProgress (SessionId sessionId) : Async<EpisodeProgress list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<EpisodeProgressRecord>(
            """SELECT * FROM episode_progress WHERE session_id = @SessionId
               ORDER BY season_number, episode_number""",
            {| SessionId = sessionId |}
        ) |> Async.AwaitTask

    return records |> Seq.map (fun r -> {
        EntryId = EntryId r.entry_id
        SessionId = SessionId r.session_id.Value
        SeriesId = SeriesId r.series_id
        SeasonNumber = r.season_number
        EpisodeNumber = r.episode_number
        IsWatched = r.is_watched = 1
        WatchedDate = parseDateTime r.watched_date
    }) |> Seq.toList
}

/// Record type for unique watched episode
[<CLIMutable>]
type private UniqueWatchedEpisodeRecord = {
    season_number: int
    episode_number: int
}

/// Get unique watched episodes across ALL sessions for an entry
/// Returns (SeasonNumber, EpisodeNumber) tuples without duplicates
let getOverallEpisodeProgress (EntryId entryId) : Async<(int * int) list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<UniqueWatchedEpisodeRecord>(
            """SELECT DISTINCT season_number, episode_number
               FROM episode_progress
               WHERE entry_id = @EntryId AND is_watched = 1
               ORDER BY season_number, episode_number""",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask

    return records |> Seq.map (fun r -> (r.season_number, r.episode_number)) |> Seq.toList
}

/// Count unique watched episodes across ALL sessions for an entry
let countOverallWatchedEpisodes (EntryId entryId) : Async<int> = async {
    use conn = getConnection()
    let! count =
        conn.ExecuteScalarAsync<int>(
            """SELECT COUNT(DISTINCT season_number || '-' || episode_number)
               FROM episode_progress
               WHERE entry_id = @EntryId AND is_watched = 1""",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask
    return count
}


/// Get the latest watched date across ALL sessions for an entry
let getOverallLastWatchedDate (EntryId entryId) : Async<DateTime option> = async {
    use conn = getConnection()
    let! result =
        conn.ExecuteScalarAsync<string>(
            """SELECT MAX(watched_date)
               FROM episode_progress
               WHERE entry_id = @EntryId AND is_watched = 1 AND watched_date IS NOT NULL""",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask
    return parseDateTime result
}

/// Record type for episode watch data used in stats
[<CLIMutable>]
type private EpisodeWatchDataRecord = {
    entry_id: int
    series_id: int
    watched_date: string
    episode_runtime: Nullable<int>
}

/// Get all watched episodes with their dates (for stats calculation)
/// Returns tuples of (EntryId, episodeRuntime, watchedDate)
let getAllWatchedEpisodeData () : Async<(EntryId * int * DateTime) list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<EpisodeWatchDataRecord>(
            """SELECT ep.entry_id, ep.series_id, ep.watched_date, s.episode_runtime_minutes as episode_runtime
               FROM episode_progress ep
               INNER JOIN series s ON s.id = ep.series_id
               WHERE ep.is_watched = 1 AND ep.watched_date IS NOT NULL AND ep.watched_date != ''"""
        ) |> Async.AwaitTask

    return records
        |> Seq.choose (fun r ->
            match parseDateTime r.watched_date with
            | Some dt ->
                let runtime = if r.episode_runtime.HasValue then r.episode_runtime.Value else 45
                Some (EntryId r.entry_id, runtime, dt)
            | None -> None)
        |> Seq.toList
}

/// Update episode progress for a session (insert or update)
let updateSessionEpisodeProgress (sessionId: SessionId) (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeNumber: int) (watched: bool) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")

    let sessionIdVal = SessionId.value sessionId
    let entryIdVal = EntryId.value entryId

    // Check if record exists for this session
    let! existingId =
        conn.ExecuteScalarAsync<Nullable<int>>(
            """SELECT id FROM episode_progress
               WHERE entry_id = @EntryId AND session_id = @SessionId
               AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber""",
            {| EntryId = entryIdVal; SessionId = sessionIdVal; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber |}
        ) |> Async.AwaitTask

    match existingId.HasValue with
    | true ->
        // Update existing record - only set watched_date if marking as watched AND no date exists
        // If unmarking, clear the date
        let! _ =
            if watched then
                conn.ExecuteAsync(
                    """UPDATE episode_progress
                       SET is_watched = 1, watched_date = COALESCE(watched_date, @WatchedDate)
                       WHERE id = @Id""",
                    {| Id = existingId.Value; WatchedDate = now |}
                ) |> Async.AwaitTask
            else
                conn.ExecuteAsync(
                    """UPDATE episode_progress SET is_watched = 0, watched_date = NULL
                       WHERE id = @Id""",
                    {| Id = existingId.Value |}
                ) |> Async.AwaitTask
        return ()
    | false ->
        // Insert new record
        let! _ =
            conn.ExecuteAsync(
                """INSERT INTO episode_progress (entry_id, session_id, series_id, season_number, episode_number, is_watched, watched_date)
                   VALUES (@EntryId, @SessionId, @SeriesId, @SeasonNumber, @EpisodeNumber, @IsWatched, @WatchedDate)""",
                {| EntryId = entryIdVal; SessionId = sessionIdVal; SeriesId = SeriesId.value seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber; IsWatched = (if watched then 1 else 0); WatchedDate = (if watched then now else null) |}
            ) |> Async.AwaitTask
        return ()
}

/// Mark all episodes in a season as watched for a session
let markSessionSeasonWatched (sessionId: SessionId) (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeCount: int) : Async<unit> = async {
    for ep in 1 .. episodeCount do
        do! updateSessionEpisodeProgress sessionId entryId seriesId seasonNumber ep true
}

/// Insert or update episode progress with a specific watched date (for imports)
/// If episode already exists, updates to the earlier date (preserves first watch)
let insertEpisodeProgressWithDate (sessionId: SessionId) (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeNumber: int) (watchedDate: DateTime option) : Async<unit> = async {
    // Skip if no watch date provided
    match watchedDate with
    | None -> return ()
    | Some date ->
        use conn = getConnection()
        let sessionIdVal = SessionId.value sessionId
        let entryIdVal = EntryId.value entryId
        let dateStr = date.ToString("o")

        // Check if record already exists
        let! existingId =
            conn.ExecuteScalarAsync<Nullable<int>>(
                """SELECT id FROM episode_progress
                   WHERE entry_id = @EntryId AND session_id = @SessionId
                   AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber""",
                {| EntryId = entryIdVal; SessionId = sessionIdVal; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber |}
            ) |> Async.AwaitTask

        if existingId.HasValue then
            // Update existing record - only if new date is earlier than existing
            let! _ =
                conn.ExecuteAsync(
                    """UPDATE episode_progress
                       SET watched_date = @WatchedDate, is_watched = 1
                       WHERE id = @Id AND (watched_date IS NULL OR watched_date > @WatchedDate)""",
                    {| Id = existingId.Value; WatchedDate = dateStr |}
                ) |> Async.AwaitTask
            return ()
        else
            // Insert new record with the specific date
            let! _ =
                conn.ExecuteAsync(
                    """INSERT INTO episode_progress (entry_id, session_id, series_id, season_number, episode_number, is_watched, watched_date)
                       VALUES (@EntryId, @SessionId, @SeriesId, @SeasonNumber, @EpisodeNumber, 1, @WatchedDate)""",
                    {| EntryId = entryIdVal; SessionId = sessionIdVal; SeriesId = SeriesId.value seriesId; SeasonNumber = seasonNumber; EpisodeNumber = episodeNumber; WatchedDate = dateStr |}
                ) |> Async.AwaitTask
            return ()
}

/// Count watched episodes for a session
let countSessionWatchedEpisodes (SessionId sessionId) : Async<int> = async {
    use conn = getConnection()
    let! count =
        conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM episode_progress WHERE session_id = @SessionId AND is_watched = 1",
            {| SessionId = sessionId |}
        ) |> Async.AwaitTask
    return count
}

/// Get episode air dates for a series (used for Trakt import to substitute air dates for binge-watched episodes)
let getEpisodeAirDates (seriesId: SeriesId) : Async<Map<(int * int), DateTime>> = async {
    use conn = getConnection()
    let seriesIdVal = SeriesId.value seriesId
    printfn "[Persistence] getEpisodeAirDates: querying for seriesId=%d" seriesIdVal

    let! records =
        conn.QueryAsync<EpisodeAirDateRecord>(
            """SELECT season_number, episode_number, air_date FROM episodes
               WHERE series_id = @SeriesId AND air_date IS NOT NULL AND air_date != ''""",
            {| SeriesId = seriesIdVal |}
        ) |> Async.AwaitTask

    let recordList = records |> Seq.toList
    printfn "[Persistence] getEpisodeAirDates: found %d records from DB" recordList.Length

    let result =
        recordList
        |> List.choose (fun r ->
            let parsed = parseDateTime r.air_date
            if parsed.IsNone then
                printfn "[Persistence] Failed to parse air_date: '%s' for S%dE%d" r.air_date r.season_number r.episode_number
            parsed |> Option.map (fun d -> ((int r.season_number, int r.episode_number), d)))
        |> Map.ofList

    printfn "[Persistence] getEpisodeAirDates: returning %d parsed dates" result.Count
    return result
}

// =====================================
// TMDB Cache
// =====================================

let getCachedTmdbResponse (cacheKey: string) : Async<string option> = async {
    use conn = getConnection()
    let! value =
        conn.QueryFirstOrDefaultAsync<string>("""
            SELECT cache_value FROM tmdb_cache
            WHERE cache_key = @CacheKey AND datetime(expires_at) > datetime('now')
        """, {| CacheKey = cacheKey |}) |> Async.AwaitTask
    return if isNull value then None else Some value
}

let setCachedTmdbResponse (cacheKey: string) (value: string) (expiresInHours: int) : Async<unit> = async {
    use conn = getConnection()
    let expiresAt = DateTime.UtcNow.AddHours(float expiresInHours).ToString("o")
    let param = {|
        CacheKey = cacheKey
        CacheValue = value
        ExpiresAt = expiresAt
    |}
    do! conn.ExecuteAsync("""
        INSERT OR REPLACE INTO tmdb_cache (cache_key, cache_value, expires_at)
        VALUES (@CacheKey, @CacheValue, @ExpiresAt)
    """, param) |> Async.AwaitTask |> Async.Ignore
}

let clearExpiredCache () : Async<int> = async {
    use conn = getConnection()
    let! deleted =
        conn.ExecuteAsync("DELETE FROM tmdb_cache WHERE datetime(expires_at) <= datetime('now')")
        |> Async.AwaitTask
    return deleted
}

/// Get all cache entries with their metadata
let getAllCacheEntries () : Async<CacheEntry list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<CacheEntryRecord>(
            "SELECT cache_key, expires_at, cache_value FROM tmdb_cache ORDER BY expires_at ASC"
        ) |> Async.AwaitTask
    return records
        |> Seq.map (fun r ->
            {
                CacheKey = r.cache_key
                ExpiresAt = DateTime.Parse(r.expires_at)
                SizeBytes = if isNull r.cache_value then 0 else System.Text.Encoding.UTF8.GetByteCount(r.cache_value)
            })
        |> Seq.toList
}

/// Get cache statistics
let getCacheStats () : Async<CacheStats> = async {
    use conn = getConnection()

    let! totalEntries =
        conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tmdb_cache")
        |> Async.AwaitTask

    let! expiredEntries =
        conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tmdb_cache WHERE datetime(expires_at) <= datetime('now')")
        |> Async.AwaitTask

    let! totalSize =
        conn.ExecuteScalarAsync<int64>("SELECT COALESCE(SUM(LENGTH(cache_value)), 0) FROM tmdb_cache")
        |> Async.AwaitTask

    // Get counts by cache key type (first part of key before ':')
    let! typeRecords =
        conn.QueryAsync<CacheTypeCountRecord>("""
            SELECT
                CASE
                    WHEN INSTR(cache_key, ':') > 0 THEN SUBSTR(cache_key, 1, INSTR(cache_key, ':') - 1)
                    ELSE cache_key
                END as type_prefix,
                COUNT(*) as cnt
            FROM tmdb_cache
            GROUP BY type_prefix
        """) |> Async.AwaitTask

    let entriesByType =
        typeRecords
        |> Seq.map (fun r -> r.type_prefix, int r.cnt)
        |> Map.ofSeq

    return {
        TotalEntries = totalEntries
        TotalSizeBytes = int totalSize
        ExpiredEntries = expiredEntries
        EntriesByType = entriesByType
    }
}

/// Clear all cache entries and return stats
let clearAllCache () : Async<ClearCacheResult> = async {
    use conn = getConnection()

    // Get stats before clearing
    let! entriesCount =
        conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tmdb_cache")
        |> Async.AwaitTask

    let! totalSize =
        conn.ExecuteScalarAsync<int64>("SELECT COALESCE(SUM(LENGTH(cache_value)), 0) FROM tmdb_cache")
        |> Async.AwaitTask

    // Clear all entries
    do! conn.ExecuteAsync("DELETE FROM tmdb_cache")
        |> Async.AwaitTask |> Async.Ignore

    return {
        EntriesRemoved = entriesCount
        BytesFreed = int totalSize
    }
}

/// Get all referenced image paths from the database
/// Only returns paths for media that is actually in the library or tracked
let getAllReferencedImagePaths () : Async<ImageCache.ReferencedImages> = async {
    use conn = getConnection()

    // Get poster paths only for movies that are in the library
    let! moviePosters =
        conn.QueryAsync<string>("""
            SELECT m.poster_path FROM movies m
            INNER JOIN library_entries le ON le.movie_id = m.id
            WHERE m.poster_path IS NOT NULL AND m.poster_path != ''
        """) |> Async.AwaitTask

    // Get poster paths only for series that are in the library
    let! seriesPosters =
        conn.QueryAsync<string>("""
            SELECT s.poster_path FROM series s
            INNER JOIN library_entries le ON le.series_id = s.id
            WHERE s.poster_path IS NOT NULL AND s.poster_path != ''
        """) |> Async.AwaitTask

    // Get backdrop paths only for movies that are in the library
    let! movieBackdrops =
        conn.QueryAsync<string>("""
            SELECT m.backdrop_path FROM movies m
            INNER JOIN library_entries le ON le.movie_id = m.id
            WHERE m.backdrop_path IS NOT NULL AND m.backdrop_path != ''
        """) |> Async.AwaitTask

    // Get backdrop paths only for series that are in the library
    let! seriesBackdrops =
        conn.QueryAsync<string>("""
            SELECT s.backdrop_path FROM series s
            INNER JOIN library_entries le ON le.series_id = s.id
            WHERE s.backdrop_path IS NOT NULL AND s.backdrop_path != ''
        """) |> Async.AwaitTask

    // Get season poster paths only for series that are in the library
    let! seasonPosters =
        conn.QueryAsync<string>("""
            SELECT sea.poster_path FROM seasons sea
            INNER JOIN series s ON sea.series_id = s.id
            INNER JOIN library_entries le ON le.series_id = s.id
            WHERE sea.poster_path IS NOT NULL AND sea.poster_path != ''
        """) |> Async.AwaitTask

    // Get episode still paths only for series that are in the library
    let! episodeStills =
        conn.QueryAsync<string>("""
            SELECT e.still_path FROM episodes e
            INNER JOIN series s ON e.series_id = s.id
            INNER JOIN library_entries le ON le.series_id = s.id
            WHERE e.still_path IS NOT NULL AND e.still_path != ''
        """) |> Async.AwaitTask

    // Get profile paths only for tracked contributors
    let! trackedProfiles =
        conn.QueryAsync<string>("""
            SELECT profile_path FROM tracked_contributors
            WHERE profile_path IS NOT NULL AND profile_path != ''
        """) |> Async.AwaitTask

    let posters =
        Seq.append moviePosters seriesPosters
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    let backdrops =
        Seq.append movieBackdrops seriesBackdrops
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    let seasonPosterSet =
        seasonPosters
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    let stillsSet =
        episodeStills
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    let profilesSet =
        trackedProfiles
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    return {
        Posters = posters
        Backdrops = backdrops
        SeasonPosters = seasonPosterSet
        Stills = stillsSet
        Profiles = profilesSet
    }
}

/// Clear expired cache entries and return stats
let clearExpiredCacheWithStats () : Async<ClearCacheResult> = async {
    use conn = getConnection()

    // Get stats before clearing
    let! entriesCount =
        conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tmdb_cache WHERE datetime(expires_at) <= datetime('now')")
        |> Async.AwaitTask

    let! totalSize =
        conn.ExecuteScalarAsync<int64>("SELECT COALESCE(SUM(LENGTH(cache_value)), 0) FROM tmdb_cache WHERE datetime(expires_at) <= datetime('now')")
        |> Async.AwaitTask

    // Clear expired entries
    do! conn.ExecuteAsync("DELETE FROM tmdb_cache WHERE datetime(expires_at) <= datetime('now')")
        |> Async.AwaitTask |> Async.Ignore

    return {
        EntriesRemoved = entriesCount
        BytesFreed = int totalSize
    }
}

// =====================================
// Library Entries CRUD
// =====================================

let private parseWatchStatus (record: LibraryEntryRecord) : WatchStatus =
    match record.watch_status with
    | "NotStarted" -> NotStarted
    | "InProgress" ->
        InProgress {
            CurrentSeason = nullableToOption record.progress_current_season
            CurrentEpisode = nullableToOption record.progress_current_episode
            LastWatchedDate = parseDateTime record.progress_last_watched_date
        }
    | "Completed" -> Completed
    | "Abandoned" ->
        Abandoned {
            AbandonedAt =
                if record.abandoned_season.HasValue || record.abandoned_episode.HasValue then
                    Some {
                        CurrentSeason = nullableToOption record.abandoned_season
                        CurrentEpisode = nullableToOption record.abandoned_episode
                        LastWatchedDate = parseDateTime record.abandoned_date
                    }
                else None
            Reason = if String.IsNullOrEmpty(record.abandoned_reason) then None else Some record.abandoned_reason
            AbandonedDate = parseDateTime record.abandoned_date
        }
    | _ -> NotStarted

let private formatWatchStatus (status: WatchStatus) : string =
    match status with
    | NotStarted -> "NotStarted"
    | InProgress _ -> "InProgress"
    | Completed -> "Completed"
    | Abandoned _ -> "Abandoned"

/// Get a library entry by ID with full media data
let getLibraryEntryById (EntryId id) : Async<LibraryEntry option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<LibraryEntryRecord>(
            "SELECT * FROM library_entries WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask

    if isNull (box record) then
        return None
    else
        // Get the media (movie or series)
        let! media = async {
            match record.media_type with
            | "Movie" ->
                let! movie = getMovieById (MovieId record.movie_id.Value)
                return movie |> Option.map LibraryMovie
            | "Series" ->
                let! series = getSeriesById (SeriesId record.series_id.Value)
                return series |> Option.map LibrarySeries
            | _ -> return None
        }

        match media with
        | None -> return None
        | Some m ->
            // Get friends for this entry
            let! friendIds = getFriendsForEntry (EntryId record.id)

            let whyAdded =
                if record.why_recommended_by_friend_id.HasValue ||
                   not (String.IsNullOrEmpty(record.why_recommended_by_name)) ||
                   not (String.IsNullOrEmpty(record.why_source)) ||
                   not (String.IsNullOrEmpty(record.why_context)) then
                    Some {
                        RecommendedBy =
                            if record.why_recommended_by_friend_id.HasValue
                            then Some (FriendId record.why_recommended_by_friend_id.Value)
                            else None
                        RecommendedByName =
                            if String.IsNullOrEmpty(record.why_recommended_by_name)
                            then None
                            else Some record.why_recommended_by_name
                        Source =
                            if String.IsNullOrEmpty(record.why_source)
                            then None
                            else Some record.why_source
                        Context =
                            if String.IsNullOrEmpty(record.why_context)
                            then None
                            else Some record.why_context
                        DateRecommended = parseDateTime record.why_date_recommended
                    }
                else None

            return Some {
                Id = EntryId record.id
                Media = m
                WhyAdded = whyAdded
                WatchStatus = parseWatchStatus record
                PersonalRating = nullableToOption record.personal_rating |> Option.bind PersonalRating.fromInt
                DateAdded = DateTime.Parse(record.date_added)
                DateFirstWatched = parseDateTime record.date_first_watched
                DateLastWatched = parseDateTime record.date_last_watched
                Notes = if String.IsNullOrEmpty(record.notes) then None else Some record.notes
                IsFavorite = record.is_favorite = 1
                Friends = friendIds
            }
}

/// Get all library entries
let getAllLibraryEntries () : Async<LibraryEntry list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<LibraryEntryRecord>("SELECT * FROM library_entries ORDER BY date_added DESC")
        |> Async.AwaitTask

    let! entries =
        records
        |> Seq.map (fun r -> getLibraryEntryById (EntryId r.id))
        |> Async.Sequential

    return entries |> Array.choose id |> Array.toList
}

/// Check if a movie is already in the library
let isMovieInLibrary (TmdbMovieId tmdbId) : Async<EntryId option> = async {
    use conn = getConnection()
    // First find the movie
    let! movie = getMovieByTmdbId (TmdbMovieId tmdbId)
    match movie with
    | None -> return None
    | Some m ->
        let! entryId =
            conn.QueryFirstOrDefaultAsync<Nullable<int>>(
                "SELECT id FROM library_entries WHERE movie_id = @MovieId",
                {| MovieId = MovieId.value m.Id |}
            ) |> Async.AwaitTask
        return if entryId.HasValue then Some (EntryId entryId.Value) else None
}

/// Check if a series is already in the library
let isSeriesInLibrary (TmdbSeriesId tmdbId) : Async<EntryId option> = async {
    use conn = getConnection()
    // First find the series
    let! series = getSeriesByTmdbId (TmdbSeriesId tmdbId)
    match series with
    | None -> return None
    | Some s ->
        let! entryId =
            conn.QueryFirstOrDefaultAsync<Nullable<int>>(
                "SELECT id FROM library_entries WHERE series_id = @SeriesId",
                {| SeriesId = SeriesId.value s.Id |}
            ) |> Async.AwaitTask
        return if entryId.HasValue then Some (EntryId entryId.Value) else None
}

/// Insert a movie into the library (creates the movie and library entry)
let insertLibraryEntryForMovie (movieDetails: TmdbMovieDetails) (request: AddMovieRequest) : Async<Result<LibraryEntry, string>> = async {
    try
        use conn = getConnection()

        // Check if movie already exists in our database
        let! existingMovie = getMovieByTmdbId movieDetails.TmdbId
        let! movieId = async {
            match existingMovie with
            | Some m -> return MovieId.value m.Id
            | None ->
                // Insert the movie
                let movie : Movie = {
                    Id = MovieId 0  // Will be set by DB
                    TmdbId = movieDetails.TmdbId
                    Title = movieDetails.Title
                    OriginalTitle = movieDetails.OriginalTitle
                    Overview = movieDetails.Overview
                    ReleaseDate = movieDetails.ReleaseDate
                    RuntimeMinutes = movieDetails.RuntimeMinutes
                    PosterPath = movieDetails.PosterPath
                    BackdropPath = movieDetails.BackdropPath
                    Genres = movieDetails.Genres
                    OriginalLanguage = movieDetails.OriginalLanguage
                    VoteAverage = movieDetails.VoteAverage
                    VoteCount = movieDetails.VoteCount
                    Tagline = movieDetails.Tagline
                    ImdbId = movieDetails.ImdbId
                    CreatedAt = DateTime.UtcNow
                    UpdatedAt = DateTime.UtcNow
                }
                return! insertMovie movie
        }

        // Check if library entry already exists for this movie
        let! existingEntry =
            conn.QueryFirstOrDefaultAsync<Nullable<int>>(
                "SELECT id FROM library_entries WHERE movie_id = @MovieId",
                {| MovieId = movieId |}
            ) |> Async.AwaitTask

        if existingEntry.HasValue then
            return Error "Movie is already in your library"
        else
            // Insert the library entry
            let now = DateTime.UtcNow.ToString("o")
            let! entryId =
                conn.ExecuteScalarAsync<int64>("""
                    INSERT INTO library_entries (
                        media_type, movie_id, why_recommended_by_friend_id, why_recommended_by_name,
                        why_source, why_context, why_date_recommended, watch_status, personal_rating,
                        notes, is_favorite, date_added, created_at, updated_at
                    ) VALUES (
                        'Movie', @MovieId, @RecommendedByFriendId, @RecommendedByName,
                        @Source, @Context, @DateRecommended, 'NotStarted', NULL,
                        NULL, 0, @DateAdded, @CreatedAt, @UpdatedAt
                    );
                    SELECT last_insert_rowid();
                """, {|
                    MovieId = movieId
                    RecommendedByFriendId =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.RecommendedBy)
                        |> Option.map FriendId.value
                        |> Option.toNullable
                    RecommendedByName =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.RecommendedByName)
                        |> Option.toObj
                    Source =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.Source)
                        |> Option.toObj
                    Context =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.Context)
                        |> Option.toObj
                    DateRecommended =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.DateRecommended)
                        |> formatDateTime
                    DateAdded = now
                    CreatedAt = now
                    UpdatedAt = now
                |}) |> Async.AwaitTask

            let entryIdInt = int entryId

            // Add friends
            for friendId in request.InitialFriends do
                do! addFriendToEntry (EntryId entryIdInt) friendId

            // Fetch and return the complete entry
            let! entry = getLibraryEntryById (EntryId entryIdInt)
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Failed to retrieve created entry"
    with
    | ex -> return Error $"Failed to add movie: {ex.Message}"
}

/// Insert a series into the library (creates the series and library entry)
let insertLibraryEntryForSeries (seriesDetails: TmdbSeriesDetails) (request: AddSeriesRequest) : Async<Result<LibraryEntry, string>> = async {
    try
        use conn = getConnection()

        // Check if series already exists in our database
        let! existingSeries = getSeriesByTmdbId seriesDetails.TmdbId
        let! seriesId = async {
            match existingSeries with
            | Some s -> return SeriesId.value s.Id
            | None ->
                // Insert the series
                let series : Series = {
                    Id = SeriesId 0  // Will be set by DB
                    TmdbId = seriesDetails.TmdbId
                    Name = seriesDetails.Name
                    OriginalName = seriesDetails.OriginalName
                    Overview = seriesDetails.Overview
                    FirstAirDate = seriesDetails.FirstAirDate
                    LastAirDate = seriesDetails.LastAirDate
                    PosterPath = seriesDetails.PosterPath
                    BackdropPath = seriesDetails.BackdropPath
                    Genres = seriesDetails.Genres
                    OriginalLanguage = seriesDetails.OriginalLanguage
                    VoteAverage = seriesDetails.VoteAverage
                    VoteCount = seriesDetails.VoteCount
                    Status =
                        match seriesDetails.Status.ToLowerInvariant() with
                        | "returning series" -> Returning
                        | "ended" -> Ended
                        | "canceled" -> Canceled
                        | "in production" -> InProduction
                        | "planned" -> Planned
                        | _ -> Unknown
                    NumberOfSeasons = seriesDetails.NumberOfSeasons
                    NumberOfEpisodes = seriesDetails.NumberOfEpisodes
                    EpisodeRunTimeMinutes = seriesDetails.EpisodeRunTimeMinutes
                    CreatedAt = DateTime.UtcNow
                    UpdatedAt = DateTime.UtcNow
                }
                return! insertSeries series
        }

        // Check if library entry already exists for this series
        let! existingEntry =
            conn.QueryFirstOrDefaultAsync<Nullable<int>>(
                "SELECT id FROM library_entries WHERE series_id = @SeriesId",
                {| SeriesId = seriesId |}
            ) |> Async.AwaitTask

        if existingEntry.HasValue then
            return Error "Series is already in your library"
        else
            // Insert the library entry
            let now = DateTime.UtcNow.ToString("o")
            let! entryId =
                conn.ExecuteScalarAsync<int64>("""
                    INSERT INTO library_entries (
                        media_type, series_id, why_recommended_by_friend_id, why_recommended_by_name,
                        why_source, why_context, why_date_recommended, watch_status, personal_rating,
                        notes, is_favorite, date_added, created_at, updated_at
                    ) VALUES (
                        'Series', @SeriesId, @RecommendedByFriendId, @RecommendedByName,
                        @Source, @Context, @DateRecommended, 'NotStarted', NULL,
                        NULL, 0, @DateAdded, @CreatedAt, @UpdatedAt
                    );
                    SELECT last_insert_rowid();
                """, {|
                    SeriesId = seriesId
                    RecommendedByFriendId =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.RecommendedBy)
                        |> Option.map FriendId.value
                        |> Option.toNullable
                    RecommendedByName =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.RecommendedByName)
                        |> Option.toObj
                    Source =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.Source)
                        |> Option.toObj
                    Context =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.Context)
                        |> Option.toObj
                    DateRecommended =
                        request.WhyAdded
                        |> Option.bind (fun w -> w.DateRecommended)
                        |> formatDateTime
                    DateAdded = now
                    CreatedAt = now
                    UpdatedAt = now
                |}) |> Async.AwaitTask

            let entryIdInt = int entryId

            // Add friends
            for friendId in request.InitialFriends do
                do! addFriendToEntry (EntryId entryIdInt) friendId

            // Create default "Personal" session for the series
            let! _ = createDefaultSession (EntryId entryIdInt) (SeriesId seriesId)

            // Fetch and return the complete entry
            let! entry = getLibraryEntryById (EntryId entryIdInt)
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Failed to retrieve created entry"
    with
    | ex -> return Error $"Failed to add series: {ex.Message}"
}

/// Delete a library entry and its associated movie/series record
let deleteLibraryEntry (EntryId id) : Async<unit> = async {
    use conn = getConnection()

    // First, get the entry to know which movie/series to delete
    let! entry =
        conn.QueryFirstOrDefaultAsync<LibraryEntryRecord>(
            "SELECT * FROM library_entries WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask

    if not (isNull (box entry)) then
        // Get image paths before deleting so we can clean up cached images
        let mutable posterPath: string option = None
        let mutable backdropPath: string option = None

        if entry.movie_id.HasValue then
            let! movie =
                conn.QueryFirstOrDefaultAsync<MovieRecord>(
                    "SELECT * FROM movies WHERE id = @Id",
                    {| Id = entry.movie_id.Value |}
                ) |> Async.AwaitTask
            if not (isNull (box movie)) then
                posterPath <- if String.IsNullOrEmpty(movie.poster_path) then None else Some movie.poster_path
                backdropPath <- if String.IsNullOrEmpty(movie.backdrop_path) then None else Some movie.backdrop_path
        elif entry.series_id.HasValue then
            let! series =
                conn.QueryFirstOrDefaultAsync<SeriesRecord>(
                    "SELECT * FROM series WHERE id = @Id",
                    {| Id = entry.series_id.Value |}
                ) |> Async.AwaitTask
            if not (isNull (box series)) then
                posterPath <- if String.IsNullOrEmpty(series.poster_path) then None else Some series.poster_path
                backdropPath <- if String.IsNullOrEmpty(series.backdrop_path) then None else Some series.backdrop_path

        // Delete the library entry
        do! conn.ExecuteAsync("DELETE FROM library_entries WHERE id = @Id", {| Id = id |})
            |> Async.AwaitTask |> Async.Ignore

        // Delete the associated movie or series record
        if entry.movie_id.HasValue then
            do! conn.ExecuteAsync("DELETE FROM movies WHERE id = @Id", {| Id = entry.movie_id.Value |})
                |> Async.AwaitTask |> Async.Ignore
        elif entry.series_id.HasValue then
            // Also delete associated seasons and episodes
            do! conn.ExecuteAsync("DELETE FROM episodes WHERE series_id = @Id", {| Id = entry.series_id.Value |})
                |> Async.AwaitTask |> Async.Ignore
            do! conn.ExecuteAsync("DELETE FROM seasons WHERE series_id = @Id", {| Id = entry.series_id.Value |})
                |> Async.AwaitTask |> Async.Ignore
            do! conn.ExecuteAsync("DELETE FROM series WHERE id = @Id", {| Id = entry.series_id.Value |})
                |> Async.AwaitTask |> Async.Ignore

        // Delete cached images
        posterPath |> Option.iter (fun path ->
            let localPath = ImageCache.getLocalImagePath "posters" path
            if IO.File.Exists(localPath) then
                try IO.File.Delete(localPath)
                with _ -> ()
        )
        backdropPath |> Option.iter (fun path ->
            let localPath = ImageCache.getLocalImagePath "backdrops" path
            if IO.File.Exists(localPath) then
                try IO.File.Delete(localPath)
                with _ -> ()
        )
}

// =====================================
// Entry Update Operations
// =====================================

/// Update personal rating for a library entry
let updatePersonalRating (EntryId id) (rating: PersonalRating option) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let ratingValue =
        rating
        |> Option.map PersonalRating.toInt
        |> optionToNullable

    let param = {|
        Id = id
        PersonalRating = ratingValue
        UpdatedAt = now
    |}

    do! conn.ExecuteAsync("""
        UPDATE library_entries SET
            personal_rating = @PersonalRating,
            updated_at = @UpdatedAt
        WHERE id = @Id
    """, param) |> Async.AwaitTask |> Async.Ignore
}

/// Toggle favorite status for a library entry
let toggleFavorite (EntryId id) : Async<bool> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")

    // Get current favorite status
    let! currentRecord =
        conn.QueryFirstOrDefaultAsync<{| is_favorite: int |}>(
            "SELECT is_favorite FROM library_entries WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask

    let newValue = if currentRecord.is_favorite = 1 then 0 else 1

    do! conn.ExecuteAsync("""
        UPDATE library_entries SET
            is_favorite = @IsFavorite,
            updated_at = @UpdatedAt
        WHERE id = @Id
    """, {| Id = id; IsFavorite = newValue; UpdatedAt = now |}) |> Async.AwaitTask |> Async.Ignore

    return newValue = 1
}

/// Update notes for a library entry
let updateNotes (EntryId id) (notes: string option) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")

    let param = {|
        Id = id
        Notes = notes |> Option.toObj
        UpdatedAt = now
    |}

    do! conn.ExecuteAsync("""
        UPDATE library_entries SET
            notes = @Notes,
            updated_at = @UpdatedAt
        WHERE id = @Id
    """, param) |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Watch Status Operations
// =====================================

/// Update watch status for a library entry
let updateWatchStatus (EntryId id) (status: WatchStatus) (watchedDate: DateTime option) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")

    let (statusStr, progressSeason, progressEpisode, progressDate, abSeason, abEpisode, abReason, abDate) =
        match status with
        | NotStarted ->
            ("NotStarted", Nullable(), Nullable(), null, Nullable(), Nullable(), null, null)
        | InProgress progress ->
            ("InProgress",
             progress.CurrentSeason |> optionToNullable,
             progress.CurrentEpisode |> optionToNullable,
             formatDateTime progress.LastWatchedDate,
             Nullable(), Nullable(), null, null)
        | Completed ->
            ("Completed", Nullable(), Nullable(), null, Nullable(), Nullable(), null, null)
        | Abandoned info ->
            let (abS, abE, abD) =
                match info.AbandonedAt with
                | Some at -> (at.CurrentSeason |> optionToNullable,
                              at.CurrentEpisode |> optionToNullable,
                              formatDateTime at.LastWatchedDate)
                | None -> (Nullable(), Nullable(), null)
            ("Abandoned", Nullable(), Nullable(), null, abS, abE,
             info.Reason |> Option.toObj, formatDateTime info.AbandonedDate)

    let dateFirstWatched =
        match status with
        | Completed | InProgress _ -> formatDateTime watchedDate
        | _ -> null

    // Use the actual watchedDate from the parameter (which comes from episode progress across all sessions)
    let dateLastWatched =
        match status with
        | Completed | InProgress _ -> formatDateTime watchedDate
        | _ -> null

    let param = {|
        Id = id
        WatchStatus = statusStr
        ProgressCurrentSeason = progressSeason
        ProgressCurrentEpisode = progressEpisode
        ProgressLastWatchedDate = progressDate
        AbandonedSeason = abSeason
        AbandonedEpisode = abEpisode
        AbandonedReason = abReason
        AbandonedDate = abDate
        DateFirstWatched = dateFirstWatched
        DateLastWatched = dateLastWatched
        UpdatedAt = now
    |}

    do! conn.ExecuteAsync("""
        UPDATE library_entries SET
            watch_status = @WatchStatus,
            progress_current_season = @ProgressCurrentSeason,
            progress_current_episode = @ProgressCurrentEpisode,
            progress_last_watched_date = @ProgressLastWatchedDate,
            abandoned_season = @AbandonedSeason,
            abandoned_episode = @AbandonedEpisode,
            abandoned_reason = @AbandonedReason,
            abandoned_date = @AbandonedDate,
            date_first_watched = COALESCE(date_first_watched, @DateFirstWatched),
            date_last_watched = COALESCE(@DateLastWatched, date_last_watched),
            updated_at = @UpdatedAt
        WHERE id = @Id
    """, param) |> Async.AwaitTask |> Async.Ignore
}

/// Mark a movie as watched
let markMovieWatched (entryId: EntryId) (watchedDate: DateTime option) : Async<unit> = async {
    let date = watchedDate |> Option.defaultValue DateTime.UtcNow
    do! updateWatchStatus entryId Completed (Some date)
}

/// Mark a movie as unwatched
let markMovieUnwatched (entryId: EntryId) : Async<unit> = async {
    do! updateWatchStatus entryId NotStarted None
}

// =====================================
// Episode Progress Operations (Default Session)
// =====================================

/// Get episode progress for an entry's default session
let getEpisodeProgress (entryId: EntryId) : Async<EpisodeProgress list> = async {
    let! defaultSession = getDefaultSession entryId
    match defaultSession with
    | Some session -> return! getSessionEpisodeProgress session.Id
    | None -> return []
}

/// Update episode progress for an entry's default session
let updateEpisodeProgress (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeNumber: int) (watched: bool) : Async<unit> = async {
    let! defaultSession = getDefaultSession entryId
    match defaultSession with
    | Some session ->
        do! updateSessionEpisodeProgress session.Id entryId seriesId seasonNumber episodeNumber watched
    | None ->
        // No default session - this shouldn't happen after migration
        ()
}

/// Update the watched date for a specific episode in a specific session
let updateSessionEpisodeWatchedDate (sessionId: SessionId) (seasonNumber: int) (episodeNumber: int) (watchedDate: DateTime option) : Async<unit> = async {
    use conn = getConnection()
    let dateStr = watchedDate |> Option.map (fun d -> d.ToString("o")) |> Option.toObj
    let! _ =
        conn.ExecuteAsync(
            """UPDATE episode_progress
               SET watched_date = @WatchedDate
               WHERE session_id = @SessionId
               AND season_number = @SeasonNumber AND episode_number = @EpisodeNumber""",
            {| SessionId = SessionId.value sessionId
               SeasonNumber = seasonNumber
               EpisodeNumber = episodeNumber
               WatchedDate = dateStr |}
        ) |> Async.AwaitTask
    return ()
}

/// Mark all episodes in a season as watched (for default session)
let markSeasonWatched (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeCount: int) : Async<unit> = async {
    let! defaultSession = getDefaultSession entryId
    match defaultSession with
    | Some session ->
        do! markSessionSeasonWatched session.Id entryId seriesId seasonNumber episodeCount
    | None -> ()
}

/// Get season details with episodes from local database
/// Returns None if the season data hasn't been cached yet
let getLocalSeasonDetails (seriesId: SeriesId) (seasonNumber: int) : Async<TmdbSeasonDetails option> = async {
    use conn = getConnection()
    let seriesIdVal = SeriesId.value seriesId

    // Get the series to find the TMDB ID
    let! series = getSeriesById seriesId
    match series with
    | None -> return None
    | Some s ->
        // Check if we have episodes for this season
        let! episodeRecords =
            conn.QueryAsync<EpisodeRecord>(
                """SELECT * FROM episodes
                   WHERE series_id = @SeriesId AND season_number = @SeasonNumber
                   ORDER BY episode_number""",
                {| SeriesId = seriesIdVal; SeasonNumber = seasonNumber |}
            ) |> Async.AwaitTask

        let episodes = episodeRecords |> Seq.toList
        if List.isEmpty episodes then
            return None
        else
            // Get season record if it exists
            let! seasonRecord =
                conn.QueryFirstOrDefaultAsync<SeasonRecord>(
                    """SELECT * FROM seasons WHERE series_id = @SeriesId AND season_number = @SeasonNumber""",
                    {| SeriesId = seriesIdVal; SeasonNumber = seasonNumber |}
                ) |> Async.AwaitTask

            let seasonDetails: TmdbSeasonDetails = {
                TmdbSeriesId = s.TmdbId
                SeasonNumber = seasonNumber
                Name = if isNull (box seasonRecord) || isNull seasonRecord.name then None else Some seasonRecord.name
                Overview = if isNull (box seasonRecord) || isNull seasonRecord.overview then None else Some seasonRecord.overview
                PosterPath = if isNull (box seasonRecord) || isNull seasonRecord.poster_path then None else Some seasonRecord.poster_path
                AirDate =
                    if isNull (box seasonRecord) || isNull seasonRecord.air_date || seasonRecord.air_date = ""
                    then None
                    else parseDateTime seasonRecord.air_date
                Episodes =
                    episodes
                    |> List.map (fun ep -> {
                        EpisodeNumber = ep.episode_number
                        Name = if isNull ep.name then $"Episode {ep.episode_number}" else ep.name
                        Overview = if isNull ep.overview then None else Some ep.overview
                        AirDate = if isNull ep.air_date || ep.air_date = "" then None else parseDateTime ep.air_date
                        RuntimeMinutes = nullableToOption ep.runtime_minutes
                        StillPath = if isNull ep.still_path then None else Some ep.still_path
                    })
            }
            return Some seasonDetails
}

/// Get episode counts for all seasons of a series
/// Returns a map of season_number -> episode_count
/// Used for calculating next episode to watch
let getSeasonEpisodeCounts (seriesId: SeriesId) : Async<Map<int, int>> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<SeasonEpisodeCountRecord>(
            """SELECT season_number, episode_count FROM seasons
               WHERE series_id = @SeriesId AND season_number > 0
               ORDER BY season_number""",
            {| SeriesId = SeriesId.value seriesId |}
        ) |> Async.AwaitTask

    return records
        |> Seq.map (fun r -> (r.season_number, r.episode_count))
        |> Map.ofSeq
}

/// Calculate the next episode to watch based on current progress
/// Returns None if series is completed or metadata is missing
let calculateNextEpisode
    (seasonEpisodeCounts: Map<int, int>)
    (watchedEpisodes: EpisodeProgress list)
    (totalSeasons: int)
    : (int * int) option =

    // If no season metadata, return None (hide from Up Next)
    if Map.isEmpty seasonEpisodeCounts then
        None
    else
        // Build set of watched (season, episode) tuples
        let watchedSet =
            watchedEpisodes
            |> List.filter (fun p -> p.IsWatched)
            |> List.map (fun p -> (p.SeasonNumber, p.EpisodeNumber))
            |> Set.ofList

        // Find first unwatched episode by iterating through seasons in order
        let rec findNext season =
            if season > totalSeasons then
                None  // Completed all seasons
            else
                match Map.tryFind season seasonEpisodeCounts with
                | None ->
                    // No metadata for this season, skip to next
                    findNext (season + 1)
                | Some episodeCount ->
                    // Find first unwatched episode in this season
                    let firstUnwatched =
                        [1 .. episodeCount]
                        |> List.tryFind (fun ep -> not (Set.contains (season, ep) watchedSet))

                    match firstUnwatched with
                    | Some ep -> Some (season, ep)
                    | None -> findNext (season + 1)  // All watched in this season

        findNext 1

/// Save season and episode metadata from TMDB to the database
/// This enables episode names to be shown in the timeline
let saveSeasonEpisodes (seriesId: SeriesId) (season: TmdbSeasonDetails) : Async<unit> = async {
    use conn = getConnection()
    let seriesIdVal = SeriesId.value seriesId
    printfn "[Persistence] saveSeasonEpisodes: seriesId=%d, season=%d, episodes=%d" seriesIdVal season.SeasonNumber season.Episodes.Length

    // Debug: check if episodes have air dates
    let epsWithDates = season.Episodes |> List.filter (fun e -> e.AirDate.IsSome) |> List.length
    printfn "[Persistence] Episodes with air dates: %d / %d" epsWithDates season.Episodes.Length

    // First, upsert the season
    let! existingSeasonId =
        conn.ExecuteScalarAsync<Nullable<int64>>(
            """SELECT id FROM seasons WHERE series_id = @SeriesId AND season_number = @SeasonNumber""",
            {| SeriesId = seriesIdVal; SeasonNumber = season.SeasonNumber |}
        ) |> Async.AwaitTask

    let! seasonId =
        if existingSeasonId.HasValue then
            async { return existingSeasonId.Value }
        else
            conn.ExecuteScalarAsync<int64>(
                """INSERT INTO seasons (series_id, tmdb_season_id, season_number, name, overview, poster_path, air_date, episode_count)
                   VALUES (@SeriesId, 0, @SeasonNumber, @Name, @Overview, @PosterPath, @AirDate, @EpisodeCount);
                   SELECT last_insert_rowid();""",
                {| SeriesId = seriesIdVal
                   SeasonNumber = season.SeasonNumber
                   Name = season.Name |> Option.toObj
                   Overview = season.Overview |> Option.toObj
                   PosterPath = season.PosterPath |> Option.toObj
                   AirDate = season.AirDate |> Option.map (fun d -> d.ToString("yyyy-MM-dd")) |> Option.toObj
                   EpisodeCount = season.Episodes.Length |}
            ) |> Async.AwaitTask

    // Now upsert each episode
    for ep in season.Episodes do
        let! _ =
            conn.ExecuteAsync(
                """INSERT INTO episodes (series_id, season_id, tmdb_episode_id, season_number, episode_number, name, overview, air_date, runtime_minutes, still_path)
                   VALUES (@SeriesId, @SeasonId, 0, @SeasonNumber, @EpisodeNumber, @Name, @Overview, @AirDate, @RuntimeMinutes, @StillPath)
                   ON CONFLICT(series_id, season_number, episode_number) DO UPDATE SET
                       name = excluded.name,
                       overview = excluded.overview,
                       air_date = excluded.air_date,
                       runtime_minutes = excluded.runtime_minutes,
                       still_path = excluded.still_path""",
                {| SeriesId = seriesIdVal
                   SeasonId = seasonId
                   SeasonNumber = season.SeasonNumber
                   EpisodeNumber = ep.EpisodeNumber
                   Name = ep.Name
                   Overview = ep.Overview |> Option.toObj
                   AirDate = ep.AirDate |> Option.map (fun d -> d.ToString("yyyy-MM-dd")) |> Option.toObj
                   RuntimeMinutes = ep.RuntimeMinutes |> Option.toNullable
                   StillPath = ep.StillPath |> Option.toObj |}
            ) |> Async.AwaitTask
        ()
    return ()
}

/// Count watched episodes for an entry (from default session)
let countWatchedEpisodes (entryId: EntryId) : Async<int> = async {
    let! defaultSession = getDefaultSession entryId
    match defaultSession with
    | Some session -> return! countSessionWatchedEpisodes session.Id
    | None -> return 0
}

/// Get series info for a library entry (returns SeriesId and total episodes)
let getSeriesInfoForEntry (EntryId entryId) : Async<(SeriesId * int) option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<SeriesInfoRecord>(
            """SELECT le.series_id, s.number_of_episodes
               FROM library_entries le
               JOIN series s ON le.series_id = s.id
               WHERE le.id = @Id AND le.media_type = 'Series'""",
            {| Id = entryId |}
        ) |> Async.AwaitTask

    if isNull (box record) || not record.series_id.HasValue then
        return None
    else
        return Some (SeriesId record.series_id.Value, record.number_of_episodes)
}

/// Update the watch status based on episode progress (uses overall count across ALL sessions)
/// Now stores the NEXT episode to watch, not the last watched episode
let updateSeriesWatchStatusFromProgress (entryId: EntryId) : Async<unit> = async {
    match! getSeriesInfoForEntry entryId with
    | None -> ()
    | Some (seriesId, totalEpisodes) ->
        let! watchedCount = countOverallWatchedEpisodes entryId
        // Get overall progress across ALL sessions (not just default)
        let! overallWatchedEpisodes = getOverallEpisodeProgress entryId
        // Convert to EpisodeProgress format for calculateNextEpisode
        // (only SeasonNumber, EpisodeNumber, IsWatched are used by calculateNextEpisode)
        let progress =
            overallWatchedEpisodes
            |> List.map (fun (season, ep) -> {
                EntryId = entryId
                SessionId = SessionId 0  // Placeholder, not used by calculateNextEpisode
                SeriesId = seriesId
                SeasonNumber = season
                EpisodeNumber = ep
                IsWatched = true
                WatchedDate = None  // Date not needed for next episode calculation
            })

        // Debug: Get series name for logging
        let! series = getSeriesById seriesId
        let seriesName = series |> Option.map (fun s -> s.Name) |> Option.defaultValue "Unknown"

        // Get last watched date from ALL sessions (not just default)
        let! lastWatchedDate = getOverallLastWatchedDate entryId

        if watchedCount = 0 then
            do! updateWatchStatus entryId NotStarted None
        elif watchedCount >= totalEpisodes then
            printfn $"[UpNext Debug] {seriesName}: watchedCount={watchedCount} >= totalEpisodes={totalEpisodes} -> Completed"
            // Use actual last watched date from all sessions, not DateTime.UtcNow
            do! updateWatchStatus entryId Completed lastWatchedDate
        else
            // Get the series to find number of seasons
            let totalSeasons = series |> Option.map (fun s -> s.NumberOfSeasons) |> Option.defaultValue 0

            // Get season episode counts for next episode calculation
            let! seasonEpisodeCounts = getSeasonEpisodeCounts seriesId

            printfn $"[UpNext Debug] {seriesName}: watchedCount={watchedCount}, totalEpisodes={totalEpisodes}, totalSeasons={totalSeasons}, seasonEpisodeCounts={seasonEpisodeCounts}"

            // Calculate NEXT episode to watch (not last watched)
            let nextEpisode = calculateNextEpisode seasonEpisodeCounts progress totalSeasons

            printfn $"[UpNext Debug] {seriesName}: nextEpisode={nextEpisode}"

            match nextEpisode with
            | Some (nextSeason, nextEp) ->
                let progressInfo : WatchProgress = {
                    CurrentSeason = Some nextSeason
                    CurrentEpisode = Some nextEp
                    LastWatchedDate = lastWatchedDate
                }
                do! updateWatchStatus entryId (InProgress progressInfo) lastWatchedDate
            | None ->
                // No next episode found (missing metadata) - mark as InProgress but hide from Up Next
                printfn $"[UpNext Debug] {seriesName}: No next episode found - hiding from Up Next"
                let progressInfo : WatchProgress = {
                    CurrentSeason = None
                    CurrentEpisode = None
                    LastWatchedDate = lastWatchedDate
                }
                do! updateWatchStatus entryId (InProgress progressInfo) lastWatchedDate
}

// =====================================
// Database Statistics
// =====================================

let getDatabaseStats () : Async<Map<string, int>> = async {
    use conn = getConnection()

    let! movieCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM movies") |> Async.AwaitTask
    let! seriesCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM series") |> Async.AwaitTask
    let! friendCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM friends") |> Async.AwaitTask
    let! collectionCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM collections") |> Async.AwaitTask
    let! contributorCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM contributors") |> Async.AwaitTask
    let! entryCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM library_entries") |> Async.AwaitTask

    return Map.ofList [
        "movies", movieCount
        "series", seriesCount
        "friends", friendCount
        "collections", collectionCount
        "contributors", contributorCount
        "library_entries", entryCount
    ]
}

// =====================================
// Collection With Items & Progress
// =====================================

/// Resolve a collection item to its display data
let private resolveCollectionItemDisplay (item: CollectionItem) : Async<CollectionItemDisplay option> = async {
    match item.ItemRef with
    | LibraryEntryRef entryId ->
        let! entry = getLibraryEntryById entryId
        return entry |> Option.map EntryDisplay

    | SeasonRef (seriesId, seasonNumber) ->
        let! series = getSeriesById seriesId
        match series with
        | None -> return None
        | Some s ->
            // Create a TmdbSeasonSummary from the season number
            // Note: We don't have full season details stored, so we create a minimal summary
            let seasonSummary: TmdbSeasonSummary = {
                SeasonNumber = seasonNumber
                Name = Some $"Season {seasonNumber}"
                Overview = None
                PosterPath = s.PosterPath  // Use series poster as fallback
                AirDate = None
                EpisodeCount = 0  // Unknown without fetching from TMDB
            }
            return Some (SeasonDisplay (s, seasonSummary))

    | EpisodeRef (seriesId, seasonNumber, episodeNumber) ->
        let! series = getSeriesById seriesId
        match series with
        | None -> return None
        | Some s ->
            // Query episode name from database
            use conn = getConnection()
            let! episodeName =
                conn.ExecuteScalarAsync<string>(
                    """SELECT name FROM episodes
                       WHERE series_id = @SeriesId
                       AND season_number = @SeasonNumber
                       AND episode_number = @EpisodeNumber""",
                    {| SeriesId = SeriesId.value seriesId
                       SeasonNumber = seasonNumber
                       EpisodeNumber = episodeNumber |}
                ) |> Async.AwaitTask
            let name = if isNull episodeName then $"Episode {episodeNumber}" else episodeName

            // Create minimal summaries for season and episode
            let seasonSummary: TmdbSeasonSummary = {
                SeasonNumber = seasonNumber
                Name = Some $"Season {seasonNumber}"
                Overview = None
                PosterPath = s.PosterPath
                AirDate = None
                EpisodeCount = 0
            }
            let episodeSummary: TmdbEpisodeSummary = {
                EpisodeNumber = episodeNumber
                Name = name
                Overview = None
                AirDate = None
                RuntimeMinutes = s.EpisodeRunTimeMinutes
                StillPath = None
            }
            return Some (EpisodeDisplay (s, seasonSummary, episodeSummary))
}

/// Get a collection with all its items and their display data
let getCollectionWithItems (collectionId: CollectionId) : Async<CollectionWithItems option> = async {
    let! collection = getCollectionById collectionId
    match collection with
    | None -> return None
    | Some c ->
        let! items = getCollectionItems collectionId
        let! itemsWithDisplays =
            items
            |> List.map (fun item -> async {
                let! display = resolveCollectionItemDisplay item
                return display |> Option.map (fun d -> (item, d))
            })
            |> Async.Sequential
        let validItems = itemsWithDisplays |> Array.choose id |> Array.toList
        return Some {
            Collection = c
            Items = validItems
        }
}

/// Calculate collection progress
let getCollectionProgress (collectionId: CollectionId) : Async<CollectionProgress option> = async {
    let! collectionWithItems = getCollectionWithItems collectionId
    match collectionWithItems with
    | None -> return None
    | Some cwi ->
        let totalItems = List.length cwi.Items

        // Helper to check if an item is completed
        let isCompleted (display: CollectionItemDisplay) =
            match display with
            | EntryDisplay entry -> entry.WatchStatus = Completed
            | SeasonDisplay _ -> false  // Seasons don't have individual watch status
            | EpisodeDisplay _ -> false  // Episodes don't have individual watch status

        // Helper to check if an item is in progress
        let isInProgress (display: CollectionItemDisplay) =
            match display with
            | EntryDisplay entry ->
                match entry.WatchStatus with
                | InProgress _ -> true
                | _ -> false
            | SeasonDisplay _ -> false
            | EpisodeDisplay _ -> false

        let completedItems =
            cwi.Items
            |> List.filter (fun (_, display) -> isCompleted display)
            |> List.length
        let inProgressItems =
            cwi.Items
            |> List.filter (fun (_, display) -> isInProgress display)
            |> List.length

        // Calculate total and watched runtime
        let totalMinutes =
            cwi.Items
            |> List.sumBy (fun (_, display) ->
                match display with
                | EntryDisplay entry ->
                    match entry.Media with
                    | LibraryMovie movie -> movie.RuntimeMinutes |> Option.defaultValue 0
                    | LibrarySeries series ->
                        series.NumberOfEpisodes * (series.EpisodeRunTimeMinutes |> Option.defaultValue 45)
                | SeasonDisplay (series, season) ->
                    season.EpisodeCount * (series.EpisodeRunTimeMinutes |> Option.defaultValue 45)
                | EpisodeDisplay (series, _, _) ->
                    series.EpisodeRunTimeMinutes |> Option.defaultValue 45)

        let watchedMinutes =
            cwi.Items
            |> List.sumBy (fun (_, display) ->
                match display with
                | EntryDisplay entry ->
                    match entry.WatchStatus, entry.Media with
                    | Completed, LibraryMovie movie -> movie.RuntimeMinutes |> Option.defaultValue 0
                    | Completed, LibrarySeries series ->
                        series.NumberOfEpisodes * (series.EpisodeRunTimeMinutes |> Option.defaultValue 45)
                    | InProgress _, LibrarySeries _ -> 0
                    | _ -> 0
                | SeasonDisplay _ -> 0  // No watch tracking for seasons in collections
                | EpisodeDisplay _ -> 0)  // No watch tracking for episodes in collections

        let completionPct =
            if totalItems > 0 then float completedItems / float totalItems * 100.0
            else 0.0

        return Some {
            CollectionId = collectionId
            TotalItems = totalItems
            CompletedItems = completedItems
            InProgressItems = inProgressItems
            TotalMinutes = totalMinutes
            WatchedMinutes = watchedMinutes
            CompletionPercentage = completionPct
        }
}

/// Get all collections that contain a specific entry
let getCollectionsForEntry (entryId: EntryId) : Async<Collection list> = async {
    use conn = getConnection()
    let entryIdVal = EntryId.value entryId
    let! records = conn.QueryAsync<CollectionRecord>(
        """SELECT c.* FROM collections c
           INNER JOIN collection_items ci ON c.id = ci.collection_id
           WHERE ci.item_type = 'entry' AND ci.entry_id = @entryId
           ORDER BY c.name""",
        {| entryId = entryIdVal |}) |> Async.AwaitTask
    return records |> Seq.map recordToCollection |> Seq.toList
}

/// Get all collections that contain a specific item (entry, season, or episode)
let getCollectionsForItem (itemRef: CollectionItemRef) : Async<Collection list> = async {
    use conn = getConnection()
    match itemRef with
    | LibraryEntryRef (EntryId entryId) ->
        let! records = conn.QueryAsync<CollectionRecord>(
            """SELECT c.* FROM collections c
               INNER JOIN collection_items ci ON c.id = ci.collection_id
               WHERE ci.item_type = 'entry' AND ci.entry_id = @entryId
               ORDER BY c.name""",
            {| entryId = entryId |}) |> Async.AwaitTask
        return records |> Seq.map recordToCollection |> Seq.toList
    | SeasonRef (SeriesId seriesId, seasonNumber) ->
        let! records = conn.QueryAsync<CollectionRecord>(
            """SELECT c.* FROM collections c
               INNER JOIN collection_items ci ON c.id = ci.collection_id
               WHERE ci.item_type = 'season' AND ci.series_id = @seriesId AND ci.season_number = @seasonNumber
               ORDER BY c.name""",
            {| seriesId = seriesId; seasonNumber = seasonNumber |}) |> Async.AwaitTask
        return records |> Seq.map recordToCollection |> Seq.toList
    | EpisodeRef (SeriesId seriesId, seasonNumber, episodeNumber) ->
        let! records = conn.QueryAsync<CollectionRecord>(
            """SELECT c.* FROM collections c
               INNER JOIN collection_items ci ON c.id = ci.collection_id
               WHERE ci.item_type = 'episode' AND ci.series_id = @seriesId AND ci.season_number = @seasonNumber AND ci.episode_number = @episodeNumber
               ORDER BY c.name""",
            {| seriesId = seriesId; seasonNumber = seasonNumber; episodeNumber = episodeNumber |}) |> Async.AwaitTask
        return records |> Seq.map recordToCollection |> Seq.toList
}

// =====================================
// Tracked Contributors CRUD
// =====================================

let private recordToTrackedContributor (r: TrackedContributorRecord) : TrackedContributor = {
    Id = TrackedContributorId r.id
    TmdbPersonId = TmdbPersonId r.tmdb_person_id
    Name = r.name
    ProfilePath = if String.IsNullOrEmpty(r.profile_path) then None else Some r.profile_path
    KnownForDepartment = if String.IsNullOrEmpty(r.known_for_department) then None else Some r.known_for_department
    CreatedAt = DateTime.Parse(r.created_at)
    Notes = if String.IsNullOrEmpty(r.notes) then None else Some r.notes
}

/// Get all tracked contributors
let getAllTrackedContributors () : Async<TrackedContributor list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<TrackedContributorRecord>("SELECT * FROM tracked_contributors ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToTrackedContributor |> Seq.toList
}

/// Get a tracked contributor by ID
let getTrackedContributorById (TrackedContributorId id) : Async<TrackedContributor option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<TrackedContributorRecord>(
            "SELECT * FROM tracked_contributors WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToTrackedContributor record)
}

/// Get a tracked contributor by TMDB person ID
let getTrackedContributorByTmdbId (TmdbPersonId tmdbId) : Async<TrackedContributor option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<TrackedContributorRecord>(
            "SELECT * FROM tracked_contributors WHERE tmdb_person_id = @TmdbId",
            {| TmdbId = tmdbId |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToTrackedContributor record)
}

/// Check if a TMDB person is tracked
let isContributorTracked (TmdbPersonId tmdbId) : Async<bool> = async {
    use conn = getConnection()
    let! count =
        conn.ExecuteScalarAsync<int64>(
            "SELECT COUNT(*) FROM tracked_contributors WHERE tmdb_person_id = @TmdbId",
            {| TmdbId = tmdbId |}
        ) |> Async.AwaitTask
    return count > 0L
}

/// Track a new contributor
let trackContributor (request: TrackContributorRequest) : Async<Result<TrackedContributor, string>> = async {
    try
        // Check if already tracked
        let! existing = getTrackedContributorByTmdbId request.TmdbPersonId
        match existing with
        | Some tc -> return Ok tc
        | None ->
            use conn = getConnection()
            let now = DateTime.UtcNow.ToString("o")
            let id = Guid.NewGuid().ToString()
            let param = {|
                Id = id
                TmdbPersonId = TmdbPersonId.value request.TmdbPersonId
                Name = request.Name
                ProfilePath = request.ProfilePath |> Option.toObj
                KnownForDepartment = request.KnownForDepartment |> Option.toObj
                CreatedAt = now
                Notes = request.Notes |> Option.toObj
            |}
            do! conn.ExecuteAsync("""
                INSERT INTO tracked_contributors (id, tmdb_person_id, name, profile_path, known_for_department, created_at, notes)
                VALUES (@Id, @TmdbPersonId, @Name, @ProfilePath, @KnownForDepartment, @CreatedAt, @Notes)
            """, param) |> Async.AwaitTask |> Async.Ignore

            // Download the profile image from TMDB
            do! ImageCache.downloadProfile request.ProfilePath

            let! inserted = getTrackedContributorById (TrackedContributorId id)
            match inserted with
            | Some tc -> return Ok tc
            | None -> return Error "Failed to retrieve tracked contributor after insert"
    with
    | ex -> return Error $"Failed to track contributor: {ex.Message}"
}

/// Untrack a contributor
let untrackContributor (TrackedContributorId id) : Async<Result<unit, string>> = async {
    try
        use conn = getConnection()
        let! rowsAffected =
            conn.ExecuteAsync(
                "DELETE FROM tracked_contributors WHERE id = @Id",
                {| Id = id |}
            ) |> Async.AwaitTask
        if rowsAffected > 0 then return Ok ()
        else return Error "Tracked contributor not found"
    with
    | ex -> return Error $"Failed to untrack contributor: {ex.Message}"
}

/// Update notes for a tracked contributor
let updateTrackedContributorNotes (TrackedContributorId id) (notes: string option) : Async<Result<TrackedContributor, string>> = async {
    try
        use conn = getConnection()
        let! rowsAffected =
            conn.ExecuteAsync(
                "UPDATE tracked_contributors SET notes = @Notes WHERE id = @Id",
                {| Id = id; Notes = notes |> Option.toObj |}
            ) |> Async.AwaitTask
        if rowsAffected > 0 then
            let! updated = getTrackedContributorById (TrackedContributorId id)
            match updated with
            | Some tc -> return Ok tc
            | None -> return Error "Tracked contributor not found after update"
        else return Error "Tracked contributor not found"
    with
    | ex -> return Error $"Failed to update notes: {ex.Message}"
}

// =====================================
// Timeline Queries
// =====================================

/// Record type for timeline queries (combines movie and episode watch events)
[<CLIMutable>]
type TimelineQueryRecord = {
    mutable entry_id: int64
    mutable watched_date: string
    mutable event_type: string  // "movie", "episode", "season_completed", "series_completed"
    mutable season_number: int64
    mutable episode_number: int64
    mutable session_id: int64  // movie_watch_session id or watch_session id (0 if no session)
}

/// Get timeline entries with filtering and pagination
/// Returns entries sorted by watched_date DESC (most recent first)
let getTimelineEntries (filter: TimelineFilter) (page: int) (pageSize: int) : Async<PagedResponse<TimelineEntry>> = async {
    try
        use conn = getConnection()

        // Build filter conditions
        let formatDate (dt: DateTime) = dt.ToString("yyyy-MM-dd")
        let dateFilter =
            match filter.StartDate, filter.EndDate with
            | Some startDate, Some endDate ->
                $"AND watched_date >= '{formatDate startDate}' AND watched_date <= '{formatDate endDate}'"
            | Some startDate, None ->
                $"AND watched_date >= '{formatDate startDate}'"
            | None, Some endDate ->
                $"AND watched_date <= '{formatDate endDate}'"
            | None, None -> ""

        let entryFilter =
            filter.EntryId
            |> Option.map (fun (EntryId id) -> $"AND entry_id = {id}")
            |> Option.defaultValue ""

        // MediaType filter - if Movies, only get movies; if Series, only get episodes
        let mediaTypeFilter =
            match filter.MediaType with
            | Some MediaType.Movie -> "Movie"
            | Some MediaType.Series -> "Series"
            | None -> "All"

        let offset = (page - 1) * pageSize

        // Build queries based on media type filter
        // Movies: use movie_watch_sessions table (each watch session is a timeline entry)
        // Series: use episode_progress table (each watched episode is a timeline entry)
        let movieQuery =
            if mediaTypeFilter = "Series" then ""
            else $"""
                SELECT
                    mws.entry_id as entry_id,
                    mws.watched_date as watched_date,
                    'movie' as event_type,
                    0 as season_number,
                    0 as episode_number,
                    mws.id as session_id
                FROM movie_watch_sessions mws
                WHERE mws.watched_date IS NOT NULL
                    AND mws.watched_date != ''
                    {dateFilter}
                    {entryFilter}
            """

        let episodeQuery =
            if mediaTypeFilter = "Movie" then ""
            else $"""
                SELECT
                    ep.entry_id,
                    ep.watched_date,
                    'episode' as event_type,
                    COALESCE(ep.season_number, 0) as season_number,
                    COALESCE(ep.episode_number, 0) as episode_number,
                    COALESCE(ep.session_id, 0) as session_id
                FROM episode_progress ep
                WHERE ep.is_watched = 1
                    AND ep.watched_date IS NOT NULL
                    AND ep.watched_date != ''
                    {dateFilter}
                    {entryFilter}
            """

        let combinedQuery =
            match movieQuery.Trim(), episodeQuery.Trim() with
            | "", "" ->
                // Neither movies nor episodes - return empty placeholder query
                "SELECT 0 as entry_id, '' as watched_date, '' as event_type, 0 as season_number, 0 as episode_number, 0 as session_id WHERE 1=0"
            | "", ep -> ep
            | mv, "" -> mv
            | mv, ep -> $"{mv} UNION ALL {ep}"

        let query = $"""
            WITH all_events AS ({combinedQuery})
            SELECT entry_id, watched_date, event_type, season_number, episode_number, session_id FROM all_events
            ORDER BY watched_date DESC
            LIMIT @PageSize OFFSET @Offset
        """

        let countQuery = $"""
            WITH all_events AS ({combinedQuery})
            SELECT COUNT(*) FROM all_events
        """

        // Get total count
        let! totalCount = conn.ExecuteScalarAsync<int>(countQuery) |> Async.AwaitTask

        // Get page of records
        let! records =
            conn.QueryAsync<TimelineQueryRecord>(query, {| PageSize = pageSize; Offset = offset |})
            |> Async.AwaitTask

        // Convert records to TimelineEntry
        let! entries =
            records
            |> Seq.map (fun r -> async {
                let! entry = getLibraryEntryById (EntryId (int r.entry_id))
                match entry, parseDateTime r.watched_date with
                | Some e, Some dt ->
                    let season = if r.season_number > 0L then int r.season_number else 1
                    let episode = if r.episode_number > 0L then int r.episode_number else 1

                    let detail =
                        match r.event_type with
                        | "movie" -> MovieWatched
                        | "episode" -> EpisodeWatched (season, episode)
                        | "season_completed" -> SeasonCompleted season
                        | "series_completed" -> SeriesCompleted
                        | _ -> MovieWatched

                    // Get episode name and still path for episode watches
                    let! (episodeName, episodeStillPath) = async {
                        match r.event_type, e.Media with
                        | "episode", LibrarySeries series ->
                            use conn2 = getConnection()
                            let! result =
                                conn2.QueryFirstOrDefaultAsync<{| name: string; still_path: string |}>(
                                    """SELECT name, still_path FROM episodes
                                       WHERE series_id = @SeriesId
                                       AND season_number = @SeasonNumber
                                       AND episode_number = @EpisodeNumber""",
                                    {| SeriesId = SeriesId.value series.Id
                                       SeasonNumber = season
                                       EpisodeNumber = episode |}
                                ) |> Async.AwaitTask
                            if isNull (box result) then
                                return (None, None)
                            else
                                let name = if isNull result.name then None else Some result.name
                                let stillPath = if isNull result.still_path then None else Some result.still_path
                                return (name, stillPath)
                        | _ -> return (None, None)
                    }

                    // Get season poster path for series entries
                    let! seasonPosterPath = async {
                        match e.Media with
                        | LibrarySeries series ->
                            use conn2 = getConnection()
                            let! posterPath =
                                conn2.ExecuteScalarAsync<string>(
                                    """SELECT poster_path FROM seasons
                                       WHERE series_id = @SeriesId
                                       AND season_number = @SeasonNumber""",
                                    {| SeriesId = SeriesId.value series.Id
                                       SeasonNumber = season |}
                                ) |> Async.AwaitTask
                            return if isNull posterPath then None else Some posterPath
                        | _ -> return None
                    }

                    // Get friends for this watch event
                    let! friends = async {
                        use conn2 = getConnection()
                        match r.event_type with
                        | "movie" when r.session_id > 0L ->
                            // Get friends from movie_session_friends
                            let! friendRecords =
                                conn2.QueryAsync<FriendRecord>(
                                    """SELECT f.* FROM friends f
                                       JOIN movie_session_friends msf ON f.id = msf.friend_id
                                       WHERE msf.session_id = @SessionId""",
                                    {| SessionId = int r.session_id |}
                                ) |> Async.AwaitTask
                            return friendRecords |> Seq.map recordToFriend |> Seq.toList
                        | "episode" when r.session_id > 0L ->
                            // Get friends from session_friends (watch session)
                            let! friendRecords =
                                conn2.QueryAsync<FriendRecord>(
                                    """SELECT f.* FROM friends f
                                       JOIN session_friends sf ON f.id = sf.friend_id
                                       WHERE sf.session_id = @SessionId""",
                                    {| SessionId = int r.session_id |}
                                ) |> Async.AwaitTask
                            return friendRecords |> Seq.map recordToFriend |> Seq.toList
                        | _ -> return []
                    }

                    return Some {
                        WatchedDate = dt
                        Entry = e
                        Detail = detail
                        EpisodeName = episodeName
                        EpisodeStillPath = episodeStillPath
                        SeasonPosterPath = seasonPosterPath
                        WatchedWithFriends = friends
                    }
                | _ -> return None
            })
            |> Async.Sequential

        let items = entries |> Array.choose id |> Array.toList
        let totalPages = if totalCount = 0 then 0 else (totalCount + pageSize - 1) / pageSize

        return {
            Items = items
            Page = page
            PageSize = pageSize
            TotalCount = totalCount
            TotalPages = totalPages
            HasPreviousPage = page > 1
            HasNextPage = page < totalPages
        }
    with
    | ex ->
        printfn "Timeline query error: %s" ex.Message
        printfn "Stack trace: %s" ex.StackTrace
        return raise ex
}

/// Get timeline entries for a specific month
let getTimelineEntriesByMonth (year: int) (month: int) : Async<TimelineEntry list> = async {
    let startDate = DateTime(year, month, 1)
    let endDate = startDate.AddMonths(1).AddDays(-1.0)

    let filter = {
        StartDate = Some startDate
        EndDate = Some endDate
        MediaType = None
        EntryId = None
    }

    // Get all entries for this month (up to 1000)
    let! result = getTimelineEntries filter 1 1000
    return result.Items
}

/// Get date range for timeline (earliest and latest watched date)
let getTimelineDateRange (filter: TimelineFilter) : Async<TimelineDateRange option> = async {
    try
        use conn = getConnection()

        // Build filter conditions
        let formatDate (dt: DateTime) = dt.ToString("yyyy-MM-dd")
        let dateFilter =
            match filter.StartDate, filter.EndDate with
            | Some startDate, Some endDate ->
                $"AND watched_date >= '{formatDate startDate}' AND watched_date <= '{formatDate endDate}'"
            | Some startDate, None ->
                $"AND watched_date >= '{formatDate startDate}'"
            | None, Some endDate ->
                $"AND watched_date <= '{formatDate endDate}'"
            | None, None -> ""

        let entryFilter =
            filter.EntryId
            |> Option.map (fun (EntryId id) -> $"AND entry_id = {id}")
            |> Option.defaultValue ""

        // MediaType filter
        let mediaTypeFilter =
            match filter.MediaType with
            | Some MediaType.Movie -> "Movie"
            | Some MediaType.Series -> "Series"
            | None -> "All"

        // Build queries based on media type filter
        let movieQuery =
            if mediaTypeFilter = "Series" then ""
            else $"""
                SELECT watched_date
                FROM movie_watch_sessions mws
                WHERE mws.watched_date IS NOT NULL
                    AND mws.watched_date != ''
                    {dateFilter}
                    {entryFilter}
            """

        let episodeQuery =
            if mediaTypeFilter = "Movie" then ""
            else $"""
                SELECT watched_date
                FROM episode_progress ep
                WHERE ep.is_watched = 1
                    AND ep.watched_date IS NOT NULL
                    AND ep.watched_date != ''
                    {dateFilter}
                    {entryFilter}
            """

        let combinedQuery =
            match movieQuery.Trim(), episodeQuery.Trim() with
            | "", "" -> "SELECT NULL as watched_date WHERE 1=0"
            | "", ep -> ep
            | mv, "" -> mv
            | mv, ep -> $"{mv} UNION ALL {ep}"

        let query = $"""
            WITH all_events AS ({combinedQuery})
            SELECT
                MIN(watched_date) as earliest_date,
                MAX(watched_date) as latest_date,
                COUNT(*) as total_count
            FROM all_events
        """

        let! result =
            conn.QueryFirstOrDefaultAsync<{| earliest_date: string; latest_date: string; total_count: int |}>(query)
            |> Async.AwaitTask

        if isNull (box result) || result.total_count = 0 then
            return None
        else
            match parseDateTime result.earliest_date, parseDateTime result.latest_date with
            | Some earliest, Some latest ->
                return Some {
                    EarliestDate = earliest
                    LatestDate = latest
                    TotalCount = result.total_count
                }
            | _ -> return None
    with
    | ex ->
        printfn "Timeline date range query error: %s" ex.Message
        return None
}

// =====================================
// Trakt Settings Operations
// =====================================

/// Record type for Trakt settings
[<CLIMutable>]
type private TraktSettingsRecord = {
    mutable id: int
    mutable access_token: string
    mutable refresh_token: string
    mutable token_expires_at: string
    mutable last_sync_at: string
    mutable auto_sync_enabled: int
    mutable created_at: string
    mutable updated_at: string
}

/// Get Trakt settings (tokens and sync state)
let getTraktSettings () : Async<{| AccessToken: string option; RefreshToken: string option; ExpiresAt: DateTime option; LastSyncAt: DateTime option; AutoSyncEnabled: bool |}> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<TraktSettingsRecord>(
            "SELECT * FROM trakt_settings WHERE id = 1"
        ) |> Async.AwaitTask

    if isNull (box record) then
        return {| AccessToken = None; RefreshToken = None; ExpiresAt = None; LastSyncAt = None; AutoSyncEnabled = true |}
    else
        return {|
            AccessToken = if String.IsNullOrEmpty(record.access_token) then None else Some record.access_token
            RefreshToken = if String.IsNullOrEmpty(record.refresh_token) then None else Some record.refresh_token
            ExpiresAt = parseDateTime record.token_expires_at
            LastSyncAt = parseDateTime record.last_sync_at
            AutoSyncEnabled = record.auto_sync_enabled = 1
        |}
}

/// Save Trakt OAuth tokens
let saveTraktTokens (accessToken: string) (refreshToken: string) (expiresAt: DateTime) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let expiresAtStr = expiresAt.ToString("o")
    let param = {| AccessToken = accessToken; RefreshToken = refreshToken; ExpiresAt = expiresAtStr; UpdatedAt = now |}
    let sql = """UPDATE trakt_settings
                 SET access_token = @AccessToken, refresh_token = @RefreshToken,
                     token_expires_at = @ExpiresAt, updated_at = @UpdatedAt
                 WHERE id = 1"""
    do! conn.ExecuteAsync(sql, param) |> Async.AwaitTask |> Async.Ignore
}

/// Clear Trakt tokens (logout)
let clearTraktTokens () : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let param = {| UpdatedAt = now |}
    let sql = """UPDATE trakt_settings
                 SET access_token = NULL, refresh_token = NULL, token_expires_at = NULL, updated_at = @UpdatedAt
                 WHERE id = 1"""
    do! conn.ExecuteAsync(sql, param) |> Async.AwaitTask |> Async.Ignore
}

/// Update the last sync timestamp
let updateTraktLastSync () : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let param = {| Now = now |}
    do! conn.ExecuteAsync("UPDATE trakt_settings SET last_sync_at = @Now, updated_at = @Now WHERE id = 1", param) |> Async.AwaitTask |> Async.Ignore
}

/// Toggle auto-sync setting
let setTraktAutoSync (enabled: bool) : Async<unit> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let param = {| Enabled = (if enabled then 1 else 0); UpdatedAt = now |}
    do! conn.ExecuteAsync("UPDATE trakt_settings SET auto_sync_enabled = @Enabled, updated_at = @UpdatedAt WHERE id = 1", param) |> Async.AwaitTask |> Async.Ignore
}

/// Get the most recent watched date from the database (for incremental sync)
/// Returns the latest watch date across both episode_progress and movie_watch_sessions
let getLastKnownWatchDate () : Async<DateTime option> = async {
    use conn = getConnection()
    let sql = """
        SELECT MAX(watched_date) FROM (
            SELECT MAX(watched_date) as watched_date FROM episode_progress WHERE watched_date IS NOT NULL
            UNION ALL
            SELECT MAX(watched_date) as watched_date FROM movie_watch_sessions WHERE watched_date IS NOT NULL
        )
    """
    let! result = conn.ExecuteScalarAsync<string>(sql) |> Async.AwaitTask
    return parseDateTime result
}
