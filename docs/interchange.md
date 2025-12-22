# External Service Interchange

This document describes how Cinemarco synchronizes with external services: **Trakt.tv** (watch history) and **TMDB** (movie/series metadata).

## Architecture Overview

```
┌─────────────────┐      ┌──────────────────┐      ┌─────────────────┐
│                 │      │                  │      │                 │
│   Trakt.tv      │◄────►│    Cinemarco     │◄────►│     TMDB        │
│  (Watch Data)   │      │     Server       │      │   (Metadata)    │
│                 │      │                  │      │                 │
└─────────────────┘      └──────────────────┘      └─────────────────┘
        │                        │                        │
        │                        │                        │
   OAuth 2.0              SQLite Storage            API Key Auth
   - History              - Library entries         - Movie details
   - Ratings              - Watch sessions          - Series details
   - Watchlist            - Episode progress        - Season/episodes
                          - Trakt tokens            - Person info
                          - TMDB cache              - Credits
```

---

## Trakt.tv Integration

### Purpose
Trakt.tv serves as the source of truth for watch history. Users can import their existing Trakt watch history into Cinemarco and optionally keep the two systems synchronized.

### Authentication Flow

**OAuth 2.0 Authorization Code Flow**

```
1. User clicks "Connect Trakt"
   └─► getAuthUrl() generates authorization URL with state token

2. User authorizes on Trakt.tv
   └─► Trakt redirects with authorization code

3. Server exchanges code for tokens
   └─► exchangeCode(code, state)
       └─► POST /oauth/token
       └─► Stores access_token, refresh_token, expires_at in DB
```

**Environment Variables Required:**
- `TRAKT_CLIENT_ID` - Application client ID
- `TRAKT_CLIENT_SECRET` - Application client secret
- `TRAKT_REDIRECT_URI` (optional) - Defaults to out-of-band flow

**Token Storage:**
- Tokens are stored in SQLite `settings` table
- In-memory cache for fast access
- Auto-loaded from DB on first API call

### API Endpoints Used

| Endpoint | Purpose | Rate Limit |
|----------|---------|------------|
| `POST /oauth/token` | Exchange code for tokens | - |
| `GET /sync/history/movies` | Get watched movie history | 1000/5min |
| `GET /sync/history/shows` | Get watched episode history | 1000/5min |
| `GET /sync/watched/shows` | Get watched series (summary) | 1000/5min |
| `GET /sync/ratings` | Get user ratings (1-10) | 1000/5min |
| `GET /sync/watchlist` | Get watchlist items | 1000/5min |

### Rate Limiting

- **Limit:** 1000 requests per 5 minutes
- **Implementation:** 50ms minimum interval between requests
- **Retry:** On HTTP 429, wait 2 seconds and retry once

### Data Models

**TraktHistoryItem** (for movies and watchlist):
```fsharp
type TraktHistoryItem = {
    TmdbId: int           // TMDB identifier (key for linking)
    MediaType: MediaType  // Movie or Series
    Title: string         // Display title
    WatchedAt: DateTime option  // When watched
    TraktRating: int option     // Rating 1-10
}
```

**TraktWatchedSeries** (for series with episode data):
```fsharp
type TraktWatchedSeries = {
    TmdbId: int
    Title: string
    LastWatchedAt: DateTime option
    WatchedEpisodes: TraktWatchedEpisode list
    TraktRating: int option
}

type TraktWatchedEpisode = {
    SeasonNumber: int
    EpisodeNumber: int
    WatchedAt: DateTime option
}
```

### Import Process

#### Full Import (Initial Import)

Used for importing complete Trakt history. Accessed via the Import page.

**Flow:**
```
1. getImportPreview() - Preview what will be imported
   └─► Fetch movies, series, watchlist from Trakt
   └─► Check which items already exist in library
   └─► Return preview with counts

2. startImport(options) - Execute import
   └─► For each movie:
       │   ├─► If exists: Add watch session (if date unique)
       │   └─► If new: Fetch TMDB details → Create library entry → Add watch session
   └─► For each series:
       │   ├─► If exists: Import episode watch data
       │   └─► If new: Fetch TMDB details → Create entry → Import episodes
   └─► Apply ratings (Trakt 1-10 → Cinemarco 1-5)
```

**Binge-Detection Algorithm:**

When importing episode watch data, the system detects binge-watching patterns:

```
IF episodes_watched_on_same_day > 4:
    # User binged - substitute air dates for more accurate timeline
    FOR each episode:
        watched_date = episode.air_date ?? trakt_watched_date
ELSE:
    # Normal watching - use Trakt dates directly
    watched_date = trakt_watched_date
```

This prevents a binge-watching day from appearing as many events on the same date.

**Rating Mapping:**
```
Trakt 1-2  →  Waste (1)
Trakt 3-4  →  Meh (2)
Trakt 5-6  →  Decent (3)
Trakt 7-8  →  Entertaining (4)
Trakt 9-10 →  Outstanding (5)
```

#### Incremental Sync (Background Sync)

Lightweight sync for keeping libraries in sync. Can be triggered from home screen.

**Characteristics:**
- Only fetches data since last sync (`start_at` parameter)
- **No binge-detection** - uses Trakt dates directly
- Adds new items to library if not present
- Syncs watchlist items
- Updates `last_sync_at` timestamp

**Flow:**
```
1. Get last_sync_at from settings (or 24h ago if first sync)

2. Fetch movies since last sync
   └─► For each: Add watch session or create new entry

3. Fetch series episodes since last sync
   └─► For existing series: Add episode progress
   └─► For new series: Create entry + import episodes

4. Sync watchlist
   └─► Add any new items to library (not marked as watched)

5. Update last_sync_at to now
```

### State Management

Import progress is tracked via `ImportState`:
```fsharp
type ImportState = {
    InProgress: bool
    CurrentItem: string option   // Title being processed
    Completed: int               // Items processed
    Total: int                   // Total items
    Errors: string list          // Error messages
    CancellationRequested: bool  // Cancel flag
}
```

The frontend can poll `getStatus()` to show progress.

---

## TMDB Integration

### Purpose
TMDB (The Movie Database) provides metadata for movies and series: titles, posters, cast, crew, episode information, and more.

### Authentication

Simple API key authentication via query parameter.

**Environment Variable:**
- `TMDB_API_KEY` - API key from TMDB account

### Rate Limiting

- **Limit:** ~40 requests per 10 seconds
- **Implementation:** 250ms minimum interval between requests
- **Retry:** On HTTP 429, wait 1 second and retry once

### API Endpoints Used

| Endpoint | Purpose | Cache Duration |
|----------|---------|----------------|
| `/search/movie` | Search movies | 1 hour |
| `/search/tv` | Search series | 1 hour |
| `/search/multi` | Search all | 1 hour |
| `/movie/{id}` | Movie details | 24 hours |
| `/movie/{id}/credits` | Movie cast/crew | 24 hours |
| `/tv/{id}` | Series details | 24 hours |
| `/tv/{id}/credits` | Series cast/crew | 24 hours |
| `/tv/{id}/season/{num}` | Season with episodes | 24 hours |
| `/person/{id}` | Person details | 1 week |
| `/person/{id}/combined_credits` | Filmography | 1 week |
| `/collection/{id}` | Movie collection | 1 week |
| `/trending/movie/week` | Trending movies | 1 hour |
| `/trending/tv/week` | Trending series | 1 hour |

### Response Caching

All TMDB responses are cached in the `tmdb_cache` SQLite table:

```sql
CREATE TABLE tmdb_cache (
    cache_key TEXT PRIMARY KEY,
    cache_value TEXT,           -- JSON response
    expires_at DATETIME
)
```

**Cache Key Format:**
- `search:movie:{query}` - Movie search results
- `search:tv:{query}` - Series search results
- `movie:{id}` - Movie details
- `tv:{id}` - Series details
- `tv:{id}:season:{num}` - Season details
- `person:{id}` - Person details
- `collection:{id}` - Collection details

**Cache Management:**
- Expired entries cleaned up periodically
- `clearExpiredCache()` - Remove expired entries
- `clearCache()` - Clear all cache (admin function)
- Cache stats available via `getCacheStats()`

### Data Models

**TmdbMovieDetails:**
```fsharp
type TmdbMovieDetails = {
    TmdbId: TmdbMovieId
    Title: string
    OriginalTitle: string option
    Overview: string option
    ReleaseDate: DateTime option
    RuntimeMinutes: int option
    PosterPath: string option     // e.g., "/abc123.jpg"
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
```

**TmdbSeriesDetails:**
```fsharp
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
    Status: string               // "Returning Series", "Ended", etc.
    NumberOfSeasons: int
    NumberOfEpisodes: int
    EpisodeRunTimeMinutes: int option
    Seasons: TmdbSeasonSummary list
    Cast: TmdbCastMember list
    Crew: TmdbCrewMember list
}
```

**TmdbSeasonDetails:**
```fsharp
type TmdbSeasonDetails = {
    TmdbSeriesId: TmdbSeriesId
    SeasonNumber: int
    Name: string option
    Overview: string option
    PosterPath: string option
    AirDate: DateTime option
    Episodes: TmdbEpisodeSummary list
}

type TmdbEpisodeSummary = {
    EpisodeNumber: int
    Name: string
    Overview: string option
    AirDate: DateTime option
    RuntimeMinutes: int option
    StillPath: string option
}
```

### Image URLs

TMDB stores only image paths. Full URLs are constructed:

```
Base URL: https://image.tmdb.org/t/p/{size}{path}

Sizes:
- Posters: w185, w342, w500, original
- Backdrops: w300, w780, w1280, original
- Profiles: w45, w185, h632, original
```

**Image Caching:**
Images are downloaded and cached locally via `ImageCache` module to reduce external requests.

---

## Synchronization Data Flow

### Adding a Movie from Trakt

```
Trakt History
     │
     ▼
┌────────────────────┐
│ TraktHistoryItem   │
│ - TmdbId: 550      │
│ - Title: "Fight..."│
│ - WatchedAt: date  │
└────────────────────┘
     │
     ▼ (lookup by TmdbId)
┌────────────────────┐
│ TMDB API           │
│ /movie/550         │
└────────────────────┘
     │
     ▼
┌────────────────────┐
│ TmdbMovieDetails   │
│ - Full metadata    │
│ - Cast/Crew        │
│ - Images           │
└────────────────────┘
     │
     ▼ (cache images)
┌────────────────────┐
│ ImageCache         │
│ - Download poster  │
│ - Download backdrop│
└────────────────────┘
     │
     ▼ (persist)
┌────────────────────┐
│ SQLite             │
│ - movies table     │
│ - library_entries  │
│ - movie_watch_sess │
│ - contributors     │
│ - media_contribut. │
└────────────────────┘
```

### Adding a Series from Trakt

```
Trakt History
     │
     ▼
┌─────────────────────┐
│ TraktWatchedSeries  │
│ - TmdbId: 1396      │
│ - WatchedEpisodes   │
│   - S01E01, S01E02  │
└─────────────────────┘
     │
     ▼ (lookup series)
┌─────────────────────┐
│ TMDB: /tv/1396      │
└─────────────────────┘
     │
     ▼ (for each watched season)
┌─────────────────────┐
│ TMDB: /tv/1396/     │
│       season/1      │
│ (get episode dates) │
└─────────────────────┘
     │
     ▼ (binge detection if full import)
┌─────────────────────┐
│ Date Assignment:    │
│ - >4 eps same day?  │
│   → Use air dates   │
│ - Otherwise         │
│   → Use Trakt dates │
└─────────────────────┘
     │
     ▼ (persist)
┌─────────────────────┐
│ SQLite              │
│ - series table      │
│ - seasons table     │
│ - episodes table    │
│ - library_entries   │
│ - watch_sessions    │
│ - episode_progress  │
└─────────────────────┘
```

---

## Error Handling

### Trakt Errors

| Error | Handling |
|-------|----------|
| No API key | Return error, prompt user to configure |
| Token expired | Prompt user to re-authenticate |
| Rate limited (429) | Wait 2s, retry once |
| Network error | Return error, preserve partial progress |
| Invalid JSON | Log and skip item, continue import |

### TMDB Errors

| Error | Handling |
|-------|----------|
| No API key | Return error, prompt user to configure |
| Not found (404) | Return error (item doesn't exist on TMDB) |
| Rate limited (429) | Wait 1s, retry once |
| Network error | Return error |
| Invalid JSON | Return parse error |

### Import Recovery

- Import state includes error list
- Individual item failures don't stop import
- Progress is tracked for resume capability
- Cancellation supported via `requestCancellation()`

---

## Configuration

### Required Environment Variables

```env
# TMDB (Required)
TMDB_API_KEY=your_tmdb_api_key

# Trakt (Optional - enables Trakt sync features)
TRAKT_CLIENT_ID=your_trakt_client_id
TRAKT_CLIENT_SECRET=your_trakt_client_secret
TRAKT_REDIRECT_URI=http://your-app/trakt/callback  # Optional
```

### Database Tables

**Trakt-related:**
```sql
-- Stored in settings table as JSON/individual keys
- trakt_access_token
- trakt_refresh_token
- trakt_expires_at
- trakt_last_sync_at
- trakt_auto_sync_enabled
```

**TMDB Cache:**
```sql
CREATE TABLE tmdb_cache (
    cache_key TEXT PRIMARY KEY,
    cache_value TEXT,
    expires_at DATETIME
);
```

---

## Testing Considerations

### Unit Testing

Key functions to test:

**TraktClient:**
- `mapTraktRating` - Rating conversion
- `parseHistoryMovie` - JSON parsing
- `parseHistoryShow` - JSON parsing
- Date parsing edge cases

**TraktImport:**
- Binge detection logic (>4 episodes same day)
- Air date substitution
- Deduplication of watched episodes

**TmdbClient:**
- Response parsing for each entity type
- Cache key generation
- Image URL construction

### Integration Testing

- Mock HTTP responses for Trakt/TMDB APIs
- Test import flow end-to-end
- Test incremental sync with various scenarios:
  - First sync (no last_sync_at)
  - Subsequent sync with new data
  - Sync with existing items

### Edge Cases

- Movies/series without TMDB IDs (should be skipped)
- Episodes with missing air dates
- Timezone handling in watched dates
- Very large watch histories (pagination)
- Concurrent import requests
