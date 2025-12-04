module Common.Components.SectionHeader.View

open Feliz
open Common.Components.SectionHeader.Types

let view (model: Model) (onAction: unit -> unit) =
    let sizeClass =
        match model.Size with
        | Large -> "text-2xl"
        | Medium -> "text-xl"
        | Small -> "text-lg"

    Html.div [
        prop.className "flex justify-between items-center"
        prop.children [
            Html.h3 [
                prop.className $"{sizeClass} font-bold"
                prop.text model.Title
            ]
            match model.Action with
            | NoAction -> Html.none
            | Link (text, icon) ->
                Html.button [
                    prop.className "flex items-center gap-2 text-sm text-primary hover:underline"
                    prop.onClick (fun _ -> onAction ())
                    prop.children [
                        Html.span [ prop.text text ]
                        match icon with
                        | Some i -> Html.span [ prop.className "w-4 h-4"; prop.children [ i ] ]
                        | None -> ()
                    ]
                ]
            | Button (text, icon) ->
                Html.button [
                    prop.className "btn btn-sm btn-ghost"
                    prop.onClick (fun _ -> onAction ())
                    prop.children [
                        match icon with
                        | Some i -> Html.span [ prop.className "w-4 h-4"; prop.children [ i ] ]
                        | None -> ()
                        Html.span [ prop.text text ]
                    ]
                ]
        ]
    ]

/// Simple title-only header
let title text = view (Model.create text) ignore

/// Title with size
let titleLarge text = view (Model.create text |> Model.large) ignore
let titleSmall text = view (Model.create text |> Model.small) ignore

/// Title with link action
let withLink title linkText icon onAction =
    view (Model.create title |> Model.withLink linkText icon) onAction

/// Title with button action
let withButton title btnText icon onAction =
    view (Model.create title |> Model.withButton btnText icon) onAction
