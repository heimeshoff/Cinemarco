# Cinemarco API Contract Specification

This document specifies all Fable.Remoting API contracts for Cinemarco. These interfaces will be defined in `src/Shared/Api.fs`.

## API Design Principles

1. **Type-safe contracts** - All operations defined as F# interfaces
2. **Result types for commands** - Operations that can fail return `Result<'T, string>`
3. **Direct return for queries** - Simple reads return data directly or Option
4. **Async everywhere** - All operations are async
5. **Grouped by domain** - APIs organized by feature area

---

## API Interfaces Overview

```
IAppApi (root)
├── Library: ILibraryApi
├── Tmdb: ITmdbApi
├── Friends: IFriendApi
├── Tags: ITagApi
├── Collections: ICollectionApi
├── Sessions: ISessionApi
├── Contributors: IContributorApi
├── Stats: IStatsApi
├── Import: IImportApi
└── Graph: IGraphApi
```

---

## Library API

Core library operations:

```fsharp
/// API for library entry operations
type ILibraryApi = {
    /// Get all library entries (paginated)
    getEntries: PagedRequest<LibraryFilter> -> Async<PagedResponse<LibraryEntry>>

    /// Get a single library entry by ID
    getEntry: EntryId -> Async<Result<LibraryEntry, string>>

    /// Get entries by watch status
    getByStatus: WatchStatus -> Async<LibraryEntry list>

    /// Get recent entries (for home page)
    getRecent: count: int -> Async<LibraryEntry list>

    /// Get entries currently in progress
    getInProgress: unit -> Async<LibraryEntry list>

    /// Get up next / watchlist entries
    getUpNext: count: int -> Async<LibraryEntry list>

    /// Get favorite entries
    getFavorites: unit -> Async<LibraryEntry list>

    /// Add a movie to the library
    addMovie: AddMovieRequest -> Async<Result<LibraryEntry, string>>

    /// Add a series to the library
    addSeries: AddSeriesRequest -> Async<Result<LibraryEntry, string>>

    /// Update a library entry
    updateEntry: UpdateEntryRequest -> Async<Result<LibraryEntry, string>>

    /// Delete a library entry
    deleteEntry: EntryId -> Async<Result<unit, string>>

    /// Mark a movie as watched
    markMovieWatched: EntryId -> watchedDate: DateTime option -> Async<Result<LibraryEntry, string>>

    /// Mark a movie as not watched
    markMovieUnwatched: EntryId -> Async<Result<LibraryEntry, string>>

    /// Update episode progress for a series
    updateEpisodeProgress: EntryId -> seasonNumber: int -> episodeNumber: int -> watched: bool -> Async<Result<unit, string>>

    /// Mark entire season as watched
    markSeasonWatched: EntryId -> seasonNumber: int -> Async<Result<unit, string>>

    /// Mark entire series as watched
    markSeriesWatched: EntryId -> Async<Result<LibraryEntry, string>>

    /// Abandon an entry
    abandonEntry: EntryId -> AbandonedInfo -> Async<Result<LibraryEntry, string>>

    /// Rate an entry
    rateEntry: EntryId -> PersonalRating option -> Async<Result<LibraryEntry, string>>

    /// Toggle favorite status
    toggleFavorite: EntryId -> Async<Result<LibraryEntry, string>>

    /// Add tags to an entry
    addTags: EntryId -> TagId list -> Async<Result<LibraryEntry, string>>

    /// Remove tags from an entry
    removeTags: EntryId -> TagId list -> Async<Result<LibraryEntry, string>>

    /// Add friends to an entry
    addFriends: EntryId -> FriendId list -> Async<Result<LibraryEntry, string>>

    /// Remove friends from an entry
    removeFriends: EntryId -> FriendId list -> Async<Result<LibraryEntry, string>>

    /// Search library entries
    search: query: string -> Async<LibraryEntry list>

    /// Check if a TMDB movie is already in library
    isMovieInLibrary: TmdbMovieId -> Async<EntryId option>

    /// Check if a TMDB series is already in library
    isSeriesInLibrary: TmdbSeriesId -> Async<EntryId option>
}
```

---

## TMDB API

External data fetching:

```fsharp
/// API for TMDB operations
type ITmdbApi = {
    /// Search for movies
    searchMovies: query: string -> Async<TmdbSearchResult list>

    /// Search for series
    searchSeries: query: string -> Async<TmdbSearchResult list>

    /// Search both movies and series
    searchAll: query: string -> Async<TmdbSearchResult list>

    /// Get full movie details
    getMovieDetails: TmdbMovieId -> Async<Result<TmdbMovieDetails, string>>

    /// Get full series details (with seasons/episodes)
    getSeriesDetails: TmdbSeriesId -> Async<Result<TmdbSeriesDetails, string>>

    /// Get season details with episodes
    getSeasonDetails: TmdbSeriesId -> seasonNumber: int -> Async<Result<TmdbSeasonDetails, string>>

    /// Get person details
    getPersonDetails: TmdbPersonId -> Async<Result<TmdbPersonDetails, string>>

    /// Get person's filmography
    getPersonFilmography: TmdbPersonId -> Async<Result<TmdbFilmography, string>>

    /// Get movie credits (cast and crew)
    getMovieCredits: TmdbMovieId -> Async<Result<TmdbCredits, string>>

    /// Get series credits (cast and crew)
    getSeriesCredits: TmdbSeriesId -> Async<Result<TmdbCredits, string>>

    /// Get TMDB collection (e.g., all Marvel movies)
    getTmdbCollection: tmdbCollectionId: int -> Async<Result<TmdbCollection, string>>

    /// Get trending movies
    getTrendingMovies: unit -> Async<TmdbSearchResult list>

    /// Get trending series
    getTrendingSeries: unit -> Async<TmdbSearchResult list>
}
```

---

## Friends API

```fsharp
/// API for friend operations
type IFriendApi = {
    /// Get all friends
    getAll: unit -> Async<Friend list>

    /// Get a friend by ID
    getById: FriendId -> Async<Result<Friend, string>>

    /// Create a new friend
    create: CreateFriendRequest -> Async<Result<Friend, string>>

    /// Update a friend
    update: UpdateFriendRequest -> Async<Result<Friend, string>>

    /// Delete a friend
    delete: FriendId -> Async<Result<unit, string>>

    /// Get entries watched with a friend
    getWatchedWith: FriendId -> Async<LibraryEntry list>

    /// Get watch sessions with a friend
    getSessionsWith: FriendId -> Async<WatchSession list>

    /// Search friends by name
    search: query: string -> Async<Friend list>

    /// Get friend statistics (count of entries, sessions, etc.)
    getStats: FriendId -> Async<FriendStats>
}

/// Statistics about a friend
type FriendStats = {
    TotalEntriesTogether: int
    TotalSessionsTogether: int
    MostWatchedGenres: string list
    FirstWatchedTogether: DateTime option
    LastWatchedTogether: DateTime option
}
```

---

## Tags API

```fsharp
/// API for tag operations
type ITagApi = {
    /// Get all tags
    getAll: unit -> Async<Tag list>

    /// Get a tag by ID
    getById: TagId -> Async<Result<Tag, string>>

    /// Create a new tag
    create: CreateTagRequest -> Async<Result<Tag, string>>

    /// Update a tag
    update: UpdateTagRequest -> Async<Result<Tag, string>>

    /// Delete a tag
    delete: TagId -> Async<Result<unit, string>>

    /// Get all entries with a tag
    getTaggedEntries: TagId -> Async<LibraryEntry list>

    /// Get sessions with a tag
    getTaggedSessions: TagId -> Async<WatchSession list>

    /// Get tag usage statistics
    getStats: TagId -> Async<TagStats>

    /// Search tags by name
    search: query: string -> Async<Tag list>

    /// Get most used tags
    getMostUsed: count: int -> Async<(Tag * int) list>
}

/// Statistics about a tag
type TagStats = {
    TotalEntries: int
    TotalSessions: int
    TotalWatchTimeMinutes: int
    RatingDistribution: Map<PersonalRating, int>
}
```

---

## Collections API

```fsharp
/// API for collection operations
type ICollectionApi = {
    /// Get all collections
    getAll: unit -> Async<Collection list>

    /// Get a collection by ID with items
    getById: CollectionId -> Async<Result<CollectionWithItems, string>>

    /// Create a new collection
    create: CreateCollectionRequest -> Async<Result<Collection, string>>

    /// Update a collection
    update: UpdateCollectionRequest -> Async<Result<Collection, string>>

    /// Delete a collection
    delete: CollectionId -> Async<Result<unit, string>>

    /// Add an entry to a collection
    addItem: CollectionId -> EntryId -> position: int option -> Async<Result<unit, string>>

    /// Remove an entry from a collection
    removeItem: CollectionId -> EntryId -> Async<Result<unit, string>>

    /// Reorder items in a collection
    reorderItems: CollectionId -> orderedEntryIds: EntryId list -> Async<Result<unit, string>>

    /// Get collection progress (how many items completed)
    getProgress: CollectionId -> Async<CollectionProgress>

    /// Get public franchise collections (MCU, Star Wars, etc.)
    getPublicFranchises: unit -> Async<Collection list>

    /// Import a TMDB collection as a local collection
    importTmdbCollection: tmdbCollectionId: int -> Async<Result<Collection, string>>
}

/// Collection with its items
type CollectionWithItems = {
    Collection: Collection
    Items: (CollectionItem * LibraryEntry) list
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
```

---

## Sessions API

```fsharp
/// API for watch session operations
type ISessionApi = {
    /// Get all sessions
    getAll: unit -> Async<WatchSession list>

    /// Get sessions for a specific entry
    getByEntry: EntryId -> Async<WatchSession list>

    /// Get a session by ID
    getById: SessionId -> Async<Result<WatchSessionWithProgress, string>>

    /// Create a new session
    create: CreateSessionRequest -> Async<Result<WatchSession, string>>

    /// Update a session
    update: UpdateSessionRequest -> Async<Result<WatchSession, string>>

    /// Delete a session
    delete: SessionId -> Async<Result<unit, string>>

    /// Get episode progress for a session
    getEpisodeProgress: SessionId -> Async<EpisodeProgress list>

    /// Update episode watched status for a session
    updateEpisodeProgress: SessionId -> seasonNumber: int -> episodeNumber: int -> watched: bool -> Async<Result<unit, string>>

    /// Mark session as completed
    completeSession: SessionId -> Async<Result<WatchSession, string>>

    /// Pause a session
    pauseSession: SessionId -> Async<Result<WatchSession, string>>

    /// Resume a session
    resumeSession: SessionId -> Async<Result<WatchSession, string>>

    /// Get active sessions
    getActive: unit -> Async<WatchSession list>
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
```

---

## Contributors API

```fsharp
/// API for contributor operations
type IContributorApi = {
    /// Get a contributor by ID
    getById: ContributorId -> Async<Result<Contributor, string>>

    /// Get contributor by TMDB ID (fetches if not cached)
    getByTmdbId: TmdbPersonId -> Async<Result<Contributor, string>>

    /// Get filmography progress for a contributor
    getFilmographyProgress: ContributorId -> Async<Result<FilmographyProgress, string>>

    /// Get contributors for a library entry
    getForEntry: EntryId -> Async<MediaContributor list>

    /// Search contributors in library
    search: query: string -> Async<Contributor list>

    /// Get directors with most entries in library
    getTopDirectors: count: int -> Async<(Contributor * int) list>

    /// Get actors with most entries in library
    getTopActors: count: int -> Async<(Contributor * int) list>

    /// Get discovery suggestions (contributors with high completion %)
    getDiscoverySuggestions: unit -> Async<FilmographyProgress list>
}
```

---

## Stats API

```fsharp
/// API for statistics operations
type IStatsApi = {
    /// Get overall watch time statistics
    getWatchTimeStats: unit -> Async<WatchTimeStats>

    /// Get backlog statistics
    getBacklogStats: unit -> Async<BacklogStats>

    /// Get year in review
    getYearInReview: year: int -> Async<Result<YearInReview, string>>

    /// Get all available years for year-in-review
    getAvailableYears: unit -> Async<int list>

    /// Get dashboard insights (for home page)
    getDashboardInsights: unit -> Async<DashboardInsights>

    /// Force recalculate statistics (admin)
    recalculateStats: unit -> Async<unit>
}

/// Insights for the dashboard
type DashboardInsights = {
    /// "You're one film away from completing Denis Villeneuve"
    NearCompletions: FilmographyProgress list

    /// Total watch time this month
    ThisMonthMinutes: int

    /// Comparison to last month
    LastMonthMinutes: int

    /// Longest unwatched entry in library
    OldestUnwatched: LibraryEntry option

    /// Series closest to completion
    AlmostDoneSeries: (LibraryEntry * float) list  // Entry and percentage
}
```

---

## Timeline API

```fsharp
/// API for timeline operations (part of Stats or separate)
type ITimelineApi = {
    /// Get timeline entries (paginated)
    getTimeline: PagedRequest<TimelineFilter> -> Async<PagedResponse<TimelineEntry>>

    /// Get timeline entries for a date range
    getByDateRange: startDate: DateTime -> endDate: DateTime -> Async<TimelineEntry list>

    /// Get timeline grouped by month
    getByMonth: year: int -> month: int -> Async<TimelineEntry list>
}

/// Filter for timeline queries
type TimelineFilter = {
    StartDate: DateTime option
    EndDate: DateTime option
    MediaType: MediaType option
    EntryId: EntryId option
}

/// A single timeline entry
type TimelineEntry = {
    WatchedDate: DateTime
    Entry: LibraryEntry
    Detail: TimelineDetail
}

/// Detail of what was watched
type TimelineDetail =
    | MovieWatched
    | EpisodeWatched of seasonNumber: int * episodeNumber: int
    | SeasonCompleted of seasonNumber: int
    | SeriesCompleted
```

---

## Graph API

```fsharp
/// API for relationship graph
type IGraphApi = {
    /// Get full graph data
    getFullGraph: unit -> Async<RelationshipGraph>

    /// Get graph centered on an entry
    getEntryGraph: EntryId -> depth: int -> Async<RelationshipGraph>

    /// Get graph centered on a friend
    getFriendGraph: FriendId -> Async<RelationshipGraph>

    /// Get graph centered on a contributor
    getContributorGraph: ContributorId -> Async<RelationshipGraph>

    /// Get graph centered on a tag
    getTagGraph: TagId -> Async<RelationshipGraph>

    /// Get filtered graph
    getFilteredGraph: GraphFilter -> Async<RelationshipGraph>
}

/// Filter options for graph
type GraphFilter = {
    IncludeMovies: bool
    IncludeSeries: bool
    IncludeFriends: bool
    IncludeContributors: bool
    IncludeTags: bool
    IncludeGenres: bool
    MaxNodes: int option
    WatchStatusFilter: WatchStatus list option
}
```

---

## Import API

```fsharp
/// API for import operations
type IImportApi = {
    /// Initialize Trakt OAuth flow
    initTraktAuth: unit -> Async<Result<TraktAuthUrl, string>>

    /// Complete Trakt OAuth with code
    completeTraktAuth: code: string -> Async<Result<unit, string>>

    /// Get Trakt import preview (what will be imported)
    getTraktImportPreview: unit -> Async<Result<TraktImportPreview, string>>

    /// Execute Trakt import
    executeTraktImport: options: TraktImportOptions -> Async<Result<TraktImportResult, string>>

    /// Get import status/progress
    getImportStatus: unit -> Async<ImportStatus option>
}

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
```

---

## Root API Interface

```fsharp
/// Main API interface combining all sub-APIs
type IAppApi = {
    Library: ILibraryApi
    Tmdb: ITmdbApi
    Friends: IFriendApi
    Tags: ITagApi
    Collections: ICollectionApi
    Sessions: ISessionApi
    Contributors: IContributorApi
    Stats: IStatsApi
    Timeline: ITimelineApi
    Graph: IGraphApi
    Import: IImportApi
}
```

---

## Request/Response Types Summary

### Create Requests

```fsharp
type CreateFriendRequest = {
    Name: string
    Nickname: string option
    Notes: string option
}

type CreateTagRequest = {
    Name: string
    Color: string option
    Description: string option
}

type CreateCollectionRequest = {
    Name: string
    Description: string option
}

type CreateSessionRequest = {
    EntryId: EntryId
    Name: string
    Friends: FriendId list
    Tags: TagId list
}
```

### Update Requests

```fsharp
type UpdateFriendRequest = {
    Id: FriendId
    Name: string option
    Nickname: string option
    Notes: string option
}

type UpdateTagRequest = {
    Id: TagId
    Name: string option
    Color: string option
    Description: string option
}

type UpdateCollectionRequest = {
    Id: CollectionId
    Name: string option
    Description: string option
}

type UpdateSessionRequest = {
    Id: SessionId
    Name: string option
    Notes: string option
    Status: SessionStatus option
}

type UpdateEntryRequest = {
    Id: EntryId
    WatchStatus: WatchStatus option
    PersonalRating: PersonalRating option
    Notes: string option
    IsFavorite: bool option
    WhyAdded: WhyAdded option
}
```

### Filter/Sort Types

```fsharp
type LibraryFilter = {
    MediaType: MediaType option
    WatchStatus: WatchStatus option
    MinRating: PersonalRating option
    MaxRating: PersonalRating option
    Tags: TagId list
    Friends: FriendId list
    SearchQuery: string option
    DateAddedFrom: DateTime option
    DateAddedTo: DateTime option
    YearFrom: int option
    YearTo: int option
    IsFavorite: bool option
}

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

type PagedRequest<'TFilter> = {
    Filter: 'TFilter
    Sort: LibrarySort
    Page: int
    PageSize: int
}

type PagedResponse<'T> = {
    Items: 'T list
    TotalCount: int
    Page: int
    PageSize: int
    TotalPages: int
    HasNextPage: bool
    HasPreviousPage: bool
}
```

---

## Error Handling

All commands return `Result<'T, string>` for consistent error handling:

```fsharp
/// Common error messages
module Errors =
    let notFound entity id = $"{entity} with ID {id} not found"
    let alreadyExists entity = $"{entity} already exists"
    let validationFailed errors = $"Validation failed: {String.concat ", " errors}"
    let tmdbError msg = $"TMDB API error: {msg}"
    let databaseError msg = $"Database error: {msg}"
```

---

## Route Builder

All APIs use consistent route building:

```fsharp
// Client-side (src/Client/Api.fs)
let api =
    Remoting.createApi()
    |> Remoting.withRouteBuilder (fun typeName methodName -> $"/api/{typeName}/{methodName}")
    |> Remoting.buildProxy<IAppApi>

// Server-side (src/Server/Api.fs)
let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder (fun typeName methodName -> $"/api/{typeName}/{methodName}")
    |> Remoting.fromValue appApi
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.buildHttpHandler
```

---

## Notes for Implementation

1. **Implement incrementally** - Start with ILibraryApi and ITmdbApi
2. **Use Result for all mutations** - Get, list operations can return directly
3. **Cache TMDB responses** - Reduce API calls
4. **Validate on server** - Never trust client input
5. **Log errors** - But return friendly messages to client
6. **Test each API** - Write tests for all operations
7. **Keep APIs focused** - One responsibility per API interface
