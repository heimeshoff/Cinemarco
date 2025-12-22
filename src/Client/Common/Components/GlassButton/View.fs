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

    let sizeClass =
        match model.Size with
        | Small -> "detail-action-btn-sm"
        | Medium -> ""
        | Large -> "detail-action-btn-lg"

    let labelClass = if model.Label.IsSome then "detail-action-btn-with-label" else ""
    let emphasisClass = if model.Emphasis then "detail-action-btn-emphasis" else ""
    let extraClass = model.ExtraClass |> Option.defaultValue ""

    let fullClass = $"{variantClass} {sizeClass} {labelClass} {emphasisClass} {extraClass}".Trim()

    let iconSizeClass =
        match model.Size with
        | Small -> "w-4 h-4"
        | Medium -> "w-5 h-5"
        | Large -> "w-6 h-6"

    Html.div [
        prop.className "tooltip tooltip-bottom detail-tooltip"
        prop.custom ("data-tip", model.Tooltip)
        prop.children [
            Html.button [
                prop.className fullClass
                prop.disabled model.IsDisabled
                prop.onClick (fun _ -> onClick ())
                prop.children [
                    Html.span [ prop.className iconSizeClass; prop.children [ model.Icon ] ]
                    match model.Label with
                    | Some label -> Html.span [ prop.className "text-sm font-medium"; prop.text label ]
                    | None -> ()
                ]
            ]
        ]
    ]

/// Render without tooltip wrapper (for use in tight spaces)
let viewNoTooltip (model: Model) (onClick: unit -> unit) =
    let variantClass =
        match model.Variant with
        | Default -> "detail-action-btn"
        | Success -> "detail-action-btn detail-action-btn-success"
        | SuccessActive -> "detail-action-btn detail-action-btn-success-active"
        | Danger -> "detail-action-btn detail-action-btn-danger"
        | Primary -> "detail-action-btn detail-action-btn-primary"
        | PrimaryActive -> "detail-action-btn detail-action-btn-primary-active"

    let sizeClass =
        match model.Size with
        | Small -> "detail-action-btn-sm"
        | Medium -> ""
        | Large -> "detail-action-btn-lg"

    let labelClass = if model.Label.IsSome then "detail-action-btn-with-label" else ""
    let emphasisClass = if model.Emphasis then "detail-action-btn-emphasis" else ""
    let extraClass = model.ExtraClass |> Option.defaultValue ""

    let fullClass = $"{variantClass} {sizeClass} {labelClass} {emphasisClass} {extraClass}".Trim()

    let iconSizeClass =
        match model.Size with
        | Small -> "w-4 h-4"
        | Medium -> "w-5 h-5"
        | Large -> "w-6 h-6"

    Html.button [
        prop.className fullClass
        prop.disabled model.IsDisabled
        prop.title model.Tooltip
        prop.onClick (fun _ -> onClick ())
        prop.children [
            Html.span [ prop.className iconSizeClass; prop.children [ model.Icon ] ]
            match model.Label with
            | Some label -> Html.span [ prop.className "text-sm font-medium"; prop.text label ]
            | None -> ()
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

/// Button with label (emphasized, always visible glass)
let withLabel icon label tooltip onClick =
    view (Model.create icon tooltip |> Model.withLabel label |> Model.withEmphasis) onClick

/// Emphasized button (always visible glass background)
let emphasized icon tooltip onClick =
    view (Model.create icon tooltip |> Model.withEmphasis) onClick

/// Small button
let small icon tooltip onClick =
    view (Model.create icon tooltip |> Model.withSize Small) onClick

/// Small emphasized button
let smallEmphasis icon tooltip onClick =
    view (Model.create icon tooltip |> Model.withSize Small |> Model.withEmphasis) onClick

/// Success with label
let successWithLabel icon label tooltip onClick =
    view (Model.success icon tooltip |> Model.withLabel label |> Model.withEmphasis) onClick

/// Danger with label
let dangerWithLabel icon label tooltip onClick =
    view (Model.danger icon tooltip |> Model.withLabel label |> Model.withEmphasis) onClick

/// Primary with label
let primaryWithLabel icon label tooltip onClick =
    view (Model.primary icon tooltip |> Model.withLabel label |> Model.withEmphasis) onClick
