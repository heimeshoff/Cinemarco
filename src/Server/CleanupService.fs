module CleanupService

open System
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

/// Background service that runs daily cleanup tasks:
/// - Clear expired TMDB cache entries
/// - Delete orphaned poster/backdrop images
type DailyCleanupService(logger: ILogger<DailyCleanupService>) =
    inherit BackgroundService()

    let cleanupInterval = TimeSpan.FromHours(24.0)

    /// Run the cleanup tasks
    let runCleanup () = async {
        logger.LogInformation("Starting daily cleanup...")

        // 1. Clear expired cache entries
        let! cacheResult = Persistence.clearExpiredCacheWithStats()
        if cacheResult.EntriesRemoved > 0 then
            let sizeKb = float cacheResult.BytesFreed / 1024.0
            logger.LogInformation(
                "Cleared {Count} expired cache entries ({Size:F1} KB freed)",
                cacheResult.EntriesRemoved,
                sizeKb
            )
        else
            logger.LogInformation("No expired cache entries to clear")

        // 2. Delete orphaned images
        let! (referencedPosters, referencedBackdrops) = Persistence.getAllReferencedImagePaths()
        let filesDeleted, bytesFreed = ImageCache.deleteOrphanedImages referencedPosters referencedBackdrops
        if filesDeleted > 0 then
            let sizeMb = float bytesFreed / (1024.0 * 1024.0)
            logger.LogInformation(
                "Deleted {Count} orphaned images ({Size:F2} MB freed)",
                filesDeleted,
                sizeMb
            )
        else
            logger.LogInformation("No orphaned images to delete")

        logger.LogInformation("Daily cleanup completed")
    }

    override _.ExecuteAsync(stoppingToken: CancellationToken) =
        task {
            logger.LogInformation("Daily cleanup service started (interval: {Interval} hours)", cleanupInterval.TotalHours)

            // Run cleanup immediately on startup
            try
                do! runCleanup() |> Async.StartAsTask
            with ex ->
                logger.LogError(ex, "Error during initial cleanup")

            // Then run periodically
            while not stoppingToken.IsCancellationRequested do
                try
                    do! Task.Delay(cleanupInterval, stoppingToken)
                    do! runCleanup() |> Async.StartAsTask
                with
                | :? OperationCanceledException -> ()
                | ex -> logger.LogError(ex, "Error during scheduled cleanup")
        }
