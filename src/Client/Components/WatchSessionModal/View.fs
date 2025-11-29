module Components.WatchSessionModal.View

open Feliz
open Shared.Domain
open Types
open Components.Modal.View

let view (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Close
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "New Watch Session" (Some "Track a separate viewing of this series") (not model.IsSubmitting) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                // Session name field
                formField "Session Name" true [
                    Html.input [
                        prop.className "input input-bordered w-full"
                        prop.placeholder "e.g., Rewatch with partner, 2024 viewing"
                        prop.value model.Name
                        prop.onChange (fun (e: string) -> dispatch (NameChanged e))
                        prop.disabled model.IsSubmitting
                        prop.autoFocus true
                    ]
                ]

                // Friends selection
                if not (List.isEmpty friends) then
                    formField "Watching With" false [
                        Html.div [
                            prop.className "flex flex-wrap gap-2"
                            prop.children [
                                for friend in friends do
                                    let isSelected = List.contains friend.Id model.SelectedFriends
                                    Html.button [
                                        prop.className (
                                            "btn btn-sm " +
                                            if isSelected then "btn-primary" else "btn-ghost"
                                        )
                                        prop.onClick (fun _ -> dispatch (ToggleFriend friend.Id))
                                        prop.disabled model.IsSubmitting
                                        prop.text friend.Name
                                    ]
                            ]
                        ]
                    ]

                // Tags selection
                if not (List.isEmpty tags) then
                    formField "Tags" false [
                        Html.div [
                            prop.className "flex flex-wrap gap-2"
                            prop.children [
                                for tag in tags do
                                    let isSelected = List.contains tag.Id model.SelectedTags
                                    Html.button [
                                        prop.className (
                                            "btn btn-sm " +
                                            if isSelected then "btn-secondary" else "btn-ghost"
                                        )
                                        prop.onClick (fun _ -> dispatch (ToggleTag tag.Id))
                                        prop.disabled model.IsSubmitting
                                        prop.text tag.Name
                                    ]
                            ]
                        ]
                    ]

                // Submit button
                submitButton "Start Session" model.IsSubmitting (fun () -> dispatch Submit)
            ]
        ]
    }
