module Pages.SeriesDetail.View

open Feliz
open Fable.React
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View
open Components.FriendSelector.View

module GlassPanel = Common.Components.GlassPanel.View
module Tabs = Common.Components.Tabs.View

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
                    Html.span [ prop.text $"{int percentage}%%" ]
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

/// Context menu position
type ContextMenuPosition = { X: float; Y: float }

/// Episode checkbox component with Ctrl+click support and context menu
[<ReactComponent>]
let private EpisodeCheckbox (seasonNum: int) (epNum: int) (epName: string) (isWatched: bool) (isHovered: bool) (onHover: int option -> unit) (dispatch: Msg -> unit) =
    let (contextMenu, setContextMenu) = React.useState<ContextMenuPosition option> None

    let bgColor =
        if isWatched then
            if isHovered then "#243324" else "#1a2e1a"
        else
            if isHovered then "#242424" else "#1a1a1a"

    let borderColor =
        if isWatched then
            if isHovered then "#3d5c3d" else "#2d4a2d"
        elif isHovered then "#3d3d3d"
        else "#2a2a2a"

    // Close context menu when clicking outside
    React.useEffect (fun () ->
        let closeMenu _ = setContextMenu None
        Browser.Dom.document.addEventListener("click", closeMenu)
        { new System.IDisposable with
            member _.Dispose() =
                Browser.Dom.document.removeEventListener("click", closeMenu)
        }
    , [| box contextMenu |])

    Html.div [
        prop.className "relative"
        prop.children [
            Html.div [
                prop.className "cursor-pointer p-2 rounded border transition-all duration-150 select-none"
                prop.title $"E{epNum}: {epName}"
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
                        dispatch (MarkEpisodesUpTo (seasonNum, epNum, newWatchedState))
                    else
                        dispatch (ToggleEpisodeWatched (seasonNum, epNum, newWatchedState)))
                prop.onContextMenu (fun e ->
                    e.preventDefault()
                    setContextMenu (Some { X = e.clientX; Y = e.clientY }))
                prop.children [
                    Html.span [
                        prop.className "text-xs font-medium"
                        prop.text $"E{epNum}"
                    ]
                ]
            ]

            // Context menu - rendered as portal to body
            match contextMenu with
            | Some pos ->
                ReactDOM.createPortal(
                    Html.div [
                        prop.className "fixed z-[9999] glass border border-white/10 rounded-xl shadow-2xl py-1 min-w-[160px] backdrop-blur-xl"
                        prop.style [
                            style.left (length.px pos.X)
                            style.top (length.px pos.Y)
                        ]
                        prop.onClick (fun e -> e.stopPropagation())
                        prop.children [
                            Html.div [
                                prop.className "px-3 py-1.5 text-xs text-base-content/50 border-b border-white/10"
                                prop.text $"S{seasonNum}E{epNum}"
                            ]
                            Html.button [
                                prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                prop.onClick (fun e ->
                                    e.stopPropagation()
                                    setContextMenu None
                                    dispatch (AddEpisodeToCollection (seasonNum, epNum)))
                                prop.children [
                                    Html.span [ prop.text "+" ]
                                    Html.span [ prop.text "Add to Collection" ]
                                ]
                            ]
                            Html.button [
                                prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                prop.onClick (fun e ->
                                    e.stopPropagation()
                                    setContextMenu None
                                    let newWatchedState = not isWatched
                                    dispatch (ToggleEpisodeWatched (seasonNum, epNum, newWatchedState)))
                                prop.children [
                                    Html.span [ prop.text (if isWatched then "✗" else "✓") ]
                                    Html.span [ prop.text (if isWatched then "Mark Unwatched" else "Mark Watched") ]
                                ]
                            ]
                        ]
                    ],
                    Browser.Dom.document.body
                )
            | None -> Html.none
        ]
    ]

/// Episode list item with proper name display
[<ReactComponent>]
let private EpisodeListItem (seasonNum: int) (ep: TmdbEpisodeSummary) (isWatched: bool) (dispatch: Msg -> unit) =
    let (contextMenu, setContextMenu) = React.useState<ContextMenuPosition option> None

    // Close context menu when clicking outside
    React.useEffect (fun () ->
        let closeMenu _ = setContextMenu None
        Browser.Dom.document.addEventListener("click", closeMenu)
        { new System.IDisposable with
            member _.Dispose() =
                Browser.Dom.document.removeEventListener("click", closeMenu)
        }
    , [| box contextMenu |])

    Html.div [
        prop.className "relative"
        prop.children [
            Html.div [
                prop.className (
                    "flex items-center gap-3 p-3 rounded-lg transition-all duration-150 cursor-pointer " +
                    if isWatched then "bg-success/10 hover:bg-success/20 border border-success/20"
                    else "bg-base-200 hover:bg-base-300 border border-base-300"
                )
                prop.onClick (fun e ->
                    e.preventDefault()
                    let newWatchedState = not isWatched
                    if e.ctrlKey then
                        dispatch (MarkEpisodesUpTo (seasonNum, ep.EpisodeNumber, newWatchedState))
                    else
                        dispatch (ToggleEpisodeWatched (seasonNum, ep.EpisodeNumber, newWatchedState)))
                prop.onContextMenu (fun e ->
                    e.preventDefault()
                    setContextMenu (Some { X = e.clientX; Y = e.clientY }))
                prop.children [
                    // Episode number badge
                    Html.div [
                        prop.className (
                            "flex-shrink-0 w-10 h-10 rounded-lg flex items-center justify-center font-semibold text-sm " +
                            if isWatched then "bg-success/20 text-success"
                            else "bg-base-300 text-base-content/60"
                        )
                        prop.text $"{ep.EpisodeNumber}"
                    ]
                    // Episode info
                    Html.div [
                        prop.className "flex-1 min-w-0"
                        prop.children [
                            Html.div [
                                prop.className "font-medium text-sm truncate"
                                prop.text ep.Name
                            ]
                            Html.div [
                                prop.className "flex items-center gap-2 text-xs text-base-content/50 mt-0.5"
                                prop.children [
                                    match ep.RuntimeMinutes with
                                    | Some mins -> Html.span [ prop.text $"{mins} min" ]
                                    | None -> Html.none
                                    match ep.AirDate with
                                    | Some date ->
                                        Html.span [ prop.text (date.ToString("MMM d, yyyy")) ]
                                    | None -> Html.none
                                ]
                            ]
                        ]
                    ]
                    // Watch indicator
                    Html.div [
                        prop.className "flex-shrink-0"
                        prop.children [
                            if isWatched then
                                Html.span [
                                    prop.className "w-5 h-5 text-success"
                                    prop.children [ check ]
                                ]
                            else
                                Html.span [
                                    prop.className "w-5 h-5 text-base-content/30"
                                    prop.children [ circle ]
                                ]
                        ]
                    ]
                ]
            ]

            // Context menu - rendered as portal to body
            match contextMenu with
            | Some pos ->
                ReactDOM.createPortal(
                    Html.div [
                        prop.className "fixed z-[9999] glass border border-white/10 rounded-xl shadow-2xl py-1 min-w-[160px] backdrop-blur-xl"
                        prop.style [
                            style.left (length.px pos.X)
                            style.top (length.px pos.Y)
                        ]
                        prop.onClick (fun e -> e.stopPropagation())
                        prop.children [
                            Html.div [
                                prop.className "px-3 py-1.5 text-xs text-base-content/50 border-b border-white/10"
                                prop.text $"S{seasonNum}E{ep.EpisodeNumber}: {ep.Name}"
                            ]
                            Html.button [
                                prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                prop.onClick (fun e ->
                                    e.stopPropagation()
                                    setContextMenu None
                                    dispatch (AddEpisodeToCollection (seasonNum, ep.EpisodeNumber)))
                                prop.children [
                                    Html.span [ prop.text "+" ]
                                    Html.span [ prop.text "Add to Collection" ]
                                ]
                            ]
                            Html.button [
                                prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                prop.onClick (fun e ->
                                    e.stopPropagation()
                                    setContextMenu None
                                    let newWatchedState = not isWatched
                                    dispatch (ToggleEpisodeWatched (seasonNum, ep.EpisodeNumber, newWatchedState)))
                                prop.children [
                                    Html.span [ prop.text (if isWatched then "✗" else "✓") ]
                                    Html.span [ prop.text (if isWatched then "Mark Unwatched" else "Mark Watched") ]
                                ]
                            ]
                        ]
                    ],
                    Browser.Dom.document.body
                )
            | None -> Html.none
        ]
    ]

/// Season episodes list with proper names
[<ReactComponent>]
let private SeasonEpisodesList (seasonNum: int) (episodes: TmdbEpisodeSummary list) (watchedEpisodes: Set<int * int>) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-2"
        prop.children [
            for ep in episodes |> List.sortBy (fun e -> e.EpisodeNumber) do
                let isWatched = Set.contains (seasonNum, ep.EpisodeNumber) watchedEpisodes
                EpisodeListItem seasonNum ep isWatched dispatch
        ]
    ]

/// Season episodes grid with Ctrl+hover preview (compact view)
[<ReactComponent>]
let private SeasonEpisodesGrid (seasonNum: int) (episodes: TmdbEpisodeSummary list) (watchedEpisodes: Set<int * int>) (dispatch: Msg -> unit) =
    let (hoveredEp, setHoveredEp) = React.useState<int option> None
    let (ctrlPressed, setCtrlPressed) = React.useState false

    React.useEffect (fun () ->
        let onKeyDown (e: Browser.Types.Event) =
            let ke = e :?> Browser.Types.KeyboardEvent
            if ke.key = "Control" then setCtrlPressed true
        let onKeyUp (e: Browser.Types.Event) =
            let ke = e :?> Browser.Types.KeyboardEvent
            if ke.key = "Control" then setCtrlPressed false
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
                let isInCtrlRange =
                    ctrlPressed &&
                    hoveredEp.IsSome &&
                    ep.EpisodeNumber <= hoveredEp.Value
                let showHover = (hoveredEp = Some ep.EpisodeNumber) || isInCtrlRange
                EpisodeCheckbox seasonNum ep.EpisodeNumber ep.Name isWatched showHover setHoveredEp dispatch
        ]
    ]

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
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (entryCollections: RemoteData<Collection list>) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex flex-wrap items-center gap-3 mt-4"
        prop.children [
            // Rating button
            ratingButton (entry.PersonalRating |> Option.map PersonalRating.toInt) isRatingOpen dispatch
            // Add to Collection button
            Html.div [
                prop.className "tooltip tooltip-bottom detail-tooltip"
                prop.custom ("data-tip", "Add to Collection")
                prop.children [
                    Html.button [
                        prop.className "detail-action-btn"
                        prop.onClick (fun _ -> dispatch OpenAddToCollectionModal)
                        prop.children [
                            Html.span [ prop.className "w-5 h-5"; prop.children [ collections ] ]
                        ]
                    ]
                ]
            ]
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
            // Collection pills
            match entryCollections with
            | Success collectionsList when not (List.isEmpty collectionsList) ->
                for collection in collectionsList do
                    Html.button [
                        prop.key (CollectionId.value collection.Id |> string)
                        prop.className "badge badge-outline badge-sm hover:badge-primary cursor-pointer transition-colors"
                        prop.onClick (fun _ -> dispatch (ViewCollectionDetail (collection.Id, collection.Name)))
                        prop.text collection.Name
                    ]
            | _ -> Html.none
        ]
    ]

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

/// Overview tab content
let private overviewTab (series: Series) (entry: LibraryEntry) (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Overview
            match series.Overview with
            | Some overview when overview <> "" ->
                Html.div [
                    Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Overview" ]
                    Html.p [ prop.className "text-base-content/70"; prop.text overview ]
                ]
            | _ -> Html.none

            // Notes
            Html.div [
                Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Notes" ]
                Html.textarea [
                    prop.className "textarea textarea-bordered w-full h-24"
                    prop.placeholder "Add your notes..."
                    prop.value (entry.Notes |> Option.defaultValue "")
                    prop.onChange (fun (e: string) -> dispatch (UpdateNotes e))
                    prop.onBlur (fun _ -> dispatch SaveNotes)
                ]
            ]
        ]
    ]

/// Cast & Crew tab content
let private castCrewTab (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            match model.Credits with
            | Success credits ->
                // Cast section - sort tracked contributors first
                let trackedCast, untrackedCast =
                    credits.Cast |> List.partition (fun c -> Set.contains c.TmdbPersonId model.TrackedPersonIds)
                let sortedCast = trackedCast @ untrackedCast

                if not (List.isEmpty sortedCast) then
                    Html.div [
                        prop.children [
                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Cast" ]
                            Html.div [
                                prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                prop.children [
                                    for castMember in sortedCast do
                                        let isTracked = model.TrackedPersonIds |> Set.contains castMember.TmdbPersonId
                                        Html.button [
                                            prop.key (TmdbPersonId.value castMember.TmdbPersonId |> string)
                                            prop.className (
                                                if isTracked then
                                                    "flex items-center gap-2 px-3 py-2 rounded-lg bg-primary/10 hover:bg-primary/20 border border-primary/30 transition-colors cursor-pointer"
                                                else
                                                    "flex items-center gap-2 px-3 py-2 rounded-lg bg-base-200 hover:bg-base-300 transition-colors cursor-pointer"
                                            )
                                            prop.onClick (fun _ -> dispatch (ViewContributor (castMember.TmdbPersonId, castMember.Name)))
                                            prop.children [
                                                Html.div [
                                                    prop.className "relative"
                                                    prop.children [
                                                        match castMember.ProfilePath with
                                                        | Some path ->
                                                            Html.img [
                                                                prop.src $"https://image.tmdb.org/t/p/w45{path}"
                                                                prop.className "w-8 h-8 rounded-full object-cover"
                                                                prop.alt castMember.Name
                                                            ]
                                                        | None ->
                                                            Html.div [
                                                                prop.className "w-8 h-8 rounded-full bg-base-300 flex items-center justify-center"
                                                                prop.children [
                                                                    Html.span [ prop.className "w-4 h-4 text-base-content/40"; prop.children [ userPlus ] ]
                                                                ]
                                                            ]
                                                        if isTracked then
                                                            Html.div [
                                                                prop.className "absolute -top-1 -right-1 w-4 h-4 rounded-full bg-primary flex items-center justify-center"
                                                                prop.children [
                                                                    Html.span [ prop.className "w-2.5 h-2.5 text-primary-content"; prop.children [ heart ] ]
                                                                ]
                                                            ]
                                                    ]
                                                ]
                                                Html.div [
                                                    prop.className "text-left"
                                                    prop.children [
                                                        Html.span [ prop.className "text-sm font-medium block"; prop.text castMember.Name ]
                                                        match castMember.Character with
                                                        | Some char ->
                                                            Html.span [ prop.className "text-xs text-base-content/60"; prop.text char ]
                                                        | None -> Html.none
                                                    ]
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]

                // Crew section
                let keyCrewRoles = ["Director"; "Screenplay"; "Writer"; "Executive Producer"; "Creator"]
                let keyCrew = credits.Crew |> List.filter (fun c -> List.contains c.Job keyCrewRoles)
                if not (List.isEmpty keyCrew) then
                    Html.div [
                        prop.className "mt-4"
                        prop.children [
                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Crew" ]
                            Html.div [
                                prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                prop.children [
                                    for crewMember in keyCrew |> List.distinctBy (fun c -> TmdbPersonId.value c.TmdbPersonId) do
                                        Html.button [
                                            prop.key (TmdbPersonId.value crewMember.TmdbPersonId |> string)
                                            prop.className "flex items-center gap-2 px-3 py-2 rounded-lg bg-base-200 hover:bg-base-300 transition-colors cursor-pointer"
                                            prop.onClick (fun _ -> dispatch (ViewContributor (crewMember.TmdbPersonId, crewMember.Name)))
                                            prop.children [
                                                match crewMember.ProfilePath with
                                                | Some path ->
                                                    Html.img [
                                                        prop.src $"https://image.tmdb.org/t/p/w45{path}"
                                                        prop.className "w-8 h-8 rounded-full object-cover"
                                                        prop.alt crewMember.Name
                                                    ]
                                                | None ->
                                                    Html.div [
                                                        prop.className "w-8 h-8 rounded-full bg-base-300 flex items-center justify-center"
                                                        prop.children [
                                                            Html.span [ prop.className "w-4 h-4 text-base-content/40"; prop.children [ userPlus ] ]
                                                        ]
                                                    ]
                                                Html.div [
                                                    prop.className "text-left"
                                                    prop.children [
                                                        Html.span [ prop.className "text-sm font-medium block"; prop.text crewMember.Name ]
                                                        Html.span [ prop.className "text-xs text-base-content/60"; prop.text crewMember.Job ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-8"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Failure _ ->
                Html.div [
                    prop.className "text-center py-8 text-base-content/60"
                    prop.text "Could not load cast and crew"
                ]
            | NotAsked -> Html.none
        ]
    ]

/// Episodes tab content
let private episodesTab (series: Series) (model: Model) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-4"
        prop.children [
            // Session tabs
            Html.div [
                prop.className "flex flex-wrap items-center gap-2 mb-4"
                prop.children [
                    match model.Sessions with
                    | Success sessions ->
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

            // Seasons
            Html.div [
                prop.className "space-y-4"
                prop.children [
                    let watchedEpisodes =
                        model.EpisodeProgress
                        |> List.filter (fun p -> p.IsWatched)
                        |> List.map (fun p -> (p.SeasonNumber, p.EpisodeNumber))
                        |> Set.ofList

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
                                                            Html.button [
                                                                prop.className "btn btn-xs btn-ghost"
                                                                prop.title "Add season to collection"
                                                                prop.onClick (fun _ -> dispatch (AddSeasonToCollection seasonNum))
                                                                prop.children [
                                                                    Html.span [ prop.text "+" ]
                                                                    Html.span [ prop.className "hidden sm:inline ml-1"; prop.text "Collection" ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            SeasonEpisodesList seasonNum seasonDetails.Episodes watchedEpisodes dispatch
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
    ]

/// Friends tab content
let private friendsTab (entry: LibraryEntry) (allFriends: Friend list) (isFriendSelectorOpen: bool) (isAddingFriend: bool) (dispatch: Msg -> unit) =
    let selectedFriendsList =
        entry.Friends
        |> List.choose (fun fid -> allFriends |> List.tryFind (fun f -> f.Id = fid))

    Html.div [
        prop.className "space-y-4"
        prop.children [
            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Watched With" ]

            // Toggle button to open/close friend selector
            Html.button [
                prop.className "btn btn-sm btn-ghost gap-2"
                prop.onClick (fun _ -> dispatch ToggleFriendSelector)
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ userPlus ] ]
                    Html.span [ prop.text (if isFriendSelectorOpen then "Close" else "Add Friends") ]
                ]
            ]

            if isFriendSelectorOpen then
                Html.div [
                    prop.children [
                        FriendSelector {
                            AllFriends = allFriends
                            SelectedFriends = entry.Friends
                            OnToggle = fun friendId -> dispatch (ToggleFriend friendId)
                            OnAddNew = fun name -> dispatch (AddNewFriend name)
                            OnSubmit = Some (fun () -> dispatch ToggleFriendSelector)
                            IsDisabled = isAddingFriend
                            Placeholder = "Search or add friends..."
                            IsRequired = false
                            AutoFocus = true
                        }
                        if isAddingFriend then
                            Html.div [
                                prop.className "flex items-center gap-2 mt-2 text-sm text-base-content/60"
                                prop.children [
                                    Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                    Html.span [ prop.text "Adding friend..." ]
                                ]
                            ]
                    ]
                ]

            // Friend pills (clickable to navigate to friend detail)
            if not (List.isEmpty selectedFriendsList) then
                Html.div [
                    prop.className "flex flex-wrap items-center gap-3 mt-4"
                    prop.children [
                        for friend in selectedFriendsList do
                            Html.div [
                                prop.key (FriendId.value friend.Id |> string)
                                prop.className "inline-flex flex-row items-center gap-2 px-3 py-1.5 rounded-full bg-base-200 border border-base-300 cursor-pointer hover:bg-base-300 transition-colors"
                                prop.onClick (fun _ -> dispatch (ViewFriendDetail (friend.Id, friend.Name)))
                                prop.children [
                                    // Friend avatar (same size as friends list)
                                    match friend.AvatarUrl with
                                    | Some url ->
                                        Html.div [
                                            prop.className "w-8 h-8 rounded-full overflow-hidden flex-shrink-0"
                                            prop.children [
                                                Html.img [
                                                    prop.src $"/images/avatars{url}"
                                                    prop.alt friend.Name
                                                    prop.className "w-full h-full object-cover"
                                                ]
                                            ]
                                        ]
                                    | None ->
                                        Html.div [
                                            prop.className "w-8 h-8 rounded-full bg-primary/30 flex items-center justify-center flex-shrink-0"
                                            prop.children [
                                                Html.span [
                                                    prop.className "text-sm font-medium"
                                                    prop.text (friend.Name.Substring(0, 1).ToUpperInvariant())
                                                ]
                                            ]
                                        ]
                                    Html.span [
                                        prop.className "text-sm font-medium whitespace-nowrap"
                                        prop.text friend.Name
                                    ]
                                ]
                            ]
                    ]
                ]
            elif not isFriendSelectorOpen then
                Html.p [
                    prop.className "text-base-content/60 text-sm"
                    prop.text "No friends added yet. Click 'Add Friends' to track who you watched with."
                ]
        ]
    ]

/// Get the tab definitions
let private seriesTabs : Common.Components.Tabs.Types.Tab list = [
    { Id = "overview"; Label = "Overview"; Icon = Some info }
    { Id = "cast-crew"; Label = "Cast & Crew"; Icon = Some friends }
    { Id = "episodes"; Label = "Episodes"; Icon = Some tv }
    { Id = "friends"; Label = "Friends"; Icon = Some userPlus }
]

/// Map SeriesTab to string ID
let private tabToId = function
    | Overview -> "overview"
    | CastCrew -> "cast-crew"
    | Episodes -> "episodes"
    | Friends -> "friends"

/// Map string ID to SeriesTab
let private idToTab = function
    | "overview" -> Overview
    | "cast-crew" -> CastCrew
    | "episodes" -> Episodes
    | "friends" -> Friends
    | _ -> Overview

let view (model: Model) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button - uses browser history for proper navigation
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> Browser.Dom.window.history.back())
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back" ]
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
                        prop.className "space-y-6"
                        prop.children [
                            // Header: Poster + Title/Meta + Actions
                            Html.div [
                                prop.className "grid grid-cols-1 md:grid-cols-3 gap-8"
                                prop.children [
                                    // Left column - Poster + Progress
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

                                    // Right column - Title, meta, actions, and tabs (on md+)
                                    Html.div [
                                        prop.className "md:col-span-2 flex flex-col"
                                        prop.children [
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
                                            actionButtonsRow entry model.IsRatingOpen model.Collections dispatch

                                            // Tab bar and content (hidden on mobile, shown on md+)
                                            Html.div [
                                                prop.className "hidden md:block mt-6"
                                                prop.children [
                                                    Tabs.view
                                                        seriesTabs
                                                        (tabToId model.ActiveTab)
                                                        (fun id -> dispatch (SetActiveTab (idToTab id)))
                                                        (match model.ActiveTab with
                                                         | Overview -> overviewTab series entry model dispatch
                                                         | CastCrew -> castCrewTab model dispatch
                                                         | Episodes -> episodesTab series model friends dispatch
                                                         | Friends -> friendsTab entry friends model.IsFriendSelectorOpen model.IsAddingFriend dispatch)
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Tab bar and content (shown on mobile only, hidden on md+)
                            Html.div [
                                prop.className "md:hidden"
                                prop.children [
                                    Tabs.view
                                        seriesTabs
                                        (tabToId model.ActiveTab)
                                        (fun id -> dispatch (SetActiveTab (idToTab id)))
                                        (match model.ActiveTab with
                                         | Overview -> overviewTab series entry model dispatch
                                         | CastCrew -> castCrewTab model dispatch
                                         | Episodes -> episodesTab series model friends dispatch
                                         | Friends -> friendsTab entry friends model.IsFriendSelectorOpen model.IsAddingFriend dispatch)
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
