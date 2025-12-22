module Common.Components.GlassButton.Types

open Feliz

type ButtonVariant =
    | Default
    | Success
    | SuccessActive
    | Danger
    | Primary
    | PrimaryActive

type ButtonSize =
    | Small
    | Medium
    | Large

type Model = {
    Icon: ReactElement
    Tooltip: string
    Variant: ButtonVariant
    IsDisabled: bool
    Label: string option
    Size: ButtonSize
    Emphasis: bool
    ExtraClass: string option
}

module Model =
    let create icon tooltip = {
        Icon = icon
        Tooltip = tooltip
        Variant = Default
        IsDisabled = false
        Label = None
        Size = Medium
        Emphasis = false
        ExtraClass = None
    }

    let withVariant v model = { model with Variant = v }
    let withLabel label model = { model with Label = Some label }
    let withSize size model = { model with Size = size }
    let withEmphasis model = { model with Emphasis = true }
    let withExtraClass cls model = { model with ExtraClass = Some cls }
    let disabled model = { model with IsDisabled = true }

    // Convenience constructors
    let success icon tooltip = create icon tooltip |> withVariant Success
    let successActive icon tooltip = create icon tooltip |> withVariant SuccessActive
    let danger icon tooltip = create icon tooltip |> withVariant Danger
    let primary icon tooltip = create icon tooltip |> withVariant Primary
    let primaryActive icon tooltip = create icon tooltip |> withVariant PrimaryActive
