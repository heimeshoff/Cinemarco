module Components.TagModal.View

open Feliz
open Types

let view (model: Model) (dispatch: Msg -> unit) =
    let title = match model.EditingTag with | Some _ -> "Edit Tag" | None -> "Add Tag"
    let buttonText = match model.EditingTag with | Some _ -> "Save Changes" | None -> "Add Tag"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not model.IsSubmitting then dispatch Close)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-md"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "p-6 border-b border-base-300"
                        prop.children [
                            Html.h2 [
                                prop.className "text-xl font-bold"
                                prop.text title
                            ]
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch Close)
                                prop.disabled model.IsSubmitting
                                prop.text "X"
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-4"
                        prop.children [
                            // Error message
                            match model.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            // Name field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Name *" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "Tag name"
                                        prop.value model.Name
                                        prop.onChange (fun (e: string) -> dispatch (NameChanged e))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]

                            // Color field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Color (optional)" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "e.g., #3B82F6 or blue"
                                        prop.value model.Color
                                        prop.onChange (fun (e: string) -> dispatch (ColorChanged e))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]

                            // Description field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Description (optional)" ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
                                        prop.placeholder "Description of this tag..."
                                        prop.value model.Description
                                        prop.onChange (fun (e: string) -> dispatch (DescriptionChanged e))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]

                            // Submit button
                            Html.button [
                                prop.className "btn btn-primary w-full"
                                prop.onClick (fun _ -> dispatch Submit)
                                prop.disabled model.IsSubmitting
                                prop.children [
                                    if model.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    else
                                        Html.span [ prop.text buttonText ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
