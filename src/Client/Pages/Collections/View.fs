module Pages.Collections.View

open Feliz
open Common.Types
open Shared.Domain
open Types

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
                        prop.onClick (fun _ -> dispatch OpenAddCollectionModal)
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

            match model.Collections with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success collectionList when List.isEmpty collectionList ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "No collections yet" ]
                        Html.p [ prop.text "Create collections to organize your library!" ]
                    ]
                ]
            | Success collectionList ->
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
                    prop.children [
                        for collection in collectionList do
                            Html.div [
                                prop.className "card bg-base-200 hover:shadow-lg transition-shadow cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewCollectionDetail collection.Id))
                                prop.children [
                                    // Cover image (if available)
                                    match collection.CoverImagePath with
                                    | Some path ->
                                        Html.figure [
                                            prop.className "h-32 bg-base-300"
                                            prop.children [
                                                Html.img [
                                                    prop.src path
                                                    prop.className "w-full h-full object-cover"
                                                    prop.alt collection.Name
                                                ]
                                            ]
                                        ]
                                    | None ->
                                        Html.figure [
                                            prop.className "h-32 bg-gradient-to-br from-primary/20 to-secondary/20"
                                        ]

                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center gap-2"
                                                prop.children [
                                                    Html.h3 [
                                                        prop.className "card-title text-base"
                                                        prop.text collection.Name
                                                    ]
                                                    if collection.IsPublicFranchise then
                                                        Html.span [
                                                            prop.className "badge badge-secondary badge-sm"
                                                            prop.text "Franchise"
                                                        ]
                                                ]
                                            ]
                                            match collection.Description with
                                            | Some desc ->
                                                Html.p [
                                                    prop.className "text-sm text-base-content/60 line-clamp-2"
                                                    prop.text desc
                                                ]
                                            | None -> Html.none

                                            // Action buttons
                                            Html.div [
                                                prop.className "card-actions justify-end mt-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenEditCollectionModal collection)
                                                        )
                                                        prop.text "Edit"
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
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
