module Components.ConfirmModal.View

open Feliz
open Shared.Domain
open Types
open Components.Modal.View

let private getDeleteInfo (target: DeleteTarget) =
    match target with
    | Friend friend ->
        ("Delete Friend?", $"Are you sure you want to delete \"{friend.Name}\"? This cannot be undone.")
    | Tag tag ->
        ("Delete Tag?", $"Are you sure you want to delete \"{tag.Name}\"? This cannot be undone.")
    | Entry _ ->
        ("Delete Entry?", "Are you sure you want to delete this entry? This cannot be undone.")

let view (model: Model) (dispatch: Msg -> unit) =
    let (title, message) = getDeleteInfo model.Target

    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Cancel
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-sm"
        Children = [
            Html.div [
                prop.className "p-6"
                prop.children [
                    Html.h2 [
                        prop.className "text-xl font-bold mb-4"
                        prop.text title
                    ]
                    Html.p [
                        prop.className "text-base-content/70 mb-6"
                        prop.text message
                    ]
                    Html.div [
                        prop.className "flex gap-2 justify-end"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.disabled model.IsSubmitting
                                prop.onClick (fun _ -> dispatch Cancel)
                                prop.text "Cancel"
                            ]
                            Html.button [
                                prop.className "btn btn-error"
                                prop.disabled model.IsSubmitting
                                prop.onClick (fun _ -> dispatch Confirm)
                                prop.children [
                                    if model.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    Html.span [ prop.text "Delete" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    }
