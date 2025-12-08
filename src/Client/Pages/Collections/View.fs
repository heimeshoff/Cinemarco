module Pages.Collections.View

open Feliz
open Common.Types
open Shared.Domain
open Types

/// Filter collections based on search query
let private filterCollections (model: Model) (collections: Collection list) =
    if System.String.IsNullOrWhiteSpace model.SearchQuery then
        collections
    else
        let query = model.SearchQuery.ToLowerInvariant()
        collections
        |> List.filter (fun c ->
            c.Name.ToLowerInvariant().Contains(query) ||
            (c.Description |> Option.map (fun d -> d.ToLowerInvariant().Contains(query)) |> Option.defaultValue false)
        )

/// Search input row
let private searchRow (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex items-center gap-4"
        prop.children [
            Html.div [
                prop.className "flex-1 max-w-xs"
                prop.children [
                    Html.input [
                        prop.type'.text
                        prop.placeholder "Search collections..."
                        prop.className "input input-bordered input-sm w-full"
                        prop.value model.SearchQuery
                        prop.onChange (fun v -> dispatch (SetSearchQuery v))
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with add button
            Html.div [
                prop.className "flex justify-between items-center"
                prop.children [
                    Html.h2 [
                        prop.className "text-2xl font-bold"
                        prop.text "Collections"
                    ]
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ -> dispatch CreateNewCollection)
                        prop.children [
                            Html.span [ prop.text "+ New Collection" ]
                        ]
                    ]
                ]
            ]

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Organize your library into collections. Create custom lists or track franchise watch orders."
            ]

            // Search
            searchRow model dispatch

            match model.Collections with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success collectionList ->
                let filtered = filterCollections model collectionList
                let sortedCollections = filtered |> List.sortBy (fun c -> c.Name.ToLowerInvariant())

                if List.isEmpty collectionList then
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.children [
                            Html.p [ prop.className "text-lg mb-2"; prop.text "No collections yet" ]
                            Html.p [ prop.text "Create collections to organize your library!" ]
                        ]
                    ]
                elif List.isEmpty filtered then
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.text "No collections match your search."
                    ]
                else
                    Html.ul [
                        prop.className "space-y-2"
                        prop.children [
                            for collection in sortedCollections do
                            Html.li [
                                prop.className "flex items-center gap-4 px-4 py-3 bg-base-200 rounded-lg hover:bg-base-300 transition-colors cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewCollectionDetail (collection.Id, collection.Name)))
                                prop.children [
                                    // Logo
                                    match collection.CoverImagePath with
                                    | Some path ->
                                        Html.img [
                                            prop.src $"/images/collections{path}"
                                            prop.className "w-12 h-12 object-cover rounded-lg"
                                            prop.alt collection.Name
                                        ]
                                    | None ->
                                        Html.div [
                                            prop.className "w-12 h-12 bg-base-300 rounded-lg flex items-center justify-center"
                                            prop.children [
                                                Html.span [
                                                    prop.className "text-xl opacity-50"
                                                    prop.text "📁"
                                                ]
                                            ]
                                        ]

                                    // Name and badges
                                    Html.div [
                                        prop.className "flex-1 min-w-0 flex items-center gap-2"
                                        prop.children [
                                            Html.span [
                                                prop.className "font-medium"
                                                prop.text collection.Name
                                            ]
                                            if collection.IsPublicFranchise then
                                                Html.span [
                                                    prop.className "badge badge-secondary badge-sm"
                                                    prop.text "Franchise"
                                                ]
                                        ]
                                    ]

                                    // Delete button
                                    Html.button [
                                        prop.className "btn btn-ghost btn-sm text-error"
                                        prop.onClick (fun e ->
                                            e.stopPropagation()
                                            dispatch (OpenDeleteCollectionModal collection)
                                        )
                                        prop.text "Delete"
                                    ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading collections: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]
