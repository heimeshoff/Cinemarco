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

/// Rating labels and descriptions
let private ratingInfo = [
    (1, "Waste", "Waste of time")
    (2, "Meh", "Didn't click, uninspiring")
    (3, "Decent", "Watchable, even if not life-changing")
    (4, "Entertaining", "Strong craft, enjoyable, recommendable")
    (5, "Outstanding", "Absolutely brilliant, stays with you")
]

/// Rating selector component with tooltips
let private ratingSelector (current: int option) (dispatch: Msg -> unit) =
    let currentRating = current |> Option.defaultValue 0
    Html.div [
        prop.className "space-y-2"
        prop.children [
            // Stars row
            Html.div [
                prop.className "flex items-center gap-1"
                prop.children [
                    for i in 1..5 do
                        let isFilled = i <= currentRating
                        let (_, label, description) = ratingInfo |> List.find (fun (n, _, _) -> n = i)
                        Html.div [
                            prop.className "tooltip tooltip-top"
                            prop.custom ("data-tip", $"{label}: {description}")
                            prop.children [
                                Html.button [
                                    prop.className (
                                        "w-8 h-8 transition-all hover:scale-110 " +
                                        if isFilled then "text-yellow-400" else "text-base-content/20 hover:text-yellow-400/50"
                                    )
                                    prop.onClick (fun _ ->
                                        // Click on current rating clears it
                                        if i = currentRating then
                                            dispatch (SetRating 0)
                                        else
                                            dispatch (SetRating i))
                                    prop.children [ starSolid ]
                                ]
                            ]
                        ]
                    // Clear button when rating is set
                    if currentRating > 0 then
                        Html.button [
                            prop.className "ml-2 text-xs text-base-content/40 hover:text-base-content/60 transition-colors"
                            prop.onClick (fun _ -> dispatch (SetRating 0))
                            prop.text "Clear"
                        ]
                ]
            ]
            // Current rating description
            match current with
            | Some r when r > 0 ->
                let (_, label, description) = ratingInfo |> List.find (fun (n, _, _) -> n = r)
                Html.p [
                    prop.className "text-sm text-base-content/60"
                    prop.children [
                        Html.span [ prop.className "font-medium text-base-content/80"; prop.text label ]
                        Html.span [ prop.text $" - {description}" ]
                    ]
                ]
            | _ ->
                Html.p [
                    prop.className "text-sm text-base-content/40"
                    prop.text "Click a star to rate"
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

                                    // Watch controls
                                    match entry.WatchStatus with
                                    | NotStarted | InProgress _ ->
                                        if watchedCount >= series.NumberOfEpisodes then
                                            Html.button [
                                                prop.className "btn btn-primary btn-sm w-full"
                                                prop.onClick (fun _ -> dispatch MarkSeriesCompleted)
                                                prop.text "Mark as Completed"
                                            ]
                                        Html.button [
                                            prop.className "btn btn-outline btn-error btn-sm w-full"
                                            prop.onClick (fun _ -> dispatch OpenAbandonModal)
                                            prop.text "Abandon"
                                        ]
                                    | Completed ->
                                        Html.div [
                                            prop.className "text-center text-success text-sm"
                                            prop.text "Series Completed"
                                        ]
                                    | Abandoned _ ->
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm w-full"
                                            prop.onClick (fun _ -> dispatch ResumeEntry)
                                            prop.text "Resume Watching"
                                        ]
                                ]
                            ]

                            // Right column - Details
                            Html.div [
                                prop.className "md:col-span-2 space-y-6"
                                prop.children [
                                    // Title and meta
                                    Html.div [
                                        Html.h1 [
                                            prop.className "text-3xl font-bold"
                                            prop.text series.Name
                                        ]
                                        Html.div [
                                            prop.className "flex items-center gap-4 mt-2 text-base-content/60"
                                            prop.children [
                                                match series.FirstAirDate with
                                                | Some d -> Html.span [ prop.text (d.Year.ToString()) ]
                                                | None -> Html.none
                                                Html.span [ prop.text $"{series.NumberOfSeasons} Seasons" ]
                                                Html.span [ prop.text $"{series.NumberOfEpisodes} Episodes" ]
                                            ]
                                        ]
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

                                    // Rating
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Your Rating" ]
                                        ratingSelector (entry.PersonalRating |> Option.map PersonalRating.toInt) dispatch
                                    ]

                                    // Favorite toggle
                                    Html.div [
                                        prop.className "flex items-center gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.className (
                                                    "btn btn-sm " +
                                                    if entry.IsFavorite then "btn-secondary" else "btn-ghost"
                                                )
                                                prop.onClick (fun _ -> dispatch ToggleFavorite)
                                                prop.children [
                                                    Html.span [ prop.className "w-4 h-4"; prop.children [ if entry.IsFavorite then heartSolid else heart ] ]
                                                    Html.span [ prop.text (if entry.IsFavorite then "Favorited" else "Add to Favorites") ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    // Delete button
                                    Html.div [
                                        prop.className "pt-4 border-t border-base-300"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-error btn-outline btn-sm"
                                                prop.onClick (fun _ -> dispatch OpenDeleteModal)
                                                prop.children [
                                                    Html.span [ prop.className "w-4 h-4"; prop.children [ trash ] ]
                                                    Html.span [ prop.text "Delete Entry" ]
                                                ]
                                            ]
                                        ]
                                    ]
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
