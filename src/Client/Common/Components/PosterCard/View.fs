module Common.Components.PosterCard.View

open Feliz
open Shared.Domain
open Common.Components.PosterCard.Types
open Components.Icons

/// Get local cached poster URL (for library items)
let getLocalPosterUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/posters/{filename}"
    | None -> ""

/// Render the rating badge (inner content)
let private renderRatingBadgeContent (badge: RatingBadge) =
    Html.div [
        prop.className "absolute top-2 left-2 px-2 py-1 rounded-md bg-black/60 backdrop-blur-sm flex items-center gap-2.5 opacity-0 group-hover:opacity-100 transition-opacity duration-200"
        prop.children [
            Html.span [
                prop.className ("w-4 h-4 flex-shrink-0 flex items-center justify-center " + badge.ColorClass)
                prop.children [ badge.Icon ]
            ]
            Html.span [
                prop.className ("text-xs font-medium leading-none " + badge.ColorClass)
                prop.text badge.Label
            ]
        ]
    ]

/// Render the "In Library" badge (top-left, green)
let private renderInLibraryBadge () =
    Html.div [
        prop.className "absolute top-2 left-2 px-2 py-1 rounded-md bg-success/90 backdrop-blur-sm text-success-content text-xs font-medium flex items-center gap-1"
        prop.children [
            Html.span [ prop.className "w-3 h-3"; prop.children [ checkCircleSolid ] ]
            Html.span [ prop.text "In Library" ]
        ]
    ]

/// Render the top-left badge based on type
let private renderTopLeftBadge (badge: TopLeftBadge) =
    match badge with
    | InLibraryBadge -> renderInLibraryBadge ()
    | RatingBadge badge -> renderRatingBadgeContent badge
    | CustomBadge element -> element

/// Render the bottom overlay based on type
let private renderBottomOverlay (overlay: BottomOverlay) =
    match overlay with
    | EpisodeBanner (text, isPrimary) ->
        let colorClass = if isPrimary then "from-primary/90 to-primary/70 text-primary-content" else "from-success/90 to-success/70 text-success-content"
        let prefix = if isPrimary then "Next: " else ""
        Html.div [
            prop.className $"absolute bottom-0 left-0 right-0 bg-gradient-to-t {colorClass} px-2 py-1.5 text-center"
            prop.children [
                Html.span [
                    prop.className "text-xs font-bold uppercase tracking-wide"
                    prop.text $"{prefix}{text}"
                ]
            ]
        ]
    | FinishedBanner ->
        Html.div [
            prop.className "absolute top-0 left-0 right-0 bg-gradient-to-b from-emerald-900/95 via-emerald-900/80 to-transparent pb-6 pt-2 px-2"
            prop.children [
                Html.div [
                    prop.className "flex items-center justify-center gap-1"
                    prop.children [
                        Html.span [
                            prop.className "w-3.5 h-3.5 text-emerald-300"
                            prop.children [ check ]
                        ]
                        Html.span [
                            prop.className "text-xs font-semibold text-emerald-200 uppercase tracking-wider"
                            prop.text "Finished"
                        ]
                    ]
                ]
            ]
        ]
    | AbandonedBanner ->
        Html.div [
            prop.className "absolute top-0 left-0 right-0 bg-gradient-to-b from-red-900/95 via-red-900/80 to-transparent pb-6 pt-2 px-2"
            prop.children [
                Html.div [
                    prop.className "flex items-center justify-center gap-1"
                    prop.children [
                        Html.span [
                            prop.className "w-3.5 h-3.5 text-red-300"
                            prop.children [ ban ]
                        ]
                        Html.span [
                            prop.className "text-xs font-semibold text-red-200 uppercase tracking-wider"
                            prop.text "Abandoned"
                        ]
                    ]
                ]
            ]
        ]
    | RoleBanner role ->
        Html.div [
            prop.className "absolute bottom-0 left-0 right-0 px-2 py-1.5 bg-gradient-to-t from-black/80 to-transparent"
            prop.children [
                Html.span [
                    prop.className "text-xs text-white/90 line-clamp-1"
                    prop.text role
                ]
            ]
        ]
    | CustomOverlay element -> element

/// Render the "In Library" overlay (full overlay, not badge)
let private renderInLibraryOverlay () =
    Html.div [
        prop.className "absolute inset-0 bg-base-300/70 flex items-center justify-center z-10"
        prop.children [
            Html.div [
                prop.className "px-2 py-1 bg-base-100/90 rounded-md text-xs font-medium text-base-content/70"
                prop.text "In Library"
            ]
        ]
    ]

/// Render the media type badge (top-right)
let private renderMediaTypeBadge (mediaType: MediaType) =
    let (icon, label) =
        match mediaType with
        | Movie -> (film, "Movie")
        | Series -> (tv, "Series")

    Html.div [
        prop.className "absolute top-2 right-2 px-2 py-1 glass rounded-md text-xs font-medium"
        prop.children [
            Html.span [
                prop.className "flex items-center gap-1"
                prop.children [
                    Html.span [
                        prop.className "w-3 h-3"
                        prop.children [ icon ]
                    ]
                    Html.span [ prop.text label ]
                ]
            ]
        ]
    ]

/// Render the add button overlay (for search results)
let private renderAddButtonOverlay () =
    Html.div [
        prop.className "poster-overlay flex items-center justify-center"
        prop.children [
            Html.div [
                prop.className "w-14 h-14 rounded-full bg-gradient-to-br from-primary to-secondary flex items-center justify-center transform scale-90 group-hover:scale-100 transition-transform shadow-lg"
                prop.children [
                    Html.span [
                        prop.className "w-6 h-6 text-white"
                        prop.children [ plus ]
                    ]
                ]
            ]
        ]
    ]

/// Render placeholder icon when no poster is available
let private renderPlaceholder (mediaType: MediaType option) =
    let icon =
        match mediaType with
        | Some Series -> tv
        | _ -> film

    Html.div [
        prop.className "w-full h-full flex items-center justify-center"
        prop.children [
            Html.span [
                prop.className "text-4xl text-base-content/20"
                prop.children [ icon ]
            ]
        ]
    ]

/// Main poster card view component
let view (config: Config) =
    let imageClass =
        let baseClass = "poster-image"
        let grayscaleClass = if config.IsGrayscale then " grayscale opacity-60" else ""
        let dimmedClass = if config.IsDimmed then " opacity-70" else ""
        baseClass + grayscaleClass + dimmedClass

    let sizeClass =
        match config.Size with
        | Mobile -> "w-full"
        | Small -> "w-32 sm:w-36 md:w-40"
        | Normal -> "w-40 sm:w-44 md:w-48"

    Html.div [
        prop.className $"poster-card group relative cursor-pointer {sizeClass}"
        prop.onClick (fun _ -> config.OnClick())
        prop.children [
            Html.div [
                prop.className "poster-image-container poster-shadow"
                prop.children [
                    // Poster image or placeholder
                    match config.PosterUrl with
                    | Some url when url <> "" ->
                        Html.img [
                            prop.src url
                            prop.alt config.Title
                            prop.className imageClass
                            prop.custom ("loading", "lazy")
                            prop.custom ("crossorigin", "anonymous")
                        ]
                    | _ -> renderPlaceholder config.MediaType

                    // Top-left badge (rating, "In Library" badge, or custom)
                    match config.TopLeftBadge with
                    | Some badge -> renderTopLeftBadge badge
                    | None -> Html.none

                    // Media type badge (top-right)
                    match config.MediaTypeBadge with
                    | Some mt -> renderMediaTypeBadge mt
                    | None -> Html.none

                    // "In Library" overlay (full overlay)
                    if config.ShowInLibraryOverlay then
                        renderInLibraryOverlay ()

                    // Add button overlay (for search results)
                    if config.ShowAddButton then
                        renderAddButtonOverlay ()

                    // Bottom overlay (episode banner, finished/abandoned, role)
                    match config.BottomOverlay with
                    | Some overlay -> renderBottomOverlay overlay
                    | None -> Html.none

                    // Shine effect (always present)
                    Html.div [ prop.className "poster-shine" ]
                ]
            ]
        ]
    ]

/// Create rating badge from PersonalRating
let ratingToBadge (rating: PersonalRating) : RatingBadge =
    match rating with
    | Outstanding -> { Icon = trophy; ColorClass = "text-amber-400"; Label = "Outstanding" }
    | Entertaining -> { Icon = thumbsUp; ColorClass = "text-lime-400"; Label = "Entertaining" }
    | Decent -> { Icon = handOkay; ColorClass = "text-yellow-400"; Label = "Decent" }
    | Meh -> { Icon = minusCircle; ColorClass = "text-orange-400"; Label = "Meh" }
    | Waste -> { Icon = thumbsDown; ColorClass = "text-red-400"; Label = "Waste" }

/// Library entry card - convenient wrapper for library items
let libraryEntry (entry: LibraryEntry) (onViewDetail: EntryId -> bool -> unit) =
    let (title, posterPath, isMovie) =
        match entry.Media with
        | LibraryMovie m -> (m.Title, m.PosterPath, true)
        | LibrarySeries s -> (s.Name, s.PosterPath, false)

    let topLeftBadge =
        entry.PersonalRating
        |> Option.map (ratingToBadge >> RatingBadge)

    // Determine bottom overlay - only for series (movies don't need Finished/Abandoned badges)
    let bottomOverlay =
        match entry.Media with
        | LibrarySeries _ ->
            match entry.WatchStatus with
            | Completed -> Some FinishedBanner
            | Abandoned _ -> Some AbandonedBanner
            | _ -> None
        | LibraryMovie _ -> None

    let config = {
        PosterUrl = posterPath |> Option.map (fun p -> getLocalPosterUrl (Some p))
        Title = title
        OnClick = fun () -> onViewDetail entry.Id isMovie
        TopLeftBadge = topLeftBadge
        BottomOverlay = bottomOverlay
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some (if isMovie then Movie else Series)
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    view config

/// Library entry card with title below - for search results and grids
let libraryEntryWithTitle (entry: LibraryEntry) (onClick: unit -> unit) =
    let (title, posterPath) =
        match entry.Media with
        | LibraryMovie m -> (m.Title, m.PosterPath)
        | LibrarySeries s -> (s.Name, s.PosterPath)

    let topLeftBadge =
        entry.PersonalRating
        |> Option.map (ratingToBadge >> RatingBadge)

    // Bottom overlay only for series (movies don't need Finished/Abandoned badges)
    let bottomOverlay =
        match entry.Media with
        | LibrarySeries _ ->
            match entry.WatchStatus with
            | Completed -> Some FinishedBanner
            | Abandoned _ -> Some AbandonedBanner
            | _ -> None
        | LibraryMovie _ -> None

    let config = {
        PosterUrl = posterPath |> Option.map (fun p -> getLocalPosterUrl (Some p))
        Title = title
        OnClick = onClick
        TopLeftBadge = topLeftBadge
        BottomOverlay = bottomOverlay
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some (match entry.Media with LibraryMovie _ -> Movie | LibrarySeries _ -> Series)
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    Html.div [
        prop.children [
            view config
            Html.p [
                prop.className "text-xs mt-3 truncate text-base-content/70 group-hover:text-primary transition-colors"
                prop.text title
            ]
        ]
    ]

/// Series card with next episode indicator
let seriesWithEpisode (entry: LibraryEntry) (progress: WatchProgress) (onClick: unit -> unit) =
    let (name, posterPath) =
        match entry.Media with
        | LibrarySeries s -> (s.Name, s.PosterPath)
        | LibraryMovie m -> (m.Title, m.PosterPath)

    let episodeText =
        match progress.CurrentSeason, progress.CurrentEpisode with
        | Some s, Some e -> Some $"S{s} E{e}"
        | Some s, None -> Some $"Season {s}"
        | None, Some e -> Some $"Episode {e}"
        | None, None -> None

    let overlay = episodeText |> Option.map (fun t -> EpisodeBanner (t, true))

    let config = {
        PosterUrl = posterPath |> Option.map (fun p -> getLocalPosterUrl (Some p))
        Title = name
        OnClick = onClick
        TopLeftBadge = None
        BottomOverlay = overlay
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some Series
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Small
    }

    view config

/// Filmography work card - for contributor filmography
let filmographyCard (posterUrl: string option) (title: string) (role: string) (isInLibrary: bool) (onClick: unit -> unit) =
    let config = {
        PosterUrl = posterUrl
        Title = title
        OnClick = onClick
        TopLeftBadge = if isInLibrary then Some InLibraryBadge else None
        BottomOverlay = Some (RoleBanner role)
        IsGrayscale = false
        IsDimmed = not isInLibrary
        MediaType = None
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = not isInLibrary
        Size = Small
    }

    view config
