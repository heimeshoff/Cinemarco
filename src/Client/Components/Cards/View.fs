module Components.Cards.View

open Feliz
open Shared.Domain
open Components.Icons
open Browser.Types
open Fable.Core.JsInterop

/// TMDB image base URL (for search results)
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Handle mouse move for shine effect
let handlePosterMouseMove (e: MouseEvent) =
    let target = e.currentTarget :?> HTMLElement
    let shine = target.querySelector(".poster-shine") :?> HTMLElement

    // Move shine based on mouse position
    if not (isNull shine) then
        let rect = target.getBoundingClientRect()
        let x = (e.clientX - rect.left) / rect.width
        let y = (e.clientY - rect.top) / rect.height
        let shineX = x * 100.0
        let shineY = y * 100.0
        shine.style.setProperty("--shine-x", $"{shineX}%%")
        shine.style.setProperty("--shine-y", $"{shineY}%%")

/// Handle mouse leave - no action needed, CSS handles the hover state
let handlePosterMouseLeave (e: MouseEvent) =
    ()

/// Get poster URL from TMDB CDN (for search results)
let getTmdbPosterUrl (size: string) (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/{size}{p}"
    | None -> ""

/// Get local cached poster URL (for library items)
let getLocalPosterUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/posters/{filename}"
    | None -> ""

/// Get local cached backdrop URL (for library items)
let getLocalBackdropUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/backdrops/{filename}"
    | None -> ""

/// Poster card component for search results (TMDB data)
/// isInLibrary: when true, shows a gray overlay to indicate the item is already in library
let posterCard (item: TmdbSearchResult) (onSelect: TmdbSearchResult -> unit) (isInLibrary: bool) =
    let year =
        item.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let mediaTypeLabel =
        match item.MediaType with
        | Movie -> "Movie"
        | Series -> "Series"

    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick (fun _ -> onSelect item)
        prop.onMouseMove handlePosterMouseMove
        prop.onMouseLeave handlePosterMouseLeave
        prop.children [
            // Poster container
            Html.div [
                prop.className "poster-image-container poster-shadow"
                prop.children [
                    // Poster image (from TMDB CDN for search results)
                    match item.PosterPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getTmdbPosterUrl "w342" item.PosterPath)
                            prop.alt item.Title
                            prop.className (if isInLibrary then "poster-image grayscale opacity-60" else "poster-image")
                            prop.custom ("loading", "lazy")
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/20"
                                    prop.children [ film ]
                                ]
                            ]
                        ]

                    // Shine effect overlay
                    Html.div [
                        prop.className "poster-shine"
                    ]

                    // "In Library" overlay for items already in library
                    if isInLibrary then
                        Html.div [
                            prop.className "absolute inset-0 bg-base-300/70 flex items-center justify-center z-10"
                            prop.children [
                                Html.div [
                                    prop.className "px-2 py-1 bg-base-100/90 rounded-md text-xs font-medium text-base-content/70"
                                    prop.text "In Library"
                                ]
                            ]
                        ]

                    // Media type badge (top right)
                    Html.div [
                        prop.className "absolute top-2 right-2 px-2 py-1 glass rounded-md text-xs font-medium"
                        prop.children [
                            Html.span [
                                prop.className "flex items-center gap-1"
                                prop.children [
                                    Html.span [
                                        prop.className "w-3 h-3"
                                        prop.children [
                                            match item.MediaType with
                                            | Movie -> film
                                            | Series -> tv
                                        ]
                                    ]
                                    Html.span [ prop.text mediaTypeLabel ]
                                ]
                            ]
                        ]
                    ]

                    // Hover overlay (only show add button if not in library)
                    if not isInLibrary then
                        Html.div [
                            prop.className "poster-overlay flex items-center justify-center"
                            prop.children [
                                // Add button
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
                ]
            ]

            // Title and year below poster
            Html.div [
                prop.className "mt-3 space-y-1"
                prop.children [
                    Html.p [
                        prop.className (if isInLibrary then "font-medium text-sm truncate text-base-content/50" else "font-medium text-sm truncate text-base-content/90")
                        prop.title item.Title
                        prop.text item.Title
                    ]
                    if year <> "" then
                        Html.p [
                            prop.className "text-xs text-base-content/50"
                            prop.text year
                        ]
                ]
            ]
        ]
    ]

/// Library entry card component (for library items)
let libraryEntryCard (entry: LibraryEntry) (onViewDetail: EntryId -> bool -> unit) =
    let (title, posterPath, isMovie) =
        match entry.Media with
        | LibraryMovie m -> (m.Title, m.PosterPath, true)
        | LibrarySeries s -> (s.Name, s.PosterPath, false)

    let ratingInfo =
        entry.PersonalRating
        |> Option.map (fun r ->
            match r with
            | Outstanding -> (trophy, "text-amber-400", "Outstanding")
            | Entertaining -> (thumbsUp, "text-lime-400", "Entertaining")
            | Decent -> (handOkay, "text-yellow-400", "Decent")
            | Meh -> (minusCircle, "text-orange-400", "Meh")
            | Waste -> (thumbsDown, "text-red-400", "Waste")
        )

    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick (fun _ -> onViewDetail entry.Id isMovie)
        prop.onMouseMove handlePosterMouseMove
        prop.onMouseLeave handlePosterMouseLeave
        prop.children [
            // Poster container
            Html.div [
                prop.className "poster-image-container poster-shadow"
                prop.children [
                    match posterPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getLocalPosterUrl posterPath)
                            prop.alt title
                            prop.className "poster-image"
                            prop.custom ("loading", "lazy")
                            prop.custom ("crossorigin", "anonymous")
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/20"
                                    prop.children [ if isMovie then film else tv ]
                                ]
                            ]
                        ]

                    // Rating badge (top left, appears on hover)
                    match ratingInfo with
                    | Some (icon, colorClass, label) ->
                        Html.div [
                            prop.className "absolute top-2 left-2 px-2 py-1 rounded-md bg-black/60 backdrop-blur-sm flex items-center gap-2.5 opacity-0 group-hover:opacity-100 transition-opacity duration-200"
                            prop.children [
                                Html.span [
                                    prop.className ("w-4 h-4 flex-shrink-0 flex items-center justify-center " + colorClass)
                                    prop.children [ icon ]
                                ]
                                Html.span [
                                    prop.className ("text-xs font-medium leading-none " + colorClass)
                                    prop.text label
                                ]
                            ]
                        ]
                    | None -> Html.none

                    // Shine effect
                    Html.div [
                        prop.className "poster-shine"
                    ]
                ]
            ]

        ]
    ]
