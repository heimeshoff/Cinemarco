module Components.WatchSessionModal.View

open Feliz
open Shared.Domain
open Types
open Components.Modal.View
open Components.FriendSelector.View

let view (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    let isWorking = model.IsSubmitting || model.IsAddingFriend
    let canSubmit = not (List.isEmpty model.SelectedFriends) && not isWorking

    wrapper {
        OnClose = fun () -> if not isWorking then dispatch Close
        CanClose = not isWorking
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "New Watch Session" (Some "Track a viewing with friends") (not isWorking) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                // Friends selection with inline submit button
                formField "Watching With" true [
                    Html.div [
                        prop.className "flex gap-2 items-start"
                        prop.children [
                            // Friend selector (flex-1 to take available space)
                            Html.div [
                                prop.className "flex-1"
                                prop.children [
                                    FriendSelector {
                                        AllFriends = friends
                                        SelectedFriends = model.SelectedFriends
                                        OnToggle = fun friendId -> dispatch (ToggleFriend friendId)
                                        OnAddNew = fun name -> dispatch (AddNewFriend name)
                                        OnSubmit = if canSubmit then Some (fun () -> dispatch Submit) else None
                                        IsDisabled = isWorking
                                        Placeholder = "Search or add friends..."
                                        IsRequired = true
                                        AutoFocus = true
                                    }
                                ]
                            ]
                            // Submit button inline
                            Html.button [
                                prop.className "btn btn-primary shrink-0"
                                prop.disabled (not canSubmit)
                                prop.onClick (fun _ -> dispatch Submit)
                                prop.children [
                                    if model.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    Html.span [ prop.text "Start" ]
                                ]
                            ]
                        ]
                    ]
                    if model.IsAddingFriend then
                        Html.div [
                            prop.className "flex items-center gap-2 mt-2 text-sm text-base-content/60"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                Html.span [ prop.text "Adding friend..." ]
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
                                        prop.disabled isWorking
                                        prop.text tag.Name
                                    ]
                            ]
                        ]
                    ]
            ]
        ]
    }
