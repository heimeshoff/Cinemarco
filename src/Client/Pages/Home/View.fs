module Pages.Home.View

open System
open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

/// Format a relative time string (e.g., "5 minutes ago", "2 hours ago")
let private formatRelativeTime (dateTime: DateTime) =
    let now = DateTime.UtcNow
    let diff = now - dateTime
    if diff.TotalMinutes < 1.0 then "just now"
    elif diff.TotalMinutes < 60.0 then $"{int diff.TotalMinutes} min ago"
    elif diff.TotalHours < 24.0 then $"{int diff.TotalHours}h ago"
    elif diff.TotalDays < 7.0 then $"{int diff.TotalDays}d ago"
    else dateTime.ToString("MMM d")

/// Trakt sync button - shows connection status, last sync, and allows manual sync
let private traktSyncButton (model: Model) (dispatch: Msg -> unit) =
    match model.TraktStatus with
    | None ->
        // Still loading status
        Html.div [
            prop.className "flex items-center gap-2 px-4 py-2 glass rounded-full text-sm text-base-content/40"
            prop.children [
                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                Html.span [ prop.text "Checking Trakt..." ]
            ]
        ]
    | Some status ->
        if not status.IsAuthenticated then
            // Not connected
            Html.a [
                prop.href "/import"
                prop.className "flex items-center gap-2 px-4 py-2 glass rounded-full text-sm text-base-content/60 hover:text-base-content hover:border-primary/30 border border-transparent transition-all"
                prop.children [
                    Html.span [
                        prop.className "w-4 h-4 text-base-content/40"
                        prop.children [ cloud ]
                    ]
                    Html.span [ prop.text "Connect Trakt" ]
                ]
            ]
        else
            // Connected - show sync button with status
            let isSyncing = match model.TraktSync with Syncing -> true | _ -> false
            let lastSyncText =
                match status.LastSyncAt with
                | Some dt -> formatRelativeTime dt
                | None -> "never"

            // Determine button text and style based on sync state
            let (buttonText, extraClass) =
                match model.TraktSync with
                | Syncing -> ("Syncing...", "text-primary border border-primary/30")
                | SyncComplete result ->
                    let total = result.NewMovieWatches + result.NewEpisodeWatches
                    if total > 0 then
                        let parts = [
                            if result.NewMovieWatches > 0 then sprintf "%d movie(s)" result.NewMovieWatches
                            if result.NewEpisodeWatches > 0 then sprintf "%d episode(s)" result.NewEpisodeWatches
                        ]
                        let syncedText = "Synced " + String.concat " & " parts
                        (syncedText, "text-success border border-success/30")
                    else
                        ("Up to date", "text-success border border-success/30")
                | SyncError _ -> ("Sync failed", "text-error border border-error/30")
                | _ -> (sprintf "Trakt · %s" lastSyncText, "text-base-content/60 hover:text-base-content hover:border-primary/30 border border-transparent")

            Html.button [
                prop.className ($"flex items-center gap-2 px-4 py-2 glass rounded-full text-sm transition-all {extraClass}")
                prop.disabled isSyncing
                prop.onClick (fun _ -> if not isSyncing then dispatch ManualSync)
                prop.children [
                    if isSyncing then
                        Html.span [ prop.className "loading loading-spinner loading-xs text-primary" ]
                    Html.span [ prop.text buttonText ]
                    if not isSyncing then
                        Html.span [
                            prop.className "w-3.5 h-3.5 text-base-content/40"
                            prop.children [ refresh ]
                        ]
                ]
            ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-10"
        prop.children [
            // Hero section
            Html.div [
                prop.className "text-center py-16 space-y-6"
                prop.children [
                    // Logo icon
                    Html.div [
                        prop.className "mx-auto w-20 h-20 rounded-2xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center mb-6 shadow-glow-primary"
                        prop.children [
                            Html.span [
                                prop.className "w-10 h-10 text-primary"
                                prop.children [ clapperboard ]
                            ]
                        ]
                    ]

                    Html.h2 [
                        prop.className "text-4xl font-bold"
                        prop.children [
                            Html.span [ prop.text "Your " ]
                            Html.span [ prop.className "text-gradient"; prop.text "Cinema" ]
                            Html.span [ prop.text " Memory Tracker" ]
                        ]
                    ]

                    Html.p [
                        prop.className "text-base-content/60 max-w-xl mx-auto text-lg leading-relaxed"
                        prop.text "Search for movies and series to add them to your personal library. Track what you've watched, who you watched with, and capture your thoughts."
                    ]

                    // Connection status buttons
                    Html.div [
                        prop.className "flex flex-wrap justify-center gap-4 mt-8"
                        prop.children [
                            // TMDB health button
                            Html.button [
                                prop.className (
                                    "flex items-center gap-2 px-4 py-2 glass rounded-full text-sm transition-all " +
                                    match model.TmdbHealth with
                                    | TmdbNotChecked -> "text-base-content/40"
                                    | TmdbChecking -> "text-base-content/40"
                                    | TmdbConnected -> "text-success border border-success/30"
                                    | TmdbError _ -> "text-error border border-error/30"
                                )
                                prop.onClick (fun _ -> dispatch CheckTmdbHealth)
                                prop.children [
                                    match model.TmdbHealth with
                                    | TmdbNotChecked ->
                                        Html.span [ prop.className "w-4 h-4"; prop.children [ database ] ]
                                        Html.span [ prop.text "TMDB" ]
                                    | TmdbChecking ->
                                        Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                        Html.span [ prop.text "Checking TMDB..." ]
                                    | TmdbConnected ->
                                        Html.span [ prop.className "w-4 h-4"; prop.children [ check ] ]
                                        Html.span [ prop.text "TMDB Connected" ]
                                    | TmdbError err ->
                                        Html.span [ prop.className "w-4 h-4"; prop.children [ error ] ]
                                        Html.span [ prop.text $"TMDB: {err}" ]
                                ]
                            ]
                            // Trakt sync button
                            traktSyncButton model dispatch
                        ]
                    ]

                    // Year in Review link
                    Html.button [
                        prop.className "mt-8 group flex items-center gap-3 mx-auto px-6 py-3 rounded-2xl bg-gradient-to-r from-amber-500/20 to-orange-500/20 hover:from-amber-500/30 hover:to-orange-500/30 border border-amber-500/30 transition-all duration-300"
                        prop.onClick (fun _ -> dispatch ViewYearInReview)
                        prop.children [
                            Html.span [
                                prop.className "w-6 h-6 text-amber-400"
                                prop.children [ sparkles ]
                            ]
                            Html.span [
                                prop.className "font-medium text-amber-200"
                                prop.text "View Your Year in Review"
                            ]
                            Html.span [
                                prop.className "w-5 h-5 text-amber-400/70 group-hover:translate-x-1 transition-transform"
                                prop.children [ arrowRight ]
                            ]
                        ]
                    ]
                ]
            ]

            // Recently added section
            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        Html.div [
                            prop.className "flex justify-between items-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "text-xl font-bold"
                                    prop.text "Recently Added"
                                ]
                                Html.button [
                                    prop.className "flex items-center gap-2 text-sm text-primary hover:underline"
                                    prop.onClick (fun _ -> dispatch ViewLibrary)
                                    prop.children [
                                        Html.span [ prop.text "View All" ]
                                        Html.span [
                                            prop.className "w-4 h-4"
                                            prop.children [ arrowRight ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                            prop.children [
                                for entry in entries |> List.sortByDescending (fun e -> e.DateAdded) |> List.truncate 12 do
                                    Html.div [
                                        prop.key (EntryId.value entry.Id)
                                        prop.children [
                                            libraryEntryCard entry (fun id isMovie ->
                                                match entry.Media with
                                                | LibraryMovie m -> dispatch (ViewMovieDetail (id, m.Title, m.ReleaseDate))
                                                | LibrarySeries s -> dispatch (ViewSeriesDetail (id, s.Name, s.FirstAirDate)))
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
            | Success _ ->
                Html.div [
                    prop.className "text-center py-16"
                    prop.children [
                        Html.div [
                            prop.className "w-20 h-20 mx-auto mb-6 rounded-full bg-base-200 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-10 h-10 text-base-content/30"
                                    prop.children [ library ]
                                ]
                            ]
                        ]
                        Html.h3 [
                            prop.className "text-xl font-semibold mb-2 text-base-content/70"
                            prop.text "Your library is empty"
                        ]
                        Html.p [
                            prop.className "text-base-content/50"
                            prop.text "Search for a movie or series above to get started"
                        ]
                    ]
                ]
            | Loading ->
                Html.div [
                    prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                    prop.children [
                        for _ in 1..6 do
                            Html.div [
                                prop.className "space-y-3"
                                prop.children [
                                    Html.div [ prop.className "skeleton aspect-[2/3] rounded-lg" ]
                                    Html.div [ prop.className "skeleton h-4 w-3/4 rounded" ]
                                    Html.div [ prop.className "skeleton h-3 w-1/2 rounded" ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.children [
                        Html.span [
                            prop.className "w-12 h-12 mx-auto mb-4 text-error/50 block"
                            prop.children [ error ]
                        ]
                        Html.p [
                            prop.className "text-error"
                            prop.text $"Error loading library: {err}"
                        ]
                    ]
                ]
            | NotAsked -> Html.none
        ]
    ]
