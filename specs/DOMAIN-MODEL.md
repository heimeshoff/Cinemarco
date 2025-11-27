# Cinemarco Domain Model Specification

This document specifies all domain types for Cinemarco. These types will be defined in `src/Shared/Domain.fs` and shared between client and server.

## Design Principles

1. **Immutable records** - All types are F# records
2. **Discriminated unions** - For enumerations and variants
3. **Single-case unions** - For type-safe IDs
4. **Result types** - For fallible operations
5. **Option types** - For optional data
6. **No nullable reference types** - Use Option instead

---

## Core Identifiers

Type-safe IDs to prevent mixing up different entity types:

```fsharp
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

/// Unique identifier for tags
type TagId = TagId of int

/// Unique identifier for collections
type CollectionId = CollectionId of int

/// Unique identifier for watch sessions
type SessionId = SessionId of int

/// Unique identifier for contributors (actors, directors, etc.)
type ContributorId = ContributorId of int

// Module helpers for ID manipulation
module MovieId =
    let create id = MovieId id
    let value (MovieId id) = id

module SeriesId =
    let create id = SeriesId id
    let value (SeriesId id) = id

// ... similar modules for other IDs
```

---

## Media Types

### MediaType

```fsharp
/// Discriminates between movies and series
type MediaType =
    | Movie
    | Series
```

### Movie

```fsharp
/// A movie with TMDB data and local metadata
type Movie = {
    Id: MovieId
    TmdbId: TmdbMovieId
    Title: string
    OriginalTitle: string option
    Overview: string option
    ReleaseDate: DateTime option
    RuntimeMinutes: int option
    PosterPath: string option       // TMDB poster path
    BackdropPath: string option     // TMDB backdrop path
    Genres: string list
    OriginalLanguage: string option
    VoteAverage: float option       // TMDB rating
    VoteCount: int option
    Tagline: string option
    ImdbId: string option
    CreatedAt: DateTime             // When added to our DB
    UpdatedAt: DateTime
}
```

### Series

```fsharp
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
    EpisodeRunTimeMinutes: int option  // Average episode length
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// Status of a TV series
type SeriesStatus =
    | Returning
    | Ended
    | Canceled
    | InProduction
    | Planned
    | Unknown
```

### Season

```fsharp
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
```

### Episode

```fsharp
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
    StillPath: string option        // Episode screenshot
}
```

---

## Library Entry

The wrapper that adds personal metadata to movies/series:

```fsharp
/// What type of media is in a library entry
type LibraryMedia =
    | LibraryMovie of Movie
    | LibrarySeries of Series

/// A personal library entry wrapping a movie or series
type LibraryEntry = {
    Id: EntryId
    Media: LibraryMedia
    WhyAdded: WhyAdded option           // Who recommended, context
    WatchStatus: WatchStatus
    PersonalRating: PersonalRating option
    DateAdded: DateTime
    DateFirstWatched: DateTime option
    DateLastWatched: DateTime option
    Notes: string option                 // Personal notes
    IsFavorite: bool
    Tags: TagId list
    Friends: FriendId list               // Friends associated with this entry
}

/// Captures why/how something was added to the library
type WhyAdded = {
    RecommendedBy: FriendId option       // Who recommended it
    RecommendedByName: string option     // Or just a name string
    Source: string option                // "Netflix", "Twitter thread", etc.
    Context: string option               // Free-form note
    DateRecommended: DateTime option
}
```

---

## Watch Status & Progress

```fsharp
/// Watch status for any media
type WatchStatus =
    | NotStarted
    | InProgress of WatchProgress
    | Completed
    | Abandoned of AbandonedInfo

/// Progress information for in-progress media
type WatchProgress = {
    // For movies: percentage or just "started"
    // For series: current season/episode
    CurrentSeason: int option
    CurrentEpisode: int option
    LastWatchedDate: DateTime option
}

/// Information about why/where something was abandoned
type AbandonedInfo = {
    AbandonedAt: WatchProgress option    // Where they stopped
    Reason: string option                // Optional reason
    AbandonedDate: DateTime option
}

/// Tracks individual episode watch status
type EpisodeProgress = {
    EntryId: EntryId
    SessionId: SessionId option          // Which watch session (null = default)
    SeriesId: SeriesId
    SeasonNumber: int
    EpisodeNumber: int
    IsWatched: bool
    WatchedDate: DateTime option
}
```

---

## Watch Sessions

For tracking rewatches and watching with different people:

```fsharp
/// A named watch session for a series
type WatchSession = {
    Id: SessionId
    EntryId: EntryId                     // Which library entry
    Name: string                         // "Rewatch with Sarah", "2024 rewatch"
    Status: SessionStatus
    StartDate: DateTime option
    EndDate: DateTime option
    Tags: TagId list
    Friends: FriendId list               // Who you're watching with
    Notes: string option
    CreatedAt: DateTime
}

/// Status of a watch session
type SessionStatus =
    | Active
    | Paused
    | SessionCompleted
```

---

## Personal Rating

The 5-tier rating system:

```fsharp
/// Personal rating scale
type PersonalRating =
    | Brilliant     // 5 - Absolutely brilliant, stays with you
    | ReallyGood    // 4 - Strong craft, enjoyable, recommendable
    | Decent        // 3 - Worth watching, even if not life-changing
    | Meh           // 2 - Didn't click, uninspiring
    | Nope          // 1 - Would not watch again

module PersonalRating =
    let toInt = function
        | Brilliant -> 5
        | ReallyGood -> 4
        | Decent -> 3
        | Meh -> 2
        | Nope -> 1

    let fromInt = function
        | 5 -> Some Brilliant
        | 4 -> Some ReallyGood
        | 3 -> Some Decent
        | 2 -> Some Meh
        | 1 -> Some Nope
        | _ -> None

    let description = function
        | Brilliant -> "Absolutely brilliant - This film stays with you."
        | ReallyGood -> "Really good - Strong craft, enjoyable, recommendable."
        | Decent -> "Decent - Worth watching, even if it won't change your life."
        | Meh -> "Meh - Didn't click; not terrible but uninspiring."
        | Nope -> "Nope - Would not watch again; time is precious."
```

---

## People

### Friends

People you know in real life:

```fsharp
/// A friend (real person you know)
type Friend = {
    Id: FriendId
    Name: string
    Nickname: string option
    AvatarUrl: string option
    Notes: string option
    CreatedAt: DateTime
}
```

### Contributors

People who make movies/series:

```fsharp
/// A contributor (actor, director, etc.)
type Contributor = {
    Id: ContributorId
    TmdbPersonId: TmdbPersonId
    Name: string
    ProfilePath: string option           // TMDB photo path
    KnownForDepartment: string option    // "Acting", "Directing", etc.
    Birthday: DateTime option
    Deathday: DateTime option
    PlaceOfBirth: string option
    Biography: string option
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// Role a contributor played in a movie/series
type ContributorRole =
    | Director
    | Actor of character: string option
    | Writer
    | Cinematographer
    | Composer
    | Producer
    | ExecutiveProducer
    | CreatedBy                           // For TV series creators
    | Other of department: string

/// Links a contributor to a movie/series
type MediaContributor = {
    ContributorId: ContributorId
    MovieId: MovieId option
    SeriesId: SeriesId option
    Role: ContributorRole
    Order: int option                     // Billing order for actors
}
```

---

## Tags & Collections

### Tags

Universal tags for organizing:

```fsharp
/// A user-defined tag
type Tag = {
    Id: TagId
    Name: string
    Color: string option                 // Hex color code
    Icon: string option                  // Emoji or icon name
    Description: string option
    CreatedAt: DateTime
}

/// Usage examples:
/// - Moods: "cozy", "intense", "feel-good"
/// - Contexts: "movie night", "solo sick day", "flight watch"
/// - Genres: "sci-fi", "documentary"
/// - Personal: "comfort rewatch", "favorites"
```

### Collections

Ordered lists (franchises, custom lists):

```fsharp
/// A curated, ordered collection
type Collection = {
    Id: CollectionId
    Name: string
    Description: string option
    CoverImagePath: string option
    IsPublicFranchise: bool              // MCU, Star Wars, etc.
    TmdbCollectionId: int option         // If from TMDB
    CreatedAt: DateTime
    UpdatedAt: DateTime
}

/// An item in a collection (ordered)
type CollectionItem = {
    CollectionId: CollectionId
    EntryId: EntryId
    Position: int                        // Order in collection
    Notes: string option                 // Per-item notes
}

/// Usage examples:
/// - MCU chronological order
/// - Star Wars timeline
/// - Ghibli marathon
/// - "2024 Oscar nominees"
/// - "Best of the decade"
```

---

## Statistics Types

```fsharp
/// Watch time statistics
type WatchTimeStats = {
    TotalMinutes: int
    MovieMinutes: int
    SeriesMinutes: int
    ByYear: Map<int, int>                // Year -> minutes
    ByTag: Map<TagId, int>               // Tag -> minutes
    ByRating: Map<PersonalRating, int>   // Rating -> minutes
}

/// Year in review summary
type YearInReview = {
    Year: int
    TotalMinutes: int
    TotalMovies: int
    TotalSeries: int
    TotalEpisodes: int
    RatingDistribution: Map<PersonalRating, int>
    TopTags: (Tag * int) list            // Tag and count
    CompletedCollections: Collection list
    NewContributorsDiscovered: Contributor list
    MostWatchedWith: (Friend * int) list // Friend and count
    GeneratedAt: DateTime
}

/// Filmography progress for a contributor
type FilmographyProgress = {
    Contributor: Contributor
    TotalWorks: int
    SeenWorks: int
    CompletionPercentage: float
    SeenList: LibraryEntry list
    UnseenList: TmdbWork list            // Not in library yet
}

/// Backlog statistics
type BacklogStats = {
    TotalEntries: int
    EstimatedMinutes: int
    OldestEntry: LibraryEntry option
    ByTag: Map<TagId, int>               // Tag -> entry count
}
```

---

## TMDB Types

Types for TMDB API responses:

```fsharp
/// Search result from TMDB
type TmdbSearchResult = {
    TmdbId: int
    MediaType: MediaType
    Title: string                        // title or name
    ReleaseDate: DateTime option
    PosterPath: string option
    Overview: string option
    VoteAverage: float option
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

/// Work in a filmography (from TMDB)
type TmdbWork = {
    TmdbId: int
    MediaType: MediaType
    Title: string
    ReleaseDate: DateTime option
    PosterPath: string option
    Role: ContributorRole
}
```

---

## Request/Response Types

### Create/Update Requests

```fsharp
/// Request to add a movie to the library
type AddMovieRequest = {
    TmdbId: TmdbMovieId
    WhyAdded: WhyAdded option
    InitialTags: TagId list
    InitialFriends: FriendId list
}

/// Request to add a series to the library
type AddSeriesRequest = {
    TmdbId: TmdbSeriesId
    WhyAdded: WhyAdded option
    InitialTags: TagId list
    InitialFriends: FriendId list
}

/// Request to update a library entry
type UpdateEntryRequest = {
    Id: EntryId
    WatchStatus: WatchStatus option
    PersonalRating: PersonalRating option
    Notes: string option
    IsFavorite: bool option
    Tags: TagId list option
    Friends: FriendId list option
}

/// Request to create a friend
type CreateFriendRequest = {
    Name: string
    Nickname: string option
    Notes: string option
}

/// Request to create a tag
type CreateTagRequest = {
    Name: string
    Color: string option
    Description: string option
}

/// Request to create a collection
type CreateCollectionRequest = {
    Name: string
    Description: string option
}

/// Request to create a watch session
type CreateSessionRequest = {
    EntryId: EntryId
    Name: string
    Friends: FriendId list
    Tags: TagId list
}
```

### Search & Filter

```fsharp
/// Filter criteria for library entries
type LibraryFilter = {
    MediaType: MediaType option
    WatchStatus: WatchStatus option
    Rating: PersonalRating option
    Tags: TagId list
    Friends: FriendId list
    SearchQuery: string option
    DateAddedFrom: DateTime option
    DateAddedTo: DateTime option
    YearFrom: int option
    YearTo: int option
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
}
```

---

## Graph Visualization Types

For the relationship graph feature:

```fsharp
/// A node in the relationship graph
type GraphNode =
    | MovieNode of EntryId * string * string option  // id, title, posterPath
    | SeriesNode of EntryId * string * string option
    | FriendNode of FriendId * string                // id, name
    | ContributorNode of ContributorId * string * string option  // id, name, photo
    | TagNode of TagId * string * string option      // id, name, color

/// An edge in the relationship graph
type GraphEdge = {
    Source: GraphNode
    Target: GraphNode
    Relationship: EdgeRelationship
}

/// Type of relationship between nodes
type EdgeRelationship =
    | WatchedWith                        // Entry <-> Friend
    | TaggedAs                           // Entry <-> Tag
    | WorkedOn of ContributorRole        // Entry <-> Contributor
    | InCollection of CollectionId       // Entry <-> Entry (same collection)

/// Full graph data
type RelationshipGraph = {
    Nodes: GraphNode list
    Edges: GraphEdge list
}
```

---

## Client-Only Types

These types are NOT shared and live only in `src/Client/Types.fs`:

```fsharp
/// Generic async data state (client-side only)
type RemoteData<'T> =
    | NotAsked
    | Loading
    | Success of 'T
    | Failure of string

/// Toast notification type
type ToastType =
    | ToastSuccess
    | ToastError
    | ToastInfo
    | ToastWarning

/// Toast notification
type Toast = {
    Id: Guid
    Message: string
    Type: ToastType
    Duration: int option                 // ms, None = sticky
}

/// Modal state
type ModalState =
    | ModalClosed
    | AddEntryModal of TmdbSearchResult option
    | EditEntryModal of EntryId
    | CreateFriendModal
    | CreateTagModal
    | CreateCollectionModal
    | CreateSessionModal of EntryId

/// Page routes
type Page =
    | HomePage
    | LibraryPage
    | MovieDetailPage of EntryId
    | SeriesDetailPage of EntryId
    | FriendsPage
    | FriendDetailPage of FriendId
    | TagsPage
    | TagDetailPage of TagId
    | CollectionsPage
    | CollectionDetailPage of CollectionId
    | ContributorDetailPage of ContributorId
    | TimelinePage
    | StatsPage
    | YearInReviewPage of int            // Year
    | GraphPage
    | ImportPage
    | NotFoundPage
```

---

## Validation Rules

Document validation rules that will be enforced:

### Friend
- Name: required, 1-100 characters
- Nickname: optional, max 50 characters

### Tag
- Name: required, 1-50 characters, unique
- Color: optional, valid hex color

### Collection
- Name: required, 1-100 characters

### Watch Session
- Name: required, 1-100 characters
- EntryId: must exist and be a series

### Library Entry
- Cannot have duplicate TmdbId for same MediaType

---

## Module Organization

Final file structure in `src/Shared/Domain.fs`:

```fsharp
module Shared.Domain

open System

// =====================================
// Identifiers
// =====================================
type MovieId = MovieId of int
type SeriesId = SeriesId of int
// ... etc

// =====================================
// Enums & Discriminated Unions
// =====================================
type MediaType = Movie | Series
type SeriesStatus = Returning | Ended | Canceled | InProduction | Planned | Unknown
type PersonalRating = Brilliant | ReallyGood | Decent | Meh | Nope
type SessionStatus = Active | Paused | SessionCompleted
type ContributorRole = Director | Actor of string option | Writer | ...
type WatchStatus = NotStarted | InProgress of WatchProgress | Completed | Abandoned of AbandonedInfo

// =====================================
// Core Entities
// =====================================
type Movie = { ... }
type Series = { ... }
type Season = { ... }
type Episode = { ... }
type LibraryEntry = { ... }
type WatchSession = { ... }
type Friend = { ... }
type Contributor = { ... }
type Tag = { ... }
type Collection = { ... }

// =====================================
// Supporting Types
// =====================================
type WhyAdded = { ... }
type WatchProgress = { ... }
type AbandonedInfo = { ... }
type EpisodeProgress = { ... }
type MediaContributor = { ... }
type CollectionItem = { ... }

// =====================================
// Statistics
// =====================================
type WatchTimeStats = { ... }
type YearInReview = { ... }
type FilmographyProgress = { ... }
type BacklogStats = { ... }

// =====================================
// TMDB Types
// =====================================
type TmdbSearchResult = { ... }
type TmdbMovieDetails = { ... }
type TmdbCastMember = { ... }
type TmdbCrewMember = { ... }
type TmdbWork = { ... }

// =====================================
// Request/Response Types
// =====================================
type AddMovieRequest = { ... }
type AddSeriesRequest = { ... }
type UpdateEntryRequest = { ... }
type CreateFriendRequest = { ... }
type CreateTagRequest = { ... }
type CreateCollectionRequest = { ... }
type CreateSessionRequest = { ... }
type LibraryFilter = { ... }
type LibrarySort = DateAddedDesc | DateAddedAsc | ...
type PagedRequest<'T> = { ... }
type PagedResponse<'T> = { ... }

// =====================================
// Graph Types
// =====================================
type GraphNode = MovieNode of ... | SeriesNode of ... | ...
type EdgeRelationship = WatchedWith | TaggedAs | ...
type GraphEdge = { ... }
type RelationshipGraph = { ... }

// =====================================
// Module Helpers
// =====================================
module MovieId = ...
module PersonalRating = ...
// ... etc
```

---

## Notes for Implementation

1. **Start simple**: Implement core types first (Movie, Series, LibraryEntry)
2. **Add incrementally**: Add related types as features are built
3. **Keep Fable compatibility**: All types must be serializable
4. **Use option liberally**: Most TMDB fields can be missing
5. **IDs are ints**: SQLite autoincrement produces ints
6. **DateTime handling**: Use DateTime, Fable handles conversion
