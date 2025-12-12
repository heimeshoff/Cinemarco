module Pages.MovieDetail.View

open Feliz
open Browser.Dom
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View
open Components.FriendSelector.View

module GlassPanel = Common.Components.GlassPanel.View
module Tabs = Common.Components.Tabs.View

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
                            if value > 0 then // Skip "Unrated" in options, show "Clear" instead
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

/// Action buttons row below the title (glassmorphism square buttons with tooltips)
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (entryCollections: RemoteData<Collection list>) (watchSessions: RemoteData<MovieWatchSession list>) (dispatch: Msg -> unit) =
    // Determine if any watch sessions exist
    let hasWatchSessions =
        match watchSessions with
        | Success sessions -> not (List.isEmpty sessions)
        | _ -> false

    Html.div [
        prop.className "flex flex-wrap items-center gap-3 mt-4"
        prop.children [
            // Watch status button - visual state based on watch sessions, always adds a new session
            if hasWatchSessions then
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Add Watch Session")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success-active"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ checkCircleSolid ] ]
                            ]
                        ]
                    ]
                ]
            else
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Mark as Watched")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
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

/// Overview tab content: overview, collections, notes
let private overviewTab (movie: Movie) (entry: LibraryEntry) (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Overview
            match movie.Overview with
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

/// Render a single cast member button
let private renderCastMember (castMember: TmdbCastMember) (isTracked: bool) (dispatch: Msg -> unit) =
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

/// Render a single crew member button
let private renderCrewMember (crewMember: TmdbCrewMember) (dispatch: Msg -> unit) =
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

/// Cast & Crew tab content
let private castCrewTab (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            match model.Credits with
            | Success credits ->
                // Sort cast by TMDB billing order (Order field, lower = top-billed)
                let sortedByBilling = credits.Cast |> List.sortBy (fun c -> c.Order)

                // Top billed cast: first 10 by billing order
                let topBilledCount = 10
                let topBilledCast = sortedByBilling |> List.truncate topBilledCount
                let remainingCast = sortedByBilling |> List.skip (min topBilledCount (List.length sortedByBilling))

                // Group crew by department for expanded view
                let crewByDepartment =
                    credits.Crew
                    |> List.distinctBy (fun c -> TmdbPersonId.value c.TmdbPersonId, c.Job)
                    |> List.groupBy (fun c -> c.Department)
                    |> List.sortBy (fun (dept, _) ->
                        // Sort departments by importance
                        match dept with
                        | "Directing" -> 0
                        | "Writing" -> 1
                        | "Production" -> 2
                        | "Camera" -> 3
                        | "Sound" -> 4
                        | "Editing" -> 5
                        | "Art" -> 6
                        | "Costume & Make-Up" -> 7
                        | "Visual Effects" -> 8
                        | _ -> 99)

                // Top Billed Cast section
                if not (List.isEmpty topBilledCast) then
                    Html.div [
                        prop.children [
                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Top Billed Cast" ]
                            Html.div [
                                prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                prop.children [
                                    for castMember in topBilledCast do
                                        let isTracked = model.TrackedPersonIds |> Set.contains castMember.TmdbPersonId
                                        renderCastMember castMember isTracked dispatch
                                ]
                            ]
                        ]
                    ]

                // Full Cast and Crew button
                let hasMoreContent = not (List.isEmpty remainingCast) || not (List.isEmpty credits.Crew)
                if hasMoreContent then
                    Html.div [
                        prop.className "mt-4"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost btn-sm gap-2 w-full justify-center border border-base-300 hover:border-primary/50"
                                prop.onClick (fun _ -> dispatch ToggleFullCastCrew)
                                prop.children [
                                    Html.span [ prop.text (if model.IsFullCastCrewExpanded then "Hide Full Cast & Crew" else "Full Cast & Crew") ]
                                    Html.span [
                                        prop.className "w-4 h-4 transition-transform"
                                        prop.style [ if model.IsFullCastCrewExpanded then style.transform (transform.rotate 180) ]
                                        prop.children [ chevronDown ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                // Expanded full cast and crew section
                if model.IsFullCastCrewExpanded then
                    Html.div [
                        prop.className "mt-6 space-y-6 animate-in fade-in slide-in-from-top-2 duration-200"
                        prop.children [
                            // Remaining cast (if any)
                            if not (List.isEmpty remainingCast) then
                                Html.div [
                                    prop.children [
                                        Html.h3 [ prop.className "font-semibold mb-3 text-base-content/70"; prop.text "Supporting Cast" ]
                                        Html.div [
                                            prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                            prop.children [
                                                for castMember in remainingCast do
                                                    let isTracked = model.TrackedPersonIds |> Set.contains castMember.TmdbPersonId
                                                    renderCastMember castMember isTracked dispatch
                                            ]
                                        ]
                                    ]
                                ]

                            // Crew grouped by department
                            for (department, crewMembers) in crewByDepartment do
                                Html.div [
                                    prop.key department
                                    prop.children [
                                        Html.h3 [ prop.className "font-semibold mb-3 text-base-content/70"; prop.text department ]
                                        Html.div [
                                            prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                            prop.children [
                                                for crewMember in crewMembers do
                                                    renderCrewMember crewMember dispatch
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

/// Friends tab content
let private friendsTab (sessions: RemoteData<MovieWatchSession list>) (allFriends: Friend list) (editingSession: (SessionId * System.DateTime) option) (dispatch: Msg -> unit) =
    let renderFriendPill (friend: Friend) =
        Html.div [
            prop.key (FriendId.value friend.Id |> string)
            prop.className "inline-flex flex-row items-center gap-2 px-3 py-1.5 rounded-full bg-base-200 border border-base-300 cursor-pointer hover:bg-base-300 transition-colors"
            prop.onClick (fun _ -> dispatch (ViewFriendDetail (friend.Id, friend.Name)))
            prop.children [
                match friend.AvatarUrl with
                | Some url ->
                    Html.div [
                        prop.className "w-6 h-6 rounded-full overflow-hidden flex-shrink-0"
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
                        prop.className "w-6 h-6 rounded-full bg-primary/30 flex items-center justify-center flex-shrink-0"
                        prop.children [
                            Html.span [
                                prop.className "text-xs font-medium"
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

    let renderMyselfPill () =
        Html.div [
            prop.className "inline-flex flex-row items-center gap-2 px-3 py-1.5 rounded-full bg-base-200 border border-base-300"
            prop.children [
                Html.div [
                    prop.className "w-6 h-6 rounded-full bg-primary/30 flex items-center justify-center flex-shrink-0"
                    prop.children [
                        Html.span [ prop.className "text-xs font-medium"; prop.text "M" ]
                    ]
                ]
                Html.span [
                    prop.className "text-sm font-medium whitespace-nowrap"
                    prop.text "Myself"
                ]
            ]
        ]

    let renderSession (session: MovieWatchSession) =
        let friends =
            session.Friends
            |> List.choose (fun fid -> allFriends |> List.tryFind (fun f -> f.Id = fid))

        let isEditing =
            match editingSession with
            | Some (editingId, _) -> editingId = session.Id
            | None -> false

        let editingDate =
            match editingSession with
            | Some (editingId, date) when editingId = session.Id -> Some date
            | _ -> None

        Html.div [
            prop.key (SessionId.value session.Id |> string)
            prop.className "flex flex-wrap items-center gap-2"
            prop.children [
                // Inline date display/editor
                if isEditing then
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.input [
                                prop.type' "date"
                                prop.className "input input-bordered input-xs w-auto"
                                prop.value ((editingDate |> Option.defaultValue session.WatchedDate).ToString("yyyy-MM-dd"))
                                prop.onChange (fun (value: string) ->
                                    match System.DateTime.TryParse(value) with
                                    | true, date -> dispatch (UpdateEditingDate date)
                                    | false, _ -> ()
                                )
                                prop.autoFocus true
                            ]
                            Html.button [
                                prop.className "btn btn-ghost btn-xs text-success"
                                prop.onClick (fun _ -> dispatch SaveSessionDate)
                                prop.title "Save"
                                prop.children [ Html.span [ prop.className "w-3 h-3"; prop.children [ check ] ] ]
                            ]
                            Html.button [
                                prop.className "btn btn-ghost btn-xs text-error"
                                prop.onClick (fun _ -> dispatch CancelEditingSessionDate)
                                prop.title "Cancel"
                                prop.children [ Html.span [ prop.className "w-3 h-3"; prop.children [ close ] ] ]
                            ]
                        ]
                    ]
                else
                    Html.span [
                        prop.className "text-sm font-medium text-base-content/70 cursor-pointer hover:text-primary hover:underline"
                        prop.title "Click to edit date"
                        prop.onClick (fun _ -> dispatch (StartEditingSessionDate (session.Id, session.WatchedDate)))
                        prop.text (session.WatchedDate.ToString("MMM d, yyyy"))
                    ]

                Html.span [
                    prop.className "text-base-content/40"
                    prop.text "·"
                ]

                // Show "Myself" if no friends, otherwise show friends
                if List.isEmpty friends then
                    renderMyselfPill ()
                else
                    for friend in friends do
                        renderFriendPill friend

                // Optional session name
                match session.Name with
                | Some name ->
                    Html.span [
                        prop.className "text-sm text-base-content/60 italic ml-2"
                        prop.text $"({name})"
                    ]
                | None -> ()

                // Action buttons
                Html.div [
                    prop.className "flex items-center gap-1 ml-auto"
                    prop.children [
                        // Edit button
                        Html.button [
                            prop.className "btn btn-ghost btn-xs opacity-50 hover:opacity-100"
                            prop.onClick (fun _ -> dispatch (EditWatchSession session))
                            prop.title "Edit session"
                            prop.children [
                                Html.span [ prop.className "w-3 h-3"; prop.children [ edit ] ]
                            ]
                        ]
                        // Delete button
                        Html.button [
                            prop.className "btn btn-ghost btn-xs opacity-50 hover:opacity-100"
                            prop.onClick (fun _ -> dispatch (DeleteWatchSession session.Id))
                            prop.title "Delete session"
                            prop.children [
                                Html.span [ prop.className "w-3 h-3"; prop.children [ trash ] ]
                            ]
                        ]
                    ]
                ]
            ]
        ]

    Html.div [
        prop.className "space-y-4"
        prop.children [
            // Header with add button
            Html.div [
                prop.className "flex items-center justify-between"
                prop.children [
                    Html.h3 [ prop.className "font-semibold"; prop.text "Watch Sessions" ]
                    Html.button [
                        prop.className "btn btn-sm btn-ghost gap-2"
                        prop.onClick (fun _ -> dispatch OpenWatchSessionModal)
                        prop.children [
                            Html.span [ prop.className "w-4 h-4"; prop.children [ plus ] ]
                            Html.span [ prop.text "Add Watch Session" ]
                        ]
                    ]
                ]
            ]

            // Sessions list
            match sessions with
            | NotAsked | Loading ->
                Html.div [
                    prop.className "flex items-center justify-center py-4"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                    ]
                ]
            | Failure err ->
                Html.p [
                    prop.className "text-error text-sm"
                    prop.text $"Failed to load watch sessions: {err}"
                ]
            | Success sessionList ->
                if List.isEmpty sessionList then
                    Html.p [
                        prop.className "text-base-content/60 text-sm"
                        prop.text "No watch sessions yet. Click 'Mark as watched' or add a watch session to track when you watched this movie."
                    ]
                else
                    Html.div [
                        prop.className "space-y-2"
                        prop.children [
                            // Sort sessions by date descending
                            for session in sessionList |> List.sortByDescending (fun s -> s.WatchedDate) do
                                renderSession session
                        ]
                    ]
        ]
    ]

/// Get the tab definitions
let private movieTabs : Common.Components.Tabs.Types.Tab list = [
    { Id = "overview"; Label = "Overview"; Icon = Some info }
    { Id = "cast-crew"; Label = "Cast & Crew"; Icon = Some friends }
    { Id = "watched"; Label = "Watched"; Icon = Some userPlus }
]

/// Map MovieTab to string ID
let private tabToId = function
    | Overview -> "overview"
    | CastCrew -> "cast-crew"
    | Friends -> "watched"

/// Map string ID to MovieTab
let private idToTab = function
    | "overview" -> Overview
    | "cast-crew" -> CastCrew
    | "watched" -> Friends
    | _ -> Overview

let view (model: Model) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button - uses browser history for proper navigation
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> window.history.back())
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
                | LibraryMovie movie ->
                    // Movie detail content
                    Html.div [
                        prop.className "space-y-6"
                        prop.children [
                            // Header: Poster + Title/Meta + Actions + Tabs (on md+)
                            Html.div [
                                prop.className "grid grid-cols-1 md:grid-cols-3 gap-8"
                                prop.children [
                                    // Left column - Poster
                                    Html.div [
                                        prop.className "relative"
                                        prop.children [
                                            Html.div [
                                                prop.className "poster-image-container poster-shadow poster-projector-glow"
                                                prop.children [
                                                    match movie.PosterPath with
                                                    | Some _ ->
                                                        Html.img [
                                                            prop.src (getLocalPosterUrl movie.PosterPath)
                                                            prop.alt movie.Title
                                                            prop.className "poster-image"
                                                            prop.custom ("crossorigin", "anonymous")
                                                        ]
                                                    | None ->
                                                        Html.div [
                                                            prop.className "w-full h-full flex items-center justify-center bg-base-200"
                                                            prop.children [
                                                                Html.span [ prop.className "text-6xl text-base-content/20"; prop.children [ film ] ]
                                                            ]
                                                        ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    // Right column - Title, meta, actions, and tabs (on md+)
                                    Html.div [
                                        prop.className "md:col-span-2 flex flex-col"
                                        prop.children [
                                            Html.h1 [
                                                prop.className "text-3xl font-bold"
                                                prop.text movie.Title
                                            ]
                                            Html.div [
                                                prop.className "flex items-center gap-2 mt-2 text-base-content/60 flex-wrap"
                                                prop.children [
                                                    // Genres
                                                    if not (List.isEmpty movie.Genres) then
                                                        Html.span [ prop.text (movie.Genres |> String.concat ", ") ]
                                                        Html.span [ prop.className "text-base-content/30"; prop.text "·" ]
                                                    // Runtime
                                                    match movie.RuntimeMinutes with
                                                    | Some r ->
                                                        Html.span [ prop.text $"{r} min" ]
                                                        match movie.ReleaseDate with
                                                        | Some _ -> Html.span [ prop.className "text-base-content/30"; prop.text "·" ]
                                                        | None -> Html.none
                                                    | None -> Html.none
                                                    // Year
                                                    match movie.ReleaseDate with
                                                    | Some d -> Html.span [ prop.text (d.Year.ToString()) ]
                                                    | None -> Html.none
                                                ]
                                            ]
                                            // Action buttons row
                                            actionButtonsRow entry model.IsRatingOpen model.Collections model.WatchSessions dispatch

                                            // Tab bar and content (hidden on mobile, shown on md+)
                                            Html.div [
                                                prop.className "hidden md:block mt-6"
                                                prop.children [
                                                    Tabs.view
                                                        movieTabs
                                                        (tabToId model.ActiveTab)
                                                        (fun id -> dispatch (SetActiveTab (idToTab id)))
                                                        (match model.ActiveTab with
                                                         | Overview -> overviewTab movie entry model dispatch
                                                         | CastCrew -> castCrewTab model dispatch
                                                         | Friends -> friendsTab model.WatchSessions friends model.EditingSessionDate dispatch)
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
                                        movieTabs
                                        (tabToId model.ActiveTab)
                                        (fun id -> dispatch (SetActiveTab (idToTab id)))
                                        (match model.ActiveTab with
                                         | Overview -> overviewTab movie entry model dispatch
                                         | CastCrew -> castCrewTab model dispatch
                                         | Friends -> friendsTab model.WatchSessions friends model.EditingSessionDate dispatch)
                                ]
                            ]
                        ]
                    ]
                | LibrarySeries _ ->
                    Html.div [
                        prop.className "text-center py-12"
                        prop.text "This is a series, not a movie"
                    ]

            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text err
                ]
            | NotAsked -> Html.none
        ]
    ]
