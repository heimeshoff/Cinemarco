module Pages.Styleguide.View

open Feliz
open Common.Components.PosterCard.Types
open Components.Icons
open Types

module PosterCard = Common.Components.PosterCard.View
module GlassPanel = Common.Components.GlassPanel.View
module SectionHeader = Common.Components.SectionHeader.View

/// Sample poster URL for demonstrations
let private samplePosterUrl = Some "/images/posters/sample-poster.jpg"
let private tmdbPosterUrl = Some "https://image.tmdb.org/t/p/w185/6FfCtAuVAW8XJjZ7eWeLibRLWTw.jpg"

/// Render a single card variation with description
let private cardExample (title: string) (description: string) (config: Config) =
    Html.div [
        prop.className "flex flex-col items-center"
        prop.children [
            PosterCard.view config
            Html.div [
                prop.className "mt-3 text-center max-w-[10rem]"
                prop.children [
                    Html.p [
                        prop.className "font-semibold text-sm text-base-content"
                        prop.text title
                    ]
                    Html.p [
                        prop.className "text-xs text-base-content/60 mt-1"
                        prop.text description
                    ]
                ]
            ]
        ]
    ]

/// Section wrapper
let private section (title: string) (subtitle: string option) (children: ReactElement list) =
    GlassPanel.standard [
        Html.div [
            prop.className "space-y-4"
            prop.children [
                Html.div [
                    prop.children [
                        Html.h2 [
                            prop.className "text-xl font-bold text-base-content"
                            prop.text title
                        ]
                        match subtitle with
                        | Some sub ->
                            Html.p [
                                prop.className "text-sm text-base-content/60 mt-1"
                                prop.text sub
                            ]
                        | None -> Html.none
                    ]
                ]
                Html.div [
                    prop.className "flex flex-wrap gap-6"
                    prop.children children
                ]
            ]
        ]
    ]

/// Basic configurations section
let private basicSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Movie"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Movie
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Basic Poster Cards" (Some "Foundation card styles without overlays or badges") [
        cardExample
            "Default"
            "Standard poster card with hover effects"
            baseConfig

        cardExample
            "No Poster"
            "Placeholder shown when poster is unavailable"
            { baseConfig with PosterUrl = None }

        cardExample
            "Grayscale"
            "Used for items already in library (search results)"
            { baseConfig with IsGrayscale = true }

        cardExample
            "Dimmed"
            "70% opacity for non-library items (filmography)"
            { baseConfig with IsDimmed = true }
    ]

/// Size variations section
let private sizesSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Movie"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Movie
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Card Sizes" (Some "PosterCard handles its own width - no parent wrapper needed") [
        cardExample
            "Small (Default)"
            "w-32 sm:w-36 md:w-40 - For horizontal scrolls and grids"
            baseConfig

        cardExample
            "Normal"
            "w-40 sm:w-44 md:w-48 - For larger displays"
            { baseConfig with Size = Normal }
    ]

/// Rating badges section
let private ratingBadgesSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Movie"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Movie
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    let outstanding = { Icon = trophy; ColorClass = "text-amber-400"; Label = "Outstanding" }
    let entertaining = { Icon = thumbsUp; ColorClass = "text-lime-400"; Label = "Entertaining" }
    let decent = { Icon = handOkay; ColorClass = "text-yellow-400"; Label = "Decent" }
    let meh = { Icon = minusCircle; ColorClass = "text-orange-400"; Label = "Meh" }
    let waste = { Icon = thumbsDown; ColorClass = "text-red-400"; Label = "Waste" }

    section "Rating Badges" (Some "Top-left badges appear on hover to show personal rating") [
        cardExample
            "Outstanding"
            "Gold trophy badge for top-rated content"
            { baseConfig with TopLeftBadge = Some (RatingBadge outstanding) }

        cardExample
            "Entertaining"
            "Green thumbs-up for enjoyable watches"
            { baseConfig with TopLeftBadge = Some (RatingBadge entertaining) }

        cardExample
            "Decent"
            "Yellow OK hand for satisfactory content"
            { baseConfig with TopLeftBadge = Some (RatingBadge decent) }

        cardExample
            "Meh"
            "Orange circle for mediocre content"
            { baseConfig with TopLeftBadge = Some (RatingBadge meh) }

        cardExample
            "Waste"
            "Red thumbs-down for regrettable watches"
            { baseConfig with TopLeftBadge = Some (RatingBadge waste) }
    ]

/// Top-left badges section
let private topLeftBadgesSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Movie"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Movie
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Top-Left Badges" (Some "Persistent badges for library/ownership status") [
        cardExample
            "In Library"
            "Green badge shown on contributor filmography cards"
            { baseConfig with TopLeftBadge = Some InLibraryBadge }

        cardExample
            "Custom Badge"
            "Supports custom ReactElement badges"
            { baseConfig with
                TopLeftBadge = Some (CustomBadge (
                    Html.div [
                        prop.className "absolute top-2 left-2 px-2 py-1 rounded-md bg-purple-600/90 backdrop-blur-sm text-white text-xs font-medium"
                        prop.text "Custom"
                    ]
                ))
            }
    ]

/// Bottom overlays section
let private bottomOverlaysSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Series"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Series
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Bottom Overlays" (Some "Contextual information displayed at the bottom of the poster") [
        cardExample
            "Next Episode"
            "Primary color banner for upcoming episode"
            { baseConfig with BottomOverlay = Some (EpisodeBanner ("S2 E5", true)) }

        cardExample
            "Last Watched"
            "Success color for recently watched episode"
            { baseConfig with BottomOverlay = Some (EpisodeBanner ("S1 E8", false)) }

        cardExample
            "Role Banner"
            "Shows contributor role in filmography"
            { baseConfig with BottomOverlay = Some (RoleBanner "Director") }

        cardExample
            "Actor Role"
            "Actor with character name"
            { baseConfig with BottomOverlay = Some (RoleBanner "Actor (John Smith)") }
    ]

/// Status badges section (top overlay)
let private statusBadgesSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample Series"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Series
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Status Badges" (Some "Top overlay banners for series completion status") [
        cardExample
            "Finished"
            "Emerald gradient for completed series"
            { baseConfig with BottomOverlay = Some FinishedBanner }

        cardExample
            "Abandoned"
            "Red gradient for dropped series"
            { baseConfig with BottomOverlay = Some AbandonedBanner }
    ]

/// Media type badges section
let private mediaTypeBadgesSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample"
        OnClick = fun () -> ()
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

    section "Media Type Badges" (Some "Top-right badge showing content type (for search results)") [
        cardExample
            "Movie Badge"
            "Film icon with 'Movie' label"
            { baseConfig with MediaTypeBadge = Some Shared.Domain.MediaType.Movie }

        cardExample
            "Series Badge"
            "TV icon with 'Series' label"
            { baseConfig with MediaTypeBadge = Some Shared.Domain.MediaType.Series }
    ]

/// Interactive overlays section
let private interactiveSection () =
    let baseConfig = {
        PosterUrl = tmdbPosterUrl
        Title = "Sample"
        OnClick = fun () -> ()
        TopLeftBadge = None
        BottomOverlay = None
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Shared.Domain.MediaType.Movie
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    section "Interactive Overlays" (Some "Hover-triggered overlays for user actions") [
        cardExample
            "Add Button"
            "Gradient circle with plus icon on hover"
            { baseConfig with ShowAddButton = true; IsDimmed = true }

        cardExample
            "In Library Overlay"
            "Full overlay showing item is already in library"
            { baseConfig with ShowInLibraryOverlay = true; IsGrayscale = true }
    ]

/// Filmography card section
let private filmographySection () =
    section "Filmography Cards" (Some "Combined configurations for contributor filmography view") [
        Html.div [
            prop.className "flex flex-col items-center"
            prop.children [
                PosterCard.filmographyCard
                    tmdbPosterUrl
                    "The Matrix"
                    "Director"
                    true
                    (fun () -> ())
                Html.div [
                    prop.className "mt-3 text-center max-w-[10rem]"
                    prop.children [
                        Html.p [
                            prop.className "font-semibold text-sm text-base-content"
                            prop.text "In Library"
                        ]
                        Html.p [
                            prop.className "text-xs text-base-content/60 mt-1"
                            prop.text "Green badge, role banner, full opacity"
                        ]
                    ]
                ]
            ]
        ]

        Html.div [
            prop.className "flex flex-col items-center"
            prop.children [
                PosterCard.filmographyCard
                    tmdbPosterUrl
                    "The Matrix"
                    "Actor (Neo)"
                    false
                    (fun () -> ())
                Html.div [
                    prop.className "mt-3 text-center max-w-[10rem]"
                    prop.children [
                        Html.p [
                            prop.className "font-semibold text-sm text-base-content"
                            prop.text "Not In Library"
                        ]
                        Html.p [
                            prop.className "text-xs text-base-content/60 mt-1"
                            prop.text "Dimmed, add button, role with character"
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Configuration API section
let private apiSection () =
    GlassPanel.standard [
        Html.div [
            prop.className "space-y-4"
            prop.children [
                Html.h2 [
                    prop.className "text-xl font-bold text-base-content"
                    prop.text "Configuration API"
                ]
                Html.p [
                    prop.className "text-sm text-base-content/60"
                    prop.text "PosterCard supports fluent configuration builders:"
                ]
                Html.pre [
                    prop.className "bg-base-300 rounded-lg p-4 text-xs overflow-x-auto"
                    prop.text """// Using Config module fluent builders
Config.empty "Title" onClick
|> Config.withPoster posterUrl
|> Config.withRating outstandingBadge
|> Config.withFinishedBanner
|> Config.withMediaType Movie
|> Config.withSize Normal  // Small (default) or Normal
|> PosterCard.view

// Or construct directly
let config = {
    PosterUrl = Some url
    Title = "Title"
    OnClick = onClick
    TopLeftBadge = Some (RatingBadge badge)
    BottomOverlay = Some FinishedBanner
    IsGrayscale = false
    IsDimmed = false
    MediaType = Some Movie
    ShowInLibraryOverlay = false
    MediaTypeBadge = None
    ShowAddButton = false
    Size = Small  // Small: w-32 sm:w-36 md:w-40, Normal: w-40 sm:w-44 md:w-48
}"""
                ]
            ]
        ]
    ]

/// Types reference section
let private typesSection () =
    GlassPanel.standard [
        Html.div [
            prop.className "space-y-4"
            prop.children [
                Html.h2 [
                    prop.className "text-xl font-bold text-base-content"
                    prop.text "Type Definitions"
                ]
                Html.pre [
                    prop.className "bg-base-300 rounded-lg p-4 text-xs overflow-x-auto"
                    prop.text """/// Poster card size configuration
type PosterSize =
    | Small   // w-32 sm:w-36 md:w-40 (horizontal scrolls, grids)
    | Normal  // w-40 sm:w-44 md:w-48 (larger displays)

/// Badge shown at top-left (mutually exclusive)
type TopLeftBadge =
    | InLibraryBadge                     // Green "In Library" badge
    | RatingBadge of RatingBadge         // Personal rating badge (hover)
    | CustomBadge of ReactElement        // Custom badge element

/// Overlay shown at bottom of poster
type BottomOverlay =
    | EpisodeBanner of text: string * isPrimary: bool
    | FinishedBanner                     // Emerald top gradient
    | AbandonedBanner                    // Red top gradient
    | RoleBanner of role: string         // Bottom gradient with role
    | CustomOverlay of ReactElement      // Custom overlay"""
                ]
            ]
        ]
    ]

let view (_model: Model) (_dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6 pb-8"
        prop.children [
            // Page header
            Html.div [
                prop.className "mb-8"
                prop.children [
                    Html.h1 [
                        prop.className "text-3xl font-bold text-base-content"
                        prop.text "PosterCard Styleguide"
                    ]
                    Html.p [
                        prop.className "text-base-content/60 mt-2"
                        prop.text "A comprehensive showcase of all PosterCard component variations and configurations."
                    ]
                ]
            ]

            // Component sections
            basicSection ()
            sizesSection ()
            ratingBadgesSection ()
            topLeftBadgesSection ()
            bottomOverlaysSection ()
            statusBadgesSection ()
            mediaTypeBadgesSection ()
            interactiveSection ()
            filmographySection ()
            apiSection ()
            typesSection ()
        ]
    ]
