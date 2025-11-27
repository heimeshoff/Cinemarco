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
type TagRecord = {
    id: int
    name: string
    color: string
    icon: string
    description: string
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
// Tags CRUD
// =====================================

let private recordToTag (r: TagRecord) : Tag = {
    Id = TagId r.id
    Name = r.name
    Color = if String.IsNullOrEmpty(r.color) then None else Some r.color
    Icon = if String.IsNullOrEmpty(r.icon) then None else Some r.icon
    Description = if String.IsNullOrEmpty(r.description) then None else Some r.description
    CreatedAt = DateTime.Parse(r.created_at)
}

let getAllTags () : Async<Tag list> = async {
    use conn = getConnection()
    let! records =
        conn.QueryAsync<TagRecord>("SELECT * FROM tags ORDER BY name")
        |> Async.AwaitTask
    return records |> Seq.map recordToTag |> Seq.toList
}

let getTagById (TagId id) : Async<Tag option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<TagRecord>(
            "SELECT * FROM tags WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToTag record)
}

let getTagByName (name: string) : Async<Tag option> = async {
    use conn = getConnection()
    let! record =
        conn.QueryFirstOrDefaultAsync<TagRecord>(
            "SELECT * FROM tags WHERE name = @Name",
            {| Name = name |}
        ) |> Async.AwaitTask
    return if isNull (box record) then None else Some (recordToTag record)
}

let insertTag (request: CreateTagRequest) : Async<Tag> = async {
    use conn = getConnection()
    let now = DateTime.UtcNow.ToString("o")
    let! id =
        conn.ExecuteScalarAsync<int64>("""
            INSERT INTO tags (name, color, description, created_at, updated_at)
            VALUES (@Name, @Color, @Description, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            Name = request.Name
            Color = request.Color |> Option.toObj
            Description = request.Description |> Option.toObj
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask
    return {
        Id = TagId (int id)
        Name = request.Name
        Color = request.Color
        Icon = None
        Description = request.Description
        CreatedAt = DateTime.UtcNow
    }
}

let updateTag (request: UpdateTagRequest) : Async<unit> = async {
    use conn = getConnection()
    let! existing = getTagById request.Id
    match existing with
    | None -> ()
    | Some tag ->
        let now = DateTime.UtcNow.ToString("o")
        let name = Option.defaultValue tag.Name request.Name
        let color = Option.toObj (Option.orElse tag.Color request.Color)
        let description = Option.toObj (Option.orElse tag.Description request.Description)
        let param = {|
            Id = TagId.value request.Id
            Name = name
            Color = color
            Description = description
            UpdatedAt = now
        |}
        do! conn.ExecuteAsync("""
            UPDATE tags SET
                name = @Name,
                color = @Color,
                description = @Description,
                updated_at = @UpdatedAt
            WHERE id = @Id
        """, param) |> Async.AwaitTask |> Async.Ignore
}

let deleteTag (TagId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM tags WHERE id = @Id", {| Id = id |})
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
    do! conn.ExecuteAsync("DELETE FROM collections WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
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
// Entry Tags Junction
// =====================================

let getTagsForEntry (EntryId entryId) : Async<TagId list> = async {
    use conn = getConnection()
    let! ids =
        conn.QueryAsync<int>(
            "SELECT tag_id FROM entry_tags WHERE entry_id = @EntryId",
            {| EntryId = entryId |}
        ) |> Async.AwaitTask
    return ids |> Seq.map TagId |> Seq.toList
}

let addTagToEntry (EntryId entryId) (TagId tagId) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("""
        INSERT OR IGNORE INTO entry_tags (entry_id, tag_id) VALUES (@EntryId, @TagId)
    """, {| EntryId = entryId; TagId = tagId |}) |> Async.AwaitTask |> Async.Ignore
}

let removeTagFromEntry (EntryId entryId) (TagId tagId) : Async<unit> = async {
    use conn = getConnection()
    let param = {| EntryId = entryId; TagId = tagId |}
    let! _ = conn.ExecuteAsync("DELETE FROM entry_tags WHERE entry_id = @EntryId AND tag_id = @TagId", param) |> Async.AwaitTask
    return ()
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
        let tagParam = {| SessionId = r.id |}
        let friendParam = {| SessionId = r.id |}
        let! tagIds =
            conn.QueryAsync<int>(
                "SELECT tag_id FROM session_tags WHERE session_id = @SessionId",
                tagParam
            ) |> Async.AwaitTask
        let! friendIds =
            conn.QueryAsync<int>(
                "SELECT friend_id FROM session_friends WHERE session_id = @SessionId",
                friendParam
            ) |> Async.AwaitTask
        return {
            Id = SessionId r.id
            EntryId = EntryId r.entry_id
            Name = r.name
            Status = parseSessionStatus r.status
            StartDate = parseDateTime r.start_date
            EndDate = parseDateTime r.end_date
            Tags = tagIds |> Seq.map TagId |> Seq.toList
            Friends = friendIds |> Seq.map FriendId |> Seq.toList
            Notes = if String.IsNullOrEmpty(r.notes) then None else Some r.notes
            CreatedAt = DateTime.Parse(r.created_at)
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
            INSERT INTO watch_sessions (entry_id, name, status, start_date, created_at, updated_at)
            VALUES (@EntryId, @Name, 'Active', @StartDate, @CreatedAt, @UpdatedAt);
            SELECT last_insert_rowid();
        """, {|
            EntryId = EntryId.value request.EntryId
            Name = request.Name
            StartDate = now
            CreatedAt = now
            UpdatedAt = now
        |}) |> Async.AwaitTask

    let sessionId = int id

    // Add tags
    for tagId in request.Tags do
        let tagParam = {| SessionId = sessionId; TagId = TagId.value tagId |}
        let! _ = conn.ExecuteAsync("INSERT INTO session_tags (session_id, tag_id) VALUES (@SessionId, @TagId)", tagParam) |> Async.AwaitTask
        ()

    // Add friends
    for friendId in request.Friends do
        let friendParam = {| SessionId = sessionId; FriendId = FriendId.value friendId |}
        let! _ = conn.ExecuteAsync("INSERT INTO session_friends (session_id, friend_id) VALUES (@SessionId, @FriendId)", friendParam) |> Async.AwaitTask
        ()

    return {
        Id = SessionId sessionId
        EntryId = request.EntryId
        Name = request.Name
        Status = Active
        StartDate = Some DateTime.UtcNow
        EndDate = None
        Tags = request.Tags
        Friends = request.Friends
        Notes = None
        CreatedAt = DateTime.UtcNow
    }
}

let deleteWatchSession (SessionId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM watch_sessions WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
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
            // Get tags and friends for this entry
            let! tagIds = getTagsForEntry (EntryId record.id)
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
                Tags = tagIds
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

            // Add tags
            for tagId in request.InitialTags do
                do! addTagToEntry (EntryId entryIdInt) tagId

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

            // Add tags
            for tagId in request.InitialTags do
                do! addTagToEntry (EntryId entryIdInt) tagId

            // Add friends
            for friendId in request.InitialFriends do
                do! addFriendToEntry (EntryId entryIdInt) friendId

            // Fetch and return the complete entry
            let! entry = getLibraryEntryById (EntryId entryIdInt)
            match entry with
            | Some e -> return Ok e
            | None -> return Error "Failed to retrieve created entry"
    with
    | ex -> return Error $"Failed to add series: {ex.Message}"
}

/// Delete a library entry
let deleteLibraryEntry (EntryId id) : Async<unit> = async {
    use conn = getConnection()
    do! conn.ExecuteAsync("DELETE FROM library_entries WHERE id = @Id", {| Id = id |})
        |> Async.AwaitTask |> Async.Ignore
}

// =====================================
// Database Statistics
// =====================================

let getDatabaseStats () : Async<Map<string, int>> = async {
    use conn = getConnection()

    let! movieCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM movies") |> Async.AwaitTask
    let! seriesCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM series") |> Async.AwaitTask
    let! friendCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM friends") |> Async.AwaitTask
    let! tagCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM tags") |> Async.AwaitTask
    let! collectionCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM collections") |> Async.AwaitTask
    let! contributorCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM contributors") |> Async.AwaitTask
    let! entryCount = conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM library_entries") |> Async.AwaitTask

    return Map.ofList [
        "movies", movieCount
        "series", seriesCount
        "friends", friendCount
        "tags", tagCount
        "collections", collectionCount
        "contributors", contributorCount
        "library_entries", entryCount
    ]
}
