module Pages.CollectionDetail.View

open Feliz
open Browser.Types
open Common.Types
open Shared.Domain
open Types

let private progressBar (progress: CollectionProgress) =
    Html.div [
        prop.className "bg-base-200 rounded-lg p-4 mb-6"
        prop.children [
            Html.div [
                prop.className "flex justify-between items-center mb-2"
                prop.children [
                    Html.span [
                        prop.className "text-sm font-medium"
                        prop.text $"{progress.CompletedItems} of {progress.TotalItems} completed"
                    ]
                    Html.span [
                        prop.className "text-sm text-base-content/60"
                        prop.text $"{progress.CompletionPercentage:F0}%%"
                    ]
                ]
            ]
            Html.progress [
                prop.className "progress progress-primary w-full"
                prop.value (int progress.CompletionPercentage)
                prop.max 100
            ]
            if progress.InProgressItems > 0 then
                Html.p [
                    prop.className "text-xs text-base-content/50 mt-1"
                    prop.text $"{progress.InProgressItems} in progress"
                ]
        ]
    ]

let private watchStatusBadge (status: WatchStatus) =
    match status with
    | NotStarted ->
        Html.span [
            prop.className "badge badge-ghost badge-sm"
            prop.text "Not started"
        ]
    | InProgress _ ->
        Html.span [
            prop.className "badge badge-info badge-sm"
            prop.text "In progress"
        ]
    | Completed ->
        Html.span [
            prop.className "badge badge-success badge-sm"
            prop.text "Completed"
        ]
    | Abandoned _ ->
        Html.span [
            prop.className "badge badge-warning badge-sm"
            prop.text "Abandoned"
        ]

let private collectionItemView (model: Model) (dispatch: Msg -> unit) (position: int) (item: CollectionItem, entry: LibraryEntry) =
    let isDragging = model.DraggingItem = Some item.EntryId
    let isDragOver = model.DragOverItem = Some item.EntryId

    Html.div [
        prop.key (EntryId.value item.EntryId |> string)
        prop.className [
            "flex items-center gap-4 p-3 bg-base-200 rounded-lg transition-all"
            if isDragging then "opacity-50 scale-95"
            elif isDragOver then "ring-2 ring-primary"
            else "hover:bg-base-300"
        ]
        prop.draggable true
        prop.onDragStart (fun e ->
            e.dataTransfer.effectAllowed <- "move"
            dispatch (StartDrag item.EntryId)
        )
        prop.onDragOver (fun e ->
            e.preventDefault()
            dispatch (DragOver item.EntryId)
        )
        prop.onDragLeave (fun _ -> dispatch DragEnd)
        prop.onDrop (fun e ->
            e.preventDefault()
            dispatch (Drop item.EntryId)
        )
        prop.onDragEnd (fun _ -> dispatch DragEnd)
        prop.children [
            // Drag handle
            Html.div [
                prop.className "cursor-grab text-base-content/40 hover:text-base-content"
                prop.children [
                    Html.span [ prop.text "⋮⋮" ]
                ]
            ]

            // Position number
            Html.div [
                prop.className "w-8 h-8 rounded-full bg-base-300 flex items-center justify-center text-sm font-bold"
                prop.text (string (position + 1))
            ]

            // Extract title, poster, year from Media
            let title, posterPath, yearText =
                match entry.Media with
                | LibraryMovie m ->
                    let y = m.ReleaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
                    (m.Title, m.PosterPath, y)
                | LibrarySeries s ->
                    let y = s.FirstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
                    (s.Name, s.PosterPath, y)

            // Poster thumbnail
            match posterPath with
            | Some path ->
                Html.img [
                    prop.src $"/images/posters{path}"
                    prop.className "w-12 h-16 object-cover rounded"
                    prop.alt title
                ]
            | None ->
                Html.div [
                    prop.className "w-12 h-16 bg-base-300 rounded flex items-center justify-center text-xl"
                    prop.text (match entry.Media with LibraryMovie _ -> "🎬" | LibrarySeries _ -> "📺")
                ]

            // Title and info
            Html.div [
                prop.className "flex-1 min-w-0 cursor-pointer"
                prop.onClick (fun _ ->
                    match entry.Media with
                    | LibraryMovie _ -> dispatch (ViewMovieDetail entry.Id)
                    | LibrarySeries _ -> dispatch (ViewSeriesDetail entry.Id)
                )
                prop.children [
                    Html.p [
                        prop.className "font-medium truncate"
                        prop.text title
                    ]
                    Html.div [
                        prop.className "flex items-center gap-2 text-sm text-base-content/60"
                        prop.children [
                            Html.span [
                                prop.text yearText
                            ]
                            watchStatusBadge entry.WatchStatus
                        ]
                    ]
                ]
            ]

            // Notes (if any)
            match item.Notes with
            | Some notes ->
                Html.div [
                    prop.className "hidden md:block text-sm text-base-content/50 max-w-xs truncate"
                    prop.title notes
                    prop.text notes
                ]
            | None -> Html.none

            // Remove button
            Html.button [
                prop.className "btn btn-ghost btn-sm btn-circle text-error"
                prop.onClick (fun e ->
                    e.stopPropagation()
                    dispatch (RemoveItem item.EntryId)
                )
                prop.title "Remove from collection"
                prop.text "✕"
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-4"
        prop.children [
            // Back button
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.button [
                        prop.className "btn btn-ghost btn-sm gap-1"
                        prop.onClick (fun _ -> dispatch GoBack)
                        prop.children [
                            Html.span [ prop.text "←" ]
                            Html.span [ prop.text "Back to Collections" ]
                        ]
                    ]
                ]
            ]

            match model.Collection with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]

            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading collection: {err}"
                ]

            | Success cwi ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Header
                        Html.div [
                            prop.className "flex items-start gap-4"
                            prop.children [
                                // Collection cover/icon
                                match cwi.Collection.CoverImagePath with
                                | Some path ->
                                    Html.img [
                                        prop.src path
                                        prop.className "w-24 h-32 object-cover rounded-lg"
                                        prop.alt cwi.Collection.Name
                                    ]
                                | None ->
                                    Html.div [
                                        prop.className "w-24 h-32 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-lg flex items-center justify-center text-4xl"
                                        prop.text (if cwi.Collection.IsPublicFranchise then "🎬" else "📚")
                                    ]

                                Html.div [
                                    prop.className "flex-1"
                                    prop.children [
                                        Html.div [
                                            prop.className "flex items-center gap-2"
                                            prop.children [
                                                Html.h1 [
                                                    prop.className "text-2xl font-bold"
                                                    prop.text cwi.Collection.Name
                                                ]
                                                if cwi.Collection.IsPublicFranchise then
                                                    Html.span [
                                                        prop.className "badge badge-secondary"
                                                        prop.text "Franchise"
                                                    ]
                                            ]
                                        ]
                                        match cwi.Collection.Description with
                                        | Some desc ->
                                            Html.p [
                                                prop.className "text-base-content/70 mt-2"
                                                prop.text desc
                                            ]
                                        | None -> Html.none
                                    ]
                                ]
                            ]
                        ]

                        // Progress bar
                        match model.Progress with
                        | Success progress -> progressBar progress
                        | _ -> Html.none

                        // Items list
                        if List.isEmpty cwi.Items then
                            Html.div [
                                prop.className "text-center py-12 text-base-content/60"
                                prop.children [
                                    Html.p [ prop.className "text-lg mb-2"; prop.text "No items in this collection" ]
                                    Html.p [ prop.text "Add items from your library!" ]
                                ]
                            ]
                        else
                            Html.div [
                                prop.className "space-y-2"
                                prop.children [
                                    Html.div [
                                        prop.className "flex justify-between items-center mb-4"
                                        prop.children [
                                            Html.h3 [
                                                prop.className "text-lg font-semibold"
                                                prop.text $"{List.length cwi.Items} Items"
                                            ]
                                            Html.p [
                                                prop.className "text-sm text-base-content/50"
                                                prop.text "Drag to reorder"
                                            ]
                                        ]
                                    ]
                                    for (i, item) in List.indexed cwi.Items do
                                        collectionItemView model dispatch i item
                                ]
                            ]
                    ]
                ]

            | NotAsked -> Html.none
        ]
    ]
