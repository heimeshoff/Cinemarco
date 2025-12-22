module Common.Components.PosterCard.Types

open Feliz
open Shared.Domain

/// Rating badge configuration for display on poster
type RatingBadge = {
    Icon: ReactElement
    ColorClass: string
    Label: string
}

/// Status overlay configuration (for "Finished"/"Abandoned" badges at top, episode banners at bottom)
type StatusOverlay =
    | NextEpisode of text: string
    | FinishedBadge
    | AbandonedBadge
    | Custom of ReactElement

/// Configuration for the PosterCard component
type Config = {
    /// URL to the poster image (local or TMDB CDN)
    PosterUrl: string option
    /// Title for alt text and accessibility
    Title: string
    /// Click handler
    OnClick: unit -> unit
    /// Optional rating badge shown on hover (top-left)
    RatingBadge: RatingBadge option
    /// Optional status overlay (Finished/Abandoned at top, episodes at bottom)
    StatusOverlay: StatusOverlay option
    /// Grayscale the poster (for "In Library" items in search)
    IsGrayscale: bool
    /// Media type icon for placeholder (film or tv)
    MediaType: MediaType option
    /// Show "In Library" overlay
    ShowInLibraryOverlay: bool
    /// Show media type badge (top-right)
    MediaTypeBadge: MediaType option
    /// Show add button on hover
    ShowAddButton: bool
}

module Config =
    /// Default configuration for library entries
    let libraryEntry posterUrl title onClick = {
        PosterUrl = posterUrl
        Title = title
        OnClick = onClick
        RatingBadge = None
        StatusOverlay = None
        IsGrayscale = false
        MediaType = None
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
    }

    /// Configuration for search results
    let searchResult posterUrl title mediaType onClick isInLibrary = {
        PosterUrl = posterUrl
        Title = title
        OnClick = onClick
        RatingBadge = None
        StatusOverlay = None
        IsGrayscale = isInLibrary
        MediaType = Some mediaType
        ShowInLibraryOverlay = isInLibrary
        MediaTypeBadge = Some mediaType
        ShowAddButton = not isInLibrary
    }

    let withRating badge config = { config with RatingBadge = Some badge }
    let withStatusOverlay overlay config = { config with StatusOverlay = Some overlay }
    let withGrayscale config = { config with IsGrayscale = true }
