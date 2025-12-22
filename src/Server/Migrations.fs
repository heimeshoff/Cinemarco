module Migrations

open System
open Microsoft.Data.Sqlite

// =====================================
// Migration System for Cinemarco
// =====================================
// Clean initial schema - no backwards compatibility
// Future migrations can be added as Version 2, 3, etc.

/// A single migration with version and SQL
type Migration = {
    Version: int
    Name: string
    Up: string
}

/// All migrations in order
let private migrations = [
    // Migration 1: Complete initial schema (fresh start)
    {
        Version = 1
        Name = "Initial schema - complete database structure"
        Up = """
-- Enable foreign key constraints
PRAGMA foreign_keys=ON;

-- =============================================
-- CORE MEDIA TABLES
-- =============================================

-- Movies table
CREATE TABLE IF NOT EXISTS movies (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_id INTEGER NOT NULL UNIQUE,
    title TEXT NOT NULL,
    original_title TEXT,
    overview TEXT,
    release_date TEXT,
    runtime_minutes INTEGER,
    poster_path TEXT,
    backdrop_path TEXT,
    genres TEXT,
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

-- Series table
CREATE TABLE IF NOT EXISTS series (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    original_name TEXT,
    overview TEXT,
    first_air_date TEXT,
    last_air_date TEXT,
    poster_path TEXT,
    backdrop_path TEXT,
    genres TEXT,
    original_language TEXT,
    vote_average REAL,
    vote_count INTEGER,
    status TEXT,
    number_of_seasons INTEGER NOT NULL DEFAULT 0,
    number_of_episodes INTEGER NOT NULL DEFAULT 0,
    episode_runtime_minutes INTEGER,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_series_tmdb_id ON series(tmdb_id);
CREATE INDEX IF NOT EXISTS idx_series_name ON series(name);
CREATE INDEX IF NOT EXISTS idx_series_first_air_date ON series(first_air_date);

-- Seasons table
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

-- Episodes table
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

-- =============================================
-- FRIENDS
-- =============================================

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

-- =============================================
-- LIBRARY & WATCH TRACKING
-- =============================================

-- Library entries table
CREATE TABLE IF NOT EXISTS library_entries (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    media_type TEXT NOT NULL,
    movie_id INTEGER,
    series_id INTEGER,
    why_recommended_by_friend_id INTEGER,
    why_recommended_by_name TEXT,
    why_source TEXT,
    why_context TEXT,
    why_date_recommended TEXT,
    watch_status TEXT NOT NULL DEFAULT 'NotStarted',
    progress_current_season INTEGER,
    progress_current_episode INTEGER,
    progress_last_watched_date TEXT,
    abandoned_season INTEGER,
    abandoned_episode INTEGER,
    abandoned_reason TEXT,
    abandoned_date TEXT,
    personal_rating INTEGER,
    notes TEXT,
    is_favorite INTEGER NOT NULL DEFAULT 0,
    date_added TEXT NOT NULL DEFAULT (datetime('now')),
    date_first_watched TEXT,
    date_last_watched TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (movie_id) REFERENCES movies(id) ON DELETE CASCADE,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    FOREIGN KEY (why_recommended_by_friend_id) REFERENCES friends(id) ON DELETE SET NULL,
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

-- Watch sessions table (for series)
CREATE TABLE IF NOT EXISTS watch_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    name TEXT NOT NULL,
    status TEXT NOT NULL DEFAULT 'Active',
    start_date TEXT,
    end_date TEXT,
    notes TEXT,
    is_default INTEGER NOT NULL DEFAULT 0,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_watch_sessions_entry_id ON watch_sessions(entry_id);
CREATE INDEX IF NOT EXISTS idx_watch_sessions_status ON watch_sessions(status);
CREATE INDEX IF NOT EXISTS idx_watch_sessions_is_default ON watch_sessions(is_default);

-- Episode progress table
CREATE TABLE IF NOT EXISTS episode_progress (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    session_id INTEGER NOT NULL,
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

-- Watch history table (for timeline)
CREATE TABLE IF NOT EXISTS watch_history (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    session_id INTEGER,
    watched_date TEXT NOT NULL,
    season_number INTEGER,
    episode_number INTEGER,
    is_completion INTEGER NOT NULL DEFAULT 0,
    notes TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_watch_history_entry_id ON watch_history(entry_id);
CREATE INDEX IF NOT EXISTS idx_watch_history_watched_date ON watch_history(watched_date);
CREATE INDEX IF NOT EXISTS idx_watch_history_is_completion ON watch_history(is_completion);

-- Movie watch sessions table (for tracking individual viewings)
CREATE TABLE IF NOT EXISTS movie_watch_sessions (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    entry_id INTEGER NOT NULL,
    watched_date TEXT NOT NULL,
    name TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_movie_watch_sessions_entry_id ON movie_watch_sessions(entry_id);
CREATE INDEX IF NOT EXISTS idx_movie_watch_sessions_watched_date ON movie_watch_sessions(watched_date);

-- =============================================
-- CONTRIBUTORS
-- =============================================

CREATE TABLE IF NOT EXISTS contributors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    tmdb_person_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    profile_path TEXT,
    known_for_department TEXT,
    birthday TEXT,
    deathday TEXT,
    place_of_birth TEXT,
    biography TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_contributors_tmdb_person_id ON contributors(tmdb_person_id);
CREATE INDEX IF NOT EXISTS idx_contributors_name ON contributors(name);

-- Media contributors (links contributors to movies/series)
CREATE TABLE IF NOT EXISTS media_contributors (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    contributor_id INTEGER NOT NULL,
    movie_id INTEGER,
    series_id INTEGER,
    role_type TEXT NOT NULL,
    role_character TEXT,
    role_order INTEGER,
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

-- Tracked contributors (personal tracking)
CREATE TABLE IF NOT EXISTS tracked_contributors (
    id TEXT PRIMARY KEY,
    tmdb_person_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    profile_path TEXT,
    known_for_department TEXT,
    created_at TEXT NOT NULL,
    notes TEXT
);

CREATE INDEX IF NOT EXISTS idx_tracked_contributors_tmdb_id ON tracked_contributors(tmdb_person_id);

-- =============================================
-- COLLECTIONS
-- =============================================

CREATE TABLE IF NOT EXISTS collections (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    description TEXT,
    cover_image_path TEXT,
    is_public_franchise INTEGER NOT NULL DEFAULT 0,
    tmdb_collection_id INTEGER,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_collections_name ON collections(name);
CREATE INDEX IF NOT EXISTS idx_collections_is_public_franchise ON collections(is_public_franchise);

-- Collection items (supports entries, seasons, and episodes)
CREATE TABLE IF NOT EXISTS collection_items (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    collection_id INTEGER NOT NULL,
    item_type TEXT NOT NULL DEFAULT 'entry',  -- 'entry', 'season', 'episode'
    entry_id INTEGER,                          -- For library entries (movies/series)
    series_id INTEGER,                         -- For seasons and episodes
    season_number INTEGER,                     -- For seasons and episodes
    episode_number INTEGER,                    -- For episodes only
    position INTEGER NOT NULL,
    notes TEXT,
    FOREIGN KEY (collection_id) REFERENCES collections(id) ON DELETE CASCADE,
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (series_id) REFERENCES series(id) ON DELETE CASCADE,
    CHECK (
        (item_type = 'entry' AND entry_id IS NOT NULL AND series_id IS NULL) OR
        (item_type = 'season' AND series_id IS NOT NULL AND season_number IS NOT NULL AND entry_id IS NULL) OR
        (item_type = 'episode' AND series_id IS NOT NULL AND season_number IS NOT NULL AND episode_number IS NOT NULL AND entry_id IS NULL)
    )
);

CREATE INDEX IF NOT EXISTS idx_collection_items_collection_id ON collection_items(collection_id);
CREATE INDEX IF NOT EXISTS idx_collection_items_entry_id ON collection_items(entry_id);
CREATE INDEX IF NOT EXISTS idx_collection_items_series_id ON collection_items(series_id);
CREATE INDEX IF NOT EXISTS idx_collection_items_position ON collection_items(position);
CREATE INDEX IF NOT EXISTS idx_collection_items_item_type ON collection_items(item_type);

-- =============================================
-- JUNCTION TABLES
-- =============================================

-- Entry friends junction table
CREATE TABLE IF NOT EXISTS entry_friends (
    entry_id INTEGER NOT NULL,
    friend_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (entry_id, friend_id),
    FOREIGN KEY (entry_id) REFERENCES library_entries(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_entry_friends_friend_id ON entry_friends(friend_id);

-- Session friends junction table
CREATE TABLE IF NOT EXISTS session_friends (
    session_id INTEGER NOT NULL,
    friend_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (session_id, friend_id),
    FOREIGN KEY (session_id) REFERENCES watch_sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_session_friends_friend_id ON session_friends(friend_id);

-- Movie session friends junction table
CREATE TABLE IF NOT EXISTS movie_session_friends (
    session_id INTEGER NOT NULL,
    friend_id INTEGER NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    PRIMARY KEY (session_id, friend_id),
    FOREIGN KEY (session_id) REFERENCES movie_watch_sessions(id) ON DELETE CASCADE,
    FOREIGN KEY (friend_id) REFERENCES friends(id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_movie_session_friends_friend_id ON movie_session_friends(friend_id);

-- =============================================
-- CACHE & STATISTICS
-- =============================================

-- TMDB cache table
CREATE TABLE IF NOT EXISTS tmdb_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    cache_key TEXT NOT NULL UNIQUE,
    cache_value TEXT NOT NULL,
    expires_at TEXT NOT NULL,
    created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_tmdb_cache_key ON tmdb_cache(cache_key);
CREATE INDEX IF NOT EXISTS idx_tmdb_cache_expires ON tmdb_cache(expires_at);

-- Year reviews table
CREATE TABLE IF NOT EXISTS year_reviews (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    year INTEGER NOT NULL UNIQUE,
    total_minutes INTEGER NOT NULL DEFAULT 0,
    total_movies INTEGER NOT NULL DEFAULT 0,
    total_series INTEGER NOT NULL DEFAULT 0,
    total_episodes INTEGER NOT NULL DEFAULT 0,
    rating_distribution TEXT,
    top_tags TEXT,
    completed_collections TEXT,
    data_json TEXT,
    generated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_year_reviews_year ON year_reviews(year);

-- Stats cache table
CREATE TABLE IF NOT EXISTS stats_cache (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    stat_key TEXT NOT NULL UNIQUE,
    stat_value TEXT NOT NULL,
    expires_at TEXT,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_stats_cache_key ON stats_cache(stat_key);

-- =============================================
-- INTEGRATIONS
-- =============================================

-- Trakt settings table (OAuth tokens and sync state)
CREATE TABLE IF NOT EXISTS trakt_settings (
    id INTEGER PRIMARY KEY CHECK (id = 1),  -- Only one row allowed
    access_token TEXT,
    refresh_token TEXT,
    token_expires_at TEXT,
    last_sync_at TEXT,
    auto_sync_enabled INTEGER NOT NULL DEFAULT 1,
    created_at TEXT NOT NULL DEFAULT (datetime('now')),
    updated_at TEXT NOT NULL DEFAULT (datetime('now'))
);

-- Insert default Trakt settings row
INSERT OR IGNORE INTO trakt_settings (id, auto_sync_enabled) VALUES (1, 1);
"""
    }
]

/// Create the migrations tracking table if it doesn't exist
let private createMigrationsTable (conn: SqliteConnection) =
    use cmd = new SqliteCommand("""
        CREATE TABLE IF NOT EXISTS migrations (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            version INTEGER NOT NULL UNIQUE,
            name TEXT NOT NULL,
            applied_at TEXT NOT NULL DEFAULT (datetime('now'))
        );
    """, conn)
    cmd.ExecuteNonQuery() |> ignore

/// Get the current database version
let private getCurrentVersion (conn: SqliteConnection) : int =
    use cmd = new SqliteCommand("SELECT MAX(version) FROM migrations", conn)
    let result = cmd.ExecuteScalar()
    if result = DBNull.Value || isNull result then 0
    else result :?> int64 |> int

/// Record a migration as applied
let private recordMigration (conn: SqliteConnection) (transaction: SqliteTransaction) (migration: Migration) =
    use cmd = new SqliteCommand(
        "INSERT INTO migrations (version, name) VALUES (@Version, @Name)",
        conn,
        transaction
    )
    cmd.Parameters.AddWithValue("@Version", migration.Version) |> ignore
    cmd.Parameters.AddWithValue("@Name", migration.Name) |> ignore
    cmd.ExecuteNonQuery() |> ignore

/// Execute a migration's SQL statements
let private executeMigration (conn: SqliteConnection) (transaction: SqliteTransaction) (migration: Migration) =
    // Split the migration into individual statements
    let statements =
        migration.Up.Split([| ";" |], StringSplitOptions.RemoveEmptyEntries)
        |> Array.map (fun s -> s.Trim())
        |> Array.filter (fun s -> not (String.IsNullOrWhiteSpace s))

    for sql in statements do
        // Skip PRAGMA statements that can't be in transactions or are already handled
        if not (sql.ToUpperInvariant().StartsWith("PRAGMA")) then
            use cmd = new SqliteCommand(sql, conn, transaction)
            cmd.ExecuteNonQuery() |> ignore

/// Run all pending migrations
let runMigrations (connectionString: string) =
    use conn = new SqliteConnection(connectionString)
    conn.Open()

    // Enable foreign keys
    use pragmaCmd = new SqliteCommand("PRAGMA foreign_keys=ON", conn)
    pragmaCmd.ExecuteNonQuery() |> ignore

    // Ensure migrations table exists
    createMigrationsTable conn

    let currentVersion = getCurrentVersion conn
    printfn $"Current database version: {currentVersion}"

    let pendingMigrations =
        migrations
        |> List.filter (fun m -> m.Version > currentVersion)
        |> List.sortBy (fun m -> m.Version)

    if List.isEmpty pendingMigrations then
        printfn "Database is up to date"
    else
        printfn $"Running {List.length pendingMigrations} pending migration(s)..."

        for migration in pendingMigrations do
            printfn $"  Applying migration {migration.Version}: {migration.Name}"

            use transaction = conn.BeginTransaction()
            try
                executeMigration conn transaction migration
                recordMigration conn transaction migration
                transaction.Commit()
                printfn $"  Migration {migration.Version} applied successfully"
            with ex ->
                transaction.Rollback()
                printfn $"  ERROR applying migration {migration.Version}: {ex.Message}"
                reraise()

        printfn "All migrations applied successfully"

/// Get the list of all migrations
let getAllMigrations () = migrations

/// Get the count of migrations
let getMigrationCount () = List.length migrations
