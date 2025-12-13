module Pages.Cache.Types

open Common.Types
open Shared.Domain

type Model = {
    Entries: RemoteData<CacheEntry list>
    Stats: RemoteData<CacheStats>
    IsClearing: bool
    IsRecalculating: bool
}

type Msg =
    | LoadCache
    | CacheLoaded of Result<CacheEntry list * CacheStats, string>
    | ClearAllCache
    | ClearExpiredCache
    | CacheCleared of Result<ClearCacheResult, string>
    | RecalculateSeriesWatchStatus
    | RecalculateComplete of Result<int, string>

type ExternalMsg =
    | NoOp
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let empty = {
        Entries = NotAsked
        Stats = NotAsked
        IsClearing = false
        IsRecalculating = false
    }
