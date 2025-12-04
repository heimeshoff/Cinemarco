module Common.Components.Tabs.View

open Feliz
open Types

module GlassPanel = Common.Components.GlassPanel.View

/// Render a tab bar
let tabBar (tabs: Tab list) (activeId: string) (onSelect: string -> unit) =
    Html.div [
        prop.className "flex gap-1"
        prop.children [
            for tab in tabs do
                let isActive = tab.Id = activeId
                Html.a [
                    prop.key tab.Id
                    prop.className (
                        "tab-button px-4 py-2 rounded-t-lg flex items-center gap-2 cursor-pointer transition-all " +
                        if isActive then "tab-button-active" else ""
                    )
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
            GlassPanel.standardWith "tab-content-panel" [ content ]
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
