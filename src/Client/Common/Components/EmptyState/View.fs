module Common.Components.EmptyState.View

open Feliz
open Common.Components.EmptyState.Types

let view (model: Model) =
    Html.div [
        prop.className "text-center py-16"
        prop.children [
            Html.div [
                prop.className "w-20 h-20 mx-auto mb-6 rounded-full bg-base-200 flex items-center justify-center"
                prop.children [
                    Html.span [
                        prop.className (model.IconClassName |> Option.defaultValue "w-10 h-10 text-base-content/30")
                        prop.children [ model.Icon ]
                    ]
                ]
            ]
            Html.h3 [
                prop.className "text-xl font-semibold mb-2 text-base-content/70"
                prop.text model.Title
            ]
            match model.Description with
            | Some desc ->
                Html.p [
                    prop.className "text-base-content/50"
                    prop.text desc
                ]
            | None -> Html.none
        ]
    ]

/// Convenience function to create empty state
let inline empty icon title = view (Model.create icon title)

/// Convenience function with description
let inline emptyWithDesc icon title desc =
    view (Model.create icon title |> Model.withDescription desc)
