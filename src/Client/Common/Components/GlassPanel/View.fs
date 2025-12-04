module Common.Components.GlassPanel.View

open Feliz
open Common.Components.GlassPanel.Types

let view (model: Model) (children: ReactElement list) =
    let variantClass =
        match model.Variant with
        | Standard -> "glass"
        | Strong -> "glass-strong"
        | Subtle -> "glass-subtle"

    Html.div [
        prop.className $"{variantClass} rounded-{model.Rounded} p-{model.Padding} {model.ExtraClasses}"
        prop.children children
    ]

/// Standard glass panel (most common)
let standard (children: ReactElement list) = view Model.standard children

/// Standard glass panel with extra classes
let standardWith extraClasses (children: ReactElement list) =
    view (Model.standard |> Model.withClasses extraClasses) children

/// Strong glass panel for emphasis
let strong (children: ReactElement list) = view Model.strong children

/// Subtle glass panel for less prominence
let subtle (children: ReactElement list) = view Model.subtle children
