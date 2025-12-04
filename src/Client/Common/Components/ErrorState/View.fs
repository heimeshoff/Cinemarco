module Common.Components.ErrorState.View

open Feliz
open Common.Components.ErrorState.Types
open Components.Icons

let view (model: Model) =
    Html.div [
        prop.className "text-center py-12"
        prop.children [
            Html.span [
                prop.className "w-12 h-12 mx-auto mb-4 text-error/50 block"
                prop.children [ error ]
            ]
            Html.p [
                prop.className "text-error"
                prop.text (
                    match model.Context with
                    | Some ctx -> $"Error {ctx}: {model.Message}"
                    | None -> model.Message
                )
            ]
        ]
    ]

/// Convenience function to create error view directly
let inline error msg = view (Model.create msg)

/// Convenience function with context
let inline errorWithContext ctx msg =
    view (Model.create msg |> Model.withContext ctx)
