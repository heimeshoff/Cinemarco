module Common.Components.EmptyState.Types

open Feliz

type Model = {
    Icon: ReactElement
    Title: string
    Description: string option
    IconClassName: string option
}

module Model =
    let create icon title = {
        Icon = icon
        Title = title
        Description = None
        IconClassName = None
    }

    let withDescription desc model = { model with Description = Some desc }
    let withIconClass cls model = { model with IconClassName = Some cls }
