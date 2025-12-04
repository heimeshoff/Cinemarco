module Common.Components.Tabs.View

open Feliz
open Types

module GlassPanel = Common.Components.GlassPanel.View

/// Render a tab bar with glassmorphism styling
let tabBar (tabs: Tab list) (activeId: string) (onSelect: string -> unit) =
    Html.div [
        prop.className "tabs tabs-boxed glass mb-4"
        prop.children [
            for tab in tabs do
                Html.a [
                    prop.key tab.Id
                    prop.className ("tab gap-2 " + if tab.Id = activeId then "tab-active" else "")
                    prop.onClick (fun _ -> onSelect tab.Id)
                    prop.children [
                        match tab.Icon with
                        | Some icon -> Html.span [ prop.className "w-4 h-4"; prop.children [ icon ] ]
                        | None -> Html.none
                        Html.span [ prop.text tab.Label ]
                    ]
                ]
        ]
    ]

/// Render tabs with content wrapped in GlassPanel
let view (tabs: Tab list) (activeId: string) (onSelect: string -> unit) (content: ReactElement) =
    Html.div [
        prop.children [
            tabBar tabs activeId onSelect
            GlassPanel.standard [ content ]
        ]
    ]

/// Render tabs with content (no GlassPanel wrapper - for when content manages its own panels)
let viewRaw (tabs: Tab list) (activeId: string) (onSelect: string -> unit) (content: ReactElement) =
    Html.div [
        prop.children [
            tabBar tabs activeId onSelect
            content
        ]
    ]
