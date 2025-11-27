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

/// Get content type for an image based on extension
let getContentType (filename: string) =
    match Path.GetExtension(filename).ToLowerInvariant() with
    | ".jpg" | ".jpeg" -> "image/jpeg"
    | ".png" -> "image/png"
    | ".gif" -> "image/gif"
    | ".webp" -> "image/webp"
    | _ -> "application/octet-stream"

/// Delete all cached images (for cleanup)
let clearCache () =
    let imagesDir = getImagesDir()
    if Directory.Exists imagesDir then
        Directory.Delete(imagesDir, true)
        printfn "Image cache cleared"

/// Get cache statistics
let getCacheStats () =
    let imagesDir = getImagesDir()
    if Directory.Exists imagesDir then
        let files = Directory.GetFiles(imagesDir, "*", SearchOption.AllDirectories)
        let totalSize = files |> Array.sumBy (fun f -> FileInfo(f).Length)
        {| FileCount = files.Length; TotalSizeBytes = totalSize |}
    else
        {| FileCount = 0; TotalSizeBytes = 0L |}
