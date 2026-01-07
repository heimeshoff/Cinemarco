module Pages.Home.View

open System
open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

// Import common components
module SectionHeader = Common.Components.SectionHeader.View
module PosterCard = Common.Components.PosterCard.View
module PosterCardTypes = Common.Components.PosterCard.Types

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

/// Get episode indicator text from watch progress
let private getNextEpisodeText (progress: WatchProgress) =
    match progress.CurrentSeason, progress.CurrentEpisode with
    | Some s, Some e -> Some $"S{s} E{e}"
    | Some s, None -> Some $"Season {s}"
    | None, Some e -> Some $"Episode {e}"
    | None, None -> None

/// Horizontal scroll poster list for home sections
let private posterScrollList (entries: LibraryEntry list) (onViewDetail: EntryId -> bool -> unit) =
    Html.div [
        prop.className "flex gap-4 overflow-x-auto py-4 px-2 -mx-2 scrollbar-thin scrollbar-thumb-base-content/20 scrollbar-track-transparent"
        prop.children [
            for entry in entries do
                Html.div [
                    prop.key (EntryId.value entry.Id)
                    prop.className "flex-shrink-0 w-32 sm:w-36 md:w-40"
                    prop.children [
                        libraryEntryCard entry onViewDetail
                        // Title below card
                        Html.div [
                            prop.className "mt-2 space-y-0.5"
                            prop.children [
                                Html.p [
                                    prop.className "text-sm font-medium truncate text-base-content/90"
                                    prop.text (
                                        match entry.Media with
                                        | LibraryMovie m -> m.Title
                                        | LibrarySeries s -> s.Name
                                    )
                                ]
                                Html.p [
                                    prop.className "text-xs text-base-content/50"
                                    prop.text (
                                        match entry.Media with
                                        | LibraryMovie m -> m.ReleaseDate |> Option.map (fun d -> string d.Year) |> Option.defaultValue ""
                                        | LibrarySeries s -> s.FirstAirDate |> Option.map (fun d -> string d.Year) |> Option.defaultValue ""
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

/// Horizontal scroll poster list for series with next episode indicator
let private seriesScrollListWithEpisode (entries: (LibraryEntry * WatchProgress) list) (onViewDetail: EntryId -> bool -> unit) =
    Html.div [
        prop.className "flex gap-4 overflow-x-auto py-4 px-2 -mx-2 scrollbar-thin scrollbar-thumb-base-content/20 scrollbar-track-transparent"
        prop.children [
            for (entry, progress) in entries do
                let (title, year) =
                    match entry.Media with
                    | LibraryMovie m -> (m.Title, m.ReleaseDate |> Option.map (fun d -> string d.Year) |> Option.defaultValue "")
                    | LibrarySeries s -> (s.Name, s.FirstAirDate |> Option.map (fun d -> string d.Year) |> Option.defaultValue "")

                Html.div [
                    prop.key (EntryId.value entry.Id)
                    prop.className "flex-shrink-0 w-32 sm:w-36 md:w-40"
                    prop.children [
                        PosterCard.seriesWithEpisode entry progress (fun () -> onViewDetail entry.Id false)
                        // Title below card
                        Html.div [
                            prop.className "mt-2 space-y-0.5"
                            prop.children [
                                Html.p [
                                    prop.className "text-sm font-medium truncate text-base-content/90"
                                    prop.text title
                                ]
                                Html.p [
                                    prop.className "text-xs text-base-content/50"
                                    prop.text year
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

/// Grid poster list for home sections
let private posterGrid (entries: LibraryEntry list) (onViewDetail: EntryId -> bool -> unit) (maxItems: int) =
    Html.div [
        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
        prop.children [
            for entry in entries |> List.truncate maxItems do
                Html.div [
                    prop.key (EntryId.value entry.Id)
                    prop.children [
                        libraryEntryCard entry onViewDetail
                    ]
                ]
        ]
    ]

/// Empty state for sections
let private sectionEmptyState (icon: ReactElement) (message: string) =
    Html.div [
        prop.className "flex flex-col items-center justify-center py-8 text-base-content/40"
        prop.children [
            Html.span [
                prop.className "w-12 h-12 mb-3 opacity-50"
                prop.children [ icon ]
            ]
            Html.p [
                prop.className "text-sm"
                prop.text message
            ]
        ]
    ]

/// Section wrapper with header
let private homeSection (title: string) (icon: ReactElement) (iconClass: string) (showViewAll: bool) (onViewAll: unit -> unit) (content: ReactElement) =
    Html.div [
        prop.className "space-y-4"
        prop.children [
            Html.div [
                prop.className "flex justify-between items-center"
                prop.children [
                    Html.div [
                        prop.className "flex items-center gap-3"
                        prop.children [
                            Html.span [
                                prop.className $"w-6 h-6 {iconClass}"
                                prop.children [ icon ]
                            ]
                            Html.h3 [
                                prop.className "text-xl font-bold"
                                prop.text title
                            ]
                        ]
                    ]
                    if showViewAll then
                        Html.button [
                            prop.className "flex items-center gap-2 text-sm text-primary hover:underline"
                            prop.onClick (fun _ -> onViewAll ())
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
            content
        ]
    ]

/// Up Next section - in-progress series with next episode indicator
let private upNextSection (entries: LibraryEntry list) (dispatch: Msg -> unit) =
    let inProgressSeriesWithProgress =
        entries
        |> List.choose (fun e ->
            match e.WatchStatus, e.Media with
            | InProgress progress, LibrarySeries _ ->
                // Only include if we have valid next episode info
                match progress.CurrentSeason, progress.CurrentEpisode with
                | Some _, Some _ -> Some (e, progress)  // Has next episode
                | _ -> None  // Missing metadata or completed - hide from Up Next
            | _ -> None)
        |> List.sortByDescending (fun (e, _) -> e.DateLastWatched |> Option.defaultValue e.DateAdded)
        |> List.truncate 24

    if List.isEmpty inProgressSeriesWithProgress then Html.none
    else
        homeSection "Up Next" playCircle "text-primary" false ignore (
            seriesScrollListWithEpisode inProgressSeriesWithProgress (fun id _ ->
                let entry = inProgressSeriesWithProgress |> List.find (fun (e, _) -> e.Id = id) |> fst
                match entry.Media with
                | LibraryMovie m -> dispatch (ViewMovieDetail (id, m.Title, m.ReleaseDate))
                | LibrarySeries s -> dispatch (ViewSeriesDetail (id, s.Name, s.FirstAirDate))
            )
        )

/// Watchlist section - movies and series not started yet
let private watchlistSection (entries: LibraryEntry list) (dispatch: Msg -> unit) =
    let notStarted =
        entries
        |> List.filter (fun e -> match e.WatchStatus with NotStarted -> true | _ -> false)
        |> List.sortByDescending (fun e -> e.DateAdded)
        |> List.truncate 12

    if List.isEmpty notStarted then Html.none
    else
        homeSection "Watchlist" clock "text-info" true (fun () -> dispatch ViewLibrary) (
            posterScrollList notStarted (fun id isMovie ->
                let entry = notStarted |> List.find (fun e -> e.Id = id)
                match entry.Media with
                | LibraryMovie m -> dispatch (ViewMovieDetail (id, m.Title, m.ReleaseDate))
                | LibrarySeries s -> dispatch (ViewSeriesDetail (id, s.Name, s.FirstAirDate))
            )
        )

/// Recently Watched section - completed or has watch date
let private recentlyWatchedSection (entries: LibraryEntry list) (dispatch: Msg -> unit) =
    let recentlyWatched =
        entries
        |> List.filter (fun e ->
            match e.WatchStatus with
            | Completed -> true
            | InProgress _ -> e.DateLastWatched.IsSome
            | _ -> false)
        |> List.filter (fun e -> e.DateLastWatched.IsSome)
        |> List.sortByDescending (fun e -> e.DateLastWatched.Value)
        |> List.truncate 12

    if List.isEmpty recentlyWatched then Html.none
    else
        homeSection "Recently Watched" eye "text-success" true (fun () -> dispatch ViewLibrary) (
            posterScrollList recentlyWatched (fun id isMovie ->
                let entry = recentlyWatched |> List.find (fun e -> e.Id = id)
                match entry.Media with
                | LibraryMovie m -> dispatch (ViewMovieDetail (id, m.Title, m.ReleaseDate))
                | LibrarySeries s -> dispatch (ViewSeriesDetail (id, s.Name, s.FirstAirDate))
            )
        )

/// Recently Added section
let private recentlyAddedSection (entries: LibraryEntry list) (dispatch: Msg -> unit) =
    let recentlyAdded =
        entries
        |> List.sortByDescending (fun e -> e.DateAdded)
        |> List.truncate 12

    if List.isEmpty recentlyAdded then Html.none
    else
        homeSection "Recently Added" plus "text-secondary" true (fun () -> dispatch ViewLibrary) (
            posterScrollList recentlyAdded (fun id isMovie ->
                let entry = recentlyAdded |> List.find (fun e -> e.Id = id)
                match entry.Media with
                | LibraryMovie m -> dispatch (ViewMovieDetail (id, m.Title, m.ReleaseDate))
                | LibrarySeries s -> dispatch (ViewSeriesDetail (id, s.Name, s.FirstAirDate))
            )
        )

/// Loading skeleton for sections
let private loadingSkeleton (count: int) =
    Html.div [
        prop.className "flex gap-4 overflow-hidden"
        prop.children [
            for _ in 1..count do
                Html.div [
                    prop.className "flex-shrink-0 w-32 sm:w-36 md:w-40 space-y-3"
                    prop.children [
                        Html.div [ prop.className "skeleton aspect-[2/3] rounded-lg" ]
                        Html.div [ prop.className "skeleton h-4 w-3/4 rounded" ]
                        Html.div [ prop.className "skeleton h-3 w-1/2 rounded" ]
                    ]
                ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-10"
        prop.children [
            // Hero section - compact when library has content
            Html.div [
                prop.className "text-center py-8 space-y-4"
                prop.children [
                    // Logo and title row
                    Html.div [
                        prop.className "flex items-center justify-center gap-4"
                        prop.children [
                            Html.div [
                                prop.className "w-14 h-14 rounded-xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center shadow-glow-primary"
                                prop.children [
                                    Html.span [
                                        prop.className "w-7 h-7 text-primary"
                                        prop.children [ clapperboard ]
                                    ]
                                ]
                            ]
                            Html.h2 [
                                prop.className "text-3xl font-bold"
                                prop.children [
                                    Html.span [ prop.className "text-gradient"; prop.text "Cinemarco" ]
                                ]
                            ]
                        ]
                    ]

                    // Connection status buttons
                    Html.div [
                        prop.className "flex flex-wrap justify-center gap-3"
                        prop.children [
                            // TMDB health button
                            Html.button [
                                prop.className (
                                    "flex items-center gap-2 px-3 py-1.5 glass rounded-full text-xs transition-all " +
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
                                        Html.span [ prop.className "w-3.5 h-3.5"; prop.children [ database ] ]
                                        Html.span [ prop.text "TMDB" ]
                                    | TmdbChecking ->
                                        Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                        Html.span [ prop.text "Checking..." ]
                                    | TmdbConnected ->
                                        Html.span [ prop.className "w-3.5 h-3.5"; prop.children [ check ] ]
                                        Html.span [ prop.text "TMDB" ]
                                    | TmdbError _ ->
                                        Html.span [ prop.className "w-3.5 h-3.5"; prop.children [ error ] ]
                                        Html.span [ prop.text "TMDB Error" ]
                                ]
                            ]
                            // Trakt sync button
                            traktSyncButton model dispatch
                            // Year in Review button
                            Html.button [
                                prop.className "flex items-center gap-2 px-3 py-1.5 glass rounded-full text-xs text-amber-400 border border-amber-500/30 hover:bg-amber-500/10 transition-all"
                                prop.onClick (fun _ -> dispatch ViewYearInReview)
                                prop.children [
                                    Html.span [
                                        prop.className "w-3.5 h-3.5"
                                        prop.children [ sparkles ]
                                    ]
                                    Html.span [ prop.text "Year in Review" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Content sections
            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.className "space-y-8"
                    prop.children [
                        // Up Next (in-progress series with next episode indicator)
                        upNextSection entries dispatch

                        // Watchlist (not started - movies and series)
                        watchlistSection entries dispatch

                        // Recently Watched
                        recentlyWatchedSection entries dispatch

                        // Recently Added
                        recentlyAddedSection entries dispatch
                    ]
                ]

            | Success _ ->
                // Empty library state
                Html.div [
                    prop.className "text-center py-16"
                    prop.children [
                        Html.div [
                            prop.className "w-24 h-24 mx-auto mb-6 rounded-2xl bg-gradient-to-br from-primary/10 to-secondary/10 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-12 h-12 text-base-content/20"
                                    prop.children [ library ]
                                ]
                            ]
                        ]
                        Html.h3 [
                            prop.className "text-2xl font-bold mb-3 text-base-content/80"
                            prop.text "Your library is empty"
                        ]
                        Html.p [
                            prop.className "text-base-content/50 mb-6 max-w-md mx-auto"
                            prop.text "Start building your cinema memory by searching for movies and series you've watched or want to watch."
                        ]
                        Html.div [
                            prop.className "flex justify-center gap-4"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-primary"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-5 h-5 mr-2"
                                            prop.children [ search ]
                                        ]
                                        Html.span [ prop.text "Search Movies & Series" ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

            | Loading ->
                Html.div [
                    prop.className "space-y-8"
                    prop.children [
                        // Continue Watching skeleton
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                Html.div [ prop.className "skeleton h-7 w-48 rounded" ]
                                loadingSkeleton 6
                            ]
                        ]
                        // Up Next skeleton
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                Html.div [ prop.className "skeleton h-7 w-32 rounded" ]
                                loadingSkeleton 6
                            ]
                        ]
                    ]
                ]

            | Failure err ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.children [
                        Html.span [
                            prop.className "w-16 h-16 mx-auto mb-4 text-error/40 block"
                            prop.children [ error ]
                        ]
                        Html.h3 [
                            prop.className "text-lg font-semibold text-error mb-2"
                            prop.text "Failed to load library"
                        ]
                        Html.p [
                            prop.className "text-base-content/50 text-sm"
                            prop.text err
                        ]
                    ]
                ]

            | NotAsked -> Html.none
        ]
    ]
