module Common.Components.BackButton.View

open Feliz
open Browser.Dom
open Components.Icons

/// Standard back button using browser history
let view () =
    Html.button [
        prop.className "btn btn-ghost btn-sm gap-2"
        prop.onClick (fun _ -> window.history.back())
        prop.children [
            Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
            Html.span [ prop.text "Back" ]
        ]
    ]

/// Back button with custom label
let withLabel (label: string) =
    Html.button [
        prop.className "btn btn-ghost btn-sm gap-2"
        prop.onClick (fun _ -> window.history.back())
        prop.children [
            Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
            Html.span [ prop.text label ]
        ]
    ]

/// Back button that calls a custom action instead of browser history
let withAction (label: string) onClick =
    Html.button [
        prop.className "btn btn-ghost btn-sm gap-2"
        prop.onClick (fun _ -> onClick())
        prop.children [
            Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
            Html.span [ prop.text label ]
        ]
    ]
