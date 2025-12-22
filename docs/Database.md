# Cinemarco Database Design

This document explains the purpose and design philosophy behind every table in the Cinemarco database. For SQL definitions and queries, see `/specs/DATABASE-SCHEMA.md`.

---

## Design Philosophy

### Local-First, Single-User
Cinemarco is designed for a single user running on their own hardware. This simplifies many decisions:
- No user authentication tables
- No multi-tenancy concerns
- Aggressive caching is safe (no invalidation across users)
- Data integrity relies on application logic rather than database-level RBAC

### Separation of TMDB Data from Personal Data
A core design principle is the **separation between external metadata and personal tracking**:

```
┌─────────────────────────────────────────────────────────────────┐
│                     TMDB DATA (External)                         │
│  movies, series, seasons, episodes, contributors                 │
│  → Fetched from TMDB API, can be refreshed/updated               │
│  → Represents "the world's knowledge" about media                │
└─────────────────────────────────────────────────────────────────┘
                              │
                              │ references
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    PERSONAL DATA (User's)                        │
│  library_entries, watch_sessions, episode_progress, friends      │
│  → Created by the user, represents their personal relationship   │
│  → "When did I watch this? With whom? How did I rate it?"        │
└─────────────────────────────────────────────────────────────────┘
```

This separation means:
1. Deleting TMDB data doesn't lose personal history (cascades are intentional)
2. TMDB data can be refreshed without touching personal preferences
3. The same movie/series can exist in multiple library entries (different watch sessions)

### Nullable vs Required Fields
- **IDs and foreign keys**: Required when the relationship is mandatory
- **Metadata fields**: Often nullable (overview, tagline, etc.) - TMDB doesn't always have complete data
- **Dates**: Nullable when the event hasn't happened yet (date_first_watched before watching)
- **Personal fields**: Nullable to allow incremental enrichment (add notes later, rate later)

---

## Table Categories

### 1. Core Media Tables

These tables store metadata fetched from TMDB about movies and TV shows.

---

#### `movies`

**Purpose**: Stores movie metadata from TMDB.

**Design Rationale**:
- `tmdb_id` is UNIQUE because we never want duplicate movies
- `genres` stored as JSON array string for flexibility (genres change, new ones appear)
- `original_title` vs `title`: International films have both
- `imdb_id` for external linking to IMDb
- `runtime_minutes` as INTEGER for easy calculations

**Why it exists**: Movies are the atomic unit of film tracking. Even if a movie isn't in someone's library, we might cache it from search results or recommendations.

---

#### `series`

**Purpose**: Stores TV series metadata from TMDB.

**Design Rationale**:
- `status` tracks if series is ongoing ("Returning Series") or finished ("Ended")
- `number_of_seasons` and `number_of_episodes` are denormalized for quick stats
- `episode_runtime_minutes` is an average (episodes vary in length)
- Separate from movies because the tracking model is fundamentally different (episodes vs single viewing)

**Why it exists**: Series require hierarchical tracking (series → seasons → episodes). This table is the root of that hierarchy.

---

#### `seasons`

**Purpose**: Groups episodes within a series.

**Design Rationale**:
- `season_number` uses TMDB convention where 0 = specials
- `episode_count` denormalized for progress calculations ("5/10 episodes watched")
- UNIQUE constraint on `(series_id, season_number)` prevents duplicates
- `poster_path` because seasons often have unique artwork

**Why it exists**: Seasons are a natural grouping for watch progress. Users often think "I finished season 2" rather than "I watched episodes 11-20".

---

#### `episodes`

**Purpose**: Individual episode metadata.

**Design Rationale**:
- `still_path` for episode-specific thumbnails
- `runtime_minutes` per episode (varies widely in modern TV)
- `air_date` for tracking new releases
- UNIQUE on `(series_id, season_number, episode_number)` for data integrity
- References both `series_id` and `season_id` - redundant but useful for queries

**Why it exists**: Episodes are what users actually watch. Progress tracking happens at this level.

---

### 2. Personal Library Tables

These tables represent the user's personal relationship with media.

---

#### `library_entries`

**Purpose**: The central "personal wrapper" around movies and series.

**Design Rationale**:
- `media_type` discriminator with CHECK constraint ensures either `movie_id` OR `series_id` is set, never both
- **Why section** (`why_*` fields): Captures "why did I add this?" - a unique Cinemarco feature
  - `why_recommended_by_friend_id`: FK to friends table if recommended by someone tracked
  - `why_recommended_by_name`: Free text for non-tracked recommenders
  - `why_source`: Where you heard about it ("Netflix", "Twitter", "Podcast")
  - `why_context`: Free-form notes about why you're interested
- **Watch status tracking**: `watch_status` uses explicit states rather than computed values
- **Abandoned tracking**: Full context for why something was dropped
- **Personal metadata**: `personal_rating` (1-5), `notes`, `is_favorite`

**Why it exists**: This is the heart of Cinemarco. TMDB knows about movies; library_entries know about YOUR relationship with movies. You don't rate "Inception" - you rate "your experience watching Inception".

**Key Design Decision**: One library entry per movie/series. If you want to track a rewatch of a movie, you use movie_watch_sessions. For series, you use watch_sessions.

---

#### `watch_sessions` (Series)

**Purpose**: Named watch-throughs of a series.

**Design Rationale**:
- `is_default`: Every series entry gets one default "Personal" session created automatically
- `name`: "Rewatch with Sarah", "2024 Summer Binge", "Solo rewatch"
- `status`: Active (currently watching), Paused, Completed
- `start_date` / `end_date`: Optional bracketing of the watch period

**Why it exists**: People rewatch series. Each rewatch is a distinct experience - different company, different life context, different attention level. Sessions let you track "I've seen Breaking Bad 3 times" with separate progress for each.

**Example**: You watched The Office seasons 1-5 alone in 2018, then rewatched the whole series with your partner in 2023. Two sessions, two sets of episode progress, two potentially different experiences.

---

#### `movie_watch_sessions`

**Purpose**: Track individual viewings of a movie.

**Design Rationale**:
- Simpler than series sessions - just date and optional name
- No "progress" concept (you watch the whole movie)
- Links to `movie_session_friends` for who you watched with

**Why it exists**: Movies can be rewatched too. "I've seen Dune 5 times in theaters" should be trackable.

---

#### `episode_progress`

**Purpose**: Tracks which episodes have been watched, per session.

**Design Rationale**:
- `session_id` is NOT NULL - all progress belongs to a session (default or named)
- `is_watched` boolean rather than timestamp-only (explicitly states watched vs not)
- `watched_date` optional (you might mark watched without knowing when)
- UNIQUE on `(entry_id, session_id, season_number, episode_number)` - one record per episode per session

**Why it exists**: This is the actual "did I watch this?" data. The watch_status on library_entries is computed from this.

**Key Design Decision**: Progress is per-session, not per-entry. If you've watched episode S01E05 in two different sessions, there are two rows.

---

#### `watch_history`

**Purpose**: Append-only log of watch events for timeline display.

**Design Rationale**:
- Separate from `episode_progress` because it's temporal, not state-based
- `is_completion` flags significant moments (finished a series, watched a movie)
- Enables queries like "what did I watch in December 2024?"

**Why it exists**: Timeline views need chronological data. Episode_progress tells you WHAT is watched; watch_history tells you WHEN.

---

### 3. People Tables

---

#### `friends`

**Purpose**: People you watch things with.

**Design Rationale**:
- Simple profile: name, nickname, optional avatar
- `notes` for remembering context ("College roommate", "Film club friend")
- No authentication - this is just your personal address book of watching companions

**Why it exists**: Cinemarco's social memory. "Who did I watch Parasite with?" is answerable.

---

#### `contributors`

**Purpose**: Cache of TMDB person data (actors, directors, etc.).

**Design Rationale**:
- `tmdb_person_id` UNIQUE - one record per person
- `known_for_department`: "Acting", "Directing", "Writing", etc.
- Biography and birth/death dates for detail pages

**Why it exists**: Performance optimization. Rather than fetching from TMDB every time, we cache contributor data locally.

---

#### `media_contributors`

**Purpose**: Links contributors to the movies/series they worked on.

**Design Rationale**:
- `role_type`: What they did ("Director", "Actor", "Writer", etc.)
- `role_character`: For actors, the character name
- `role_order`: Billing order (lead actors first)
- CHECK constraint ensures each record links to exactly one movie or series

**Why it exists**: Enables filmography browsing. "Show me all Christopher Nolan films" or "What else has this actor been in?"

---

#### `tracked_contributors`

**Purpose**: Contributors the user explicitly wants to follow.

**Design Rationale**:
- `id` is TEXT (UUID) for portability
- `notes` for personal context ("My favorite director", "Watch everything they do")
- Separate from `contributors` because tracking is a personal choice, not just cached data

**Why it exists**: Proactive discovery. When a tracked director releases a new film, you want to know. This table stores your "following" list.

---

### 4. Organization Tables

---

#### `collections`

**Purpose**: User-created groupings of media.

**Design Rationale**:
- `is_public_franchise`: Distinguishes "MCU" (TMDB-defined) from "My Comfort Movies" (user-defined)
- `tmdb_collection_id`: Links to TMDB's collection data if applicable
- `cover_image_path`: Custom cover or pulled from TMDB

**Why it exists**: Users want to organize beyond genres. "Oscar Winners I've Seen", "Movies to Watch with Mom", "Christopher Nolan Filmography Progress".

---

#### `collection_items`

**Purpose**: Links items to collections with ordering.

**Design Rationale**:
- `item_type`: Supports 'entry' (full movies/series), 'season', or 'episode'
- `position`: Explicit ordering for ranked lists or franchise timelines
- `notes`: Per-item notes within collection context

**Why it exists**: Collections need flexible membership. The MCU collection needs specific movie ordering. A "Best Episodes" collection needs individual episode references.

**Design Evolution**: Originally only supported library entries. Extended to support seasons and individual episodes for more flexible "Best Of" lists.

---

### 5. Junction Tables

These handle many-to-many relationships.

---

#### `entry_friends`

**Purpose**: Links library entries to friends who watched with you.

**Why it exists**: "Who did I watch this with?" at the entry level. For series, this might mean "we started watching together" rather than episode-by-episode.

---

#### `session_friends`

**Purpose**: Links watch sessions (series) to friends.

**Why it exists**: More granular than entry_friends. "I watched seasons 1-3 alone, but rewatched with Sarah" - two sessions, different friend associations.

---

#### `movie_session_friends`

**Purpose**: Links movie watch sessions to friends.

**Why it exists**: Per-viewing companion tracking. "First time alone, second time with film club, third time with parents."

---

### 6. Cache & Statistics Tables

---

#### `tmdb_cache`

**Purpose**: HTTP cache for TMDB API responses.

**Design Rationale**:
- `cache_key`: Request identifier (e.g., "movie:550", "search:inception")
- `cache_value`: Full JSON response
- `expires_at`: TTL-based invalidation

**Why it exists**: TMDB has rate limits. Caching reduces API calls and improves performance.

---

#### `year_reviews`

**Purpose**: Pre-computed yearly statistics.

**Design Rationale**:
- Computed once, stored as JSON
- `total_minutes`, `total_movies`, etc. for quick dashboard display
- `data_json` holds complete computed stats

**Why it exists**: "Year in Review" features require aggregating lots of data. Computing on-demand is slow; pre-computing and caching is fast.

---

#### `stats_cache`

**Purpose**: General-purpose statistics cache.

**Design Rationale**:
- Key-value store for arbitrary stats
- `expires_at` for time-based invalidation

**Why it exists**: Some stats (total watch time, backlog estimates) are expensive to compute. Cache them.

---

### 7. Integration Tables

---

#### `trakt_settings`

**Purpose**: Stores Trakt.tv OAuth tokens and sync state.

**Design Rationale**:
- `id` CHECK constraint ensures only one row (singleton pattern)
- OAuth tokens for API access
- `last_sync_at` for incremental sync
- `auto_sync_enabled` user preference

**Why it exists**: Trakt integration requires persistent token storage and sync state tracking.

---

### 8. System Tables

---

#### `migrations`

**Purpose**: Tracks which database migrations have been applied.

**Design Rationale**:
- `version` is UNIQUE - each migration runs exactly once
- `applied_at` for debugging/auditing

**Why it exists**: Database schema evolves. Migrations ensure the schema stays in sync with the application code.

---

## Relationship Diagram

```
                                 TMDB DATA
    ┌────────────────────────────────────────────────────────────────┐
    │                                                                 │
    │   movies ◄─────────────────┐                                   │
    │                            │                                   │
    │   series ◄──────┬──────────┼──────────────────────────────┐    │
    │      │          │          │                              │    │
    │      │          │          │                              │    │
    │      ▼          │          │      media_contributors      │    │
    │   seasons       │          │           │                  │    │
    │      │          │          │           ▼                  │    │
    │      ▼          │          │      contributors            │    │
    │   episodes      │          │                              │    │
    │                 │          │                              │    │
    └─────────────────┼──────────┼──────────────────────────────┼────┘
                      │          │                              │
                      │          │                              │
                      ▼          ▼                              │
    ┌────────────────────────────────────────────────────────────────┐
    │                      PERSONAL DATA                              │
    │                                                                 │
    │              library_entries ◄──────────────────────────────────┤
    │               (the wrapper)                                     │
    │                    │                                            │
    │         ┌──────────┴──────────┐                                │
    │         │                     │                                │
    │         ▼                     ▼                                │
    │  watch_sessions        movie_watch_sessions                    │
    │  (for series)          (for movies)                            │
    │         │                     │                                │
    │         ▼                     │                                │
    │  episode_progress             │                                │
    │         │                     │                                │
    │         ▼                     ▼                                │
    │  ───────────────  session_friends / movie_session_friends      │
    │                         │                                       │
    │                         ▼                                       │
    │                      friends                                    │
    │                                                                 │
    │  collections ◄────► collection_items ◄────► entries/episodes  │
    │                                                                 │
    │  tracked_contributors (personal following list)                │
    │                                                                 │
    └─────────────────────────────────────────────────────────────────┘
```

---

## Key Design Decisions Explained

### 1. Why `library_entries` wraps both movies and series

Alternative: Separate `movie_entries` and `series_entries` tables.

**We chose single table because**:
- Queries across "all your media" are simpler
- UI naturally treats them together in timelines, lists
- Shared concepts (rating, notes, why_added) apply to both
- The CHECK constraint provides type safety

### 2. Why sessions are explicit (not implicit from progress)

Alternative: Infer sessions from gaps in viewing dates.

**We chose explicit sessions because**:
- Users name their sessions meaningfully
- Sessions can be planned before starting
- Clear ownership of progress records
- "Rewatch with Sarah" is a first-class concept

### 3. Why `episode_progress` is per-session, not per-entry

Alternative: Track progress at entry level, let sessions be metadata-only.

**We chose per-session because**:
- Rewatches are independent experiences
- "I'm on S3E5 in my solo watch but S1E3 with my partner" is valid
- Each session can complete at its own pace

### 4. Why contributors are cached, not live-fetched

Alternative: Always fetch from TMDB.

**We chose caching because**:
- Performance (no API calls for every page load)
- Offline capability
- Rate limit protection
- Person data changes rarely

### 5. Why no tags table (removed)

**Original design** included tags for flexible categorization.

**Removed because**:
- Genres from TMDB cover most use cases
- Collections provide user-defined grouping
- Tags added complexity without clear user value in MVP
- Can be re-added later if needed

---

## Performance Considerations

1. **Indexes on every foreign key**: All FK columns have indexes for join performance
2. **Indexes on filter columns**: watch_status, personal_rating, date_added are commonly filtered
3. **Denormalized counts**: episode_count in seasons, number_of_episodes in series
4. **JSON for flexible data**: genres stored as JSON arrays (no join table needed)
5. **Cache tables**: Expensive computations stored, not recalculated

---

## Future Extensibility

The schema is designed to accommodate future features:

- **Import/Export**: All data is self-contained, no external dependencies
- **Multi-device sync**: Add sync metadata columns as needed
- **New media types**: The pattern (TMDB data + library_entries wrapper) extends to books, games, etc.
- **Social features**: Friends table can grow; invite codes, sharing could be added
- **Advanced stats**: stats_cache and year_reviews patterns extend to any computed data
