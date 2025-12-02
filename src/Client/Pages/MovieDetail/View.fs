module Pages.MovieDetail.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View
open Components.FriendSelector.View

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
                | LibraryMovie movie ->
                    // Movie detail content
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

                            // Right column - Details
                            Html.div [
                                prop.className "md:col-span-2 space-y-6"
                                prop.children [
                                    // Title, meta, and actions
                                    Html.div [
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
                                    ]

                                    // Overview
                                    match movie.Overview with
                                    | Some overview when overview <> "" ->
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Overview" ]
                                            Html.p [ prop.className "text-base-content/70"; prop.text overview ]
                                        ]
                                    | _ -> Html.none

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

                                    // Tags
                                    if not (List.isEmpty tags) then
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Tags" ]
                                            Html.div [
                                                prop.className "flex flex-wrap gap-2"
                                                prop.children [
                                                    for tag in tags do
                                                        let isSelected = List.contains tag.Id entry.Tags
                                                        Html.button [
                                                            prop.type' "button"
                                                            prop.className (
                                                                "px-3 py-1 rounded-full text-sm transition-all " +
                                                                if isSelected then "bg-secondary text-secondary-content"
                                                                else "bg-base-200 text-base-content/60 hover:bg-base-300"
                                                            )
                                                            prop.onClick (fun _ -> dispatch (ToggleTag tag.Id))
                                                            prop.text tag.Name
                                                        ]
                                                ]
                                            ]
                                        ]

                                    // Friends - using FriendSelector component
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Watched With" ]
                                        FriendSelector {
                                            AllFriends = friends
                                            SelectedFriends = entry.Friends
                                            OnToggle = fun friendId -> dispatch (ToggleFriend friendId)
                                            OnAddNew = fun name -> dispatch (AddNewFriend name)
                                            OnSubmit = None
                                            IsDisabled = model.IsAddingFriend
                                            Placeholder = "Search or add friends..."
                                            IsRequired = false
                                            AutoFocus = false
                                        }
                                        if model.IsAddingFriend then
                                            Html.div [
                                                prop.className "flex items-center gap-2 mt-2 text-sm text-base-content/60"
                                                prop.children [
                                                    Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                                    Html.span [ prop.text "Adding friend..." ]
                                                ]
                                            ]
                                    ]

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
