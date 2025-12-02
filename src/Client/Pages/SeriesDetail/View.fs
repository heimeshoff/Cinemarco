module Pages.SeriesDetail.View

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

/// Wrapper for backward compatibility
let private episodeCheckbox seasonNum epNum isWatched dispatch =
    renderEpisodeCheckbox seasonNum epNum isWatched false (fun _ -> ()) dispatch

/// Rating labels, descriptions and icons
let private ratingOptions = [
    (0, "Unrated", "No rating yet", questionCircle, "text-base-content/50")
    (1, "Waste", "Waste of time", thumbsDown, "text-red-400")
    (2, "Meh", "Didn't click, uninspiring", minusCircle, "text-orange-400")
    (3, "Decent", "Watchable, even if not life-changing", handOkay, "text-yellow-400")
    (4, "Entertaining", "Strong craft, enjoyable", thumbsUp, "text-lime-400")
    (5, "Outstanding", "Absolutely brilliant, stays with you", trophy, "text-amber-400")
]

/// Get rating icon and color for current rating
let private getRatingDisplay (rating: int option) =
    let r = rating |> Option.defaultValue 0
    ratingOptions |> List.find (fun (n, _, _, _, _) -> n = r)

/// Rating button with dropdown
let private ratingButton (current: int option) (isOpen: bool) (dispatch: Msg -> unit) =
    let (_, label, _, icon, colorClass) = getRatingDisplay current
    let btnClass = "detail-action-btn " + colorClass
    Html.div [
        prop.className "relative"
        prop.children [
            // Main button
            Html.div [
                prop.className "tooltip tooltip-bottom detail-tooltip"
                prop.custom ("data-tip", label)
                prop.children [
                    Html.button [
                        prop.className btnClass
                        prop.onClick (fun _ -> dispatch ToggleRatingDropdown)
                        prop.children [
                            Html.span [ prop.className "w-5 h-5"; prop.children [ icon ] ]
                        ]
                    ]
                ]
            ]
            // Dropdown
            if isOpen then
                Html.div [
                    prop.className "absolute top-full left-0 mt-2 z-50 rating-dropdown"
                    prop.children [
                        for (value, name, description, ratingIcon, ratingColor) in ratingOptions do
                            if value > 0 then
                                let isActive = current = Some value
                                let itemClass = if isActive then "rating-dropdown-item rating-dropdown-item-active" else "rating-dropdown-item"
                                let iconClass = "w-5 h-5 " + ratingColor
                                Html.button [
                                    prop.className itemClass
                                    prop.onClick (fun _ -> dispatch (SetRating value))
                                    prop.children [
                                        Html.span [ prop.className iconClass; prop.children [ ratingIcon ] ]
                                        Html.div [
                                            prop.className "flex flex-col items-start"
                                            prop.children [
                                                Html.span [ prop.className "font-medium"; prop.text name ]
                                                Html.span [ prop.className "text-xs text-base-content/50"; prop.text description ]
                                            ]
                                        ]
                                    ]
                                ]
                        // Clear option if rated
                        if current.IsSome && current.Value > 0 then
                            Html.button [
                                prop.className "rating-dropdown-item rating-dropdown-item-clear"
                                prop.onClick (fun _ -> dispatch (SetRating 0))
                                prop.children [
                                    Html.span [ prop.className "w-5 h-5 text-base-content/40"; prop.children [ questionCircle ] ]
                                    Html.span [ prop.className "font-medium text-base-content/60"; prop.text "Clear rating" ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Action buttons row for series (rating, abandon/resume, delete)
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex items-center gap-3 mt-4"
        prop.children [
            // Rating button
            ratingButton (entry.PersonalRating |> Option.map PersonalRating.toInt) isRatingOpen dispatch
            // Abandon/Resume button
            match entry.WatchStatus with
            | NotStarted | InProgress _ ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Abandon Series")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn text-warning/70"
                            prop.onClick (fun _ -> dispatch OpenAbandonModal)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ ban ] ]
                            ]
                        ]
                    ]
                ]
            | Completed ->
                Html.none
            | Abandoned _ ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Resume Watching")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success"
                            prop.onClick (fun _ -> dispatch ResumeEntry)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ undo ] ]
                            ]
                        ]
                    ]
                ]
            // Delete button
            Html.div [
                prop.className "tooltip tooltip-bottom detail-tooltip"
                prop.custom ("data-tip", "Delete Entry")
                prop.children [
                    Html.button [
                        prop.className "detail-action-btn detail-action-btn-danger"
                        prop.onClick (fun _ -> dispatch OpenDeleteModal)
                        prop.children [
                            Html.span [ prop.className "w-5 h-5"; prop.children [ trash ] ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Group episode progress by season
let private groupBySeasons (progress: EpisodeProgress list) : Map<int, int list> =
    progress
    |> List.filter (fun p -> p.IsWatched)
    |> List.groupBy (fun p -> p.SeasonNumber)
    |> List.map (fun (season, eps) -> season, eps |> List.map (fun e -> e.EpisodeNumber))
    |> Map.ofList

/// Generate a display name for a session based on its friends
let private sessionDisplayName (session: WatchSession) (allFriends: Friend list) =
    if session.IsDefault then
        "Personal"
    else
        let friendNames =
            session.Friends
            |> List.choose (fun fid -> allFriends |> List.tryFind (fun f -> f.Id = fid))
            |> List.map (fun f -> f.Name)
        match friendNames with
        | [] -> "Personal"
        | [name] -> $"with {name}"
        | names ->
            let allButLast = names |> List.take (names.Length - 1) |> String.concat ", "
            let last = List.last names
            $"with {allButLast} and {last}"

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
                    Html.span [ prop.text "Back to Library" ]
                ]
            ]

            match model.Entry with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-16"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success entry ->
                match entry.Media with
                | LibrarySeries series ->
                    let watchedCount =
                        model.EpisodeProgress
                        |> List.filter (fun p -> p.IsWatched)
                        |> List.length

                    Html.div [
                        prop.className "grid grid-cols-1 md:grid-cols-3 gap-8"
                        prop.children [
                            // Left column - Poster
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    Html.div [
                                        prop.className "poster-image-container poster-shadow poster-projector-glow"
                                        prop.children [
                                            match series.PosterPath with
                                            | Some _ ->
                                                Html.img [
                                                    prop.src (getLocalPosterUrl series.PosterPath)
                                                    prop.alt series.Name
                                                    prop.className "poster-image"
                                                    prop.custom ("crossorigin", "anonymous")
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full flex items-center justify-center bg-base-200"
                                                    prop.children [
                                                        Html.span [ prop.className "text-6xl text-base-content/20"; prop.children [ tv ] ]
                                                    ]
                                                ]
                                        ]
                                    ]
                                    // Progress
                                    progressBar watchedCount series.NumberOfEpisodes

                                    // Mark as completed button (when all episodes watched)
                                    match entry.WatchStatus with
                                    | NotStarted | InProgress _ when watchedCount >= series.NumberOfEpisodes ->
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm w-full"
                                            prop.onClick (fun _ -> dispatch MarkSeriesCompleted)
                                            prop.text "Mark as Completed"
                                        ]
                                    | Completed ->
                                        Html.div [
                                            prop.className "text-center text-success text-sm"
                                            prop.text "Series Completed"
                                        ]
                                    | _ -> Html.none
                                ]
                            ]

                            // Right column - Details
                            Html.div [
                                prop.className "md:col-span-2 space-y-6"
                                prop.children [
                                    // Title, meta, and action buttons
                                    Html.div [
                                        Html.h1 [
                                            prop.className "text-3xl font-bold"
                                            prop.text series.Name
                                        ]
                                        Html.div [
                                            prop.className "flex items-center gap-2 mt-2 text-base-content/60 flex-wrap"
                                            prop.children [
                                                // Genres
                                                if not (List.isEmpty series.Genres) then
                                                    Html.span [ prop.text (series.Genres |> String.concat ", ") ]
                                                    Html.span [ prop.className "text-base-content/30"; prop.text "·" ]
                                                // Year
                                                match series.FirstAirDate with
                                                | Some d ->
                                                    Html.span [ prop.text (d.Year.ToString()) ]
                                                    Html.span [ prop.className "text-base-content/30"; prop.text "·" ]
                                                | None -> Html.none
                                                Html.span [ prop.text $"{series.NumberOfSeasons} Seasons" ]
                                                Html.span [ prop.className "text-base-content/30"; prop.text "·" ]
                                                Html.span [ prop.text $"{series.NumberOfEpisodes} Episodes" ]
                                            ]
                                        ]
                                        // Action buttons row
                                        actionButtonsRow entry model.IsRatingOpen dispatch
                                    ]

                                    // Overview
                                    match series.Overview with
                                    | Some overview when overview <> "" ->
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Overview" ]
                                            Html.p [ prop.className "text-base-content/70"; prop.text overview ]
                                        ]
                                    | _ -> Html.none

                                    // Episode progress by season with session tabs
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-4"; prop.text "Episodes" ]

                                        // Session tabs
                                        Html.div [
                                            prop.className "flex flex-wrap items-center gap-2 mb-4"
                                            prop.children [
                                                match model.Sessions with
                                                | Success sessions ->
                                                    // Session tabs - default first, then by start date (oldest to newest)
                                                    let sortedSessions =
                                                        sessions
                                                        |> List.sortBy (fun s ->
                                                            if s.IsDefault then (0, System.DateTime.MinValue)
                                                            else (1, s.StartDate |> Option.defaultValue System.DateTime.MaxValue))
                                                    for session in sortedSessions do
                                                        let isSelected = model.SelectedSessionId = Some session.Id
                                                        Html.div [
                                                            prop.className "flex items-center gap-0.5"
                                                            prop.children [
                                                                Html.button [
                                                                    prop.className (
                                                                        "btn btn-sm " +
                                                                        if isSelected then "btn-primary" else "btn-ghost"
                                                                    )
                                                                    prop.onClick (fun _ -> dispatch (SelectSession session.Id))
                                                                    prop.text (sessionDisplayName session friends)
                                                                ]
                                                                // Delete button for non-default sessions
                                                                if not session.IsDefault then
                                                                    Html.button [
                                                                        prop.className "btn btn-ghost btn-xs text-base-content/40 hover:text-error px-1"
                                                                        prop.onClick (fun e ->
                                                                            e.stopPropagation()
                                                                            dispatch (DeleteSession session.Id))
                                                                        prop.children [
                                                                            Html.span [ prop.className "w-3 h-3"; prop.children [ close ] ]
                                                                        ]
                                                                    ]
                                                            ]
                                                        ]
                                                    // New session button
                                                    Html.button [
                                                        prop.className "btn btn-sm btn-ghost gap-1"
                                                        prop.onClick (fun _ -> dispatch OpenNewSessionModal)
                                                        prop.children [
                                                            Html.span [ prop.className "w-4 h-4"; prop.children [ plus ] ]
                                                            Html.span [ prop.text "New" ]
                                                        ]
                                                    ]
                                                | Loading ->
                                                    Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                | Failure _ | NotAsked -> Html.none
                                            ]
                                        ]

                                        Html.div [
                                            prop.className "space-y-4"
                                            prop.children [
                                                // Build a lookup of watched episodes
                                                let watchedEpisodes =
                                                    model.EpisodeProgress
                                                    |> List.filter (fun p -> p.IsWatched)
                                                    |> List.map (fun p -> (p.SeasonNumber, p.EpisodeNumber))
                                                    |> Set.ofList

                                                // Show each season from TMDB data
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
                                            ]
                                        ]
                                    ]

                                    // Add to Collection
                                    Html.button [
                                        prop.className "btn btn-sm btn-ghost"
                                        prop.onClick (fun _ -> dispatch OpenAddToCollectionModal)
                                        prop.children [
                                            Html.span [ prop.className "w-4 h-4"; prop.children [ collections ] ]
                                            Html.span [ prop.text "Add to Collection" ]
                                        ]
                                    ]

                                    // Collections this entry belongs to
                                    match model.Collections with
                                    | Success collectionsList when not (List.isEmpty collectionsList) ->
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "In Collections" ]
                                            Html.div [
                                                prop.className "flex flex-wrap gap-2"
                                                prop.children [
                                                    for collection in collectionsList do
                                                        Html.span [
                                                            prop.className "badge badge-outline gap-1"
                                                            prop.children [
                                                                Html.span [ prop.text (if collection.IsPublicFranchise then "🎬" else "📚") ]
                                                                Html.span [ prop.text collection.Name ]
                                                            ]
                                                        ]
                                                ]
                                            ]
                                        ]
                                    | _ -> Html.none
                                ]
                            ]
                        ]
                    ]
                | LibraryMovie _ ->
                    Html.div [
                        prop.className "text-center py-12"
                        prop.text "This is a movie, not a series"
                    ]

            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text err
                ]
            | NotAsked -> Html.none
        ]
    ]
