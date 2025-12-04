module Common.Components.FilterChip.View

open Feliz
open Common.Components.FilterChip.Types

let view (model: Model) (onToggle: unit -> unit) =
    let (activeClass, inactiveClass) =
        match model.Color with
        | Primary ->
            ("bg-primary/20 text-primary border border-primary/30",
             "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70")
        | Secondary ->
            ("bg-secondary/20 text-secondary border border-secondary/30",
             "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70")
        | Custom activeClass ->
            (activeClass,
             "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70")

    Html.button [
        prop.type' "button"
        prop.className (
            "px-3 py-1 rounded-full text-xs font-medium transition-all flex items-center gap-1 " +
            if model.IsActive then activeClass else inactiveClass
        )
        prop.onClick (fun _ -> onToggle ())
        prop.children [
            match model.Icon with
            | Some icon ->
                Html.span [
                    prop.className "w-3 h-3"
                    prop.children [ icon ]
                ]
            | None -> ()
            Html.span [ prop.text model.Label ]
        ]
    ]

/// Simple filter chip
let chip label isActive onToggle =
    view (Model.create label isActive) onToggle

/// Filter chip with icon
let chipWithIcon icon label isActive onToggle =
    view (Model.create label isActive |> Model.withIcon icon) onToggle

/// Filter chip with custom active color
let chipWithColor color label isActive onToggle =
    view (Model.create label isActive |> Model.withColor color) onToggle
