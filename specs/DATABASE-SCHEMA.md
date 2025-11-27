# Cinemarco Database Schema Specification

This document specifies the SQLite database schema for Cinemarco. All tables, indexes, and relationships are defined here.

## Database Configuration

```sql
-- Enable WAL mode for better concurrent access
PRAGMA journal_mode=WAL;

-- Enable foreign key constraints
PRAGMA foreign_keys=ON;

-- Use UTF-8 encoding
PRAGMA encoding='UTF-8';
```

---

## Schema Overview

```
┌────────────────────────────────────────────────────────────────────────────┐
│                           CORE MEDIA TABLES                                 │
├────────────────────────────────────────────────────────────────────────────┤
│  movies          │  series          │  seasons         │  episodes         │
│  └─────────────────────────────────────────────────────────────────────┐   │
│                                   │                                     │   │
│                           library_entries                               │   │
│                           (personal wrapper)                            │   │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│                          TRACKING TABLES                                    │
├────────────────────────────────────────────────────────────────────────────┤
│  watch_sessions     │  episode_progress   │  watch_history                 │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│                          PEOPLE TABLES                                      │
├────────────────────────────────────────────────────────────────────────────┤
│  friends            │  contributors        │  media_contributors           │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│                       ORGANIZATION TABLES                                   │
├────────────────────────────────────────────────────────────────────────────┤
│  tags               │  collections         │  collection_items             │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│                        JUNCTION TABLES                                      │
├────────────────────────────────────────────────────────────────────────────┤
│  entry_tags         │  entry_friends       │  session_tags                 │
│  session_friends    │                      │                               │
└────────────────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────────────────┐
│                         CACHE TABLES                                        │
├────────────────────────────────────────────────────────────────────────────┤
│  tmdb_cache         │  year_reviews        │  stats_cache                  │
└────────────────────────────────────────────────────────────────────────────┘
```

---

## Core Media Tables

### movies

```sql
CREATE TABLE IF NOT EXISTS movies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_id INTEGER NOT NULL UNIQUE,
    title TEXT NOT NULL,
    original_title TEXT,
    overview TEXT,
    release_date TEXT,                    -- ISO 8601 format
    runtime_minutes INTEGER,
    poster_path TEXT,
    backdrop_path TEXT,
    genres TEXT,                          -- JSON array: '["Action", "Sci-Fi"]'
    original_language TEXT,
    vote_average REAL,
    vote_count INTEGER,
    tagline TEXT,
    imdb_id TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_movies_tmdb_id ON movies(tmdb_id);
CREATE INDEX IF NOT EXISTS idx_movies_title ON movies(title);
CREATE INDEX IF NOT EXISTS idx_movies_release_date ON movies(release_date);
```

### series

```sql
CREATE TABLE IF NOT EXISTS series (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    original_name TEXT,
    overview TEXT,
    first_air_date TEXT,                  -- ISO 8601 format
    last_air_date TEXT,
    poster_path TEXT,
    backdrop_path TEXT,
    genres TEXT,                          -- JSON array
    original_language TEXT,
    vote_average REAL,
    vote_count INTEGER,
    status TEXT,                          -- 'Returning', 'Ended', etc.
    number_of_seasons INTEGER NOT NULL DEFAULT 0,
    number_of_episodes INTEGER NOT NULL DEFAULT 0,
    episode_runtime_minutes INTEGER,      -- Average
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_series_tmdb_id ON series(tmdb_id);
CREATE INDEX IF NOT EXISTS idx_series_name ON series(name);
CREATE INDEX IF NOT EXISTS idx_series_first_air_date ON series(first_air_date);
```

### seasons

```sql
CREATE TABLE IF NOT EXISTS seasons (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    series_id INTEGER NOT NULL,
    tmdb_season_id INTEGER NOT NULL,
    season_number INTEGER NOT NULL,
    name TEXT,
    overview TEXT,
    poster_path TEXT,
    air_date TEXT,
    episode_count INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    UNIQUE (series_id, season_number)
);

CREATE INDEX IF NOT EXISTS idx_seasons_series_id ON seasons(series_id);
```

### episodes

```sql
CREATE TABLE IF NOT EXISTS episodes (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    series_id INTEGER NOT NULL,
    season_id INTEGER NOT NULL,
    tmdb_episode_id INTEGER NOT NULL,
    season_number INTEGER NOT NULL,
    episode_number INTEGER NOT NULL,
    name TEXT NOT NULL,
    overview TEXT,
    air_date TEXT,
    runtime_minutes INTEGER,
    still_path TEXT,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    FOREIGN KEY (season_id) REFERENCES seasons(id) ON DELETE CASCADE,
    UNIQUE (series_id, season_number, episode_number)
);

CREATE INDEX IF NOT EXISTS idx_episodes_series_id ON episodes(series_id);
CREATE INDEX IF NOT EXISTS idx_episodes_season_id ON episodes(season_id);
```

---

## Library Entry Table

### library_entries

The central table that represents items in the personal library:

```sql
CREATE TABLE IF NOT EXISTS library_entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    media_type TEXT NOT NULL,             -- 'Movie' or 'Series'
    movie_id INTEGER,                     -- FK to movies, NULL if series
    series_id INTEGER,                    -- FK to series, NULL if movie

    -- Why added metadata
    why_recommended_by_friend_id INTEGER, -- FK to friends
    why_recommended_by_name TEXT,         -- Or just a name string
    why_source TEXT,                      -- "Netflix", "Twitter", etc.
    why_context TEXT,                     -- Free-form note
    why_date_recommended TEXT,

    -- Watch status
    watch_status TEXT NOT NULL DEFAULT 'NotStarted',  -- 'NotStarted', 'InProgress', 'Completed', 'Abandoned'

    -- Progress (for InProgress)
    progress_current_season INTEGER,
    progress_current_episode INTEGER,
    progress_last_watched_date TEXT,

    -- Abandoned info
    abandoned_season INTEGER,
    abandoned_episode INTEGER,
    abandoned_reason TEXT,
    abandoned_date TEXT,

    -- Personal metadata
    personal_rating INTEGER,              -- 1-5 (maps to PersonalRating DU)
    notes TEXT,
    is_favorite INTEGER NOT NULL DEFAULT 0,

    -- Timestamps
    date_added TEXT NOT NULL DEFAULT (datetime('now')),
    date_first_watched TEXT,
    date_last_watched TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),

    FOREIGN KEY (movie_id) REFERENCES movies(id) ON DELETE CASCADE,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    FOREIGN KEY (why_recommended_by_friend_id) REFERENCES friends(id) ON DELETE SET NULL,

    -- Ensure either movie_id or series_id is set (not both)
    CHECK (
        (media_type = 'Movie' AND movie_id IS NOT NULL AND series_id IS NULL) OR
        (media_type = 'Series' AND series_id IS NOT NULL AND movie_id IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS idx_library_entries_media_type ON library_entries(media_type);
CREATE INDEX IF NOT EXISTS idx_library_entries_movie_id ON library_entries(movie_id);
CREATE INDEX IF NOT EXISTS idx_library_entries_series_id ON library_entries(series_id);
CREATE INDEX IF NOT EXISTS idx_library_entries_watch_status ON library_entries(watch_status);
CREATE INDEX IF NOT EXISTS idx_library_entries_personal_rating ON library_entries(personal_rating);
CREATE INDEX IF NOT EXISTS idx_library_entries_date_added ON library_entries(date_added);
CREATE INDEX IF NOT EXISTS idx_library_entries_is_favorite ON library_entries(is_favorite);
```

---

## Tracking Tables

### watch_sessions

Named watch sessions for rewatches:

```sql
CREATE TABLE IF NOT EXISTS watch_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,            -- Must be a series entry
    name TEXT NOT NULL,                   -- "Rewatch with Sarah", "2024 rewatch"
    status TEXT NOT NULL DEFAULT 'Active', -- 'Active', 'Paused', 'Completed'
    start_date TEXT,
    end_date TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_watch_sessions_entry_id ON watch_sessions(entry_id);
CREATE INDEX IF NOT EXISTS idx_watch_sessions_status ON watch_sessions(status);
```

### episode_progress

Tracks watched episodes (per session or globally):

```sql
CREATE TABLE IF NOT EXISTS episode_progress (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    session_id INTEGER,                   -- NULL = default/global progress
    series_id INTEGER NOT NULL,
    season_number INTEGER NOT NULL,
    episode_number INTEGER NOT NULL,
    is_watched INTEGER NOT NULL DEFAULT 0,
    watched_date TEXT,
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    UNIQUE (entry_id, session_id, season_number, episode_number)
);

CREATE INDEX IF NOT EXISTS idx_episode_progress_entry_id ON episode_progress(entry_id);
CREATE INDEX IF NOT EXISTS idx_episode_progress_session_id ON episode_progress(session_id);
CREATE INDEX IF NOT EXISTS idx_episode_progress_is_watched ON episode_progress(is_watched);
```

### watch_history

Log of when things were watched (for timeline):

```sql
CREATE TABLE IF NOT EXISTS watch_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    session_id INTEGER,                   -- NULL for movies or default session
    watched_date TEXT NOT NULL,

    -- For series episodes
    season_number INTEGER,
    episode_number INTEGER,

    -- For movies or full series completion
    is_completion INTEGER NOT NULL DEFAULT 0,  -- Marks full completion

    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_watch_history_entry_id ON watch_history(entry_id);
CREATE INDEX IF NOT EXISTS idx_watch_history_watched_date ON watch_history(watched_date);
CREATE INDEX IF NOT EXISTS idx_watch_history_is_completion ON watch_history(is_completion);
```

---

## People Tables

### friends

```sql
CREATE TABLE IF NOT EXISTS friends (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    nickname TEXT,
    avatar_url TEXT,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_friends_name ON friends(name);
```

### contributors

```sql
CREATE TABLE IF NOT EXISTS contributors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_person_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    profile_path TEXT,                    -- TMDB photo path
    known_for_department TEXT,            -- "Acting", "Directing", etc.
    birthday TEXT,
    deathday TEXT,
    place_of_birth TEXT,
    biography TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_contributors_tmdb_person_id ON contributors(tmdb_person_id);
CREATE INDEX IF NOT EXISTS idx_contributors_name ON contributors(name);
```

### media_contributors

Links contributors to movies/series:

```sql
CREATE TABLE IF NOT EXISTS media_contributors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    contributor_id INTEGER NOT NULL,
    movie_id INTEGER,
    series_id INTEGER,
    role_type TEXT NOT NULL,              -- 'Director', 'Actor', 'Writer', etc.
    role_character TEXT,                  -- Character name for actors
    role_order INTEGER,                   -- Billing order
    FOREIGN KEY (contributor_id) REFERENCES contributors(id) ON DELETE CASCADE,
    FOREIGN KEY (movie_id) REFERENCES movies(id) ON DELETE CASCADE,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    CHECK (
        (movie_id IS NOT NULL AND series_id IS NULL) OR
        (series_id IS NOT NULL AND movie_id IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS idx_media_contributors_contributor_id ON media_contributors(contributor_id);
CREATE INDEX IF NOT EXISTS idx_media_contributors_movie_id ON media_contributors(movie_id);
CREATE INDEX IF NOT EXISTS idx_media_contributors_series_id ON media_contributors(series_id);
CREATE INDEX IF NOT EXISTS idx_media_contributors_role_type ON media_contributors(role_type);
```

---

## Organization Tables

### tags

```sql
CREATE TABLE IF NOT EXISTS tags (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL UNIQUE,
    color TEXT,                           -- Hex color code
    icon TEXT,                            -- Emoji or icon name
    description TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_tags_name ON tags(name);
```

### collections

```sql
CREATE TABLE IF NOT EXISTS collections (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    cover_image_path TEXT,
    is_public_franchise INTEGER NOT NULL DEFAULT 0,  -- MCU, Star Wars, etc.
    tmdb_collection_id INTEGER,           -- If from TMDB
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_collections_name ON collections(name);
CREATE INDEX IF NOT EXISTS idx_collections_is_public_franchise ON collections(is_public_franchise);
```

### collection_items

```sql
CREATE TABLE IF NOT EXISTS collection_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    collection_id INTEGER NOT NULL,
    entry_id INTEGER NOT NULL,
    position INTEGER NOT NULL,            -- Order in collection
    notes TEXT,
    FOREIGN KEY (collection_id) REFERENCES collections(id) ON DELETE CASCADE,
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    UNIQUE (collection_id, entry_id)
);

CREATE INDEX IF NOT EXISTS idx_collection_items_collection_id ON collection_items(collection_id);
CREATE INDEX IF NOT EXISTS idx_collection_items_entry_id ON collection_items(entry_id);
CREATE INDEX IF NOT EXISTS idx_collection_items_position ON collection_items(position);
```

---

## Junction Tables

### entry_tags

```sql
CREATE TABLE IF NOT EXISTS entry_tags (
    entry_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (entry_id, tag_id),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_entry_tags_tag_id ON entry_tags(tag_id);
```

### entry_friends

```sql
CREATE TABLE IF NOT EXISTS entry_friends (
    entry_id INTEGER NOT NULL,
    friend_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (entry_id, friend_id),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_entry_friends_friend_id ON entry_friends(friend_id);
```

### session_tags

```sql
CREATE TABLE IF NOT EXISTS session_tags (
    session_id INTEGER NOT NULL,
    tag_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (session_id, tag_id),
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (tag_id) REFERENCES tags(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_session_tags_tag_id ON session_tags(tag_id);
```

### session_friends

```sql
CREATE TABLE IF NOT EXISTS session_friends (
    session_id INTEGER NOT NULL,
    friend_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (session_id, friend_id),
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_session_friends_friend_id ON session_friends(friend_id);
```

---

## Cache Tables

### tmdb_cache

Cache TMDB API responses to reduce API calls:

```sql
CREATE TABLE IF NOT EXISTS tmdb_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cache_key TEXT NOT NULL UNIQUE,       -- e.g., "movie:550" or "search:star wars"
    cache_value TEXT NOT NULL,            -- JSON response
    expires_at TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_tmdb_cache_key ON tmdb_cache(cache_key);
CREATE INDEX IF NOT EXISTS idx_tmdb_cache_expires ON tmdb_cache(expires_at);
```

### year_reviews

Pre-computed year in review data:

```sql
CREATE TABLE IF NOT EXISTS year_reviews (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    year INTEGER NOT NULL UNIQUE,
    total_minutes INTEGER NOT NULL DEFAULT 0,
    total_movies INTEGER NOT NULL DEFAULT 0,
    total_series INTEGER NOT NULL DEFAULT 0,
    total_episodes INTEGER NOT NULL DEFAULT 0,
    rating_distribution TEXT,             -- JSON: {"5": 10, "4": 20, ...}
    top_tags TEXT,                        -- JSON array
    completed_collections TEXT,           -- JSON array of IDs
    data_json TEXT,                       -- Full computed data
    generated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_year_reviews_year ON year_reviews(year);
```

### stats_cache

General statistics cache:

```sql
CREATE TABLE IF NOT EXISTS stats_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    stat_key TEXT NOT NULL UNIQUE,        -- e.g., "total_watch_time", "backlog_estimate"
    stat_value TEXT NOT NULL,             -- JSON or simple value
    expires_at TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_stats_cache_key ON stats_cache(stat_key);
```

---

## Migration System

### migrations table

Track applied migrations:

```sql
CREATE TABLE IF NOT EXISTS migrations (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    version INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    applied_at TEXT NOT NULL DEFAULT (datetime('now'))
);
```

### Migration Template

```fsharp
// src/Server/Migrations.fs

module Migrations

open Microsoft.Data.Sqlite

type Migration = {
    Version: int
    Name: string
    Up: SqliteConnection -> unit
}

let migrations = [
    {
        Version = 1
        Name = "Initial schema"
        Up = fun conn ->
            // Create all initial tables
            ()
    }
    {
        Version = 2
        Name = "Add tags color column"
        Up = fun conn ->
            use cmd = new SqliteCommand("ALTER TABLE tags ADD COLUMN color TEXT", conn)
            cmd.ExecuteNonQuery() |> ignore
    }
    // ... more migrations
]

let runMigrations (conn: SqliteConnection) =
    // Ensure migrations table exists
    // Get current version
    // Apply pending migrations in order
    ()
```

---

## Common Queries

### Get library entries with filters

```sql
SELECT
    le.*,
    m.title as movie_title,
    m.poster_path as movie_poster,
    m.release_date as movie_release_date,
    s.name as series_name,
    s.poster_path as series_poster,
    s.first_air_date as series_first_air_date
FROM library_entries le
LEFT JOIN movies m ON le.movie_id = m.id
LEFT JOIN series s ON le.series_id = s.id
WHERE le.watch_status = @status
  AND le.personal_rating >= @minRating
ORDER BY le.date_added DESC
LIMIT @pageSize OFFSET @offset;
```

### Get entries by tag

```sql
SELECT le.*, m.*, s.*
FROM library_entries le
LEFT JOIN movies m ON le.movie_id = m.id
LEFT JOIN series s ON le.series_id = s.id
INNER JOIN entry_tags et ON le.id = et.entry_id
WHERE et.tag_id = @tagId
ORDER BY le.date_added DESC;
```

### Get entries watched with friend

```sql
SELECT le.*, m.*, s.*
FROM library_entries le
LEFT JOIN movies m ON le.movie_id = m.id
LEFT JOIN series s ON le.series_id = s.id
INNER JOIN entry_friends ef ON le.id = ef.entry_id
WHERE ef.friend_id = @friendId
ORDER BY le.date_last_watched DESC;
```

### Calculate total watch time

```sql
SELECT
    SUM(CASE
        WHEN le.media_type = 'Movie' THEN m.runtime_minutes
        ELSE (
            SELECT COALESCE(SUM(e.runtime_minutes), 0)
            FROM episodes e
            INNER JOIN episode_progress ep ON e.series_id = ep.series_id
                AND e.season_number = ep.season_number
                AND e.episode_number = ep.episode_number
            WHERE ep.entry_id = le.id AND ep.is_watched = 1
        )
    END) as total_minutes
FROM library_entries le
LEFT JOIN movies m ON le.movie_id = m.id
WHERE le.watch_status IN ('Completed', 'InProgress');
```

### Get filmography progress

```sql
-- Get all works by contributor that user has seen
SELECT mc.*, m.title, s.name, le.id as entry_id
FROM media_contributors mc
LEFT JOIN movies m ON mc.movie_id = m.id
LEFT JOIN series s ON mc.series_id = s.id
LEFT JOIN library_entries le ON (le.movie_id = mc.movie_id OR le.series_id = mc.series_id)
WHERE mc.contributor_id = @contributorId
ORDER BY m.release_date DESC, s.first_air_date DESC;
```

### Timeline query

```sql
SELECT
    wh.watched_date,
    wh.season_number,
    wh.episode_number,
    wh.is_completion,
    le.*,
    m.title as movie_title,
    m.poster_path as movie_poster,
    s.name as series_name,
    s.poster_path as series_poster
FROM watch_history wh
INNER JOIN library_entries le ON wh.entry_id = le.id
LEFT JOIN movies m ON le.movie_id = m.id
LEFT JOIN series s ON le.series_id = s.id
WHERE wh.watched_date BETWEEN @startDate AND @endDate
ORDER BY wh.watched_date DESC
LIMIT @pageSize OFFSET @offset;
```

---

## Data Integrity Rules

1. **Library entries must have either movie_id OR series_id, not both**
   - Enforced by CHECK constraint

2. **Watch sessions must reference series entries**
   - Application-level validation

3. **Episode progress must have valid season/episode numbers**
   - Application-level validation against episodes table

4. **Collection items must be unique per collection**
   - Enforced by UNIQUE constraint

5. **Tags must have unique names**
   - Enforced by UNIQUE constraint

6. **TMDB IDs must be unique per media type**
   - Enforced by UNIQUE constraints on movies.tmdb_id and series.tmdb_id

---

## Performance Considerations

1. **Indexes on foreign keys** - All FK columns have indexes
2. **Indexes on filter columns** - watch_status, personal_rating, date_added
3. **WAL mode** - Better concurrent read/write performance
4. **JSON for flexible data** - genres, rating_distribution stored as JSON
5. **Cache tables** - Pre-compute expensive calculations
6. **Pagination** - All list queries should use LIMIT/OFFSET

---

## Backup Strategy

```bash
# Daily backup script
sqlite3 data/cinemarco.db ".backup data/backups/cinemarco_$(date +%Y%m%d).db"

# Compress older backups
find data/backups -name "*.db" -mtime +7 -exec gzip {} \;

# Remove very old backups
find data/backups -name "*.gz" -mtime +30 -delete
```

---

## Notes for Implementation

1. **Use Dapper** for queries (not raw ADO.NET)
2. **Always parameterize** - Never string concatenate SQL
3. **ISO 8601 dates** - Store as TEXT in SQLite
4. **JSON arrays** - Use System.Text.Json for serialization
5. **Transactions** - Wrap multi-table operations in transactions
6. **Connection per request** - Create new connections, don't share
7. **Run migrations on startup** - In Program.fs initialization
