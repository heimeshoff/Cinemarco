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
module RatingButton = Common.Components.RatingButton.View
module CastCrewSection = Common.Components.CastCrewSection.View
module BackButton = Common.Components.BackButton.View

/// Action buttons row below the title (glassmorphism square buttons with tooltips)
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (entryCollections: RemoteData<Collection list>) (dispatch: Msg -> unit) =
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

/// Cast & Crew tab content
let private castCrewTab (model: Model) (dispatch: Msg -> unit) =
    CastCrewSection.viewWithLoading
        model.Credits
        model.TrackedPersonIds
        model.IsFullCastCrewExpanded
        (fun (id, name) -> dispatch (ViewContributor (id, name)))
        (fun () -> dispatch ToggleFullCastCrew)

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
                | LibraryMovie movie ->
                    // Check if backdrop is available for responsive layout
                    let hasBackdrop = movie.BackdropPath.IsSome

                    // Grid classes: use backdrop layout (no poster column) on md-4xl when backdrop exists
                    let gridClasses =
                        if hasBackdrop
                        then "detail-grid-with-backdrop grid grid-cols-1 gap-8"
                        else "grid grid-cols-1 md:grid-cols-3 gap-8"

                    // Poster visibility: hide on md-4xl when backdrop exists
                    let posterClasses =
                        if hasBackdrop
                        then "detail-poster-column relative md:hidden"
                        else "relative"

                    // Info column span: full width on md-4xl when backdrop, otherwise spans 2 cols
                    let infoColClasses =
                        if hasBackdrop
                        then "detail-info-column flex flex-col"
                        else "md:col-span-2 flex flex-col"

                    // Movie detail content - negative top margin on mobile to touch viewport
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
                                                    prop.src (getLocalBackdropUrl movie.BackdropPath)
                                                    prop.alt movie.Title
                                                    prop.className "detail-backdrop-image"
                                                ]
                                                Html.div [ prop.className "detail-backdrop-overlay" ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Header: Poster + Title/Meta + Actions + Tabs (on md+)
                            Html.div [
                                prop.className gridClasses
                                prop.children [
                                    // Left column - Poster (hidden on md-4xl when backdrop exists)
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
                                        ]
                                    ]

                                    // Right column - Title, meta, actions, and tabs (on md+)
                                    Html.div [
                                        prop.className infoColClasses
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
                                            actionButtonsRow entry model.IsRatingOpen model.Collections dispatch

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
