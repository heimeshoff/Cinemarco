module Common.Components.RatingButton.Types

open Feliz

/// Rating option for display in dropdown
type RatingOption = {
    Value: int
    Name: string
    Description: string
    Icon: ReactElement
    ColorClass: string
}

/// Configuration for the RatingButton component
type Config = {
    /// Current rating value (0 = unrated, 1-5 = rated)
    CurrentRating: int option
    /// Whether the dropdown is currently open
    IsOpen: bool
    /// Called when rating is changed
    OnSetRating: int -> unit
    /// Called when dropdown is toggled
    OnToggle: unit -> unit
}

module Config =
    let create currentRating isOpen onSetRating onToggle = {
        CurrentRating = currentRating
        IsOpen = isOpen
        OnSetRating = onSetRating
        OnToggle = onToggle
    }
