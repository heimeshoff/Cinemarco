module Pages.Tags.View

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
                        prop.text "Tags"
                    ]
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ -> dispatch OpenAddTagModal)
                        prop.children [
                            Html.span [ prop.text "+ Add Tag" ]
                        ]
                    ]
                ]
            ]

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Organize your library with tags. Click on a tag to see all entries with that tag."
            ]

            match model.Tags with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success tagList when List.isEmpty tagList ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "No tags yet" ]
                        Html.p [ prop.text "Create tags to organize your library!" ]
                    ]
                ]
            | Success tagList ->
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
                    prop.children [
                        for tag in tagList do
                            Html.div [
                                prop.className "card bg-base-200 hover:shadow-lg transition-shadow cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewTagDetail tag.Id))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center gap-3"
                                                prop.children [
                                                    // Color indicator
                                                    Html.div [
                                                        prop.className "w-4 h-4 rounded-full"
                                                        prop.style [
                                                            Feliz.style.backgroundColor (tag.Color |> Option.defaultValue "#6366f1")
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex-1"
                                                        prop.children [
                                                            Html.h3 [
                                                                prop.className "font-bold"
                                                                prop.text tag.Name
                                                            ]
                                                            match tag.Description with
                                                            | Some desc ->
                                                                Html.p [
                                                                    prop.className "text-sm text-base-content/60 truncate"
                                                                    prop.text desc
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            // Action buttons
                                            Html.div [
                                                prop.className "card-actions justify-end mt-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenEditTagModal tag)
                                                        )
                                                        prop.text "Edit"
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenDeleteTagModal tag)
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
                    prop.text $"Error loading tags: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]
