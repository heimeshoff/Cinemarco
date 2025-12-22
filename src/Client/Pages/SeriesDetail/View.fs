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
module RatingButton = Common.Components.RatingButton.View
module CastCrewSection = Common.Components.CastCrewSection.View
module BackButton = Common.Components.BackButton.View
module ProgressBar = Common.Components.ProgressBar.View

/// Progress bar component (using shared component)
let private progressBar (current: int) (total: int) = ProgressBar.simple current total

/// Context menu position
type ContextMenuPosition = { X: float; Y: float }

/// Episode card with still image - watched episodes have full color, unwatched are semi-transparent
[<ReactComponent>]
let private EpisodeCard (seasonNum: int) (ep: TmdbEpisodeSummary) (isWatched: bool) (watchedDate: System.DateTime option) (dispatch: Msg -> unit) =
    let (contextMenu, setContextMenu) = React.useState<ContextMenuPosition option> None
    let (isEditingDate, setIsEditingDate) = React.useState false
    let (pendingDate, setPendingDate) = React.useState<string>(
        watchedDate
        |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
        |> Option.defaultValue ""
    )

    // Close context menu when clicking outside
    React.useEffect (fun () ->
        let closeMenu _ = setContextMenu None
        Browser.Dom.document.addEventListener("click", closeMenu)
        { new System.IDisposable with
            member _.Dispose() =
                Browser.Dom.document.removeEventListener("click", closeMenu)
        }
    , [| box contextMenu |])

    let stillUrl = getLocalStillUrl ep.StillPath

    // Format date in German format (DD.MM.YYYY)
    let formatGermanDate (date: System.DateTime) = date.ToString("dd.MM.yyyy")

    Html.div [
        prop.className "relative"
        prop.children [
            Html.div [
                prop.className "episode-card group"
                prop.onContextMenu (fun e ->
                    e.preventDefault()
                    setContextMenu (Some { X = e.clientX; Y = e.clientY }))
                prop.children [
                    // Episode still image container
                    Html.div [
                        prop.className (
                            "relative aspect-video rounded-lg overflow-hidden transition-all duration-200 " +
                            if isWatched then "" else "opacity-40 grayscale"
                        )
                        prop.children [
                            if stillUrl <> "" then
                                Html.img [
                                    prop.src stillUrl
                                    prop.alt ep.Name
                                    prop.className "w-full h-full object-cover"
                                ]
                            else
                                // Placeholder when no still available
                                Html.div [
                                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-2xl text-base-content/20"
                                            prop.children [ tv ]
                                        ]
                                    ]
                                ]
                            // Hover overlay with duration and watch date
                            Html.div [
                                prop.className "absolute inset-0 bg-black/0 group-hover:bg-black/50 transition-colors duration-200 flex items-end justify-between p-1.5 opacity-0 group-hover:opacity-100"
                                prop.children [
                                    // Duration on left
                                    match ep.RuntimeMinutes with
                                    | Some mins ->
                                        Html.span [
                                            prop.className "text-xs text-white/90 font-medium"
                                            prop.text $"{mins}m"
                                        ]
                                    | None -> Html.span []
                                    // Watch date on right (German format)
                                    if isWatched then
                                        match watchedDate with
                                        | Some d ->
                                            Html.span [
                                                prop.className "text-xs text-white/90 font-medium"
                                                prop.text (formatGermanDate d)
                                            ]
                                        | None -> Html.none
                                ]
                            ]
                        ]
                    ]
                    // Episode info below image
                    Html.div [
                        prop.className "mt-1.5 px-0.5"
                        prop.children [
                            Html.div [
                                prop.className (
                                    "text-xs font-medium truncate " +
                                    if isWatched then "text-base-content" else "text-base-content/50"
                                )
                                prop.title ep.Name
                                prop.text $"{ep.EpisodeNumber}: {ep.Name}"
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
                        prop.className "fixed z-[9999] glass border border-white/10 rounded-xl shadow-2xl py-1 min-w-[200px] backdrop-blur-xl"
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
                            // For unwatched: Mark Watched first
                            if not isWatched then
                                Html.button [
                                    prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                    prop.onClick (fun e ->
                                        e.stopPropagation()
                                        setContextMenu None
                                        dispatch (ToggleEpisodeWatched (seasonNum, ep.EpisodeNumber, true)))
                                    prop.children [
                                        Html.span [ prop.className "w-4 h-4 text-base-content/60"; prop.children [ check ] ]
                                        Html.span [ prop.text "Mark Watched" ]
                                    ]
                                ]
                            // For watched: Change date first, then Mark Unwatched
                            if isWatched then
                                if isEditingDate then
                                    Html.div [
                                        prop.className "px-3 py-2"
                                        prop.children [
                                            Html.div [
                                                prop.className "text-xs text-base-content/50 mb-1"
                                                prop.text "Watch date"
                                            ]
                                            Html.div [
                                                prop.className "flex items-center gap-1"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-xs input-bordered w-28 bg-base-100"
                                                        prop.type' "date"
                                                        prop.value pendingDate
                                                        prop.onClick (fun e -> e.stopPropagation())
                                                        prop.onChange (fun (value: string) -> setPendingDate value)
                                                        prop.autoFocus true
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-xs btn-ghost text-success"
                                                        prop.title "Save"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            if System.String.IsNullOrEmpty(pendingDate) then
                                                                dispatch (UpdateEpisodeWatchedDate (seasonNum, ep.EpisodeNumber, None))
                                                            else
                                                                match System.DateTime.TryParse(pendingDate) with
                                                                | true, date ->
                                                                    dispatch (UpdateEpisodeWatchedDate (seasonNum, ep.EpisodeNumber, Some date))
                                                                | _ -> ()
                                                            setIsEditingDate false
                                                            setContextMenu None)
                                                        prop.children [ Html.span [ prop.className "w-3 h-3"; prop.children [ check ] ] ]
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-xs btn-ghost text-error"
                                                        prop.title "Cancel"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            setPendingDate (
                                                                watchedDate
                                                                |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
                                                                |> Option.defaultValue ""
                                                            )
                                                            setIsEditingDate false)
                                                        prop.children [ Html.span [ prop.className "w-3 h-3"; prop.children [ close ] ] ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                else
                                    Html.button [
                                        prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                        prop.onClick (fun e ->
                                            e.stopPropagation()
                                            setPendingDate (
                                                watchedDate
                                                |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
                                                |> Option.defaultValue ""
                                            )
                                            setIsEditingDate true)
                                        prop.children [
                                            Html.span [ prop.className "w-4 h-4 text-base-content/60"; prop.children [ clock ] ]
                                            Html.span [
                                                prop.text (
                                                    match watchedDate with
                                                    | Some d -> $"Change date ({formatGermanDate d})"
                                                    | None -> "Set watch date"
                                                )
                                            ]
                                        ]
                                    ]
                                Html.button [
                                    prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                    prop.onClick (fun e ->
                                        e.stopPropagation()
                                        setContextMenu None
                                        dispatch (ToggleEpisodeWatched (seasonNum, ep.EpisodeNumber, false)))
                                    prop.children [
                                        Html.span [ prop.className "w-4 h-4 text-base-content/60"; prop.children [ close ] ]
                                        Html.span [ prop.text "Mark Unwatched" ]
                                    ]
                                ]
                            // Add to Collection is always last
                            Html.button [
                                prop.className "w-full text-left px-3 py-2 text-sm hover:bg-white/10 transition-colors flex items-center gap-2"
                                prop.onClick (fun e ->
                                    e.stopPropagation()
                                    setContextMenu None
                                    dispatch (AddEpisodeToCollection (seasonNum, ep.EpisodeNumber)))
                                prop.children [
                                    Html.span [ prop.className "w-4 h-4 text-base-content/60"; prop.children [ plus ] ]
                                    Html.span [ prop.text "Add to Collection" ]
                                ]
                            ]
                        ]
                    ],
                    Browser.Dom.document.body
                )
            | None -> Html.none
        ]
    ]

/// Season episodes grid with still images
[<ReactComponent>]
let private SeasonEpisodesGrid (seasonNum: int) (episodes: TmdbEpisodeSummary list) (watchedEpisodes: Set<int * int>) (episodeProgress: EpisodeProgress list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-3"
        prop.children [
            for ep in episodes |> List.sortBy (fun e -> e.EpisodeNumber) do
                let isWatched = Set.contains (seasonNum, ep.EpisodeNumber) watchedEpisodes
                let watchedDate =
                    episodeProgress
                    |> List.tryFind (fun p -> p.SeasonNumber = seasonNum && p.EpisodeNumber = ep.EpisodeNumber)
                    |> Option.bind (fun p -> p.WatchedDate)
                EpisodeCard seasonNum ep isWatched watchedDate dispatch
        ]
    ]

/// Action buttons row for series (rating, abandon/resume, delete)
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (isFinished: bool) (entryCollections: RemoteData<Collection list>) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex flex-wrap items-center gap-3 mt-4"
        prop.children [
            // Rating button
            RatingButton.button
                (entry.PersonalRating |> Option.map PersonalRating.toInt)
                isRatingOpen
                (fun rating -> dispatch (SetRating rating))
                (fun () -> dispatch ToggleRatingDropdown)
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
            // Abandon/Resume toggle button (hidden when finished)
            if not isFinished then
                match entry.WatchStatus with
                | Abandoned _ ->
                    Html.div [
                        prop.className "tooltip tooltip-bottom detail-tooltip"
                        prop.custom ("data-tip", "Resume Watching")
                        prop.children [
                            Html.button [
                                prop.className "detail-action-btn detail-action-btn-success"
                                prop.onClick (fun _ -> dispatch ToggleAbandoned)
                                prop.children [
                                    Html.span [ prop.className "w-5 h-5"; prop.children [ undo ] ]
                                ]
                            ]
                        ]
                    ]
                | _ ->
                    Html.div [
                        prop.className "tooltip tooltip-bottom detail-tooltip"
                        prop.custom ("data-tip", "Abandon Series")
                        prop.children [
                            Html.button [
                                prop.className "detail-action-btn text-warning/70"
                                prop.onClick (fun _ -> dispatch ToggleAbandoned)
                                prop.children [
                                    Html.span [ prop.className "w-5 h-5"; prop.children [ ban ] ]
                                ]
                            ]
                        ]
                    ]
            // View in Graph button
            Html.div [
                prop.className "tooltip tooltip-bottom detail-tooltip"
                prop.custom ("data-tip", "View in Graph")
                prop.children [
                    Html.button [
                        prop.className "detail-action-btn"
                        prop.onClick (fun _ -> dispatch ViewInGraph)
                        prop.children [
                            Html.span [ prop.className "w-5 h-5"; prop.children [ graph ] ]
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

/// Generate a display name for a session based on its friends (simple string version for button text)
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

/// Generate friend name display for a session (plain text, no links)
let private sessionFriendNames (session: WatchSession) (allFriends: Friend list) =
    let sessionFriends =
        session.Friends
        |> List.choose (fun fid -> allFriends |> List.tryFind (fun f -> f.Id = fid))

    match sessionFriends with
    | [] -> "Personal"
    | [ friend ] -> $"with {friend.Name}"
    | friends ->
        let names = friends |> List.map (fun f -> f.Name)
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
    CastCrewSection.viewWithLoading
        model.Credits
        model.TrackedPersonIds
        model.IsFullCastCrewExpanded
        (fun (id, name) -> dispatch (ViewContributor (id, name)))
        (fun () -> dispatch ToggleFullCastCrew)

/// Session context menu (three-dot menu for navigating to friends, removing, or adding)
[<ReactComponent>]
let private SessionContextMenu (session: WatchSession) (allFriends: Friend list) (isOpen: bool) (dispatch: Msg -> unit) =
    // Close dropdown when clicking outside
    React.useEffect ((fun () ->
        let closeDropdown (e: Browser.Types.Event) =
            dispatch CloseSessionFriendEditor
        if isOpen then
            Browser.Dom.window.setTimeout((fun () ->
                Browser.Dom.document.addEventListener("click", closeDropdown)
            ), 100) |> ignore
        { new System.IDisposable with
            member _.Dispose() =
                Browser.Dom.document.removeEventListener("click", closeDropdown)
        }
    ), [| box isOpen |])

    // Get friends in this session and friends not in this session
    let sessionFriends =
        session.Friends
        |> List.choose (fun fid -> allFriends |> List.tryFind (fun f -> f.Id = fid))
    let availableFriends =
        allFriends
        |> List.filter (fun f -> not (List.contains f.Id session.Friends))

    Html.div [
        prop.className "relative inline-flex items-center"
        prop.children [
            // Three-dot button
            Html.button [
                prop.className "w-6 h-6 flex items-center justify-center rounded-full text-base-content/40 hover:text-base-content hover:bg-base-300 transition-colors"
                prop.onClick (fun e ->
                    e.stopPropagation()
                    if isOpen then dispatch CloseSessionFriendEditor
                    else dispatch (OpenSessionFriendEditor session.Id))
                prop.title "Session options"
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ ellipsisVertical ] ]
                ]
            ]

            // Context menu dropdown
            if isOpen then
                Html.div [
                    prop.className "absolute right-0 top-full mt-1 z-50 min-w-[220px] rounded-lg shadow-xl border border-base-300 overflow-hidden"
                    prop.style [ style.backgroundColor "#1d232a" ]
                    prop.onClick (fun e -> e.stopPropagation())
                    prop.children [
                        // Current friends section (if any)
                        if not (List.isEmpty sessionFriends) then
                            Html.div [
                                prop.className "px-3 py-2 text-xs text-base-content/50 border-b border-base-300 uppercase tracking-wide"
                                prop.text "Watching with"
                            ]
                            for friend in sessionFriends do
                                Html.div [
                                    prop.key (FriendId.value friend.Id |> string)
                                    prop.className "flex items-center justify-between px-3 py-2 hover:bg-base-300/30"
                                    prop.children [
                                        // Friend name (clickable to navigate)
                                        Html.button [
                                            prop.className "flex-1 text-left hover:text-primary transition-colors"
                                            prop.onClick (fun e ->
                                                e.stopPropagation()
                                                dispatch CloseSessionFriendEditor
                                                dispatch (ViewFriendDetail (friend.Id, friend.Name)))
                                            prop.children [
                                                Html.span [
                                                    prop.className "font-medium"
                                                    prop.text friend.Name
                                                ]
                                                match friend.Nickname with
                                                | Some nick ->
                                                    Html.span [
                                                        prop.className "text-base-content/50 text-sm ml-2"
                                                        prop.text $"({nick})"
                                                    ]
                                                | None -> Html.none
                                            ]
                                        ]
                                        // Remove button
                                        Html.button [
                                            prop.className "w-6 h-6 flex items-center justify-center rounded-full text-base-content/40 hover:text-error hover:bg-error/10 transition-colors ml-2"
                                            prop.onClick (fun e ->
                                                e.stopPropagation()
                                                dispatch (ToggleSessionFriend (session.Id, friend.Id)))
                                            prop.title $"Remove {friend.Name}"
                                            prop.children [
                                                Html.span [ prop.className "w-4 h-4"; prop.children [ close ] ]
                                            ]
                                        ]
                                    ]
                                ]

                        // Add friends section (if any available)
                        if not (List.isEmpty availableFriends) then
                            Html.div [
                                prop.className "px-3 py-2 text-xs text-base-content/50 border-t border-base-300 uppercase tracking-wide"
                                prop.text "Add friend"
                            ]
                            for friend in availableFriends do
                                Html.button [
                                    prop.key (FriendId.value friend.Id |> string)
                                    prop.className "w-full flex items-center gap-2 px-3 py-2 hover:bg-base-300/30 text-left"
                                    prop.onClick (fun e ->
                                        e.stopPropagation()
                                        dispatch (ToggleSessionFriend (session.Id, friend.Id)))
                                    prop.children [
                                        Html.span [ prop.className "w-4 h-4 text-primary"; prop.children [ plus ] ]
                                        Html.span [
                                            prop.className "font-medium"
                                            prop.text friend.Name
                                        ]
                                        match friend.Nickname with
                                        | Some nick ->
                                            Html.span [
                                                prop.className "text-base-content/50 text-sm"
                                                prop.text $"({nick})"
                                            ]
                                        | None -> Html.none
                                    ]
                                ]

                        // Empty state
                        if List.isEmpty sessionFriends && List.isEmpty availableFriends then
                            Html.div [
                                prop.className "px-4 py-3 text-base-content/50 text-sm"
                                prop.text "No friends yet"
                            ]
                    ]
                ]
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
                            let isEditing = model.EditingSessionId = Some session.Id
                            Html.div [
                                prop.className "flex items-center gap-1"
                                prop.children [
                                    // Session pill with glassmorphism background
                                    Html.div [
                                        prop.className (
                                            "flex items-center gap-2 px-4 py-2 rounded-xl cursor-pointer transition-all backdrop-blur-md border " +
                                            if isSelected
                                            then "bg-primary/20 border-primary/40 text-primary-content shadow-lg shadow-primary/10"
                                            else "bg-white/5 border-white/10 hover:bg-white/10 hover:border-white/20"
                                        )
                                        prop.onClick (fun _ -> dispatch (SelectSession session.Id))
                                        prop.children [
                                            Html.span [
                                                prop.className "text-sm font-medium"
                                                prop.text (sessionFriendNames session friends)
                                            ]
                                            // Three-dot context menu
                                            SessionContextMenu session friends isEditing dispatch
                                        ]
                                    ]
                                    // Delete button (only for non-default sessions)
                                    if not session.IsDefault then
                                        Html.button [
                                            prop.className "w-6 h-6 flex items-center justify-center rounded-full text-base-content/40 hover:text-error hover:bg-error/10 transition-colors"
                                            prop.onClick (fun e ->
                                                e.stopPropagation()
                                                dispatch (DeleteSession session.Id))
                                            prop.title "Delete session"
                                            prop.children [
                                                Html.span [ prop.className "w-4 h-4"; prop.children [ close ] ]
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
                                            SeasonEpisodesGrid seasonNum seasonDetails.Episodes watchedEpisodes model.EpisodeProgress dispatch
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

/// Get the tab definitions
let private seriesTabs : Common.Components.Tabs.Types.Tab list = [
    { Id = "overview"; Label = "Overview"; Icon = Some info }
    { Id = "cast-crew"; Label = "Cast & Crew"; Icon = Some friends }
    { Id = "episodes"; Label = "Episodes"; Icon = Some tv }
]

/// Map SeriesTab to string ID
let private tabToId = function
    | Overview -> "overview"
    | CastCrew -> "cast-crew"
    | Episodes -> "episodes"

/// Map string ID to SeriesTab
let private idToTab = function
    | "overview" -> Overview
    | "cast-crew" -> CastCrew
    | "episodes" -> Episodes
    | _ -> Overview

let view (model: Model) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative"
        prop.children [
            // Back button - overlays poster on mobile, normal flow on desktop
            Html.div [
                prop.className "detail-back-button absolute top-2 left-0 z-10 md:relative md:top-0 md:mb-6"
                prop.children [ BackButton.view() ]
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
                    // Use overall watched episodes (unique across all sessions) for the total count
                    let watchedCount = model.OverallWatchedEpisodes.Count

                    // Check if backdrop is available for responsive layout
                    let hasBackdrop = series.BackdropPath.IsSome

                    // Grid classes: use backdrop layout (no poster column) on md-4xl when backdrop exists
                    let gridClasses =
                        if hasBackdrop
                        then "detail-grid-with-backdrop grid grid-cols-1 gap-8"
                        else "grid grid-cols-1 md:grid-cols-3 gap-8"

                    // Poster visibility: hide on md-4xl when backdrop exists
                    let posterClasses =
                        if hasBackdrop
                        then "detail-poster-column space-y-4 md:hidden"
                        else "space-y-4"

                    // Info column span: full width on md-4xl when backdrop, otherwise spans 2 cols
                    let infoColClasses =
                        if hasBackdrop
                        then "detail-info-column flex flex-col"
                        else "md:col-span-2 flex flex-col"

                    // Status badge helper
                    let allEpisodesWatched = watchedCount >= series.NumberOfEpisodes && series.NumberOfEpisodes > 0
                    let isAbandoned = match entry.WatchStatus with Abandoned _ -> true | _ -> false
                    let statusBadge =
                        if allEpisodesWatched then
                            Html.div [
                                prop.className "absolute bottom-0 left-0 right-0 bg-gradient-to-t from-emerald-900/95 via-emerald-900/80 to-transparent pt-8 pb-3 px-2"
                                prop.children [
                                    Html.div [
                                        prop.className "flex items-center justify-center gap-1.5"
                                        prop.children [
                                            Html.span [
                                                prop.className "w-4 h-4 text-emerald-300"
                                                prop.children [ check ]
                                            ]
                                            Html.span [
                                                prop.className "text-sm font-semibold text-emerald-200 uppercase tracking-wider"
                                                prop.text "Finished"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        elif isAbandoned then
                            Html.div [
                                prop.className "absolute bottom-0 left-0 right-0 bg-gradient-to-t from-red-900/95 via-red-900/80 to-transparent pt-8 pb-3 px-2"
                                prop.children [
                                    Html.div [
                                        prop.className "flex items-center justify-center gap-1.5"
                                        prop.children [
                                            Html.span [
                                                prop.className "w-4 h-4 text-red-300"
                                                prop.children [ ban ]
                                            ]
                                            Html.span [
                                                prop.className "text-sm font-semibold text-red-200 uppercase tracking-wider"
                                                prop.text "Abandoned"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        else
                            Html.none

                    // Series detail content - negative top margin on mobile to touch viewport
                    Html.div [
                        prop.className "detail-page-content space-y-6 -mt-4 md:mt-0"
                        prop.children [
                            // Backdrop hero (visible on md-4xl only, when backdrop exists)
                            if hasBackdrop then
                                Html.div [
                                    prop.className "detail-backdrop-section hidden md:block 4xl:hidden mb-6"
                                    prop.children [
                                        Html.div [
                                            prop.className "detail-backdrop-container"
                                            prop.children [
                                                Html.img [
                                                    prop.src (getLocalBackdropUrl series.BackdropPath)
                                                    prop.alt series.Name
                                                    prop.className "detail-backdrop-image"
                                                ]
                                                Html.div [ prop.className "detail-backdrop-overlay" ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Header: Poster + Title/Meta + Actions
                            Html.div [
                                prop.className gridClasses
                                prop.children [
                                    // Left column - Poster + Progress (hidden on md-4xl when backdrop exists)
                                    Html.div [
                                        prop.className posterClasses
                                        prop.children [
                                            // Poster wrapper - full bleed on mobile (horizontal only, top handled by content container)
                                            Html.div [
                                                prop.className "detail-poster-mobile -mx-4 md:mx-0"
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
                                                            statusBadge
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    // Right column - Title, meta, actions, and tabs (on md+)
                                    Html.div [
                                        prop.className infoColClasses
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
                                                    let episodePercentage = if series.NumberOfEpisodes > 0 then int (float watchedCount / float series.NumberOfEpisodes * 100.0) else 0
                                                    Html.span [ prop.text $"{watchedCount}/{series.NumberOfEpisodes} Episodes ({episodePercentage}%%)" ]
                                                ]
                                            ]
                                            // Action buttons row
                                            actionButtonsRow entry model.IsRatingOpen allEpisodesWatched model.Collections dispatch

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
                                                         | Episodes -> episodesTab series model friends dispatch)
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
                                         | Episodes -> episodesTab series model friends dispatch)
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
