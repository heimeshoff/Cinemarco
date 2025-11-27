module Persistence

open System
open System.IO

// =====================================
// Cinemarco Persistence Layer
// =====================================
// This module will contain database operations.
// Full implementation will be added in Milestone 2.

/// Data directory is configurable via DATA_DIR environment variable
/// Default: ~/app/data/
let private dataDir =
    match Environment.GetEnvironmentVariable("DATA_DIR") with
    | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "app", "cinemarco")
    | path -> path

/// Path to the SQLite database file
let private dbFile = Path.Combine(dataDir, "cinemarco.db")

/// Get the absolute path to the database file
let getDatabasePath () = dbFile

/// Ensure the data directory exists
let ensureDataDir () =
    if not (Directory.Exists dataDir) then
        Directory.CreateDirectory dataDir |> ignore
        printfn $"Created data directory: {dataDir}"
