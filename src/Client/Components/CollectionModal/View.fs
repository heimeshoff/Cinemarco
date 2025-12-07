module Components.CollectionModal.View

open Feliz
open Browser.Types
open Types
open Components.Modal.View

let private handleFileSelect (dispatch: Msg -> unit) (e: Event) =
    let input = e.target :?> HTMLInputElement
    if input.files.length > 0 then
        let file = input.files.[0]
        let reader = Browser.Dom.FileReader.Create()
        reader.onload <- fun _ ->
            let result = reader.result :?> string
            dispatch (LogoSelected result)
        reader.readAsDataURL(file)

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

                // Logo upload field
                formField "Logo" false [
                    Html.div [
                        prop.className "flex items-center gap-4"
                        prop.children [
                            // Preview
                            match model.LogoPreview with
                            | Some preview ->
                                Html.div [
                                    prop.className "relative"
                                    prop.children [
                                        Html.img [
                                            prop.src preview
                                            prop.className "w-20 h-20 object-cover rounded-lg"
                                            prop.alt "Collection logo"
                                        ]
                                        // Remove button
                                        Html.button [
                                            prop.type'.button
                                            prop.className "absolute -top-2 -right-2 btn btn-circle btn-xs btn-error"
                                            prop.onClick (fun e ->
                                                e.preventDefault()
                                                dispatch LogoRemoved)
                                            prop.disabled model.IsSubmitting
                                            prop.text "×"
                                        ]
                                    ]
                                ]
                            | None ->
                                Html.div [
                                    prop.className "w-20 h-20 border-2 border-dashed border-base-300 rounded-lg flex items-center justify-center text-base-content/40"
                                    prop.children [
                                        Html.span [ prop.text "No logo" ]
                                    ]
                                ]

                            // Upload button
                            Html.label [
                                prop.className "btn btn-sm btn-ghost"
                                prop.children [
                                    Html.span [ prop.text (if model.LogoPreview.IsSome then "Change" else "Upload") ]
                                    Html.input [
                                        prop.type'.file
                                        prop.accept "image/png,image/jpeg,image/gif,image/webp"
                                        prop.className "hidden"
                                        prop.onChange (handleFileSelect dispatch)
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

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

                // Description/Note field
                formField "Note" false [
                    Html.textarea [
                        prop.className "textarea textarea-bordered h-24 w-full"
                        prop.placeholder "Add a note about this collection..."
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
