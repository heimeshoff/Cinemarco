module Components.CollectionModal.View

open Feliz
open Types
open Components.Modal.View

let view (model: Model) (dispatch: Msg -> unit) =
    let title = match model.EditingCollection with | Some _ -> "Edit Collection" | None -> "New Collection"
    let buttonText = match model.EditingCollection with | Some _ -> "Save Changes" | None -> "Create Collection"

    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Close
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header title None (not model.IsSubmitting) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                // Name field
                formField "Name" true [
                    Html.input [
                        prop.className "input input-bordered w-full"
                        prop.placeholder "Collection name"
                        prop.value model.Name
                        prop.onChange (fun (e: string) -> dispatch (NameChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Description field
                formField "Description" false [
                    Html.textarea [
                        prop.className "textarea textarea-bordered h-24 w-full"
                        prop.placeholder "What is this collection about?"
                        prop.value model.Description
                        prop.onChange (fun (e: string) -> dispatch (DescriptionChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Submit button
                submitButton buttonText model.IsSubmitting (fun () -> dispatch Submit)
            ]
        ]
    }
