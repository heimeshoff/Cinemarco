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
}
