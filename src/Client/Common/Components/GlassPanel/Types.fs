module Common.Components.GlassPanel.Types

type GlassVariant =
    | Standard   // .glass
    | Strong     // .glass-strong
    | Subtle     // .glass-subtle

type Model = {
    Variant: GlassVariant
    Rounded: string      // "xl", "lg", "md", etc.
    Padding: string      // "4", "6", etc.
    ExtraClasses: string
}

module Model =
    let standard = { Variant = Standard; Rounded = "xl"; Padding = "4"; ExtraClasses = "" }
    let strong = { Variant = Strong; Rounded = "xl"; Padding = "4"; ExtraClasses = "" }
    let subtle = { Variant = Subtle; Rounded = "lg"; Padding = "3"; ExtraClasses = "" }

    let withRounded r model = { model with Rounded = r }
    let withPadding p model = { model with Padding = p }
    let withClasses cls model = { model with ExtraClasses = cls }
