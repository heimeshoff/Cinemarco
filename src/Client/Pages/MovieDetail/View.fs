module Pages.MovieDetail.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View
open Components.FriendSelector.View

/// Glassmorphism action buttons overlayed on the poster (movies only - no abandon)
let private posterActionButtons (entry: LibraryEntry) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "poster-actions-container"
        prop.children [
            match entry.WatchStatus with
            | NotStarted ->
                // Mark as Watched button (outlined check for unwatched)
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Mark as Watched")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
                // Delete button
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Delete Entry")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-danger"
                            prop.onClick (fun _ -> dispatch OpenDeleteModal)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ trash ] ]
                            ]
                        ]
                    ]
                ]
            | InProgress _ ->
                // Mark as Completed button (outlined check for not yet completed)
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Mark as Completed")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
                // Delete button
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Delete Entry")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-danger"
                            prop.onClick (fun _ -> dispatch OpenDeleteModal)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ trash ] ]
                            ]
                        ]
                    ]
                ]
            | Completed ->
                // Watched indicator (filled check for watched)
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Watched")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkUnwatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5 text-green-400"; prop.children [ checkCircleSolid ] ]
                            ]
                        ]
                    ]
                ]
                // Delete button
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Delete Entry")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-danger"
                            prop.onClick (fun _ -> dispatch OpenDeleteModal)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ trash ] ]
                            ]
                        ]
                    ]
                ]
            | Abandoned _ ->
                // This state shouldn't occur for movies, but handle gracefully
                // Mark as Completed button
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Mark as Completed")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-success"
                            prop.onClick (fun _ -> dispatch MarkWatched)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ success ] ]
                            ]
                        ]
                    ]
                ]
                // Delete button
                Html.div [
                    prop.className "tooltip tooltip-left"
                    prop.custom ("data-tip", "Delete Entry")
                    prop.children [
                        Html.button [
                            prop.className "poster-action-btn poster-action-btn-danger"
                            prop.onClick (fun _ -> dispatch OpenDeleteModal)
                            prop.children [
                                Html.span [ prop.className "w-5 h-5"; prop.children [ trash ] ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

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
                            // Left column - Poster with action buttons
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
                                            // Glassmorphism action buttons on poster
                                            posterActionButtons entry dispatch
                                        ]
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
                                            prop.text movie.Title
                                        ]
                                        Html.div [
                                            prop.className "flex items-center gap-4 mt-2 text-base-content/60"
                                            prop.children [
                                                match movie.ReleaseDate with
                                                | Some d -> Html.span [ prop.text (d.Year.ToString()) ]
                                                | None -> Html.none
                                                match movie.RuntimeMinutes with
                                                | Some r -> Html.span [ prop.text $"{r} min" ]
                                                | None -> Html.none
                                            ]
                                        ]
                                    ]

                                    // Overview
                                    match movie.Overview with
                                    | Some overview when overview <> "" ->
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Overview" ]
                                            Html.p [ prop.className "text-base-content/70"; prop.text overview ]
                                        ]
                                    | _ -> Html.none

                                    // Rating
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Your Rating" ]
                                        ratingSelector (entry.PersonalRating |> Option.map PersonalRating.toInt) dispatch
                                    ]

                                    // Favorite toggle and Add to Collection
                                    Html.div [
                                        prop.className "flex items-center gap-2 flex-wrap"
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
                                            Html.button [
                                                prop.className "btn btn-sm btn-ghost"
                                                prop.onClick (fun _ -> dispatch OpenAddToCollectionModal)
                                                prop.children [
                                                    Html.span [ prop.className "w-4 h-4"; prop.children [ collections ] ]
                                                    Html.span [ prop.text "Add to Collection" ]
                                                ]
                                            ]
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
