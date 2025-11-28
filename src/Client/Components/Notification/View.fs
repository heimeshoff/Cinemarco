module Components.Notification.View

open Feliz
open Types
open Components.Icons

let view (model: Model) (dispatch: Msg -> unit) =
    if not model.IsVisible then
        Html.none
    else
        Html.div [
            prop.className "toast-container"
            prop.children [
                Html.div [
                    prop.className (
                        "toast " +
                        if model.IsSuccess then "toast-success" else "toast-error"
                    )
                    prop.children [
                        Html.span [
                            prop.className (
                                "w-5 h-5 " +
                                if model.IsSuccess then "text-success" else "text-error"
                            )
                            prop.children [
                                if model.IsSuccess then success else error
                            ]
                        ]
                        Html.span [
                            prop.className "flex-1 text-sm"
                            prop.text model.Message
                        ]
                        Html.button [
                            prop.className "w-6 h-6 rounded-full hover:bg-white/10 flex items-center justify-center transition-colors"
                            prop.onClick (fun _ -> dispatch Hide)
                            prop.children [
                                Html.span [
                                    prop.className "w-4 h-4 text-base-content/50"
                                    prop.children [ close ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
