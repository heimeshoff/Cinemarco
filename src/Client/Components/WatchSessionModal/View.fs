module Components.WatchSessionModal.View

open Feliz
open Shared.Domain
open Types
open Components.Modal.View

let view (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    let canSubmit = not (List.isEmpty model.SelectedFriends) && not model.IsSubmitting

    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Close
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "New Watch Session" (Some "Track a viewing with friends") (not model.IsSubmitting) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                // Friends selection (required)
                if not (List.isEmpty friends) then
                    formField "Watching With" true [
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
                        if List.isEmpty model.SelectedFriends then
                            Html.p [
                                prop.className "text-sm text-base-content/60 mt-2"
                                prop.text "Select at least one friend"
                            ]
                    ]
                else
                    Html.div [
                        prop.className "alert alert-warning"
                        prop.text "Please add some friends first before creating a watch session."
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

                // Submit button (disabled if no friends selected)
                Html.div [
                    prop.className "flex justify-end mt-6"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-primary"
                            prop.disabled (not canSubmit)
                            prop.onClick (fun _ -> dispatch Submit)
                            prop.children [
                                if model.IsSubmitting then
                                    Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                Html.span [ prop.text "Start Session" ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    }
