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
    entry_id: int
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
    Notes = if String.IsNullOrEmpty(r.notes) then None else Some r.notes
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
            INSERT INTO friends (name, nickname, notes, created_at, updated_at)
            VALUES (@Name, @Nickname, @Notes, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            Name = request.Name
            Nickname = request.Nickname |> Option.toObj
            Notes = request.Notes |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return {
        Id = FriendId (int id)
        Name = request.Name
        Nickname = request.Nickname
        AvatarUrl = None
        Notes = request.Notes
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
        let notes = Option.toObj (Option.orElse friend.Notes request.Notes)
        let param = {|
            Id = FriendId.value request.Id
            Name = name
            Nickname = nickname
            Notes = notes
            UpdatedAt = now
        |}
        do! conn.ExecuteAsync("""
            UPDATE friends SET
                name = @Name,
                nickname = @Nickname,
                notes = @Notes,
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
    return {
        Id = CollectionId (int id)
        Name = request.Name
        Description = request.Description
        CoverImagePath = None
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
        let param = {|
            Id = CollectionId.value request.Id
            Name = name
            Description = description
            UpdatedAt = now
        |}
        do! conn.ExecuteAsync("""
            UPDATE collections SET
                name = @Name,
                description = @Description,
                updated_at = @UpdatedAt
            WHERE id = @Id
        """, param) |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Collection Items CRUD
// =====================================

let private recordToCollectionItem (r: CollectionItemRecord) : CollectionItem = {
    CollectionId = CollectionId r.collection_id
    EntryId = EntryId r.entry_id
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

let addItemToCollection (CollectionId collectionId) (EntryId entryId) (notes: string option) : Async<unit> = async {
    use conn = getConnection()
    // Get the max position for this collection
    let! maxPos = conn.ExecuteScalarAsync<Nullable<int>>("SELECT MAX(position) FROM collection_items WHERE collection_id = @CollectionId", {| CollectionId = collectionId |}) |> Async.AwaitTask
    let nextPos = if maxPos.HasValue then maxPos.Value + 1 else 0
    let! _ = conn.ExecuteAsync("INSERT OR IGNORE INTO collection_items (collection_id, entry_id, position, notes) VALUES (@CollectionId, @EntryId, @Position, @Notes)", {| CollectionId = collectionId; EntryId = entryId; Position = nextPos; Notes = notes |> Option.toObj |}) |> Async.AwaitTask
    ()
}

let removeItemFromCollection (CollectionId collectionId) (EntryId entryId) : Async<unit> = async {
    use conn = getConnection()
    let! _ = conn.ExecuteAsync("DELETE FROM collection_items WHERE collection_id = @CollectionId AND entry_id = @EntryId", {| CollectionId = collectionId; EntryId = entryId |}) |> Async.AwaitTask
    // Reorder remaining items to fill gaps
    let! _ = conn.ExecuteAsync("UPDATE collection_items SET position = (SELECT COUNT(*) - 1 FROM collection_items AS ci2 WHERE ci2.collection_id = collection_items.collection_id AND ci2.position <= collection_items.position) WHERE collection_id = @CollectionId", {| CollectionId = collectionId |}) |> Async.AwaitTask
    ()
}

let reorderCollectionItems (CollectionId collectionId) (entryIds: EntryId list) : Async<unit> = async {
    use conn = getConnection()
    // Update positions based on the order of entry IDs provided
    for (position, EntryId entryId) in List.indexed entryIds do
        let! _ = conn.ExecuteAsync("UPDATE collection_items SET position = @Position WHERE collection_id = @CollectionId AND entry_id = @EntryId", {| CollectionId = collectionId; EntryId = entryId; Position = position |}) |> Async.AwaitTask
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
let getEntriesWatchedWithFriend (FriendId friendId) : Async<int list> = async {
    use conn = getConnection()
    let! entryIds =
        conn.QueryAsync<int>(
            "SELECT entry_id FROM entry_friends WHERE friend_id = @FriendId",
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
        // Update existing record
        let! _ =
            conn.ExecuteAsync(
                """UPDATE episode_progress SET is_watched = @IsWatched, watched_date = @WatchedDate
                   WHERE id = @Id""",
                {| Id = existingId.Value; IsWatched = (if watched then 1 else 0); WatchedDate = (if watched then now else null) |}
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

/// Get all referenced image paths from the database (posters and backdrops)
/// Only returns paths for movies/series that are actually in the library
let getAllReferencedImagePaths () : Async<Set<string> * Set<string>> = async {
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

    let posters =
        Seq.append moviePosters seriesPosters
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    let backdrops =
        Seq.append movieBackdrops seriesBackdrops
        |> Seq.filter (not << String.IsNullOrEmpty)
        |> Set.ofSeq

    return posters, backdrops
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

    let dateLastWatched =
        match status with
        | Completed | InProgress _ -> formatDateTime (Some DateTime.UtcNow)
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

/// Mark all episodes in a season as watched (for default session)
let markSeasonWatched (entryId: EntryId) (seriesId: SeriesId) (seasonNumber: int) (episodeCount: int) : Async<unit> = async {
    let! defaultSession = getDefaultSession entryId
    match defaultSession with
    | Some session ->
        do! markSessionSeasonWatched session.Id entryId seriesId seasonNumber episodeCount
    | None -> ()
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

/// Update the watch status based on episode progress
let updateSeriesWatchStatusFromProgress (entryId: EntryId) : Async<unit> = async {
    match! getSeriesInfoForEntry entryId with
    | None -> ()
    | Some (_, totalEpisodes) ->
        let! watchedCount = countWatchedEpisodes entryId
        let! progress = getEpisodeProgress entryId

        if watchedCount = 0 then
            do! updateWatchStatus entryId NotStarted None
        elif watchedCount >= totalEpisodes then
            do! updateWatchStatus entryId Completed (Some DateTime.UtcNow)
        else
            // Find the latest watched episode
            let lastWatched =
                progress
                |> List.filter (fun p -> p.IsWatched)
                |> List.sortByDescending (fun p -> (p.SeasonNumber, p.EpisodeNumber))
                |> List.tryHead

            match lastWatched with
            | Some ep ->
                let progressInfo : WatchProgress = {
                    CurrentSeason = Some ep.SeasonNumber
                    CurrentEpisode = Some ep.EpisodeNumber
                    LastWatchedDate = ep.WatchedDate
                }
                do! updateWatchStatus entryId (InProgress progressInfo) ep.WatchedDate
            | None ->
                do! updateWatchStatus entryId NotStarted None
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

/// Get a collection with all its items and their library entries
let getCollectionWithItems (collectionId: CollectionId) : Async<CollectionWithItems option> = async {
    let! collection = getCollectionById collectionId
    match collection with
    | None -> return None
    | Some c ->
        let! items = getCollectionItems collectionId
        let! itemsWithEntries =
            items
            |> List.map (fun item -> async {
                let! entry = getLibraryEntryById item.EntryId
                return entry |> Option.map (fun e -> (item, e))
            })
            |> Async.Sequential
        let validItems = itemsWithEntries |> Array.choose id |> Array.toList
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
        let completedItems =
            cwi.Items
            |> List.filter (fun (_, entry) ->
                match entry.WatchStatus with
                | Completed -> true
                | _ -> false)
            |> List.length
        let inProgressItems =
            cwi.Items
            |> List.filter (fun (_, entry) ->
                match entry.WatchStatus with
                | InProgress _ -> true
                | _ -> false)
            |> List.length

        // Calculate total and watched runtime
        let totalMinutes =
            cwi.Items
            |> List.sumBy (fun (_, entry) ->
                match entry.Media with
                | LibraryMovie movie -> movie.RuntimeMinutes |> Option.defaultValue 0
                | LibrarySeries series ->
                    series.NumberOfEpisodes * (series.EpisodeRunTimeMinutes |> Option.defaultValue 45))
        let watchedMinutes =
            cwi.Items
            |> List.sumBy (fun (_, entry) ->
                match entry.WatchStatus, entry.Media with
                | Completed, LibraryMovie movie -> movie.RuntimeMinutes |> Option.defaultValue 0
                | Completed, LibrarySeries series ->
                    series.NumberOfEpisodes * (series.EpisodeRunTimeMinutes |> Option.defaultValue 45)
                | InProgress _, LibrarySeries _ ->
                    // For in-progress series, we'd need to calculate watched episodes
                    // For now, estimate based on progress - this is simplified
                    0
                | _ -> 0)

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
           WHERE ci.entry_id = @entryId
           ORDER BY c.name""",
        {| entryId = entryIdVal |}) |> Async.AwaitTask
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
