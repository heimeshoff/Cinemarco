module Shared.Api

open Shared.Domain

/// Health check response
type HealthCheckResponse = {
    Status: string
    Version: string
    Timestamp: System.DateTime
}

// =====================================
// Main API
// =====================================
// Note: Fable.Remoting requires all fields to be functions returning Async<'t>.
// Nested API interfaces are not supported, so all operations are flat.

/// API contract for Cinemarco operations
type ICinemarcoApi = {
    /// Health check endpoint
    healthCheck: unit -> Async<HealthCheckResponse>

    // =====================================
    // Library Operations
    // =====================================

    /// Add a movie to the library
    libraryAddMovie: AddMovieRequest -> Async<Result<LibraryEntry, string>>

    /// Add a series to the library
    libraryAddSeries: AddSeriesRequest -> Async<Result<LibraryEntry, string>>

    /// Get all library entries
    libraryGetAll: unit -> Async<LibraryEntry list>

    /// Get a library entry by ID
    libraryGetById: EntryId -> Async<Result<LibraryEntry, string>>

    /// Get a library entry by slug
    libraryGetBySlug: string -> Async<Result<LibraryEntry, string>>

    /// Check if a TMDB movie is already in library (returns entry ID if exists)
    libraryIsMovieInLibrary: TmdbMovieId -> Async<EntryId option>

    /// Check if a TMDB series is already in library (returns entry ID if exists)
    libraryIsSeriesInLibrary: TmdbSeriesId -> Async<EntryId option>

    /// Delete a library entry
    libraryDeleteEntry: EntryId -> Async<Result<unit, string>>

    // =====================================
    // Watch Status Operations
    // =====================================

    /// Mark a movie as watched
    libraryMarkMovieWatched: EntryId * System.DateTime option -> Async<Result<LibraryEntry, string>>

    /// Mark a movie as unwatched
    libraryMarkMovieUnwatched: EntryId -> Async<Result<LibraryEntry, string>>

    /// Update episode progress for a series (toggle watched state)
    libraryUpdateEpisodeProgress: EntryId * int * int * bool -> Async<Result<unit, string>>

    /// Update episode watched date for a specific session
    sessionsUpdateEpisodeWatchedDate: SessionId * int * int * System.DateTime option -> Async<Result<unit, string>>

    /// Mark an entire season as watched
    libraryMarkSeasonWatched: EntryId * int -> Async<Result<unit, string>>

    /// Mark an entire series as completed
    libraryMarkSeriesCompleted: EntryId -> Async<Result<LibraryEntry, string>>

    /// Abandon an entry with optional reason
    libraryAbandonEntry: EntryId * AbandonRequest -> Async<Result<LibraryEntry, string>>

    /// Resume a previously abandoned entry
    libraryResumeEntry: EntryId -> Async<Result<LibraryEntry, string>>

    /// Get episode progress for a series entry
    libraryGetEpisodeProgress: EntryId -> Async<EpisodeProgress list>

    // =====================================
    // Entry Update Operations
    // =====================================

    /// Set or clear the personal rating for an entry
    librarySetRating: EntryId * PersonalRating option -> Async<Result<LibraryEntry, string>>

    /// Toggle favorite status for an entry
    libraryToggleFavorite: EntryId -> Async<Result<LibraryEntry, string>>

    /// Update notes for an entry
    libraryUpdateNotes: EntryId * string option -> Async<Result<LibraryEntry, string>>

    /// Add or remove a friend from an entry
    libraryToggleFriend: EntryId * FriendId -> Async<Result<LibraryEntry, string>>

    // =====================================
    // Friends Operations
    // =====================================

    /// Get all friends
    friendsGetAll: unit -> Async<Friend list>

    /// Get friend by ID
    friendsGetById: int -> Async<Result<Friend, string>>

    /// Get friend by slug
    friendsGetBySlug: string -> Async<Result<Friend, string>>

    /// Create a new friend
    friendsCreate: CreateFriendRequest -> Async<Result<Friend, string>>

    /// Update an existing friend
    friendsUpdate: UpdateFriendRequest -> Async<Result<Friend, string>>

    /// Delete a friend
    friendsDelete: int -> Async<Result<unit, string>>

    /// Get library entries watched with a friend
    friendsGetWatchedWith: int -> Async<LibraryEntry list>

    // =====================================
    // TMDB Operations
    // =====================================

    /// Search for movies on TMDB
    tmdbSearchMovies: string -> Async<TmdbSearchResult list>

    /// Search for series on TMDB
    tmdbSearchSeries: string -> Async<TmdbSearchResult list>

    /// Search both movies and series on TMDB
    tmdbSearchAll: string -> Async<TmdbSearchResult list>

    /// Get full movie details from TMDB
    tmdbGetMovieDetails: TmdbMovieId -> Async<Result<TmdbMovieDetails, string>>

    /// Get full series details from TMDB (with seasons summary)
    tmdbGetSeriesDetails: TmdbSeriesId -> Async<Result<TmdbSeriesDetails, string>>

    /// Get season details with episodes from TMDB
    tmdbGetSeasonDetails: TmdbSeriesId * int -> Async<Result<TmdbSeasonDetails, string>>

    /// Get person details from TMDB
    tmdbGetPersonDetails: TmdbPersonId -> Async<Result<TmdbPersonDetails, string>>

    /// Get person's filmography from TMDB
    tmdbGetPersonFilmography: TmdbPersonId -> Async<Result<TmdbFilmography, string>>

    /// Get movie credits (cast and crew) from TMDB
    tmdbGetMovieCredits: TmdbMovieId -> Async<Result<TmdbCredits, string>>

    /// Get series credits (cast and crew) from TMDB
    tmdbGetSeriesCredits: TmdbSeriesId -> Async<Result<TmdbCredits, string>>

    /// Get TMDB collection (e.g., all Marvel movies)
    tmdbGetCollection: int -> Async<Result<TmdbCollection, string>>

    /// Get trending movies from TMDB
    tmdbGetTrendingMovies: unit -> Async<TmdbSearchResult list>

    /// Get trending series from TMDB
    tmdbGetTrendingSeries: unit -> Async<TmdbSearchResult list>

    // =====================================
    // Watch Session Operations
    // =====================================

    /// Get all sessions for a series entry
    sessionsGetForEntry: EntryId -> Async<WatchSession list>

    /// Get a session by ID with full progress information
    sessionsGetById: SessionId -> Async<Result<WatchSessionWithProgress, string>>

    /// Get a session by slug with full progress information
    sessionsGetBySlug: string -> Async<Result<WatchSessionWithProgress, string>>

    /// Create a new watch session
    sessionsCreate: CreateSessionRequest -> Async<Result<WatchSession, string>>

    /// Update an existing session
    sessionsUpdate: UpdateSessionRequest -> Async<Result<WatchSession, string>>

    /// Delete a session
    sessionsDelete: SessionId -> Async<Result<unit, string>>

    /// Toggle a friend on a session
    sessionsToggleFriend: SessionId * FriendId -> Async<Result<WatchSession, string>>

    /// Update episode progress for a session
    sessionsUpdateEpisodeProgress: SessionId * int * int * bool -> Async<Result<EpisodeProgress list, string>>

    /// Mark an entire season as watched for a session
    sessionsMarkSeasonWatched: SessionId * int -> Async<Result<EpisodeProgress list, string>>

    /// Get episode progress for a session
    sessionsGetProgress: SessionId -> Async<EpisodeProgress list>

    // =====================================
    // Movie Watch Session Operations
    // =====================================

    /// Get all watch sessions for a movie entry
    movieSessionsGetForEntry: EntryId -> Async<MovieWatchSession list>

    /// Create a new movie watch session
    movieSessionsCreate: CreateMovieWatchSessionRequest -> Async<Result<MovieWatchSession, string>>

    /// Delete a movie watch session
    movieSessionsDelete: SessionId -> Async<Result<unit, string>>

    /// Update the date of a movie watch session
    movieSessionsUpdateDate: UpdateMovieWatchSessionDateRequest -> Async<Result<MovieWatchSession, string>>

    /// Update a movie watch session (date, friends, name)
    movieSessionsUpdate: UpdateMovieWatchSessionRequest -> Async<Result<MovieWatchSession, string>>

    // =====================================
    // Collection Operations
    // =====================================

    /// Get all collections
    collectionsGetAll: unit -> Async<Collection list>

    /// Get a collection by ID with all its items and entries
    collectionsGetById: CollectionId -> Async<Result<CollectionWithItems, string>>

    /// Get a collection by slug with all its items and entries
    collectionsGetBySlug: string -> Async<Result<CollectionWithItems, string>>

    /// Create a new collection
    collectionsCreate: CreateCollectionRequest -> Async<Result<Collection, string>>

    /// Update an existing collection
    collectionsUpdate: UpdateCollectionRequest -> Async<Result<Collection, string>>

    /// Delete a collection
    collectionsDelete: CollectionId -> Async<Result<unit, string>>

    /// Add an item to a collection (movie, series, season, or episode)
    collectionsAddItem: CollectionId * CollectionItemRef * string option -> Async<Result<CollectionWithItems, string>>

    /// Remove an item from a collection
    collectionsRemoveItem: CollectionId * CollectionItemRef -> Async<Result<CollectionWithItems, string>>

    /// Reorder items in a collection (takes full list of item refs in new order)
    collectionsReorderItems: CollectionId * CollectionItemRef list -> Async<Result<CollectionWithItems, string>>

    /// Get collection progress (completion stats)
    collectionsGetProgress: CollectionId -> Async<Result<CollectionProgress, string>>

    /// Get collections that contain a specific entry
    collectionsGetForEntry: EntryId -> Async<Collection list>

    /// Get collections that contain a specific item (entry, season, or episode)
    collectionsGetForItem: CollectionItemRef -> Async<Collection list>

    // =====================================
    // Cache Management Operations
    // =====================================

    /// Get all cache entries grouped by expiration
    cacheGetEntries: unit -> Async<CacheEntry list>

    /// Get cache statistics
    cacheGetStats: unit -> Async<CacheStats>

    /// Clear all expired cache entries
    cacheClearExpired: unit -> Async<ClearCacheResult>

    /// Clear all cache entries
    cacheClearAll: unit -> Async<ClearCacheResult>

    // =====================================
    // Tracked Contributors Operations
    // =====================================

    /// Get all tracked contributors
    contributorsGetAll: unit -> Async<TrackedContributor list>

    /// Get a tracked contributor by ID
    contributorsGetById: TrackedContributorId -> Async<Result<TrackedContributor, string>>

    /// Get a tracked contributor by slug
    contributorsGetBySlug: string -> Async<Result<TrackedContributor, string>>

    /// Track a new contributor
    contributorsTrack: TrackContributorRequest -> Async<Result<TrackedContributor, string>>

    /// Untrack a contributor
    contributorsUntrack: TrackedContributorId -> Async<Result<unit, string>>

    /// Update notes for a tracked contributor
    contributorsUpdateNotes: TrackedContributorId * string option -> Async<Result<TrackedContributor, string>>

    /// Check if a TMDB person is tracked
    contributorsIsTracked: TmdbPersonId -> Async<bool>

    /// Get a tracked contributor by TMDB person ID (returns None if not tracked)
    contributorsGetByTmdbId: TmdbPersonId -> Async<TrackedContributor option>

    // =====================================
    // Time Intelligence / Stats Operations
    // =====================================

    /// Get complete time intelligence dashboard stats
    statsGetTimeIntelligence: unit -> Async<TimeIntelligenceStats>

    /// Get watch time stats (lifetime)
    statsGetWatchTime: unit -> Async<WatchTimeStats>

    /// Get watch time stats for a specific year
    statsGetWatchTimeForYear: int -> Async<WatchTimeStats>

    /// Get backlog stats (unwatched items)
    statsGetBacklog: unit -> Async<BacklogStats>

    /// Get top series by time investment
    statsGetTopSeriesByTime: int -> Async<SeriesTimeInvestment list>

    // =====================================
    // Timeline Operations
    // =====================================

    /// Get timeline entries (paged, chronological watch history)
    timelineGetEntries: TimelineFilter * int * int -> Async<PagedResponse<TimelineEntry>>

    /// Get timeline entries grouped by month (year, month)
    timelineGetByMonth: int * int -> Async<TimelineEntry list>
}
