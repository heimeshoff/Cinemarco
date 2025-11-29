module Components.TagModal.View

open Feliz
open Types
open Components.Modal.View

let view (model: Model) (dispatch: Msg -> unit) =
    let title = match model.EditingTag with | Some _ -> "Edit Tag" | None -> "Add Tag"
    let buttonText = match model.EditingTag with | Some _ -> "Save Changes" | None -> "Add Tag"

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
                        prop.placeholder "Tag name"
                        prop.value model.Name
                        prop.onChange (fun (e: string) -> dispatch (NameChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Color field
                formField "Color" false [
                    Html.input [
                        prop.className "input input-bordered w-full"
                        prop.placeholder "e.g., #3B82F6 or blue"
                        prop.value model.Color
                        prop.onChange (fun (e: string) -> dispatch (ColorChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Description field
                formField "Description" false [
                    Html.textarea [
                        prop.className "textarea textarea-bordered h-20 w-full"
                        prop.placeholder "Description of this tag..."
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
