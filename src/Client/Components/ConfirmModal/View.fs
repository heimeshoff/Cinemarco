module Components.ConfirmModal.View

open Feliz
open Shared.Domain
open Types

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

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not model.IsSubmitting then dispatch Cancel)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-sm p-6"
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
    ]
