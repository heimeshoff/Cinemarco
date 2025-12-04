module Common.Components.FilterChip.Types

open Feliz

type ChipColor =
    | Primary
    | Secondary
    | Custom of activeClass: string

type Model = {
    Label: string
    IsActive: bool
    Icon: ReactElement option
    Color: ChipColor
}

module Model =
    let create label isActive = {
        Label = label
        IsActive = isActive
        Icon = None
        Color = Primary
    }

    let withIcon icon model = { model with Icon = Some icon }
    let withColor color model = { model with Color = color }
