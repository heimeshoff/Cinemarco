module Pages.Home.State

open System
open Elmish
open Common.Types
open Shared.Domain
open Types

type HomeApi = {
    GetLibrary: unit -> Async<LibraryEntry list>
    GetTraktSyncStatus: unit -> Async<TraktSyncStatus>
    TraktIncrementalSync: unit -> Async<Result<TraktSyncResult, string>>
    TmdbHealthCheck: unit -> Async<Result<string, string>>
}

let init () : Model * Cmd<Msg> =
    // Load library, check Trakt sync status, and check TMDB health
    let cmds = Cmd.batch [
        Cmd.ofMsg LoadLibrary
        Cmd.ofMsg CheckTraktSync
        Cmd.ofMsg CheckTmdbHealth
    ]
    Model.empty, cmds

let update (api: HomeApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadLibrary ->
        let cmd =
            Cmd.OfAsync.either
                api.GetLibrary
                ()
                (Ok >> LibraryLoaded)
                (fun ex -> Error ex.Message |> LibraryLoaded)
        { model with Library = Loading }, cmd, NoOp

    | LibraryLoaded (Ok entries) ->
        { model with Library = Success entries }, Cmd.none, NoOp

    | LibraryLoaded (Error err) ->
        { model with Library = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title, releaseDate) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title, releaseDate)

    | ViewSeriesDetail (entryId, name, firstAirDate) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name, firstAirDate)

    | ViewLibrary ->
        model, Cmd.none, NavigateToLibrary

    | ViewYearInReview ->
        model, Cmd.none, NavigateToYearInReview

    // Trakt sync handling
    | CheckTraktSync ->
        let cmd =
            Cmd.OfAsync.either
                api.GetTraktSyncStatus
                ()
                (Ok >> TraktSyncStatusReceived)
                (fun ex -> Error ex.Message |> TraktSyncStatusReceived)
        { model with TraktSync = SyncChecking }, cmd, NoOp

    | TraktSyncStatusReceived (Ok status) ->
        // Store the status for display
        let model' = { model with TraktStatus = Some status }

        if status.IsAuthenticated && status.AutoSyncEnabled then
            // Only auto-sync if last sync was more than 5 minutes ago
            let shouldSync =
                match status.LastSyncAt with
                | None -> true  // Never synced before
                | Some lastSync ->
                    let minutesSinceLastSync = (DateTime.UtcNow - lastSync).TotalMinutes
                    minutesSinceLastSync >= 5.0

            if shouldSync then
                // Start incremental sync
                let cmd =
                    Cmd.OfAsync.either
                        api.TraktIncrementalSync
                        ()
                        TraktSyncCompleted
                        (fun ex -> Error ex.Message |> TraktSyncCompleted)
                { model' with TraktSync = Syncing }, cmd, NoOp
            else
                // Synced recently, skip
                { model' with TraktSync = SyncIdle }, Cmd.none, NoOp
        else
            // Not authenticated or auto-sync disabled
            { model' with TraktSync = SyncIdle }, Cmd.none, NoOp

    | TraktSyncStatusReceived (Error _) ->
        // Failed to check status, just continue
        { model with TraktSync = SyncIdle }, Cmd.none, NoOp

    | TraktSyncCompleted (Ok result) ->
        // Update the last sync time in status
        let updatedStatus =
            model.TraktStatus |> Option.map (fun s -> { s with LastSyncAt = Some DateTime.UtcNow })
        let hasChanges = result.NewMovieWatches > 0 || result.NewEpisodeWatches > 0
        if hasChanges then
            // Reload library to show new items
            let cmd = Cmd.ofMsg LoadLibrary
            { model with TraktSync = SyncComplete result; TraktStatus = updatedStatus }, cmd, NoOp
        else
            { model with TraktSync = SyncComplete result; TraktStatus = updatedStatus }, Cmd.none, NoOp

    | TraktSyncCompleted (Error err) ->
        { model with TraktSync = SyncError err }, Cmd.none, NoOp

    | DismissSyncNotification ->
        { model with TraktSync = SyncIdle }, Cmd.none, NoOp

    | ManualSync ->
        // User clicked sync button - always sync regardless of time
        match model.TraktStatus with
        | Some status when status.IsAuthenticated ->
            let cmd =
                Cmd.OfAsync.either
                    api.TraktIncrementalSync
                    ()
                    TraktSyncCompleted
                    (fun ex -> Error ex.Message |> TraktSyncCompleted)
            { model with TraktSync = Syncing }, cmd, NoOp
        | _ ->
            // Not authenticated
            model, Cmd.none, NoOp

    // TMDB health check
    | CheckTmdbHealth ->
        let cmd =
            Cmd.OfAsync.either
                api.TmdbHealthCheck
                ()
                TmdbHealthReceived
                (fun ex -> Error ex.Message |> TmdbHealthReceived)
        { model with TmdbHealth = TmdbChecking }, cmd, NoOp

    | TmdbHealthReceived (Ok _) ->
        { model with TmdbHealth = TmdbConnected }, Cmd.none, NoOp

    | TmdbHealthReceived (Error err) ->
        { model with TmdbHealth = TmdbError err }, Cmd.none, NoOp
