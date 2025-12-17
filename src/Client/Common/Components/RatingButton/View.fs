module Common.Components.RatingButton.View

open Feliz
open Common.Components.RatingButton.Types
open Components.Icons

/// Rating options with their display properties
let ratingOptions : RatingOption list = [
    { Value = 0; Name = "Unrated"; Description = "No rating yet"; Icon = questionCircle; ColorClass = "text-base-content/50" }
    { Value = 1; Name = "Waste"; Description = "Waste of time"; Icon = thumbsDown; ColorClass = "text-red-400" }
    { Value = 2; Name = "Meh"; Description = "Didn't click, uninspiring"; Icon = minusCircle; ColorClass = "text-orange-400" }
    { Value = 3; Name = "Decent"; Description = "Watchable, even if not life-changing"; Icon = handOkay; ColorClass = "text-yellow-400" }
    { Value = 4; Name = "Entertaining"; Description = "Strong craft, enjoyable"; Icon = thumbsUp; ColorClass = "text-lime-400" }
    { Value = 5; Name = "Outstanding"; Description = "Absolutely brilliant, stays with you"; Icon = trophy; ColorClass = "text-amber-400" }
]

/// Get rating option for a given rating value
let getRatingOption (rating: int option) =
    let r = rating |> Option.defaultValue 0
    ratingOptions |> List.find (fun opt -> opt.Value = r)

/// Rating button with dropdown
let view (config: Config) =
    let currentOption = getRatingOption config.CurrentRating
    let btnClass = "detail-action-btn " + currentOption.ColorClass

    Html.div [
        prop.className "relative"
        prop.children [
            // Main button
            Html.div [
                prop.className "tooltip tooltip-bottom detail-tooltip"
                prop.custom ("data-tip", currentOption.Name)
                prop.children [
                    Html.button [
                        prop.className btnClass
                        prop.onClick (fun _ -> config.OnToggle())
                        prop.children [
                            Html.span [ prop.className "w-5 h-5"; prop.children [ currentOption.Icon ] ]
                        ]
                    ]
                ]
            ]

            // Dropdown
            if config.IsOpen then
                Html.div [
                    prop.className "absolute top-full left-0 mt-2 z-50 rating-dropdown"
                    prop.children [
                        // Rating options (1-5, skip 0)
                        for opt in ratingOptions do
                            if opt.Value > 0 then
                                let isActive = config.CurrentRating = Some opt.Value
                                let itemClass =
                                    if isActive then "rating-dropdown-item rating-dropdown-item-active"
                                    else "rating-dropdown-item"
                                let iconClass = "w-5 h-5 " + opt.ColorClass

                                Html.button [
                                    prop.className itemClass
                                    prop.onClick (fun _ -> config.OnSetRating opt.Value)
                                    prop.children [
                                        Html.span [ prop.className iconClass; prop.children [ opt.Icon ] ]
                                        Html.div [
                                            prop.className "flex flex-col items-start"
                                            prop.children [
                                                Html.span [ prop.className "font-medium"; prop.text opt.Name ]
                                                Html.span [ prop.className "text-xs text-base-content/50"; prop.text opt.Description ]
                                            ]
                                        ]
                                    ]
                                ]

                        // Clear option if currently rated
                        if config.CurrentRating.IsSome && config.CurrentRating.Value > 0 then
                            Html.button [
                                prop.className "rating-dropdown-item rating-dropdown-item-clear"
                                prop.onClick (fun _ -> config.OnSetRating 0)
                                prop.children [
                                    Html.span [ prop.className "w-5 h-5 text-base-content/40"; prop.children [ questionCircle ] ]
                                    Html.span [ prop.className "font-medium text-base-content/60"; prop.text "Clear rating" ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Convenience wrapper that takes individual parameters
let button currentRating isOpen onSetRating onToggle =
    view (Config.create currentRating isOpen onSetRating onToggle)
