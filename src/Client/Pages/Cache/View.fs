module Pages.Cache.View

open System
open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons

module GlassButton = Common.Components.GlassButton.View

/// Format bytes to human-readable string
let private formatBytes (bytes: int) : string =
    if bytes < 1024 then sprintf "%d B" bytes
    elif bytes < 1024 * 1024 then sprintf "%.1f KB" (float bytes / 1024.0)
    else sprintf "%.2f MB" (float bytes / (1024.0 * 1024.0))

/// Format cache key for display (extract type and identifier)
let private formatCacheKey (key: string) : string * string =
    match key.IndexOf(':') with
    | -1 -> key, ""
    | i ->
        let keyType = key.Substring(0, i)
        let keyId = key.Substring(i + 1)
        keyType, keyId

/// Check if a cache entry is expired
let private isExpired (entry: CacheEntry) : bool =
    entry.ExpiresAt < DateTime.UtcNow

/// Group entries by expiration status
let private groupByExpiration (entries: CacheEntry list) =
    let expired = entries |> List.filter isExpired
    let valid = entries |> List.filter (not << isExpired)
    expired, valid

/// Stats card component
let private statsCard (title: string) (value: string) (subtitle: string option) =
    Html.div [
        prop.className "card bg-base-200"
        prop.children [
            Html.div [
                prop.className "card-body p-4"
                prop.children [
                    Html.h3 [
                        prop.className "text-sm text-base-content/60"
                        prop.text title
                    ]
                    Html.p [
                        prop.className "text-2xl font-bold"
                        prop.text value
                    ]
                    match subtitle with
                    | Some s ->
                        Html.p [
                            prop.className "text-xs text-base-content/50"
                            prop.text s
                        ]
                    | None -> Html.none
                ]
            ]
        ]
    ]

/// Cache entry row component
let private cacheEntryRow (entry: CacheEntry) =
    let keyType, keyId = formatCacheKey entry.CacheKey
    let expired = isExpired entry

    Html.tr [
        prop.className (if expired then "opacity-50" else "")
        prop.children [
            Html.td [
                prop.className "font-mono"
                prop.children [
                    Html.span [
                        prop.className "badge badge-sm mr-2"
                        prop.text keyType
                    ]
                    Html.span [
                        prop.className "text-sm text-base-content/70"
                        prop.text (if keyId.Length > 40 then keyId.Substring(0, 40) + "..." else keyId)
                    ]
                ]
            ]
            Html.td [
                prop.className "text-right"
                prop.text (formatBytes entry.SizeBytes)
            ]
            Html.td [
                prop.className "text-right"
                prop.children [
                    if expired then
                        Html.span [
                            prop.className "badge badge-error badge-sm"
                            prop.text "Expired"
                        ]
                    else
                        Html.span [
                            prop.className "text-sm text-base-content/70"
                            prop.text (entry.ExpiresAt.ToString("yyyy-MM-dd HH:mm"))
                        ]
                ]
            ]
        ]
    ]

/// Stats section component
let private statsSection (stats: CacheStats) =
    Html.div [
        prop.className "grid grid-cols-2 md:grid-cols-4 gap-4 mb-8"
        prop.children [
            statsCard "Total Entries" (string stats.TotalEntries) None
            statsCard "Total Size" (formatBytes stats.TotalSizeBytes) None
            statsCard "Expired" (string stats.ExpiredEntries)
                (if stats.ExpiredEntries > 0 then Some "Can be cleared" else None)
            statsCard "Cache Types" (string stats.EntriesByType.Count) None
        ]
    ]

/// Type breakdown component
let private typeBreakdown (stats: CacheStats) =
    if stats.EntriesByType.IsEmpty then Html.none
    else
        Html.div [
            prop.className "mb-8"
            prop.children [
                Html.h3 [
                    prop.className "text-lg font-semibold mb-4"
                    prop.text "Cache by Type"
                ]
                Html.div [
                    prop.className "flex flex-wrap gap-2"
                    prop.children [
                        for KeyValue(keyType, count) in stats.EntriesByType do
                            Html.div [
                                prop.className "badge badge-lg gap-2"
                                prop.children [
                                    Html.span [ prop.text keyType ]
                                    Html.span [
                                        prop.className "badge badge-sm"
                                        prop.text (string count)
                                    ]
                                ]
                            ]
                    ]
                ]
            ]
        ]

/// Entries table component
let private entriesTable (entries: CacheEntry list) =
    let expired, valid = groupByExpiration entries

    Html.div [
        prop.className "overflow-x-auto"
        prop.children [
            // Expired entries section
            if not expired.IsEmpty then
                Html.div [
                    prop.className "mb-6"
                    prop.children [
                        Html.h3 [
                            prop.className "text-lg font-semibold mb-3 text-error"
                            prop.text $"Expired Entries ({expired.Length})"
                        ]
                        Html.table [
                            prop.className "table table-sm"
                            prop.children [
                                Html.thead [
                                    Html.tr [
                                        Html.th [ prop.text "Cache Key" ]
                                        Html.th [ prop.className "text-right"; prop.text "Size" ]
                                        Html.th [ prop.className "text-right"; prop.text "Expired" ]
                                    ]
                                ]
                                Html.tbody [
                                    for entry in expired |> List.sortBy (fun e -> e.ExpiresAt) do
                                        cacheEntryRow entry
                                ]
                            ]
                        ]
                    ]
                ]

            // Valid entries section
            if not valid.IsEmpty then
                Html.div [
                    prop.children [
                        Html.h3 [
                            prop.className "text-lg font-semibold mb-3"
                            prop.text $"Active Entries ({valid.Length})"
                        ]
                        Html.table [
                            prop.className "table table-sm"
                            prop.children [
                                Html.thead [
                                    Html.tr [
                                        Html.th [ prop.text "Cache Key" ]
                                        Html.th [ prop.className "text-right"; prop.text "Size" ]
                                        Html.th [ prop.className "text-right"; prop.text "Expires" ]
                                    ]
                                ]
                                Html.tbody [
                                    for entry in valid |> List.sortBy (fun e -> e.ExpiresAt) do
                                        cacheEntryRow entry
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.children [
            // Header
            Html.div [
                prop.className "flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-8"
                prop.children [
                    Html.div [
                        Html.h1 [
                            prop.className "text-3xl font-bold"
                            prop.text "Cache & Maintenance"
                        ]
                        Html.p [
                            prop.className "text-base-content/60 mt-1"
                            prop.text "Manage cached TMDB responses and run maintenance tasks"
                        ]
                    ]

                    // Action buttons
                    Html.div [
                        prop.className "flex gap-3"
                        prop.children [
                            if model.IsClearing then
                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                            else
                                GlassButton.withLabel clock "Clear Expired" "Clear expired cache entries" (fun () -> dispatch ClearExpiredCache)
                                GlassButton.dangerWithLabel trash "Clear All" "Clear all cache entries" (fun () -> dispatch ClearAllCache)
                        ]
                    ]
                ]
            ]

            // Maintenance section
            Html.div [
                prop.className "mb-8 p-4 bg-base-200 rounded-lg"
                prop.children [
                    Html.h3 [
                        prop.className "text-lg font-semibold mb-2"
                        prop.text "Maintenance"
                    ]
                    Html.p [
                        prop.className "text-sm text-base-content/60 mb-4"
                        prop.text "Fix series watch status if it wasn't properly set during Trakt import. This will recalculate the InProgress/Completed status for all series based on episode watch data."
                    ]
                    if model.IsRecalculating then
                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                    else
                        GlassButton.primaryWithLabel refresh "Recalculate" "Recalculate series watch status" (fun () -> dispatch RecalculateSeriesWatchStatus)
                ]
            ]

            // Content
            match model.Stats, model.Entries with
            | Loading, _ | _, Loading ->
                Html.div [
                    prop.className "flex justify-center py-16"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]

            | Failure err, _ | _, Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading cache: {err}"
                ]

            | Success stats, Success entries ->
                Html.div [
                    prop.children [
                        statsSection stats
                        typeBreakdown stats

                        if entries.IsEmpty then
                            Html.div [
                                prop.className "text-center py-16"
                                prop.children [
                                    Html.p [
                                        prop.className "text-base-content/60 text-lg"
                                        prop.text "Cache is empty"
                                    ]
                                    Html.p [
                                        prop.className "text-base-content/40 text-sm mt-2"
                                        prop.text "TMDB responses will be cached as you browse"
                                    ]
                                ]
                            ]
                        else
                            entriesTable entries
                    ]
                ]

            | NotAsked, _
            | _, NotAsked ->
                Html.none
        ]
    ]
