module Components.AddToCollectionModal.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Modal.View

let view (model: Model) (dispatch: Msg -> unit) =
    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Close
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "Add to Collection" (Some $"Add \"{model.EntryTitle}\" to a collection") (not model.IsSubmitting) (fun () -> dispatch Close)

            // Body
            body [
                // Error message
                errorAlert model.Error

                match model.Collections with
                | Loading ->
                    Html.div [
                        prop.className "flex justify-center py-8"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-md" ]
                        ]
                    ]

                | Failure err ->
                    Html.div [
                        prop.className "alert alert-error"
                        prop.text $"Error loading collections: {err}"
                    ]

                | Success collections ->
                    if List.isEmpty collections then
                        Html.div [
                            prop.className "text-center py-8 text-base-content/60"
                            prop.children [
                                Html.p [ prop.text "No collections yet" ]
                                Html.p [ prop.className "text-sm mt-2"; prop.text "Create a collection first from the Collections page" ]
                            ]
                        ]
                    else
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                // Collection selector
                                formField "Select Collection" true [
                                    Html.div [
                                        prop.className "space-y-2 max-h-48 overflow-y-auto"
                                        prop.children [
                                            for collection in collections do
                                                let isSelected = model.SelectedCollectionId = Some collection.Id
                                                Html.button [
                                                    prop.type' "button"
                                                    prop.className (
                                                        "w-full flex items-center gap-3 p-3 rounded-lg transition-all text-left " +
                                                        if isSelected then "bg-primary text-primary-content"
                                                        else "bg-base-200 hover:bg-base-300"
                                                    )
                                                    prop.onClick (fun _ -> dispatch (SelectCollection collection.Id))
                                                    prop.disabled model.IsSubmitting
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "text-2xl"
                                                            prop.text (if collection.IsPublicFranchise then "🎬" else "📚")
                                                        ]
                                                        Html.div [
                                                            prop.className "flex-1"
                                                            prop.children [
                                                                Html.p [
                                                                    prop.className "font-medium"
                                                                    prop.text collection.Name
                                                                ]
                                                                match collection.Description with
                                                                | Some desc ->
                                                                    Html.p [
                                                                        prop.className (
                                                                            "text-xs truncate " +
                                                                            if isSelected then "text-primary-content/70"
                                                                            else "text-base-content/50"
                                                                        )
                                                                        prop.text desc
                                                                    ]
                                                                | None -> Html.none
                                                            ]
                                                        ]
                                                        if isSelected then
                                                            Html.span [
                                                                prop.className "text-xl"
                                                                prop.text "✓"
                                                            ]
                                                    ]
                                                ]
                                        ]
                                    ]
                                ]

                                // Notes input
                                formField "Notes" false [
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20 w-full"
                                        prop.placeholder "Add notes for this item in the collection..."
                                        prop.value model.Notes
                                        prop.onChange (fun (s: string) -> dispatch (NotesChanged s))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]

                                // Submit button
                                submitButton "Add to Collection" model.IsSubmitting (fun () -> dispatch Submit)
                            ]
                        ]

                | NotAsked -> Html.none
            ]
        ]
    }
