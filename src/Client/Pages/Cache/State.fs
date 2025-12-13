module Pages.Cache.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type CacheApi = {
    GetEntries: unit -> Async<CacheEntry list>
    GetStats: unit -> Async<CacheStats>
    ClearAll: unit -> Async<ClearCacheResult>
    ClearExpired: unit -> Async<ClearCacheResult>
    RecalculateSeriesWatchStatus: unit -> Async<int>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadCache

let update (api: CacheApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCache ->
        let cmd =
            Cmd.OfAsync.either
                (fun () -> async {
                    let! entries = api.GetEntries ()
                    let! stats = api.GetStats ()
                    return entries, stats
                })
                ()
                (Ok >> CacheLoaded)
                (fun ex -> Error ex.Message |> CacheLoaded)
        { model with Entries = Loading; Stats = Loading }, cmd, NoOp

    | CacheLoaded (Ok (entries, stats)) ->
        { model with Entries = Success entries; Stats = Success stats }, Cmd.none, NoOp

    | CacheLoaded (Error err) ->
        { model with Entries = Failure err; Stats = Failure err }, Cmd.none, NoOp

    | ClearAllCache ->
        let cmd =
            Cmd.OfAsync.either
                api.ClearAll
                ()
                (Ok >> CacheCleared)
                (fun ex -> Error ex.Message |> CacheCleared)
        { model with IsClearing = true }, cmd, NoOp

    | ClearExpiredCache ->
        let cmd =
            Cmd.OfAsync.either
                api.ClearExpired
                ()
                (Ok >> CacheCleared)
                (fun ex -> Error ex.Message |> CacheCleared)
        { model with IsClearing = true }, cmd, NoOp

    | CacheCleared (Ok result) ->
        let message =
            let sizeKb = float result.BytesFreed / 1024.0
            sprintf "Cleared %d entries (%.1f KB freed)" result.EntriesRemoved sizeKb
        { model with IsClearing = false },
        Cmd.ofMsg LoadCache,
        ShowNotification (message, true)

    | CacheCleared (Error err) ->
        { model with IsClearing = false },
        Cmd.none,
        ShowNotification ($"Failed to clear cache: {err}", false)

    | RecalculateSeriesWatchStatus ->
        let cmd =
            Cmd.OfAsync.either
                api.RecalculateSeriesWatchStatus
                ()
                (Ok >> RecalculateComplete)
                (fun ex -> Error ex.Message |> RecalculateComplete)
        { model with IsRecalculating = true }, cmd, NoOp

    | RecalculateComplete (Ok count) ->
        { model with IsRecalculating = false },
        Cmd.none,
        ShowNotification ($"Recalculated watch status for {count} series", true)

    | RecalculateComplete (Error err) ->
        { model with IsRecalculating = false },
        Cmd.none,
        ShowNotification ($"Failed to recalculate: {err}", false)
