module Common.Components.GlassButton.Types

open Feliz

type ButtonVariant =
    | Default
    | Success
    | SuccessActive
    | Danger
    | Primary
    | PrimaryActive

type Model = {
    Icon: ReactElement
    Tooltip: string
    Variant: ButtonVariant
    IsDisabled: bool
}

module Model =
    let create icon tooltip = {
        Icon = icon
        Tooltip = tooltip
        Variant = Default
        IsDisabled = false
    }

    let withVariant v model = { model with Variant = v }
    let disabled model = { model with IsDisabled = true }

    // Convenience constructors
    let success icon tooltip = create icon tooltip |> withVariant Success
    let successActive icon tooltip = create icon tooltip |> withVariant SuccessActive
    let danger icon tooltip = create icon tooltip |> withVariant Danger
    let primary icon tooltip = create icon tooltip |> withVariant Primary
    let primaryActive icon tooltip = create icon tooltip |> withVariant PrimaryActive
