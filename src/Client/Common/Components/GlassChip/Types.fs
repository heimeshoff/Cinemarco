module Common.Components.GlassChip.Types

open Feliz

type Model = {
    Icon: ReactElement option
    Text: string
    IsRounded: bool  // rounded-full vs rounded-lg
}

module Model =
    let create text = { Icon = None; Text = text; IsRounded = true }
    let withIcon icon model = { model with Icon = Some icon }
    let squared model = { model with IsRounded = false }
