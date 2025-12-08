module ImageCache

open System
open System.IO
open System.Net.Http

// =====================================
// Image Caching Module
// =====================================
// Downloads and caches images from TMDB locally

/// TMDB image base URL
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Shared HTTP client for downloading images
let private httpClient =
    let client = new HttpClient()
    client.Timeout <- TimeSpan.FromSeconds(30.0)
    client

/// Get the images directory path (inside data directory)
let getImagesDir () =
    let dataDir =
        match Environment.GetEnvironmentVariable("DATA_DIR") with
        | null | "" -> Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "app", "cinemarco")
        | path -> path
    Path.Combine(dataDir, "images")

/// Ensure the images directory exists
let ensureImagesDir () =
    let imagesDir = getImagesDir()
    if not (Directory.Exists imagesDir) then
        Directory.CreateDirectory imagesDir |> ignore
        printfn $"Created images directory: {imagesDir}"

/// Get the subdirectory for a specific image type
let private getImageTypeDir (imageType: string) =
    let dir = Path.Combine(getImagesDir(), imageType)
    if not (Directory.Exists dir) then
        Directory.CreateDirectory dir |> ignore
    dir

/// Convert a TMDB path to a local filename
/// TMDB paths look like "/abc123.jpg" - we strip the leading slash
let private tmdbPathToFilename (tmdbPath: string) =
    if tmdbPath.StartsWith("/") then tmdbPath.Substring(1)
    else tmdbPath

/// Get the local file path for an image
let getLocalImagePath (imageType: string) (tmdbPath: string) =
    let filename = tmdbPathToFilename tmdbPath
    Path.Combine(getImageTypeDir imageType, filename)

/// Check if an image is already cached
let isImageCached (imageType: string) (tmdbPath: string) =
    let localPath = getLocalImagePath imageType tmdbPath
    File.Exists localPath

/// Download an image from TMDB and cache it locally
let downloadImage (imageType: string) (size: string) (tmdbPath: string) : Async<Result<string, string>> = async {
    try
        ensureImagesDir()
        let localPath = getLocalImagePath imageType tmdbPath

        // Check if already cached
        if File.Exists localPath then
            return Ok localPath
        else
            // Build TMDB URL
            let url = $"{tmdbImageBase}/{size}{tmdbPath}"

            // Download the image
            let! response = httpClient.GetAsync(url) |> Async.AwaitTask

            if response.IsSuccessStatusCode then
                let! bytes = response.Content.ReadAsByteArrayAsync() |> Async.AwaitTask
                do! File.WriteAllBytesAsync(localPath, bytes) |> Async.AwaitTask
                printfn $"Cached image: {tmdbPath} -> {localPath}"
                return Ok localPath
            else
                return Error $"Failed to download image: {response.StatusCode}"
    with
    | ex -> return Error $"Error downloading image: {ex.Message}"
}

/// Download a poster image (uses w500 size for good quality)
let downloadPoster (tmdbPath: string option) : Async<unit> = async {
    match tmdbPath with
    | None -> ()
    | Some path ->
        let! _ = downloadImage "posters" "w500" path
        ()
}

/// Download a backdrop image (uses w1280 size)
let downloadBackdrop (tmdbPath: string option) : Async<unit> = async {
    match tmdbPath with
    | None -> ()
    | Some path ->
        let! _ = downloadImage "backdrops" "w1280" path
        ()
}

/// Download a profile image (uses w185 size)
let downloadProfile (tmdbPath: string option) : Async<unit> = async {
    match tmdbPath with
    | None -> ()
    | Some path ->
        let! _ = downloadImage "profiles" "w185" path
        ()
}

/// Get the cached image bytes, or None if not cached
let getCachedImage (imageType: string) (tmdbPath: string) : byte[] option =
    let localPath = getLocalImagePath imageType tmdbPath
    if File.Exists localPath then
        Some (File.ReadAllBytes localPath)
    else
        None

/// Save a collection logo from base64 data
/// Returns the path to store in the database (e.g., "/collections/abc123.png")
let saveCollectionLogo (collectionId: int) (base64Data: string) : Result<string, string> =
    try
        // Parse the base64 data (may include data URL prefix)
        let cleanBase64, extension =
            if base64Data.StartsWith("data:image/") then
                let parts = base64Data.Split([|','|], 2)
                if parts.Length = 2 then
                    let mimeType = parts.[0].Replace("data:", "").Replace(";base64", "")
                    let ext = match mimeType with
                              | "image/png" -> ".png"
                              | "image/jpeg" | "image/jpg" -> ".jpg"
                              | "image/gif" -> ".gif"
                              | "image/webp" -> ".webp"
                              | _ -> ".png"
                    parts.[1], ext
                else
                    base64Data, ".png"
            else
                base64Data, ".png"

        let bytes = Convert.FromBase64String(cleanBase64)
        let filename = $"{collectionId}{extension}"
        let collectionsDir = getImageTypeDir "collections"
        let localPath = Path.Combine(collectionsDir, filename)

        // Delete any existing logo for this collection (different extension)
        for ext in [".png"; ".jpg"; ".gif"; ".webp"] do
            let existingPath = Path.Combine(collectionsDir, $"{collectionId}{ext}")
            if File.Exists(existingPath) then
                File.Delete(existingPath)

        File.WriteAllBytes(localPath, bytes)
        printfn $"Saved collection logo: {localPath}"
        Ok $"/{filename}"
    with ex ->
        Error $"Failed to save logo: {ex.Message}"

/// Delete a collection logo
let deleteCollectionLogo (logoPath: string) =
    try
        if not (String.IsNullOrEmpty logoPath) then
            let filename = if logoPath.StartsWith("/") then logoPath.Substring(1) else logoPath
            let localPath = Path.Combine(getImageTypeDir "collections", filename)
            if File.Exists(localPath) then
                File.Delete(localPath)
                printfn $"Deleted collection logo: {localPath}"
    with ex ->
        printfn $"Failed to delete logo: {ex.Message}"

/// Save a friend avatar from base64 data
/// Returns the path to store in the database (e.g., "/avatars/123.png")
let saveFriendAvatar (friendId: int) (base64Data: string) : Result<string, string> =
    try
        // Parse the base64 data (may include data URL prefix)
        let cleanBase64, extension =
            if base64Data.StartsWith("data:image/") then
                let parts = base64Data.Split([|','|], 2)
                if parts.Length = 2 then
                    let mimeType = parts.[0].Replace("data:", "").Replace(";base64", "")
                    let ext = match mimeType with
                              | "image/png" -> ".png"
                              | "image/jpeg" | "image/jpg" -> ".jpg"
                              | "image/gif" -> ".gif"
                              | "image/webp" -> ".webp"
                              | _ -> ".png"
                    parts.[1], ext
                else
                    base64Data, ".png"
            else
                base64Data, ".png"

        let bytes = Convert.FromBase64String(cleanBase64)
        let filename = $"{friendId}{extension}"
        let avatarsDir = getImageTypeDir "avatars"
        let localPath = Path.Combine(avatarsDir, filename)

        // Delete any existing avatar for this friend (different extension)
        for ext in [".png"; ".jpg"; ".gif"; ".webp"] do
            let existingPath = Path.Combine(avatarsDir, $"{friendId}{ext}")
            if File.Exists(existingPath) then
                File.Delete(existingPath)

        File.WriteAllBytes(localPath, bytes)
        printfn $"Saved friend avatar: {localPath}"
        Ok $"/{filename}"
    with ex ->
        Error $"Failed to save avatar: {ex.Message}"

/// Delete a friend avatar
let deleteFriendAvatar (avatarPath: string) =
    try
        if not (String.IsNullOrEmpty avatarPath) then
            let filename = if avatarPath.StartsWith("/") then avatarPath.Substring(1) else avatarPath
            let localPath = Path.Combine(getImageTypeDir "avatars", filename)
            if File.Exists(localPath) then
                File.Delete(localPath)
                printfn $"Deleted friend avatar: {localPath}"
    with ex ->
        printfn $"Failed to delete avatar: {ex.Message}"

/// Get content type for an image based on extension
let getContentType (filename: string) =
    match Path.GetExtension(filename).ToLowerInvariant() with
    | ".jpg" | ".jpeg" -> "image/jpeg"
    | ".png" -> "image/png"
    | ".gif" -> "image/gif"
    | ".webp" -> "image/webp"
    | _ -> "application/octet-stream"

/// Delete orphaned images that are no longer referenced in the database
/// Returns the number of files deleted and bytes freed
let deleteOrphanedImages (referencedPosters: Set<string>) (referencedBackdrops: Set<string>) : int * int64 =
    let mutable filesDeleted = 0
    let mutable bytesFreed = 0L

    // Helper to convert local filename back to TMDB path format
    let filenameToTmdbPath (filename: string) = "/" + filename

    // Clean up posters
    let postersDir = Path.Combine(getImagesDir(), "posters")
    if Directory.Exists(postersDir) then
        for file in Directory.GetFiles(postersDir) do
            let filename = Path.GetFileName(file)
            let tmdbPath = filenameToTmdbPath filename
            if not (referencedPosters.Contains(tmdbPath)) then
                try
                    let fileInfo = FileInfo(file)
                    bytesFreed <- bytesFreed + fileInfo.Length
                    File.Delete(file)
                    filesDeleted <- filesDeleted + 1
                    printfn $"Deleted orphaned poster: {filename}"
                with ex ->
                    printfn $"Failed to delete poster {filename}: {ex.Message}"

    // Clean up backdrops
    let backdropsDir = Path.Combine(getImagesDir(), "backdrops")
    if Directory.Exists(backdropsDir) then
        for file in Directory.GetFiles(backdropsDir) do
            let filename = Path.GetFileName(file)
            let tmdbPath = filenameToTmdbPath filename
            if not (referencedBackdrops.Contains(tmdbPath)) then
                try
                    let fileInfo = FileInfo(file)
                    bytesFreed <- bytesFreed + fileInfo.Length
                    File.Delete(file)
                    filesDeleted <- filesDeleted + 1
                    printfn $"Deleted orphaned backdrop: {filename}"
                with ex ->
                    printfn $"Failed to delete backdrop {filename}: {ex.Message}"

    filesDeleted, bytesFreed

/// Get cache statistics
let getCacheStats () =
    let imagesDir = getImagesDir()
    if Directory.Exists imagesDir then
        let files = Directory.GetFiles(imagesDir, "*", SearchOption.AllDirectories)
        let totalSize = files |> Array.sumBy (fun f -> FileInfo(f).Length)
        {| FileCount = files.Length; TotalSizeBytes = totalSize |}
    else
        {| FileCount = 0; TotalSizeBytes = 0L |}
