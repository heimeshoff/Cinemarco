module Pages.SessionDetail.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

/// Progress bar component
let private progressBar (current: int) (total: int) =
    let percentage = if total > 0 then float current / float total * 100.0 else 0.0
    Html.div [
        prop.className "w-full"
        prop.children [
            Html.div [
                prop.className "flex justify-between text-sm mb-1"
                prop.children [
                    Html.span [ prop.text $"{current} / {total}" ]
                    Html.span [ prop.text $"{percentage:F0}%%" ]
                ]
            ]
            Html.div [
                prop.className "w-full bg-base-300 rounded-full h-2"
                prop.children [
                    Html.div [
                        prop.className "bg-primary h-2 rounded-full transition-all duration-300"
                        prop.style [ Feliz.style.width (Feliz.length.percent (int percentage)) ]
                    ]
                ]
            ]
        ]
    ]

/// Episode checkbox component with Ctrl+click support
/// Uses subtle colors: unwatched = dark gray, watched = subtle muted green
let private renderEpisodeCheckbox (seasonNum: int) (epNum: int) (isWatched: bool) (isHovered: bool) (onHover: int option -> unit) (dispatch: Msg -> unit) =
    // Subtle color scheme matching dark theme
    let bgColor =
        if isWatched then
            if isHovered then "#243324" else "#1a2e1a"  // subtle muted green
        else
            if isHovered then "#242424" else "#1a1a1a"  // dark gray

    let borderColor =
        if isWatched then
            if isHovered then "#3d5c3d" else "#2d4a2d"  // subtle green border
        elif isHovered then "#3d3d3d"  // lighter gray on hover
        else "#2a2a2a"  // dark border

    Html.div [
        prop.className "cursor-pointer p-2 rounded border transition-all duration-150 select-none"
        prop.style [
            style.backgroundColor bgColor
            style.borderColor borderColor
        ]
        prop.onMouseEnter (fun _ -> onHover (Some epNum))
        prop.onMouseLeave (fun _ -> onHover None)
        prop.onClick (fun e ->
            e.preventDefault()
            let newWatchedState = not isWatched
            if e.ctrlKey then
                // Ctrl+click: mark all episodes from 1 to this one
                dispatch (MarkEpisodesUpTo (seasonNum, epNum, newWatchedState))
            else
                // Normal click: toggle single episode
                dispatch (ToggleEpisodeWatched (seasonNum, epNum, newWatchedState)))
        prop.children [
            Html.span [
                prop.className "text-xs font-medium"
                prop.text $"E{epNum}"
            ]
        ]
    ]

/// Season episodes grid with Ctrl+hover preview
[<ReactComponent>]
let private SeasonEpisodesGrid (seasonNum: int) (episodes: TmdbEpisodeSummary list) (watchedEpisodes: Set<int * int>) (dispatch: Msg -> unit) =
    let (hoveredEp, setHoveredEp) = React.useState<int option> None
    let (ctrlPressed, setCtrlPressed) = React.useState false

    // Track Ctrl key state - detect specifically when Control key is pressed/released
    React.useEffect (fun () ->
        let onKeyDown (e: Browser.Types.Event) =
            let ke = e :?> Browser.Types.KeyboardEvent
            if ke.key = "Control" then setCtrlPressed true
        let onKeyUp (e: Browser.Types.Event) =
            let ke = e :?> Browser.Types.KeyboardEvent
            if ke.key = "Control" then setCtrlPressed false
        // Also handle window blur to reset Ctrl state
        let onBlur _ = setCtrlPressed false

        Browser.Dom.window.addEventListener("keydown", onKeyDown)
        Browser.Dom.window.addEventListener("keyup", onKeyUp)
        Browser.Dom.window.addEventListener("blur", onBlur)

        { new System.IDisposable with
            member _.Dispose() =
                Browser.Dom.window.removeEventListener("keydown", onKeyDown)
                Browser.Dom.window.removeEventListener("keyup", onKeyUp)
                Browser.Dom.window.removeEventListener("blur", onBlur)
        }
    , [||])

    Html.div [
        prop.className "grid grid-cols-5 sm:grid-cols-8 md:grid-cols-10 gap-1"
        prop.children [
            for ep in episodes |> List.sortBy (fun e -> e.EpisodeNumber) do
                let isWatched = Set.contains (seasonNum, ep.EpisodeNumber) watchedEpisodes
                // Show hover state if: directly hovered, OR (Ctrl pressed AND episode <= hovered episode)
                let isInCtrlRange =
                    ctrlPressed &&
                    hoveredEp.IsSome &&
                    ep.EpisodeNumber <= hoveredEp.Value
                let showHover = (hoveredEp = Some ep.EpisodeNumber) || isInCtrlRange
                renderEpisodeCheckbox seasonNum ep.EpisodeNumber isWatched showHover setHoveredEp dispatch
        ]
    ]

/// Session status badge
let private statusBadge (status: SessionStatus) =
    let (colorClass, text) =
        match status with
        | Active -> ("badge-success", "Active")
        | Paused -> ("badge-warning", "Paused")
        | SessionCompleted -> ("badge-info", "Completed")
    Html.span [
        prop.className $"badge {colorClass}"
        prop.text text
    ]

/// Status controls
let private statusControls (status: SessionStatus) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex gap-2 flex-wrap"
        prop.children [
            match status with
            | Active ->
                Html.button [
                    prop.className "btn btn-sm btn-warning"
                    prop.onClick (fun _ -> dispatch (UpdateStatus Paused))
                    prop.text "Pause"
                ]
                Html.button [
                    prop.className "btn btn-sm btn-info"
                    prop.onClick (fun _ -> dispatch (UpdateStatus SessionCompleted))
                    prop.text "Mark Completed"
                ]
            | Paused ->
                Html.button [
                    prop.className "btn btn-sm btn-success"
                    prop.onClick (fun _ -> dispatch (UpdateStatus Active))
                    prop.text "Resume"
                ]
                Html.button [
                    prop.className "btn btn-sm btn-info"
                    prop.onClick (fun _ -> dispatch (UpdateStatus SessionCompleted))
                    prop.text "Mark Completed"
                ]
            | SessionCompleted ->
                Html.button [
                    prop.className "btn btn-sm btn-success"
                    prop.onClick (fun _ -> dispatch (UpdateStatus Active))
                    prop.text "Reactivate"
                ]
        ]
    ]

let view (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> dispatch GoBack)
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back to Series" ]
                ]
            ]

            match model.SessionData with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-16"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success data ->
                let session = data.Session
                let entry = data.Entry

                let seriesName =
                    match entry.Media with
                    | LibrarySeries s -> s.Name
                    | LibraryMovie m -> m.Title

                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Header
                        Html.div [
                            prop.className "flex flex-col md:flex-row md:items-center justify-between gap-4"
                            prop.children [
                                Html.div [
                                    Html.h1 [
                                        prop.className "text-2xl font-bold"
                                        prop.text session.Name
                                    ]
                                    Html.p [
                                        prop.className "text-base-content/60"
                                        prop.text $"Session for {seriesName}"
                                    ]
                                    Html.div [
                                        prop.className "flex items-center gap-2 mt-2"
                                        prop.children [
                                            statusBadge session.Status
                                            match session.StartDate with
                                            | Some d ->
                                                Html.span [
                                                    prop.className "text-sm text-base-content/50"
                                                    prop.text $"Started {d.ToShortDateString()}"
                                                ]
                                            | None -> Html.none
                                        ]
                                    ]
                                ]
                                statusControls session.Status dispatch
                            ]
                        ]

                        // Progress card
                        Html.div [
                            prop.className "card bg-base-200"
                            prop.children [
                                Html.div [
                                    prop.className "card-body"
                                    prop.children [
                                        Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Progress" ]
                                        progressBar data.WatchedEpisodes data.TotalEpisodes
                                    ]
                                ]
                            ]
                        ]

                        // Friends
                        if not (List.isEmpty friends) then
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Watching With" ]
                                            Html.div [
                                                prop.className "flex flex-wrap gap-2"
                                                prop.children [
                                                    for friend in friends do
                                                        let isSelected = List.contains friend.Id session.Friends
                                                        Html.button [
                                                            prop.className (
                                                                "btn btn-sm " +
                                                                if isSelected then "btn-primary" else "btn-ghost"
                                                            )
                                                            prop.onClick (fun _ -> dispatch (ToggleFriend friend.Id))
                                                            prop.text friend.Name
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                        // Tags
                        if not (List.isEmpty tags) then
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Tags" ]
                                            Html.div [
                                                prop.className "flex flex-wrap gap-2"
                                                prop.children [
                                                    for tag in tags do
                                                        let isSelected = List.contains tag.Id session.Tags
                                                        Html.button [
                                                            prop.className (
                                                                "btn btn-sm " +
                                                                if isSelected then "btn-secondary" else "btn-ghost"
                                                            )
                                                            prop.onClick (fun _ -> dispatch (ToggleTag tag.Id))
                                                            prop.text tag.Name
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                        // Episodes by season (using TMDB data)
                        Html.div [
                            Html.h3 [ prop.className "font-semibold mb-4"; prop.text "Episodes" ]
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    // Build a lookup of watched episodes
                                    let watchedEpisodes =
                                        data.EpisodeProgress
                                        |> List.filter (fun p -> p.IsWatched)
                                        |> List.map (fun p -> (p.SeasonNumber, p.EpisodeNumber))
                                        |> Set.ofList

                                    // Get number of seasons from the series
                                    match entry.Media with
                                    | LibrarySeries series ->
                                        for seasonNum in 1 .. series.NumberOfSeasons do
                                            let seasonDetailsOpt = Map.tryFind seasonNum model.SeasonDetails
                                            let isLoading = Set.contains seasonNum model.LoadingSeasons

                                            Html.div [
                                                prop.className "card bg-base-200"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "card-body p-4"
                                                        prop.children [
                                                            match seasonDetailsOpt with
                                                            | Some seasonDetails ->
                                                                let totalInSeason = seasonDetails.Episodes.Length
                                                                let watchedInSeason =
                                                                    seasonDetails.Episodes
                                                                    |> List.filter (fun ep -> Set.contains (seasonNum, ep.EpisodeNumber) watchedEpisodes)
                                                                    |> List.length

                                                                Html.div [
                                                                    prop.className "flex justify-between items-center mb-3"
                                                                    prop.children [
                                                                        Html.h4 [
                                                                            prop.className "font-semibold"
                                                                            prop.text $"Season {seasonNum}"
                                                                        ]
                                                                        Html.div [
                                                                            prop.className "flex items-center gap-2"
                                                                            prop.children [
                                                                                Html.span [
                                                                                    prop.className "text-sm text-base-content/60"
                                                                                    prop.text $"{watchedInSeason}/{totalInSeason}"
                                                                                ]
                                                                                Html.button [
                                                                                    prop.className "btn btn-xs btn-ghost"
                                                                                    prop.onClick (fun _ -> dispatch (MarkSeasonWatched seasonNum))
                                                                                    prop.text "Mark All"
                                                                                ]
                                                                            ]
                                                                        ]
                                                                    ]
                                                                ]
                                                                SeasonEpisodesGrid seasonNum seasonDetails.Episodes watchedEpisodes dispatch
                                                            | None when isLoading ->
                                                                Html.div [
                                                                    prop.className "flex items-center gap-2"
                                                                    prop.children [
                                                                        Html.h4 [
                                                                            prop.className "font-semibold"
                                                                            prop.text $"Season {seasonNum}"
                                                                        ]
                                                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                                    ]
                                                                ]
                                                            | None ->
                                                                Html.div [
                                                                    prop.className "flex items-center gap-2"
                                                                    prop.children [
                                                                        Html.h4 [
                                                                            prop.className "font-semibold"
                                                                            prop.text $"Season {seasonNum}"
                                                                        ]
                                                                        Html.button [
                                                                            prop.className "btn btn-xs btn-ghost"
                                                                            prop.onClick (fun _ -> dispatch (LoadSeasonDetails seasonNum))
                                                                            prop.text "Load episodes"
                                                                        ]
                                                                    ]
                                                                ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                    | LibraryMovie _ -> Html.none
                                ]
                            ]
                        ]

                        // Delete button
                        Html.div [
                            prop.className "pt-4 border-t border-base-300"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-error btn-outline btn-sm"
                                    prop.onClick (fun _ -> dispatch DeleteSession)
                                    prop.children [
                                        Html.span [ prop.className "w-4 h-4"; prop.children [ trash ] ]
                                        Html.span [ prop.text "Delete Session" ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text err
                ]
            | NotAsked -> Html.none
        ]
    ]
