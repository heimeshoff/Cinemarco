module Shared.Domain

open System

// =====================================
// Core Identifiers
// =====================================

/// Unique identifier for movies in our library
type MovieId = MovieId of int

/// Unique identifier for series in our library
type SeriesId = SeriesId of int

/// Unique identifier for library entries (wrapper around movie/series)
type EntryId = EntryId of int

/// TMDB movie identifier
type TmdbMovieId = TmdbMovieId of int

/// TMDB series identifier
type TmdbSeriesId = TmdbSeriesId of int

/// TMDB person identifier
type TmdbPersonId = TmdbPersonId of int

/// Unique identifier for friends
type FriendId = FriendId of int

/// Unique identifier for collections
type CollectionId = CollectionId of int

/// Unique identifier for watch sessions
type SessionId = SessionId of int

/// Unique identifier for contributors (actors, directors, etc.)
type ContributorId = ContributorId of int

/// Unique identifier for tracked contributors
type TrackedContributorId = TrackedContributorId of string

// =====================================
// ID Module Helpers
// =====================================

module MovieId =
    let create id = MovieId id
    let value (MovieId id) = id

module SeriesId =
    let create id = SeriesId id
    let value (SeriesId id) = id

module EntryId =
    let create id = EntryId id
    let value (EntryId id) = id

module TmdbMovieId =
    let create id = TmdbMovieId id
    let value (TmdbMovieId id) = id

module TmdbSeriesId =
    let create id = TmdbSeriesId id
    let value (TmdbSeriesId id) = id

module TmdbPersonId =
    let create id = TmdbPersonId id
    let value (TmdbPersonId id) = id

module FriendId =
    let create id = FriendId id
    let value (FriendId id) = id

module CollectionId =
    let create id = CollectionId id
    let value (CollectionId id) = id

module SessionId =
    let create id = SessionId id
    let value (SessionId id) = id

module ContributorId =
    let create id = ContributorId id
    let value (ContributorId id) = id

module TrackedContributorId =
    let create id = TrackedContributorId id
    let value (TrackedContributorId id) = id

// =====================================
// Enums & Discriminated Unions
// =====================================

/// Discriminates between movies and series
type MediaType =
    | Movie
    | Series

/// Status of a TV series
type SeriesStatus =
    | Returning
    | Ended
    | Canceled
    | InProduction
    | Planned
    | Unknown

/// Personal rating scale
type PersonalRating =
    | Outstanding   // 5 - Absolutely brilliant, stays with you
    | Entertaining  // 4 - Strong craft, enjoyable, recommendable
    | Decent        // 3 - Watchable, even if not life-changing
    | Meh           // 2 - Didn't click, uninspiring
    | Waste         // 1 - Waste of time

module PersonalRating =
    let toInt = function
        | Outstanding -> 5
        | Entertaining -> 4
        | Decent -> 3
        | Meh -> 2
        | Waste -> 1

    let fromInt = function
        | 5 -> Some Outstanding
        | 4 -> Some Entertaining
        | 3 -> Some Decent
        | 2 -> Some Meh
        | 1 -> Some Waste
        | _ -> None

    let description = function
        | Outstanding -> "Outstanding - Absolutely brilliant, stays with you."
        | Entertaining -> "Entertaining - Strong craft, enjoyable, recommendable."
        | Decent -> "Decent - Watchable, even if not life-changing."
        | Meh -> "Meh - Didn't click, uninspiring."
        | Waste -> "Waste - Waste of time."

    let shortLabel = function
        | Outstanding -> "Outstanding"
        | Entertaining -> "Entertaining"
        | Decent -> "Decent"
        | Meh -> "Meh"
        | Waste -> "Waste"

/// Status of a watch session
type SessionStatus =
    | Active
    | Paused
    | SessionCompleted

/// Role a contributor played in a movie/series
type ContributorRole =
    | Director
    | Actor of character: string option
    | Writer
    | Cinematographer
    | Composer
    | Producer
    | ExecutiveProducer
    | CreatedBy
    | Other of department: string

// =====================================
// Watch Status & Progress
// =====================================

/// Progress information for in-progress media
type WatchProgress = {
    CurrentSeason: int option
    CurrentEpisode: int option
    LastWatchedDate: DateTime option
}

/// Information about why/where something was abandoned
type AbandonedInfo = {
    AbandonedAt: WatchProgress option
    Reason: string option
    AbandonedDate: DateTime option
}

/// Watch status for any media
type WatchStatus =
    | NotStarted
    | InProgress of WatchProgress
    | Completed
    | Abandoned of AbandonedInfo

// =====================================
// Core Media Entities
// =====================================

/// A movie with TMDB data and local metadata
type Movie = {
    Id: MovieId
    TmdbId: TmdbMovieId
    Title: string
    OriginalTitle: string option
    Overview: string option
    ReleaseDate: DateTime option
    RuntimeMinutes: int option
    PosterPath: string option
    BackdropPath: string option
    Genres: string list
    OriginalLanguage: string option
    VoteAverage: float option
    VoteCount: int option
    Tagline: string option
    ImdbId: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// A TV series with TMDB data
type Series = {
    Id: SeriesId
    TmdbId: TmdbSeriesId
    Name: string
    OriginalName: string option
    Overview: string option
    FirstAirDate: DateTime option
    LastAirDate: DateTime option
    PosterPath: string option
    BackdropPath: string option
    Genres: string list
    OriginalLanguage: string option
    VoteAverage: float option
    VoteCount: int option
    Status: SeriesStatus
    NumberOfSeasons: int
    NumberOfEpisodes: int
    EpisodeRunTimeMinutes: int option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// A season of a TV series
type Season = {
    Id: int
    SeriesId: SeriesId
    TmdbSeasonId: int
    SeasonNumber: int
    Name: string option
    Overview: string option
    PosterPath: string option
    AirDate: DateTime option
    EpisodeCount: int
}

/// An episode of a TV series
type Episode = {
    Id: int
    SeriesId: SeriesId
    SeasonId: int
    TmdbEpisodeId: int
    SeasonNumber: int
    EpisodeNumber: int
    Name: string
    Overview: string option
    AirDate: DateTime option
    RuntimeMinutes: int option
    StillPath: string option
}

// =====================================
// People
// =====================================

/// A friend (real person you know)
type Friend = {
    Id: FriendId
    Name: string
    Nickname: string option
    AvatarUrl: string option
    CreatedAt: DateTime
}

/// A contributor (actor, director, etc.)
type Contributor = {
    Id: ContributorId
    TmdbPersonId: TmdbPersonId
    Name: string
    ProfilePath: string option
    KnownForDepartment: string option
    Birthday: DateTime option
    Deathday: DateTime option
    PlaceOfBirth: string option
    Biography: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// Links a contributor to a movie/series
type MediaContributor = {
    ContributorId: ContributorId
    MovieId: MovieId option
    SeriesId: SeriesId option
    Role: ContributorRole
    Order: int option
}

/// A tracked contributor (actor, director, etc.) from the user's personal list
type TrackedContributor = {
    Id: TrackedContributorId
    TmdbPersonId: TmdbPersonId
    Name: string
    ProfilePath: string option
    KnownForDepartment: string option
    CreatedAt: DateTime
    Notes: string option
}

// =====================================
// Organization
// =====================================

/// A curated, ordered collection
type Collection = {
    Id: CollectionId
    Name: string
    Description: string option
    CoverImagePath: string option
    IsPublicFranchise: bool
    TmdbCollectionId: int option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// Reference to an item that can be added to a collection
type CollectionItemRef =
    | LibraryEntryRef of EntryId
    | SeasonRef of seriesId: SeriesId * seasonNumber: int
    | EpisodeRef of seriesId: SeriesId * seasonNumber: int * episodeNumber: int

/// An item in a collection (ordered)
type CollectionItem = {
    CollectionId: CollectionId
    ItemRef: CollectionItemRef
    Position: int
    Notes: string option
}

// =====================================
// Library Entry
// =====================================

/// Captures why/how something was added to the library
type WhyAdded = {
    RecommendedBy: FriendId option
    RecommendedByName: string option
    Source: string option
    Context: string option
    DateRecommended: DateTime option
}

/// What type of media is in a library entry
type LibraryMedia =
    | LibraryMovie of Movie
    | LibrarySeries of Series

/// A personal library entry wrapping a movie or series
type LibraryEntry = {
    Id: EntryId
    Media: LibraryMedia
    WhyAdded: WhyAdded option
    WatchStatus: WatchStatus
    PersonalRating: PersonalRating option
    DateAdded: DateTime
    DateFirstWatched: DateTime option
    DateLastWatched: DateTime option
    Notes: string option
    IsFavorite: bool
    Friends: FriendId list
}

// =====================================
// Watch Sessions & Progress
// =====================================

/// A watch session for a series (requires at least one friend)
type WatchSession = {
    Id: SessionId
    EntryId: EntryId
    Status: SessionStatus
    StartDate: DateTime option
    EndDate: DateTime option
    Friends: FriendId list
    Notes: string option
    CreatedAt: DateTime
    IsDefault: bool
}

/// A watch session for a movie (tracking when and with whom you watched)
type MovieWatchSession = {
    Id: SessionId
    EntryId: EntryId
    WatchedDate: DateTime
    Friends: FriendId list
    Name: string option  // Optional session name like "Movie night with Sarah"
    CreatedAt: DateTime
}

/// Tracks individual episode watch status (always tied to a session)
type EpisodeProgress = {
    EntryId: EntryId
    SessionId: SessionId
    SeriesId: SeriesId
    SeasonNumber: int
    EpisodeNumber: int
    IsWatched: bool
    WatchedDate: DateTime option
}

// =====================================
// Statistics Types
// =====================================

/// Watch time statistics
type WatchTimeStats = {
    TotalMinutes: int
    MovieMinutes: int
    SeriesMinutes: int
    ByYear: Map<int, int>
    ByRating: Map<PersonalRating, int>
}

/// Backlog statistics
type BacklogStats = {
    TotalEntries: int
    EstimatedMinutes: int
    OldestEntry: LibraryEntry option
}

/// Year in review summary
type YearInReview = {
    Year: int
    TotalMinutes: int
    TotalMovies: int
    TotalSeries: int
    TotalEpisodes: int
    RatingDistribution: Map<PersonalRating, int>
    CompletedCollections: Collection list
    NewContributorsDiscovered: Contributor list
    MostWatchedWith: (Friend * int) list
    GeneratedAt: DateTime
}

/// Filmography progress for a contributor
type FilmographyProgress = {
    Contributor: Contributor
    TotalWorks: int
    SeenWorks: int
    CompletionPercentage: float
    SeenList: LibraryEntry list
    UnseenList: TmdbWork list
}

// =====================================
// TMDB Types
// =====================================

/// Work in a filmography (from TMDB)
and TmdbWork = {
    TmdbId: int
    MediaType: MediaType
    Title: string
    ReleaseDate: DateTime option
    PosterPath: string option
    Role: ContributorRole
}

/// Search result from TMDB
type TmdbSearchResult = {
    TmdbId: int
    MediaType: MediaType
    Title: string
    ReleaseDate: DateTime option
    PosterPath: string option
    Overview: string option
    VoteAverage: float option
}

/// Cast member from TMDB
type TmdbCastMember = {
    TmdbPersonId: TmdbPersonId
    Name: string
    Character: string option
    ProfilePath: string option
    Order: int
}

/// Crew member from TMDB
type TmdbCrewMember = {
    TmdbPersonId: TmdbPersonId
    Name: string
    Department: string
    Job: string
    ProfilePath: string option
}

/// Full movie details from TMDB
type TmdbMovieDetails = {
    TmdbId: TmdbMovieId
    Title: string
    OriginalTitle: string option
    Overview: string option
    ReleaseDate: DateTime option
    RuntimeMinutes: int option
    PosterPath: string option
    BackdropPath: string option
    Genres: string list
    OriginalLanguage: string option
    VoteAverage: float option
    VoteCount: int option
    Tagline: string option
    ImdbId: string option
    Cast: TmdbCastMember list
    Crew: TmdbCrewMember list
}

/// Full series details from TMDB
type TmdbSeriesDetails = {
    TmdbId: TmdbSeriesId
    Name: string
    OriginalName: string option
    Overview: string option
    FirstAirDate: DateTime option
    LastAirDate: DateTime option
    PosterPath: string option
    BackdropPath: string option
    Genres: string list
    OriginalLanguage: string option
    VoteAverage: float option
    VoteCount: int option
    Status: string
    NumberOfSeasons: int
    NumberOfEpisodes: int
    EpisodeRunTimeMinutes: int option
    Seasons: TmdbSeasonSummary list
    Cast: TmdbCastMember list
    Crew: TmdbCrewMember list
}

/// Season summary from TMDB series details
and TmdbSeasonSummary = {
    SeasonNumber: int
    Name: string option
    Overview: string option
    PosterPath: string option
    AirDate: DateTime option
    EpisodeCount: int
}

/// Full season details from TMDB
type TmdbSeasonDetails = {
    TmdbSeriesId: TmdbSeriesId
    SeasonNumber: int
    Name: string option
    Overview: string option
    PosterPath: string option
    AirDate: DateTime option
    Episodes: TmdbEpisodeSummary list
}

/// Episode summary from TMDB season details
and TmdbEpisodeSummary = {
    EpisodeNumber: int
    Name: string
    Overview: string option
    AirDate: DateTime option
    RuntimeMinutes: int option
    StillPath: string option
}

/// Person details from TMDB
type TmdbPersonDetails = {
    TmdbPersonId: TmdbPersonId
    Name: string
    ProfilePath: string option
    KnownForDepartment: string option
    Birthday: DateTime option
    Deathday: DateTime option
    PlaceOfBirth: string option
    Biography: string option
}

/// Filmography from TMDB
type TmdbFilmography = {
    PersonId: TmdbPersonId
    CastCredits: TmdbWork list
    CrewCredits: TmdbWork list
}

/// Credits (cast and crew) from TMDB
type TmdbCredits = {
    Cast: TmdbCastMember list
    Crew: TmdbCrewMember list
}

/// TMDB collection (e.g., all Marvel movies)
type TmdbCollection = {
    TmdbCollectionId: int
    Name: string
    Overview: string option
    PosterPath: string option
    BackdropPath: string option
    Parts: TmdbSearchResult list
}

// =====================================
// Request/Response Types
// =====================================

/// Request to add a movie to the library
type AddMovieRequest = {
    TmdbId: TmdbMovieId
    WhyAdded: WhyAdded option
    InitialFriends: FriendId list
}

/// Request to add a series to the library
type AddSeriesRequest = {
    TmdbId: TmdbSeriesId
    WhyAdded: WhyAdded option
    InitialFriends: FriendId list
}

/// Request to update a library entry
type UpdateEntryRequest = {
    Id: EntryId
    WatchStatus: WatchStatus option
    PersonalRating: PersonalRating option
    Notes: string option
    IsFavorite: bool option
    Friends: FriendId list option
    WhyAdded: WhyAdded option
}

/// Request to create a friend
type CreateFriendRequest = {
    Name: string
    Nickname: string option
}

/// Request to update a friend
type UpdateFriendRequest = {
    Id: FriendId
    Name: string option
    Nickname: string option
    /// Base64 encoded avatar image (PNG/JPG), None = keep existing, Some "" = remove
    AvatarBase64: string option
}

/// Request to create a collection
type CreateCollectionRequest = {
    Name: string
    Description: string option
    /// Base64 encoded logo image (PNG/JPG)
    LogoBase64: string option
}

/// Request to update a collection
type UpdateCollectionRequest = {
    Id: CollectionId
    Name: string option
    Description: string option
    /// Base64 encoded logo image (PNG/JPG), None = keep existing, Some "" = remove
    LogoBase64: string option
}

/// Request to create a watch session (requires at least one friend)
type CreateSessionRequest = {
    EntryId: EntryId
    Friends: FriendId list
}

/// Request to create a movie watch session
type CreateMovieWatchSessionRequest = {
    EntryId: EntryId
    WatchedDate: DateTime
    Friends: FriendId list
    Name: string option
}

/// Request to update a movie watch session's date
type UpdateMovieWatchSessionDateRequest = {
    SessionId: SessionId
    NewDate: DateTime
}

/// Request to update a movie watch session (full update: date, friends, name)
type UpdateMovieWatchSessionRequest = {
    SessionId: SessionId
    WatchedDate: DateTime
    Friends: FriendId list
    Name: string option
}

/// Request to abandon an entry
type AbandonRequest = {
    Reason: string option
    AbandonedAtSeason: int option
    AbandonedAtEpisode: int option
}

/// Request to update a watch session
type UpdateSessionRequest = {
    Id: SessionId
    Notes: string option
    Status: SessionStatus option
}

/// Request to track a contributor
type TrackContributorRequest = {
    TmdbPersonId: TmdbPersonId
    Name: string
    ProfilePath: string option
    KnownForDepartment: string option
    Notes: string option
}

/// Request to update a tracked contributor's notes
type UpdateTrackedContributorRequest = {
    Id: TrackedContributorId
    Notes: string option
}

// =====================================
// Filter/Sort Types
// =====================================

/// Filter criteria for library entries
type LibraryFilter = {
    MediaType: MediaType option
    WatchStatus: WatchStatus option
    MinRating: PersonalRating option
    MaxRating: PersonalRating option
    Friends: FriendId list
    SearchQuery: string option
    DateAddedFrom: DateTime option
    DateAddedTo: DateTime option
    YearFrom: int option
    YearTo: int option
    IsFavorite: bool option
}

/// Sort options for library entries
type LibrarySort =
    | DateAddedDesc
    | DateAddedAsc
    | TitleAsc
    | TitleDesc
    | ReleaseYearDesc
    | ReleaseYearAsc
    | RatingDesc
    | RatingAsc
    | LastWatchedDesc

/// Paginated request
type PagedRequest<'TFilter> = {
    Filter: 'TFilter
    Sort: LibrarySort
    Page: int
    PageSize: int
}

/// Paginated response
type PagedResponse<'T> = {
    Items: 'T list
    TotalCount: int
    Page: int
    PageSize: int
    TotalPages: int
    HasNextPage: bool
    HasPreviousPage: bool
}

// =====================================
// Timeline Types
// =====================================

/// Filter for timeline queries
type TimelineFilter = {
    StartDate: DateTime option
    EndDate: DateTime option
    MediaType: MediaType option
    EntryId: EntryId option
}

/// Detail of what was watched
type TimelineDetail =
    | MovieWatched
    | EpisodeWatched of seasonNumber: int * episodeNumber: int
    | SeasonCompleted of seasonNumber: int
    | SeriesCompleted

/// A single timeline entry
type TimelineEntry = {
    WatchedDate: DateTime
    Entry: LibraryEntry
    Detail: TimelineDetail
}

// =====================================
// Graph Types
// =====================================

/// A node in the relationship graph
type GraphNode =
    | MovieNode of EntryId * title: string * posterPath: string option
    | SeriesNode of EntryId * name: string * posterPath: string option
    | FriendNode of FriendId * name: string
    | ContributorNode of ContributorId * name: string * profilePath: string option

/// Type of relationship between nodes
type EdgeRelationship =
    | WatchedWith
    | WorkedOn of ContributorRole
    | InCollection of CollectionId

/// An edge in the relationship graph
type GraphEdge = {
    Source: GraphNode
    Target: GraphNode
    Relationship: EdgeRelationship
}

/// Full graph data
type RelationshipGraph = {
    Nodes: GraphNode list
    Edges: GraphEdge list
}

/// Filter options for graph
type GraphFilter = {
    IncludeMovies: bool
    IncludeSeries: bool
    IncludeFriends: bool
    IncludeContributors: bool
    IncludeGenres: bool
    MaxNodes: int option
    WatchStatusFilter: WatchStatus list option
}

// =====================================
// Statistics Response Types
// =====================================

/// Statistics about a friend
type FriendStats = {
    TotalEntriesTogether: int
    TotalSessionsTogether: int
    MostWatchedGenres: string list
    FirstWatchedTogether: DateTime option
    LastWatchedTogether: DateTime option
}

/// Display data for a collection item (resolved from CollectionItemRef)
type CollectionItemDisplay =
    | EntryDisplay of LibraryEntry
    | SeasonDisplay of series: Series * season: TmdbSeasonSummary
    | EpisodeDisplay of series: Series * season: TmdbSeasonSummary * episode: TmdbEpisodeSummary

/// Collection with its items and resolved display data
type CollectionWithItems = {
    Collection: Collection
    Items: (CollectionItem * CollectionItemDisplay) list
}

/// Progress information for a collection
type CollectionProgress = {
    CollectionId: CollectionId
    TotalItems: int
    CompletedItems: int
    InProgressItems: int
    TotalMinutes: int
    WatchedMinutes: int
    CompletionPercentage: float
}

/// Session with full progress information
type WatchSessionWithProgress = {
    Session: WatchSession
    Entry: LibraryEntry
    EpisodeProgress: EpisodeProgress list
    TotalEpisodes: int
    WatchedEpisodes: int
    CompletionPercentage: float
}

/// Insights for the dashboard
type DashboardInsights = {
    NearCompletions: FilmographyProgress list
    ThisMonthMinutes: int
    LastMonthMinutes: int
    OldestUnwatched: LibraryEntry option
    AlmostDoneSeries: (LibraryEntry * float) list
}

// =====================================
// Import Types
// =====================================

/// Trakt auth URL for OAuth
type TraktAuthUrl = {
    Url: string
    State: string
}

/// Preview of what will be imported
type TraktImportPreview = {
    Movies: TmdbSearchResult list
    Series: TmdbSearchResult list
    TotalItems: int
    AlreadyInLibrary: int
    NewItems: int
}

/// Options for import
type TraktImportOptions = {
    ImportWatchedMovies: bool
    ImportWatchedSeries: bool
    ImportRatings: bool
    ImportWatchlist: bool
}

/// Result of import operation
type TraktImportResult = {
    MoviesImported: int
    SeriesImported: int
    RatingsImported: int
    Errors: string list
}

/// Current import status
type ImportStatus = {
    InProgress: bool
    CurrentItem: string option
    Completed: int
    Total: int
    Errors: string list
}

// =====================================
// Cache Management Types
// =====================================

/// A cached TMDB response entry
type CacheEntry = {
    CacheKey: string
    ExpiresAt: DateTime
    SizeBytes: int
}

/// Summary of cache statistics
type CacheStats = {
    TotalEntries: int
    TotalSizeBytes: int
    ExpiredEntries: int
    EntriesByType: Map<string, int>
}

/// Result of clearing the cache
type ClearCacheResult = {
    EntriesRemoved: int
    BytesFreed: int
}

// =====================================
// URL Slug Utilities
// =====================================

/// Utilities for generating and matching URL slugs
module Slug =
    open System.Text.RegularExpressions

    /// Generate a URL-friendly slug from text
    let generate (text: string) : string =
        text.ToLowerInvariant()
        |> fun s -> Regex.Replace(s, @"[^a-z0-9\s-]", "")
        |> fun s -> Regex.Replace(s, @"\s+", "-")
        |> fun s -> Regex.Replace(s, @"-+", "-")
        |> fun s -> s.Trim('-')
        |> fun s -> if String.IsNullOrEmpty(s) then "item" else s

    /// Generate slug for a movie (includes year to avoid duplicates)
    let forMovie (title: string) (releaseDate: DateTime option) : string =
        let year = releaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
        let base' = generate title
        if String.IsNullOrEmpty(year) then base' else $"{base'}-{year}"

    /// Generate slug for a series (includes year to avoid duplicates)
    let forSeries (name: string) (firstAirDate: DateTime option) : string =
        let year = firstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
        let base' = generate name
        if String.IsNullOrEmpty(year) then base' else $"{base'}-{year}"

    /// Generate slug for a friend
    let forFriend (name: string) : string = generate name

    /// Generate slug for a collection
    let forCollection (name: string) : string = generate name

    /// Generate slug for a contributor
    let forContributor (name: string) : string = generate name

    /// Generate slug for a session (based on series name and session number or date)
    let forSession (seriesName: string) (sessionIndex: int) : string =
        let base' = generate seriesName
        $"{base'}-session-{sessionIndex}"

    /// Generate a unique slug by appending _1, _2, etc. if the base slug is taken
    let makeUnique (baseSlug: string) (existingSlugs: string list) : string =
        if not (List.contains baseSlug existingSlugs) then
            baseSlug
        else
            let rec findAvailable n =
                let candidate = $"{baseSlug}_{n}"
                if List.contains candidate existingSlugs then
                    findAvailable (n + 1)
                else
                    candidate
            findAvailable 1

    /// Check if a slug matches (case-insensitive)
    let matches (slug: string) (target: string) : bool =
        String.Equals(slug, target, StringComparison.OrdinalIgnoreCase)

    /// Extract base slug and suffix from a slug (e.g., "my-collection_2" -> ("my-collection", Some 2))
    let parseSlugWithSuffix (slug: string) : string * int option =
        let regex = Regex(@"^(.+)_(\d+)$")
        let m = regex.Match(slug)
        if m.Success then
            (m.Groups.[1].Value, Some (int m.Groups.[2].Value))
        else
            (slug, None)

    /// Given items sorted by ID (oldest first), compute unique slug for item at given index
    /// Index 0 gets base slug, index 1 gets base_1, index 2 gets base_2, etc.
    let slugForIndex (baseSlug: string) (index: int) : string =
        if index = 0 then baseSlug
        else $"{baseSlug}_{index}"

    /// Find the index of an item in a duplicate group based on slug suffix
    /// Returns None for base slug (index 0), Some n for _n suffix
    let indexFromSlug (slug: string) : int =
        match parseSlugWithSuffix slug with
        | (_, None) -> 0
        | (_, Some n) -> n
