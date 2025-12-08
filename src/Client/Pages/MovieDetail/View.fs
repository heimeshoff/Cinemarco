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
let private actionButtonsRow (entry: LibraryEntry) (isRatingOpen: bool) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex items-center gap-3 mt-4"
        prop.children [
            // Watch status button
            match entry.WatchStatus with
            | NotStarted ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Mark as Watched")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
            | InProgress _ ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Mark as Completed")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
            | Completed ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Mark as Unwatched")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success-active"
                            prop.onClick (fun _ -> dispatch MarkUnwatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ checkCircleSolid ] ]
                            ]
                        ]
                    ]
                ]
            | Abandoned _ ->
                Html.div [
                    prop.className "tooltip tooltip-bottom detail-tooltip"
                    prop.custom ("data-tip", "Mark as Completed")
                    prop.children [
                        Html.button [
                            prop.className "detail-action-btn detail-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
            // Rating button
            ratingButton (entry.PersonalRating |> Option.map PersonalRating.toInt) isRatingOpen dispatch
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

            // Add to Collection button
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
                                    prop.className "badge badge-outline"
                                    prop.text collection.Name
                                ]
                        ]
                    ]
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
                                prop.className "flex flex-wrap gap-2"
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
                                                // Profile image with tracked indicator
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
                                                        // Tracked indicator badge
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

                // Crew section (directors, writers, etc.)
                let keyCrewRoles = ["Director"; "Screenplay"; "Writer"; "Director of Photography"; "Original Music Composer"]
                let keyCrew = credits.Crew |> List.filter (fun c -> List.contains c.Job keyCrewRoles)
                if not (List.isEmpty keyCrew) then
                    Html.div [
                        prop.className "mt-4"
                        prop.children [
                            Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Crew" ]
                            Html.div [
                                prop.className "flex flex-wrap gap-2"
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
                // Friend selector input (when open)
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

            // Friend pills
            if not (List.isEmpty selectedFriendsList) then
                Html.div [
                    prop.className "flex flex-wrap items-center gap-2 mt-4"
                    prop.children [
                        for friend in selectedFriendsList do
                            Html.span [
                                prop.key (FriendId.value friend.Id |> string)
                                prop.className "friend-pill"
                                prop.text friend.Name
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
let private movieTabs : Common.Components.Tabs.Types.Tab list = [
    { Id = "overview"; Label = "Overview"; Icon = Some info }
    { Id = "cast-crew"; Label = "Cast & Crew"; Icon = Some friends }
    { Id = "friends"; Label = "Friends"; Icon = Some userPlus }
]

/// Map MovieTab to string ID
let private tabToId = function
    | Overview -> "overview"
    | CastCrew -> "cast-crew"
    | Friends -> "friends"

/// Map string ID to MovieTab
let private idToTab = function
    | "overview" -> Overview
    | "cast-crew" -> CastCrew
    | "friends" -> Friends
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
                                            actionButtonsRow entry model.IsRatingOpen dispatch

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
                                        movieTabs
                                        (tabToId model.ActiveTab)
                                        (fun id -> dispatch (SetActiveTab (idToTab id)))
                                        (match model.ActiveTab with
                                         | Overview -> overviewTab movie entry model dispatch
                                         | CastCrew -> castCrewTab model dispatch
                                         | Friends -> friendsTab entry friends model.IsFriendSelectorOpen model.IsAddingFriend dispatch)
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
