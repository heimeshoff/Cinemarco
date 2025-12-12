module Pages.CollectionDetail.View

open Feliz
open Browser.Dom
open Browser.Types
open Common.Types
open Shared.Domain
open Types
open Components.Icons

let private handleFileSelect (dispatch: Msg -> unit) (e: Event) =
    let input = e.target :?> HTMLInputElement
    if input.files.length > 0 then
        let file = input.files.[0]
        let reader = FileReader.Create()
        reader.onload <- fun _ ->
            let result = reader.result :?> string
            dispatch (LogoSelected result)
        reader.readAsDataURL(file)

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
                        prop.text $"{int progress.CompletionPercentage}%%"
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

/// Generate a unique key for a collection item
let private itemKey (item: CollectionItem) =
    match item.ItemRef with
    | LibraryEntryRef (EntryId id) -> $"entry-{id}"
    | SeasonRef (SeriesId sid, sn) -> $"season-{sid}-{sn}"
    | EpisodeRef (SeriesId sid, sn, en) -> $"episode-{sid}-{sn}-{en}"

let private collectionItemView (model: Model) (dispatch: Msg -> unit) (position: int) (item: CollectionItem, display: CollectionItemDisplay) =
    let isDragging = model.DraggingItem = Some item.ItemRef
    let showDropBefore = model.DropTarget = Some (Before item.ItemRef)
    let showDropAfter = model.DropTarget = Some (After item.ItemRef)

    // Extract title, poster, year, and click handler from display type
    let title, posterPath, subtitle, icon, onClick =
        match display with
        | EntryDisplay entry ->
            match entry.Media with
            | LibraryMovie m ->
                let year = m.ReleaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
                (m.Title, m.PosterPath, year, "🎬", fun () -> dispatch (ViewMovieDetail (entry.Id, m.Title, m.ReleaseDate)))
            | LibrarySeries s ->
                let year = s.FirstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
                (s.Name, s.PosterPath, year, "📺", fun () -> dispatch (ViewSeriesDetail (entry.Id, s.Name, s.FirstAirDate)))
        | SeasonDisplay (series, season) ->
            let seasonName = season.Name |> Option.defaultValue $"Season {season.SeasonNumber}"
            (seasonName, series.PosterPath, series.Name, "📀", fun () -> dispatch (ViewSeasonDetail series.Name))
        | EpisodeDisplay (series, season, episode) ->
            let epTitle = $"S{season.SeasonNumber}E{episode.EpisodeNumber}: {episode.Name}"
            (epTitle, series.PosterPath, series.Name, "▶", fun () -> dispatch (ViewEpisodeDetail series.Name))

    // Watch status badge only for library entries
    let statusBadge =
        match display with
        | EntryDisplay entry -> watchStatusBadge entry.WatchStatus
        | SeasonDisplay _ -> Html.span [ prop.className "badge badge-ghost badge-sm"; prop.text "Season" ]
        | EpisodeDisplay _ -> Html.span [ prop.className "badge badge-ghost badge-sm"; prop.text "Episode" ]

    let dropIndicatorBefore =
        if showDropBefore then
            Html.div [
                prop.className "absolute -top-1 left-0 right-0 h-0.5 bg-white z-20"
            ]
        else Html.none

    let dropIndicatorAfter =
        if showDropAfter then
            Html.div [
                prop.className "absolute -bottom-1 left-0 right-0 h-0.5 bg-white z-20"
            ]
        else Html.none

    Html.div [
        prop.key (itemKey item)
        prop.className "relative py-1"
        prop.children [
            dropIndicatorBefore
            dropIndicatorAfter

            Html.div [
                prop.className [
                    "flex items-center gap-4 p-3 bg-base-200 rounded-lg"
                    if isDragging then "opacity-50"
                    else "hover:bg-base-300"
                ]
                prop.draggable true
                prop.onDragStart (fun e ->
                    e.dataTransfer.effectAllowed <- "move"
                    dispatch (StartDrag item.ItemRef)
                )
                prop.onDragOver (fun e ->
                    e.preventDefault()
                    e.stopPropagation()
                    // Determine if we're in the top or bottom half of the item
                    let rect = (e.currentTarget :?> Browser.Types.HTMLElement).getBoundingClientRect()
                    let midY = rect.top + rect.height / 2.0
                    let dropPos =
                        if e.clientY < midY then Before item.ItemRef
                        else After item.ItemRef
                    dispatch (DragOver dropPos)
                )
                prop.onDrop (fun e ->
                    e.preventDefault()
                    dispatch Drop
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
                            prop.text icon
                        ]

                    // Title and info
                    Html.div [
                        prop.className "flex-1 min-w-0 cursor-pointer"
                        prop.onClick (fun _ -> onClick ())
                        prop.children [
                            Html.p [
                                prop.className "font-medium truncate"
                                prop.text title
                            ]
                            Html.div [
                                prop.className "flex items-center gap-2 text-sm text-base-content/60"
                                prop.children [
                                    Html.span [
                                        prop.text subtitle
                                    ]
                                    statusBadge
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
                            dispatch (RemoveItem item.ItemRef)
                        )
                        prop.title "Remove from collection"
                        prop.text "✕"
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-4"
        prop.children [
            // Back button - uses browser history for proper navigation
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.button [
                        prop.className "btn btn-ghost btn-sm gap-1"
                        prop.onClick (fun _ -> window.history.back())
                        prop.children [
                            Html.span [ prop.text "←" ]
                            Html.span [ prop.text "Back" ]
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
                        // Header with logo, name, and actions
                        Html.div [
                            prop.className "flex items-start gap-4"
                            prop.children [
                                // Collection logo - clickable to upload new logo
                                Html.label [
                                    prop.className "relative cursor-pointer group"
                                    prop.children [
                                        match cwi.Collection.CoverImagePath with
                                        | Some path ->
                                            Html.img [
                                                prop.src $"/images/collections{path}"
                                                prop.className "w-24 h-24 object-cover rounded-lg transition-opacity group-hover:opacity-75"
                                                prop.alt cwi.Collection.Name
                                            ]
                                        | None ->
                                            Html.div [
                                                prop.className "w-24 h-24 bg-gradient-to-br from-primary/20 to-secondary/20 rounded-lg flex items-center justify-center transition-opacity group-hover:opacity-75"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "text-3xl opacity-50"
                                                        prop.text "📁"
                                                    ]
                                                ]
                                            ]
                                        // Upload overlay
                                        Html.div [
                                            prop.className "absolute inset-0 rounded-lg flex items-center justify-center opacity-0 group-hover:opacity-100 bg-black/50 transition-opacity"
                                            prop.children [
                                                if model.UploadingLogo then
                                                    Html.span [ prop.className "loading loading-spinner loading-sm text-white" ]
                                                else
                                                    Html.span [
                                                        prop.className "text-white text-xs font-medium"
                                                        prop.text "Change"
                                                    ]
                                            ]
                                        ]
                                        // Hidden file input
                                        Html.input [
                                            prop.type'.file
                                            prop.accept "image/png,image/jpeg,image/gif,image/webp"
                                            prop.className "hidden"
                                            prop.onChange (handleFileSelect dispatch)
                                            prop.disabled model.UploadingLogo
                                        ]
                                    ]
                                ]

                                Html.div [
                                    prop.className "flex-1"
                                    prop.children [
                                        // Editable name
                                        if model.EditingName then
                                            Html.div [
                                                prop.className "flex items-center gap-2"
                                                prop.children [
                                                    Html.input [
                                                        prop.className "input input-bordered text-2xl font-bold h-auto py-1 px-2"
                                                        prop.ref (fun el -> if not (isNull el) then (el :?> HTMLElement).focus())
                                                        prop.value model.NameText
                                                        prop.onChange (fun (v: string) -> dispatch (NameChanged v))
                                                        prop.disabled model.SavingName
                                                        prop.onKeyDown (fun e ->
                                                            if e.key = "Enter" then dispatch SaveName
                                                            elif e.key = "Escape" then dispatch CancelEditName
                                                        )
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-sm btn-primary"
                                                        prop.onClick (fun _ -> dispatch SaveName)
                                                        prop.disabled model.SavingName
                                                        prop.children [
                                                            if model.SavingName then
                                                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                                            else
                                                                Html.span [ prop.text "Save" ]
                                                        ]
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-sm btn-ghost"
                                                        prop.onClick (fun _ -> dispatch CancelEditName)
                                                        prop.disabled model.SavingName
                                                        prop.text "Cancel"
                                                    ]
                                                    if cwi.Collection.IsPublicFranchise then
                                                        Html.span [
                                                            prop.className "badge badge-secondary"
                                                            prop.text "Franchise"
                                                        ]
                                                ]
                                            ]
                                        else
                                            Html.div [
                                                prop.className "flex items-center gap-2"
                                                prop.children [
                                                    Html.h1 [
                                                        prop.className "text-2xl font-bold cursor-pointer hover:text-primary transition-colors"
                                                        prop.title "Click to rename"
                                                        prop.onClick (fun _ -> dispatch StartEditName)
                                                        prop.text cwi.Collection.Name
                                                    ]
                                                    if cwi.Collection.IsPublicFranchise then
                                                        Html.span [
                                                            prop.className "badge badge-secondary"
                                                            prop.text "Franchise"
                                                        ]
                                                ]
                                            ]
                                        // Editable note below name
                                        if model.EditingNote then
                                            Html.div [
                                                prop.className "mt-2"
                                                prop.children [
                                                    Html.textarea [
                                                        prop.className "textarea textarea-bordered w-full text-sm resize-none"
                                                        prop.placeholder "Add a note about this collection..."
                                                        prop.ref (fun el -> if not (isNull el) then (el :?> HTMLElement).focus())
                                                        prop.value model.NoteText
                                                        prop.onChange (fun (v: string) -> dispatch (NoteChanged v))
                                                        prop.disabled model.SavingNote
                                                        prop.onKeyDown (fun e ->
                                                            if e.ctrlKey && e.key = "Enter" then dispatch SaveNote
                                                            elif e.key = "Escape" then dispatch CancelEditNote
                                                        )
                                                        prop.style [
                                                            style.custom ("fieldSizing", "content")
                                                            style.minHeight (length.rem 3)
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex gap-2 mt-2"
                                                        prop.children [
                                                            Html.button [
                                                                prop.className "btn btn-sm btn-primary"
                                                                prop.onClick (fun _ -> dispatch SaveNote)
                                                                prop.disabled model.SavingNote
                                                                prop.children [
                                                                    if model.SavingNote then
                                                                        Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                                                    else
                                                                        Html.span [ prop.text "Save" ]
                                                                ]
                                                            ]
                                                            Html.button [
                                                                prop.className "btn btn-sm btn-ghost"
                                                                prop.onClick (fun _ -> dispatch CancelEditNote)
                                                                prop.disabled model.SavingNote
                                                                prop.text "Cancel"
                                                            ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        else
                                            match cwi.Collection.Description with
                                            | Some desc when not (System.String.IsNullOrWhiteSpace desc) ->
                                                Html.div [
                                                    prop.className "text-base-content/70 mt-2 text-sm cursor-pointer hover:text-base-content whitespace-pre-wrap"
                                                    prop.title "Click to edit"
                                                    prop.onClick (fun _ -> dispatch StartEditNote)
                                                    prop.text desc
                                                ]
                                            | _ ->
                                                Html.button [
                                                    prop.className "btn btn-ghost btn-xs mt-2 text-base-content/50"
                                                    prop.onClick (fun _ -> dispatch StartEditNote)
                                                    prop.text "+ Add note"
                                                ]
                                    ]
                                ]

                                // Action buttons
                                Html.div [
                                    prop.className "flex items-center gap-2"
                                    prop.children [
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
