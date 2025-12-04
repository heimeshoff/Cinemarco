module Common.Components.GlassButton.View

open Feliz
open Common.Components.GlassButton.Types

let view (model: Model) (onClick: unit -> unit) =
    let variantClass =
        match model.Variant with
        | Default -> "detail-action-btn"
        | Success -> "detail-action-btn detail-action-btn-success"
        | SuccessActive -> "detail-action-btn detail-action-btn-success-active"
        | Danger -> "detail-action-btn detail-action-btn-danger"
        | Primary -> "detail-action-btn detail-action-btn-primary"
        | PrimaryActive -> "detail-action-btn detail-action-btn-primary-active"

    Html.div [
        prop.className "tooltip tooltip-bottom detail-tooltip"
        prop.custom ("data-tip", model.Tooltip)
        prop.children [
            Html.button [
                prop.className variantClass
                prop.disabled model.IsDisabled
                prop.onClick (fun _ -> onClick ())
                prop.children [
                    Html.span [ prop.className "w-5 h-5"; prop.children [ model.Icon ] ]
                ]
            ]
        ]
    ]

/// Default button
let button icon tooltip onClick =
    view (Model.create icon tooltip) onClick

/// Success button (green tint)
let success icon tooltip onClick =
    view (Model.success icon tooltip) onClick

/// Active success button (solid green)
let successActive icon tooltip onClick =
    view (Model.successActive icon tooltip) onClick

/// Danger button (red tint)
let danger icon tooltip onClick =
    view (Model.danger icon tooltip) onClick

/// Primary button (primary color tint)
let primary icon tooltip onClick =
    view (Model.primary icon tooltip) onClick

/// Active primary button (solid primary)
let primaryActive icon tooltip onClick =
    view (Model.primaryActive icon tooltip) onClick

/// Disabled button
let disabled icon tooltip =
    view (Model.create icon tooltip |> Model.disabled) ignore
