module Components.MovieWatchSessionModal.View

open Feliz
open Shared.Domain
open Types
open Components.Modal.View
open Components.FriendSelector.View

let view (model: Model) (friends: Friend list) (dispatch: Msg -> unit) =
    let isWorking = model.IsSubmitting || model.IsAddingFriend
    let canSubmit = not isWorking

    let isEditMode =
        match model.Mode with
        | Edit _ -> true
        | Create _ -> false

    let headerTitle = if isEditMode then "Edit Watch Session" else "Add Watch Session"
    let headerSubtitle = if isEditMode then Some "Update the details of this watch session" else Some "Record when you watched this movie"
    let submitButtonText = if isEditMode then "Save Changes" else "Add Watch Session"

    wrapper {
        OnClose = fun () -> if not isWorking then dispatch Close
        CanClose = not isWorking
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header headerTitle headerSubtitle (not isWorking) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                // Date picker
                formField "Watched Date" true [
                    Html.input [
                        prop.type' "date"
                        prop.className "input input-bordered w-full"
                        prop.value (model.WatchedDate.ToString("yyyy-MM-dd"))
                        prop.onChange (fun (value: string) ->
                            match System.DateTime.TryParse(value) with
                            | true, date -> dispatch (SetWatchedDate date)
                            | false, _ -> ()
                        )
                        prop.disabled isWorking
                    ]
                ]

                // Friends selection (optional)
                formField "Watched With" false [
                    Html.div [
                        prop.className "space-y-2"
                        prop.children [
                            FriendSelector {
                                AllFriends = friends
                                SelectedFriends = model.SelectedFriends
                                OnToggle = fun friendId -> dispatch (ToggleFriend friendId)
                                OnAddNew = fun name -> dispatch (AddNewFriend name)
                                OnSubmit = None
                                IsDisabled = isWorking
                                Placeholder = "Search or add friends (leave empty for 'Myself')..."
                                IsRequired = false
                                AutoFocus = false
                            }
                            if model.IsAddingFriend then
                                Html.div [
                                    prop.className "flex items-center gap-2 text-sm text-base-content/60"
                                    prop.children [
                                        Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                        Html.span [ prop.text "Adding friend..." ]
                                    ]
                                ]
                            if List.isEmpty model.SelectedFriends then
                                Html.p [
                                    prop.className "text-sm text-base-content/60 italic"
                                    prop.text "No friends selected - will be recorded as 'Myself'"
                                ]
                        ]
                    ]
                ]

                // Optional session name
                formField "Session Name (optional)" false [
                    Html.input [
                        prop.type' "text"
                        prop.className "input input-bordered w-full"
                        prop.placeholder "e.g., 'Movie night', 'Rewatch'..."
                        prop.value model.SessionName
                        prop.onChange (fun value -> dispatch (SetSessionName value))
                        prop.disabled isWorking
                    ]
                ]
            ]

            // Footer with submit button
            footer [
                Html.button [
                    prop.className "btn btn-ghost"
                    prop.onClick (fun _ -> dispatch Close)
                    prop.disabled isWorking
                    prop.text "Cancel"
                ]
                Html.button [
                    prop.className "btn btn-primary"
                    prop.disabled (not canSubmit)
                    prop.onClick (fun _ -> dispatch Submit)
                    prop.children [
                        if model.IsSubmitting then
                            Html.span [ prop.className "loading loading-spinner loading-sm" ]
                        Html.span [ prop.text submitButtonText ]
                    ]
                ]
            ]
        ]
    }
