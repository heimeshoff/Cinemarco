module Components.FriendModal.View

open Feliz
open Types
open Components.Modal.View

let view (model: Model) (dispatch: Msg -> unit) =
    let title = match model.EditingFriend with | Some _ -> "Edit Friend" | None -> "Add Friend"
    let buttonText = match model.EditingFriend with | Some _ -> "Save Changes" | None -> "Add Friend"

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
                        prop.placeholder "Friend's name"
                        prop.value model.Name
                        prop.onChange (fun (e: string) -> dispatch (NameChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Nickname field
                formField "Nickname" false [
                    Html.input [
                        prop.className "input input-bordered w-full"
                        prop.placeholder "Optional nickname"
                        prop.value model.Nickname
                        prop.onChange (fun (e: string) -> dispatch (NicknameChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Notes field
                formField "Notes" false [
                    Html.textarea [
                        prop.className "textarea textarea-bordered h-20 w-full"
                        prop.placeholder "Notes about this friend..."
                        prop.value model.Notes
                        prop.onChange (fun (e: string) -> dispatch (NotesChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Submit button
                submitButton buttonText model.IsSubmitting (fun () -> dispatch Submit)
            ]
        ]
    }
