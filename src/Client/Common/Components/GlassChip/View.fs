module Common.Components.GlassChip.View

open Feliz
open Common.Components.GlassChip.Types

let view (model: Model) =
    let roundedClass = if model.IsRounded then "rounded-full" else "rounded-lg"

    Html.div [
        prop.className $"flex items-center gap-2 px-4 py-2 glass {roundedClass} text-sm text-base-content/60"
        prop.children [
            match model.Icon with
            | Some icon ->
                Html.span [
                    prop.className "w-4 h-4"
                    prop.children [ icon ]
                ]
            | None -> ()
            Html.span [ prop.text model.Text ]
        ]
    ]

/// Simple text-only chip
let chip text = view (Model.create text)

/// Chip with icon
let chipWithIcon icon text = view (Model.create text |> Model.withIcon icon)

/// Squared chip
let squaredChip text = view (Model.create text |> Model.squared)
