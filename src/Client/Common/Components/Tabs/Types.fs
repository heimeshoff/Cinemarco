module Common.Components.Tabs.Types

open Feliz

/// Represents a single tab
type Tab = {
    Id: string
    Label: string
    Icon: ReactElement option
}
