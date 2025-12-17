module Common.Components.CastCrewSection.Types

open Shared.Domain

/// Configuration for the CastCrewSection component
type Config = {
    /// The credits to display
    Credits: TmdbCredits
    /// Set of tracked person IDs
    TrackedPersonIds: Set<TmdbPersonId>
    /// Whether the full cast/crew section is expanded
    IsExpanded: bool
    /// Called when a contributor is clicked
    OnViewContributor: TmdbPersonId * string -> unit
    /// Called when the expand/collapse button is clicked
    OnToggleExpanded: unit -> unit
}

module Config =
    let create credits trackedIds isExpanded onViewContributor onToggleExpanded = {
        Credits = credits
        TrackedPersonIds = trackedIds
        IsExpanded = isExpanded
        OnViewContributor = onViewContributor
        OnToggleExpanded = onToggleExpanded
    }
