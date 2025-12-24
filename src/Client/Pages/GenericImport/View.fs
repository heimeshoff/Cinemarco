module Pages.GenericImport.View

open Feliz
open Fable.React
open Pages.GenericImport.Types
open Common.Types
open Shared.Domain

module GlassPanel = Common.Components.GlassPanel.View
module GlassButton = Common.Components.GlassButton.View
module SectionHeader = Common.Components.SectionHeader.View
module RemoteDataView = Common.Components.RemoteDataView.View
module PosterCard = Common.Components.PosterCard.View
module PosterCardTypes = Common.Components.PosterCard.Types
module Icons = Components.Icons

// =====================================
// Poster URL Helper
// =====================================

/// Get poster URL - uses TMDB direct URL for import preview, cached URL for library items
let private getPosterUrl (path: string) (isInLibrary: bool) =
    if isInLibrary then
        // Use locally cached image
        $"/images/posters{path}"
    else
        // Use TMDB direct URL for items not yet in library
        $"https://image.tmdb.org/t/p/w500{path}"

// =====================================
// Step Indicator
// =====================================

let private stepIndicator (currentStep: ImportStep) =
    let steps = [
        SelectFile, "Select File"
        MatchingPreview, "Preview"
        ResolveAmbiguous, "Resolve"
        Importing, "Import"
        Complete, "Complete"
    ]

    let stepOrder step =
        match step with
        | SelectFile -> 0
        | MatchingPreview -> 1
        | ResolveAmbiguous -> 2
        | Importing -> 3
        | Complete -> 4

    let currentOrder = stepOrder currentStep

    Html.div [
        prop.className "flex items-center justify-center gap-2 mb-8"
        prop.children [
            for (step, label) in steps do
                let order = stepOrder step
                let isActive = order = currentOrder
                let isCompleted = order < currentOrder

                Html.div [
                    prop.className "flex items-center gap-2"
                    prop.children [
                        Html.div [
                            prop.className (
                                "w-8 h-8 rounded-full flex items-center justify-center text-sm font-medium transition-colors " +
                                if isCompleted then "bg-success text-success-content"
                                elif isActive then "bg-primary text-primary-content"
                                else "bg-base-300 text-base-content/50"
                            )
                            prop.children [
                                if isCompleted then Icons.check
                                else Html.text (string (order + 1))
                            ]
                        ]
                        Html.span [
                            prop.className (
                                "text-sm hidden sm:inline " +
                                if isActive then "text-primary font-medium"
                                elif isCompleted then "text-success"
                                else "text-base-content/50"
                            )
                            prop.text label
                        ]
                        if order < 4 then
                            Html.div [
                                prop.className (
                                    "w-8 h-0.5 " +
                                    if order < currentOrder then "bg-success"
                                    else "bg-base-300"
                                )
                            ]
                    ]
                ]
        ]
    ]

// =====================================
// Select File Step
// =====================================

let private selectFileStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // File Upload Area
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-8"
                    prop.children [
                        match model.SelectedFileName with
                        | Some fileName ->
                            // File selected
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    Html.div [
                                        prop.className "text-success"
                                        prop.children [ Icons.check ]
                                    ]
                                    Html.p [
                                        prop.className "text-lg font-medium"
                                        prop.text fileName
                                    ]
                                    Html.div [
                                        prop.className "flex justify-center gap-4"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm"
                                                prop.onClick (fun _ -> dispatch ClearFile)
                                                prop.text "Clear"
                                            ]
                                            match model.ParsedResult with
                                            | NotAsked ->
                                                Html.button [
                                                    prop.className "btn btn-primary btn-sm"
                                                    prop.onClick (fun _ -> dispatch ParseFile)
                                                    prop.text "Parse File"
                                                ]
                                            | Loading ->
                                                Html.button [
                                                    prop.className "btn btn-primary btn-sm loading"
                                                    prop.disabled true
                                                    prop.text "Parsing..."
                                                ]
                                            | Success result ->
                                                Html.div [
                                                    prop.className "flex items-center gap-2 text-success"
                                                    prop.children [
                                                        Icons.check
                                                        Html.span [
                                                            let collectionText =
                                                                if result.Collections.Length > 0 then
                                                                    $", {result.Collections.Length} collections"
                                                                else ""
                                                            prop.text $"{result.Items.Length} items{collectionText} found"
                                                        ]
                                                    ]
                                                ]
                                            | Failure _ -> ()
                                        ]
                                    ]
                                ]
                            ]
                        | None ->
                            // No file selected - show upload area
                            Html.label [
                                prop.className "cursor-pointer block"
                                prop.children [
                                    Html.div [
                                        prop.className "border-2 border-dashed border-base-300 rounded-lg p-8 hover:border-primary transition-colors"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex flex-col items-center gap-4"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "text-base-content/50"
                                                        prop.children [ Icons.import ]
                                                    ]
                                                    Html.p [
                                                        prop.className "text-base-content/70"
                                                        prop.text "Click to select a JSON file"
                                                    ]
                                                    Html.p [
                                                        prop.className "text-sm text-base-content/50"
                                                        prop.text "Supports .json files"
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.type' "file"
                                        prop.className "hidden"
                                        prop.accept ".json"
                                        prop.onChange (fun (e: Browser.Types.Event) ->
                                            let target = e.target :?> Browser.Types.HTMLInputElement
                                            let files = target.files
                                            if files.length > 0 then
                                                let file = files.[0]
                                                let reader = Browser.Dom.FileReader.Create()
                                                reader.onload <- fun _ ->
                                                    let content = reader.result :?> string
                                                    dispatch (FileSelected (file.name, content))
                                                reader.readAsText(file)
                                        )
                                    ]
                                ]
                            ]
                    ]
                ]
            ]

            // Error message
            match model.Error with
            | Some err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.children [
                        Icons.error
                        Html.span [ prop.text err ]
                    ]
                ]
            | None -> ()

            // Parse result / Continue button
            match model.ParsedResult with
            | Success result when result.Items.Length > 0 ->
                Html.div [
                    prop.className "flex justify-end"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-primary"
                            prop.onClick (fun _ -> dispatch ProceedToMatching)
                            prop.children [
                                Html.span [ prop.text $"Match {result.Items.Length} items with TMDB" ]
                                Icons.arrowRight
                            ]
                        ]
                    ]
                ]
            | _ -> ()

            // Help section
            GlassPanel.subtle [
                Html.div [
                    prop.className "flex items-start gap-3"
                    prop.children [
                        Html.div [
                            prop.className "text-info"
                            prop.children [ Icons.info ]
                        ]
                        Html.div [
                            prop.className "space-y-2 text-sm text-base-content/70"
                            prop.children [
                                Html.p [
                                    prop.className "font-medium text-base-content"
                                    prop.text "Expected JSON Format"
                                ]
                                Html.pre [
                                    prop.className "bg-base-200 p-3 rounded text-xs overflow-x-auto"
                                    prop.text """{
  "items": [
    {
      "title": "The Matrix",
      "year": 1999,
      "type": "movie",
      "watched": ["2020-03-15"],
      "rating": "Outstanding"
    }
  ]
}"""
                                ]
                                Html.p [
                                    prop.text "Use AI to convert your watch history (Excel, notes, etc.) into this format. See docs/import.md for the full specification."
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Matching Preview Step
// =====================================

let private matchingPreviewStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Summary cards
            RemoteDataView.withSpinner model.Preview (fun preview ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Stats cards
                        Html.div [
                            prop.className "grid grid-cols-2 md:grid-cols-5 gap-4"
                            prop.children [
                                GlassPanel.subtle [
                                    Html.div [
                                        prop.className "text-center"
                                        prop.children [
                                            Html.p [ prop.className "text-2xl font-bold"; prop.text (string preview.TotalItems) ]
                                            Html.p [ prop.className "text-sm text-base-content/70"; prop.text "Total" ]
                                        ]
                                    ]
                                ]
                                GlassPanel.subtle [
                                    Html.div [
                                        prop.className "text-center"
                                        prop.children [
                                            Html.p [ prop.className "text-2xl font-bold text-success"; prop.text (string preview.ExactMatches) ]
                                            Html.p [ prop.className "text-sm text-base-content/70"; prop.text "Matched" ]
                                        ]
                                    ]
                                ]
                                GlassPanel.subtle [
                                    Html.div [
                                        prop.className "text-center"
                                        prop.children [
                                            Html.p [ prop.className "text-2xl font-bold text-warning"; prop.text (string preview.AmbiguousMatches) ]
                                            Html.p [ prop.className "text-sm text-base-content/70"; prop.text "Ambiguous" ]
                                        ]
                                    ]
                                ]
                                GlassPanel.subtle [
                                    Html.div [
                                        prop.className "text-center"
                                        prop.children [
                                            Html.p [ prop.className "text-2xl font-bold text-error"; prop.text (string preview.NoMatches) ]
                                            Html.p [ prop.className "text-sm text-base-content/70"; prop.text "No Match" ]
                                        ]
                                    ]
                                ]
                                GlassPanel.subtle [
                                    Html.div [
                                        prop.className "text-center"
                                        prop.children [
                                            Html.p [ prop.className "text-2xl font-bold text-info"; prop.text (string preview.AlreadyInLibrary) ]
                                            Html.p [ prop.className "text-sm text-base-content/70"; prop.text "In Library" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                        // New friends to create
                        if preview.NewFriendsToCreate.Length > 0 then
                            let friendNames = String.concat ", " preview.NewFriendsToCreate
                            Html.div [
                                prop.className "alert alert-info"
                                prop.children [
                                    Icons.info
                                    Html.span [
                                        prop.text $"Will create {preview.NewFriendsToCreate.Length} new friend(s): {friendNames}"
                                    ]
                                ]
                            ]

                        // Collection suggestions
                        if model.CollectionSuggestions.Length > 0 then
                            GlassPanel.standard [
                                Html.div [
                                    prop.className "space-y-4"
                                    prop.children [
                                        Html.div [
                                            prop.className "flex items-center justify-between"
                                            prop.children [
                                                Html.h4 [
                                                    prop.className "font-medium"
                                                    prop.text $"Collection Suggestions ({model.CollectionSuggestions.Length})"
                                                ]
                                                Html.div [
                                                    prop.className "flex gap-2"
                                                    prop.children [
                                                        Html.button [
                                                            prop.className "btn btn-xs btn-ghost"
                                                            prop.onClick (fun _ -> dispatch SelectAllCollections)
                                                            prop.text "Select All"
                                                        ]
                                                        Html.button [
                                                            prop.className "btn btn-xs btn-ghost"
                                                            prop.onClick (fun _ -> dispatch DeselectAllCollections)
                                                            prop.text "Deselect All"
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                        Html.p [
                                            prop.className "text-sm text-base-content/70"
                                            prop.text "Select which collections to create during import:"
                                        ]
                                        Html.div [
                                            prop.className "space-y-2 max-h-48 overflow-y-auto"
                                            prop.children [
                                                for (idx, suggestion) in model.CollectionSuggestions |> List.indexed do
                                                    let resolvedCount =
                                                        suggestion.ResolvedItems
                                                        |> List.filter (fun r ->
                                                            match r with
                                                            | Resolved _ -> true
                                                            | Unresolved _ -> false)
                                                        |> List.length
                                                    let totalCount = suggestion.ResolvedItems.Length
                                                    Html.label [
                                                        prop.className "flex items-center gap-3 p-3 rounded-lg bg-base-200/50 cursor-pointer hover:bg-base-200"
                                                        prop.children [
                                                            Html.input [
                                                                prop.type' "checkbox"
                                                                prop.className "checkbox checkbox-primary"
                                                                prop.isChecked suggestion.Selected
                                                                prop.onChange (fun (_: bool) -> dispatch (ToggleCollectionSelection idx))
                                                            ]
                                                            Html.div [
                                                                prop.className "flex-1 min-w-0"
                                                                prop.children [
                                                                    Html.p [
                                                                        prop.className "font-medium"
                                                                        prop.text suggestion.Collection.Name
                                                                    ]
                                                                    match suggestion.Collection.Description with
                                                                    | Some desc ->
                                                                        Html.p [
                                                                            prop.className "text-sm text-base-content/70 truncate"
                                                                            prop.text desc
                                                                        ]
                                                                    | None -> ()
                                                                ]
                                                            ]
                                                            Html.span [
                                                                prop.className (
                                                                    "badge " +
                                                                    if resolvedCount = totalCount then "badge-success"
                                                                    elif resolvedCount > 0 then "badge-warning"
                                                                    else "badge-error"
                                                                )
                                                                prop.text $"{resolvedCount}/{totalCount} items"
                                                            ]
                                                        ]
                                                    ]
                                            ]
                                        ]
                                        let selectedCount = model.CollectionSuggestions |> List.filter (fun c -> c.Selected) |> List.length
                                        if selectedCount > 0 then
                                            Html.p [
                                                prop.className "text-sm text-success"
                                                prop.text $"{selectedCount} collection(s) will be created during import"
                                            ]
                                    ]
                                ]
                            ]

                        // Item list
                        GlassPanel.standard [
                            Html.div [
                                prop.className "space-y-2 max-h-96 overflow-y-auto"
                                prop.children [
                                    for item in model.EditingItems do
                                        Html.div [
                                            prop.className "flex items-center gap-4 p-3 rounded-lg bg-base-200/50"
                                            prop.children [
                                                // Poster (if matched)
                                                match item.MatchStatus with
                                                | ExactMatch r | MatchConfirmed r ->
                                                    match r.PosterPath with
                                                    | Some path ->
                                                        Html.img [
                                                            prop.className "w-12 h-18 rounded object-cover"
                                                            prop.src (getPosterUrl path item.ExistsInLibrary)
                                                            prop.alt r.Title
                                                        ]
                                                    | None ->
                                                        Html.div [
                                                            prop.className "w-12 h-18 rounded bg-base-300 flex items-center justify-center text-base-content/50"
                                                            prop.text "?"
                                                        ]
                                                | _ ->
                                                    Html.div [
                                                        prop.className "w-12 h-18 rounded bg-base-300 flex items-center justify-center text-base-content/50"
                                                        prop.text "?"
                                                    ]

                                                // Title and info
                                                Html.div [
                                                    prop.className "flex-1 min-w-0"
                                                    prop.children [
                                                        Html.p [
                                                            prop.className "font-medium truncate"
                                                            prop.text item.ImportItem.Title
                                                        ]
                                                        Html.p [
                                                            prop.className "text-sm text-base-content/70"
                                                            prop.text (
                                                                match item.ImportItem.MediaType with
                                                                | ImportMovie -> "Movie"
                                                                | ImportSeries -> "Series"
                                                            )
                                                        ]
                                                    ]
                                                ]

                                                // Match status badge
                                                Html.div [
                                                    prop.className (
                                                        "badge " +
                                                        match item.MatchStatus with
                                                        | ExactMatch _ | MatchConfirmed _ -> "badge-success"
                                                        | MultipleMatches _ -> "badge-warning"
                                                        | NoMatchFound -> "badge-error"
                                                        | NotMatched -> "badge-ghost"
                                                    )
                                                    prop.text (
                                                        match item.MatchStatus with
                                                        | ExactMatch _ -> "Matched"
                                                        | MatchConfirmed _ -> "Confirmed"
                                                        | MultipleMatches matches -> $"{matches.Length} matches"
                                                        | NoMatchFound -> "No match"
                                                        | NotMatched -> "Pending"
                                                    )
                                                ]

                                                // In library badge
                                                if item.ExistsInLibrary then
                                                    Html.div [
                                                        prop.className "badge badge-info"
                                                        prop.text "In Library"
                                                    ]
                                            ]
                                        ]
                                ]
                            ]
                        ]

                        // Action buttons
                        Html.div [
                            prop.className "flex justify-between"
                            prop.children [
                                Html.button [
                                    prop.className "btn btn-ghost"
                                    prop.onClick (fun _ -> dispatch BackToFile)
                                    prop.children [
                                        Icons.arrowLeft
                                        Html.span [ prop.text "Back" ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex gap-4"
                                    prop.children [
                                        if preview.AmbiguousMatches > 0 then
                                            Html.button [
                                                prop.className "btn btn-warning"
                                                prop.onClick (fun _ -> dispatch StartResolving)
                                                prop.children [
                                                    Html.span [ prop.text $"Resolve {preview.AmbiguousMatches} Ambiguous" ]
                                                ]
                                            ]
                                        if preview.ExactMatches > 0 then
                                            Html.button [
                                                prop.className "btn btn-primary"
                                                prop.onClick (fun _ -> dispatch ProceedToImport)
                                                prop.children [
                                                    Html.span [ prop.text $"Import {preview.ExactMatches} Items" ]
                                                    Icons.arrowRight
                                                ]
                                            ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            )
        ]
    ]

// =====================================
// Resolve Ambiguous Step
// =====================================

let private renderMatchCard (index: int) (m: TmdbSearchResult) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "cursor-pointer group"
        prop.onClick (fun _ -> dispatch (ConfirmMatch (index, m)))
        prop.children [
            GlassPanel.subtle [
                Html.div [
                    prop.className "space-y-2 group-hover:opacity-80 transition-opacity"
                    prop.children [
                        match m.PosterPath with
                        | Some path ->
                            Html.img [
                                prop.className "w-full aspect-[2/3] rounded object-cover"
                                prop.src (getPosterUrl path false)  // Not in library yet
                                prop.alt m.Title
                            ]
                        | None ->
                            Html.div [
                                prop.className "w-full aspect-[2/3] rounded bg-base-300 flex items-center justify-center"
                                prop.text "No Image"
                            ]
                        Html.p [
                            prop.className "font-medium text-sm truncate"
                            prop.text m.Title
                        ]
                        Html.p [
                            prop.className "text-xs text-base-content/70"
                            prop.text (
                                m.ReleaseDate
                                |> Option.map (fun d -> d.Year.ToString())
                                |> Option.defaultValue "Unknown year"
                            )
                        ]
                    ]
                ]
            ]
        ]
    ]

let private resolveAmbiguousStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            match model.ResolvingIndex with
            | Some index when index < model.EditingItems.Length ->
                let item = model.EditingItems.[index]
                match item.MatchStatus with
                | MultipleMatches matches ->
                    // Item info
                    GlassPanel.standard [
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                Html.h3 [
                                    prop.className "text-lg font-bold"
                                    prop.text $"Resolve: {item.ImportItem.Title}"
                                ]
                                Html.p [
                                    prop.className "text-base-content/70"
                                    prop.text $"Found {matches.Length} possible matches. Select the correct one or search for it:"
                                ]
                            ]
                        ]
                    ]

                    // Manual search section
                    GlassPanel.subtle [
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                Html.div [
                                    prop.className "flex gap-2"
                                    prop.children [
                                        Html.input [
                                            prop.className "input input-bordered flex-1"
                                            prop.type' "text"
                                            prop.placeholder "Search TMDB for a different title..."
                                            prop.value model.SearchQuery
                                            prop.onChange (SetSearchQuery >> dispatch)
                                            prop.onKeyDown (fun e ->
                                                if e.key = "Enter" then dispatch SearchTmdb)
                                        ]
                                        Html.button [
                                            prop.className "btn btn-primary"
                                            prop.onClick (fun _ -> dispatch SearchTmdb)
                                            prop.disabled (model.SearchResults = Loading)
                                            prop.children [
                                                if model.SearchResults = Loading then
                                                    Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                else
                                                    Icons.search
                                            ]
                                        ]
                                        if model.SearchQuery <> "" then
                                            Html.button [
                                                prop.className "btn btn-ghost"
                                                prop.onClick (fun _ -> dispatch ClearSearch)
                                                prop.children [ Icons.close ]
                                            ]
                                    ]
                                ]

                                // Search results
                                match model.SearchResults with
                                | Success [] ->
                                    Html.p [
                                        prop.className "text-sm text-warning"
                                        prop.text "No results found. Try a different search query."
                                    ]
                                | Success results ->
                                    Html.div [
                                        prop.className "space-y-2"
                                        prop.children [
                                            Html.p [
                                                prop.className "text-sm font-medium text-base-content/70"
                                                prop.text $"Search results ({results.Length}):"
                                            ]
                                            Html.div [
                                                prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                                                prop.children [
                                                    for m in results do
                                                        renderMatchCard index m dispatch
                                                ]
                                            ]
                                        ]
                                    ]
                                | Failure err ->
                                    Html.p [
                                        prop.className "text-sm text-error"
                                        prop.text $"Search failed: {err}"
                                    ]
                                | Loading ->
                                    Html.div [
                                        prop.className "flex justify-center py-4"
                                        prop.children [
                                            Html.span [ prop.className "loading loading-spinner loading-md" ]
                                        ]
                                    ]
                                | NotAsked -> ()
                            ]
                        ]
                    ]

                    // Original match options (when no search is active)
                    if model.SearchResults = NotAsked then
                        Html.div [
                            prop.className "space-y-2"
                            prop.children [
                                Html.p [
                                    prop.className "text-sm font-medium text-base-content/70"
                                    prop.text "Original matches:"
                                ]
                                Html.div [
                                    prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                                    prop.children [
                                        for m in matches do
                                            renderMatchCard index m dispatch
                                    ]
                                ]
                            ]
                        ]

                    // Skip button
                    Html.div [
                        prop.className "flex justify-center"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch (SkipItem index))
                                prop.text "Skip this item"
                            ]
                        ]
                    ]
                | _ ->
                    Html.div [
                        prop.className "text-center py-8"
                        prop.children [
                            Html.p [ prop.text "No more items to resolve" ]
                            Html.button [
                                prop.className "btn btn-primary mt-4"
                                prop.onClick (fun _ -> dispatch (GoToStep MatchingPreview))
                                prop.text "Back to Preview"
                            ]
                        ]
                    ]
            | _ ->
                Html.div [
                    prop.className "text-center py-8"
                    prop.children [
                        Html.p [ prop.text "All ambiguous items resolved!" ]
                        Html.button [
                            prop.className "btn btn-primary mt-4"
                            prop.onClick (fun _ -> dispatch (GoToStep MatchingPreview))
                            prop.text "Back to Preview"
                        ]
                    ]
                ]
        ]
    ]

// =====================================
// Importing Step
// =====================================

let private importingStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-8 space-y-6"
                    prop.children [
                        Html.div [
                            prop.className "loading loading-spinner loading-lg text-primary"
                        ]
                        Html.h3 [
                            prop.className "text-xl font-bold"
                            prop.text "Importing..."
                        ]
                        match model.Progress.CurrentItem with
                        | Some item ->
                            Html.p [
                                prop.className "text-base-content/70"
                                prop.text item
                            ]
                        | None -> ()

                        // Progress bar
                        Html.div [
                            prop.className "w-full max-w-md mx-auto"
                            prop.children [
                                Html.progress [
                                    prop.className "progress progress-primary w-full"
                                    prop.value model.Progress.CurrentIndex
                                    prop.max model.Progress.TotalItems
                                ]
                                Html.p [
                                    prop.className "text-sm text-base-content/70 mt-2"
                                    prop.text $"{model.Progress.CurrentIndex} / {model.Progress.TotalItems}"
                                ]
                            ]
                        ]

                        // Stats
                        Html.div [
                            prop.className "flex justify-center gap-8"
                            prop.children [
                                Html.div [
                                    prop.className "text-success"
                                    prop.children [
                                        Html.span [ prop.className "font-bold"; prop.text (string model.Progress.CompletedSuccessfully) ]
                                        Html.span [ prop.className "text-sm ml-1"; prop.text "imported" ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "text-warning"
                                    prop.children [
                                        Html.span [ prop.className "font-bold"; prop.text (string model.Progress.Skipped) ]
                                        Html.span [ prop.className "text-sm ml-1"; prop.text "skipped" ]
                                    ]
                                ]
                                if model.Progress.Errors.Length > 0 then
                                    Html.div [
                                        prop.className "text-error"
                                        prop.children [
                                            Html.span [ prop.className "font-bold"; prop.text (string model.Progress.Errors.Length) ]
                                            Html.span [ prop.className "text-sm ml-1"; prop.text "errors" ]
                                        ]
                                    ]
                            ]
                        ]

                        // Cancel button
                        Html.button [
                            prop.className "btn btn-ghost btn-sm"
                            prop.onClick (fun _ -> dispatch CancelImport)
                            prop.text "Cancel"
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Complete Step
// =====================================

let private renderImportedItemCard (item: ImportedItemInfo) (isImported: bool) =
    // Build poster URL
    let posterUrl =
        item.PosterPath
        |> Option.map (fun path -> getPosterUrl path isImported)

    // Convert rating to badge
    let ratingBadge =
        item.Rating
        |> Option.map PosterCard.ratingToBadge

    // Create a custom status overlay only for skipped items
    let statusOverlay =
        if isImported then
            None
        else
            let badgeElement =
                Html.div [
                    prop.className "absolute top-0 left-0 right-0 px-2 pt-2"
                    prop.children [
                        Html.div [
                            prop.className "flex justify-end"
                            prop.children [
                                Html.span [
                                    prop.className "badge badge-warning badge-sm gap-1"
                                    prop.children [
                                        Icons.close
                                        Html.span [ prop.text "Skipped" ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            Some (PosterCardTypes.Custom badgeElement)

    // Build config
    let config : PosterCardTypes.Config = {
        PosterUrl = posterUrl
        Title = item.Title
        OnClick = fun () -> () // No click action in import results
        RatingBadge = ratingBadge
        StatusOverlay = statusOverlay
        IsGrayscale = not isImported
        MediaType = Some item.MediaType
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
    }

    Html.div [
        prop.className "space-y-1"
        prop.children [
            // Poster card with shine effect and rating on hover
            PosterCard.view config

            // Title
            Html.p [
                prop.className "font-medium text-sm truncate mt-2"
                prop.text item.Title
            ]

            // Year
            Html.p [
                prop.className "text-xs text-base-content/70"
                prop.text (item.Year |> Option.map string |> Option.defaultValue "?")
            ]

            // Watch date on separate line
            match item.WatchDate with
            | Some date ->
                Html.p [
                    prop.className "text-xs text-base-content/50"
                    prop.text (date.ToString("MMM d, yyyy"))
                ]
            | None -> ()
        ]
    ]

let private completeStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with stats
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-6 space-y-4"
                    prop.children [
                        Html.div [
                            prop.className "text-success text-5xl"
                            prop.children [ Icons.check ]
                        ]
                        Html.h3 [
                            prop.className "text-2xl font-bold"
                            prop.text "Import Complete!"
                        ]

                        // Summary stats
                        Html.div [
                            prop.className "flex justify-center gap-8"
                            prop.children [
                                Html.div [
                                    prop.className "text-center"
                                    prop.children [
                                        Html.p [ prop.className "text-3xl font-bold text-success"; prop.text (string model.Progress.CompletedSuccessfully) ]
                                        Html.p [ prop.className "text-sm text-base-content/70"; prop.text "Imported" ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "text-center"
                                    prop.children [
                                        Html.p [ prop.className "text-3xl font-bold text-warning"; prop.text (string model.Progress.Skipped) ]
                                        Html.p [ prop.className "text-sm text-base-content/70"; prop.text "Skipped" ]
                                    ]
                                ]
                            ]
                        ]

                        // Errors if any
                        if model.Progress.Errors.Length > 0 then
                            Html.div [
                                prop.className "mt-4 text-left max-w-md mx-auto"
                                prop.children [
                                    Html.h4 [
                                        prop.className "text-error font-medium mb-2"
                                        prop.text $"{model.Progress.Errors.Length} Errors:"
                                    ]
                                    Html.ul [
                                        prop.className "text-sm text-base-content/70 space-y-1 max-h-32 overflow-y-auto"
                                        prop.children [
                                            for err in model.Progress.Errors do
                                                Html.li [
                                                    prop.className "truncate"
                                                    prop.text err
                                                ]
                                        ]
                                    ]
                                ]
                            ]

                        // Action buttons
                        Html.div [
                            prop.className "flex justify-center gap-4 mt-4"
                            prop.children [
                                Html.a [
                                    prop.className "btn btn-primary"
                                    prop.href "/library"
                                    prop.text "Go to Library"
                                ]
                                Html.button [
                                    prop.className "btn btn-ghost"
                                    prop.onClick (fun _ -> dispatch (GoToStep SelectFile))
                                    prop.text "Import More"
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Imported items section - grouped by who they were watched with
            if model.Progress.ImportedItems.Length > 0 then
                // Group items by friends (sorted list of friend names as key)
                let groupedByFriends =
                    model.Progress.ImportedItems
                    |> List.groupBy (fun item ->
                        item.FriendNames |> List.sort |> String.concat ", ")
                    |> List.sortBy (fun (friends, _) ->
                        // Sort: items with friends first, then by friend names
                        if friends = "" then "zzz" else friends)

                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        for (friendsKey, items) in groupedByFriends do
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    // Section header based on friends
                                    if friendsKey = "" then
                                        SectionHeader.title "Imported"
                                    else
                                        SectionHeader.title (sprintf "Watched with %s" friendsKey)

                                    Html.div [
                                        prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-3"
                                        prop.children [
                                            for item in items do
                                                renderImportedItemCard item true
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]

            // Skipped items section
            if model.Progress.SkippedItems.Length > 0 then
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        SectionHeader.title "Skipped"
                        Html.div [
                            prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 lg:grid-cols-8 gap-3"
                            prop.children [
                                for item in model.Progress.SkippedItems do
                                    renderImportedItemCard item false
                            ]
                        ]
                    ]
                ]
        ]
    ]

// =====================================
// Main View
// =====================================

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "container mx-auto px-4 py-8 max-w-4xl"
        prop.children [
            // Header
            SectionHeader.titleLarge "Import from JSON"

            // Step indicator
            stepIndicator model.CurrentStep

            // Step content
            match model.CurrentStep with
            | SelectFile -> selectFileStep model dispatch
            | MatchingPreview -> matchingPreviewStep model dispatch
            | ResolveAmbiguous -> resolveAmbiguousStep model dispatch
            | Importing -> importingStep model dispatch
            | Complete -> completeStep model dispatch
        ]
    ]
