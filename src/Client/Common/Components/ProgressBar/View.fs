module Common.Components.ProgressBar.View

open Feliz

/// Simple progress bar with current/total display
let simple (current: int) (total: int) =
    let percentage = if total > 0 then float current / float total * 100.0 else 0.0
    Html.div [
        prop.className "w-full"
        prop.children [
            Html.div [
                prop.className "flex justify-between text-sm mb-1"
                prop.children [
                    Html.span [ prop.text $"{current} / {total}" ]
                    Html.span [ prop.text $"{int percentage}%%" ]
                ]
            ]
            Html.div [
                prop.className "w-full bg-base-300 rounded-full h-2"
                prop.children [
                    Html.div [
                        prop.className "bg-primary h-2 rounded-full transition-all duration-300"
                        prop.style [ style.width (length.percent (int percentage)) ]
                    ]
                ]
            ]
        ]
    ]

/// Progress bar with "X of Y seen" label and gradient styling
let withSeenLabel (seen: int) (total: int) =
    let percentage = if total > 0 then (float seen / float total) * 100.0 else 0.0
    let percentText = $"{int (System.Math.Round(percentage))}%%"

    Html.div [
        prop.className "space-y-2"
        prop.children [
            Html.div [
                prop.className "flex justify-between items-center text-sm"
                prop.children [
                    Html.span [
                        prop.className "text-base-content/70"
                        prop.text $"{seen} of {total} seen"
                    ]
                    Html.span [
                        prop.className "font-semibold text-primary"
                        prop.text percentText
                    ]
                ]
            ]
            Html.div [
                prop.className "h-3 bg-base-300 rounded-full overflow-hidden"
                prop.children [
                    Html.div [
                        prop.className "h-full bg-gradient-to-r from-primary to-secondary transition-all duration-500 ease-out rounded-full"
                        prop.style [ style.width (length.percent percentage) ]
                    ]
                ]
            ]
        ]
    ]

/// Progress bar with "X of Y completed" label and optional in-progress count
let withCompletedLabel (completed: int) (total: int) (inProgress: int option) (percentage: float) =
    Html.div [
        prop.className "bg-base-200 rounded-lg p-4 mb-6"
        prop.children [
            Html.div [
                prop.className "flex justify-between items-center mb-2"
                prop.children [
                    Html.span [
                        prop.className "text-sm font-medium"
                        prop.text $"{completed} of {total} completed"
                    ]
                    Html.span [
                        prop.className "text-sm text-base-content/60"
                        prop.text $"{int percentage}%%"
                    ]
                ]
            ]
            Html.progress [
                prop.className "progress progress-primary w-full"
                prop.value (int percentage)
                prop.max 100
            ]
            match inProgress with
            | Some count when count > 0 ->
                Html.p [
                    prop.className "text-xs text-base-content/50 mt-1"
                    prop.text $"{count} in progress"
                ]
            | _ -> Html.none
        ]
    ]

/// Minimal progress bar (no text, just the bar)
let minimal (percentage: float) =
    Html.div [
        prop.className "w-full bg-base-300 rounded-full h-2"
        prop.children [
            Html.div [
                prop.className "bg-primary h-2 rounded-full transition-all duration-300"
                prop.style [ style.width (length.percent (int percentage)) ]
            ]
        ]
    ]
