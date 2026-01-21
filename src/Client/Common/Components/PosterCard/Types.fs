module Common.Components.PosterCard.Types

open Feliz
open Shared.Domain

/// Rating badge configuration for display on poster
type RatingBadge = {
    Icon: ReactElement
    ColorClass: string
    Label: string
}

/// Badge shown at top-left (mutually exclusive)
type TopLeftBadge =
    | InLibraryBadge
    | RatingBadge of RatingBadge
    | CustomBadge of ReactElement

/// Overlay shown at bottom of poster
type BottomOverlay =
    | EpisodeBanner of text: string * isPrimary: bool
    | FinishedBanner
    | AbandonedBanner
    | RoleBanner of role: string
    | CustomOverlay of ReactElement

/// Poster card size configuration
type PosterSize =
    | Small   // w-32 sm:w-36 md:w-40 (horizontal scrolls, grids)
    | Normal  // w-40 sm:w-44 md:w-48 (larger displays)

/// Configuration for the PosterCard component
type Config = {
    /// URL to the poster image (local or TMDB CDN)
    PosterUrl: string option
    /// Title for alt text and accessibility
    Title: string
    /// Click handler
    OnClick: unit -> unit
    /// Optional badge shown at top-left (rating, "In Library", or custom)
    TopLeftBadge: TopLeftBadge option
    /// Optional overlay at bottom (episode banner, finished/abandoned, role)
    BottomOverlay: BottomOverlay option
    /// Grayscale the poster (for "In Library" items in search)
    IsGrayscale: bool
    /// Dim the poster (70% opacity for non-library items)
    IsDimmed: bool
    /// Media type icon for placeholder (film or tv)
    MediaType: MediaType option
    /// Show "In Library" overlay (full overlay, not badge)
    ShowInLibraryOverlay: bool
    /// Show media type badge (top-right)
    MediaTypeBadge: MediaType option
    /// Show add button on hover
    ShowAddButton: bool
    /// Size of the card (controls width)
    Size: PosterSize
}

module Config =
    /// Empty configuration with required fields
    let empty title onClick = {
        PosterUrl = None
        Title = title
        OnClick = onClick
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = None
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    /// Default configuration for library entries
    let libraryEntry posterUrl title onClick = {
        PosterUrl = posterUrl
        Title = title
        OnClick = onClick
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = None
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    /// Configuration for search results
    let searchResult posterUrl title mediaType onClick isInLibrary = {
        PosterUrl = posterUrl
        Title = title
        OnClick = onClick
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = isInLibrary
        IsDimmed = false
        MediaType = Some mediaType
        ShowInLibraryOverlay = isInLibrary
        MediaTypeBadge = Some mediaType
        ShowAddButton = not isInLibrary
        Size = Small
    }

    // Fluent builders
    let withPoster url (config: Config) = { config with PosterUrl = Some url }
    let withRating badge (config: Config) = { config with TopLeftBadge = Some (RatingBadge badge) }
    let withInLibraryBadge (config: Config) = { config with TopLeftBadge = Some InLibraryBadge }
    let withBottomOverlay overlay (config: Config) = { config with BottomOverlay = Some overlay }
    let withEpisodeBanner text isPrimary (config: Config) = { config with BottomOverlay = Some (EpisodeBanner (text, isPrimary)) }
    let withFinishedBanner (config: Config) = { config with BottomOverlay = Some FinishedBanner }
    let withAbandonedBanner (config: Config) = { config with BottomOverlay = Some AbandonedBanner }
    let withRoleBanner role (config: Config) = { config with BottomOverlay = Some (RoleBanner role) }
    let withGrayscale (config: Config) = { config with IsGrayscale = true }
    let withDimmed (config: Config) = { config with IsDimmed = true }
    let withMediaType mt (config: Config) = { config with MediaType = Some mt }
    let withMediaTypeBadge mt (config: Config) = { config with MediaTypeBadge = Some mt }
    let withAddButton (config: Config) = { config with ShowAddButton = true }
    let withSize size (config: Config) = { config with Size = size }
