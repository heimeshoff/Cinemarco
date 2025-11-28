module Components.Cards.View

open Feliz
open Shared.Domain
open Components.Icons

/// TMDB image base URL (for search results)
let private tmdbImageBase = "https://image.tmdb.org/t/p"

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
let posterCard (item: TmdbSearchResult) (onSelect: TmdbSearchResult -> unit) =
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
                            prop.className "poster-image"
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

                    // Hover overlay
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
                        prop.className "font-medium text-sm truncate text-base-content/90"
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
    let (title, posterPath, year, isMovie) =
        match entry.Media with
        | LibraryMovie m ->
            let y = m.ReleaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
            (m.Title, m.PosterPath, y, true)
        | LibrarySeries s ->
            let y = s.FirstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
            (s.Name, s.PosterPath, y, false)

    let watchStatusInfo =
        match entry.WatchStatus with
        | NotStarted -> None
        | InProgress _ -> Some ("Watching", "from-info/80 to-info/40", "text-info")
        | Completed -> Some ("Watched", "from-success/80 to-success/40", "text-success")
        | Abandoned _ -> Some ("Dropped", "from-warning/80 to-warning/40", "text-warning")

    let ratingStars =
        entry.PersonalRating
        |> Option.map (fun r -> PersonalRating.toInt r)

    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick (fun _ -> onViewDetail entry.Id isMovie)
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

                    // Watch status badge (top left)
                    match watchStatusInfo with
                    | Some (label, gradient, _) ->
                        Html.div [
                            prop.className $"absolute top-2 left-2 px-2 py-0.5 rounded-md text-xs font-medium bg-gradient-to-r {gradient} backdrop-blur-sm"
                            prop.text label
                        ]
                    | None -> Html.none

                    // Favorite indicator (top right)
                    if entry.IsFavorite then
                        Html.div [
                            prop.className "absolute top-2 right-2 w-7 h-7 rounded-full bg-black/50 backdrop-blur-sm flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-4 h-4 text-yellow-400"
                                    prop.children [ heartSolid ]
                                ]
                            ]
                        ]

                    // Shine effect
                    Html.div [
                        prop.className "poster-shine"
                    ]

                    // Hover overlay with view button
                    Html.div [
                        prop.className "poster-overlay flex flex-col justify-end p-3"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center justify-center gap-2 text-sm font-medium"
                                prop.children [
                                    Html.span [
                                        prop.className "w-4 h-4"
                                        prop.children [ eye ]
                                    ]
                                    Html.span [ prop.text "View Details" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Title and meta info
            Html.div [
                prop.className "mt-3 space-y-1"
                prop.children [
                    Html.p [
                        prop.className "font-medium text-sm truncate text-base-content/90"
                        prop.title title
                        prop.text title
                    ]
                    Html.div [
                        prop.className "flex justify-between items-center"
                        prop.children [
                            // Year
                            Html.span [
                                prop.className "text-xs text-base-content/50"
                                prop.text (if year <> "" then year else "-")
                            ]

                            // Rating stars
                            match ratingStars with
                            | Some stars ->
                                Html.div [
                                    prop.className "flex gap-0.5"
                                    prop.children [
                                        for i in 1..5 do
                                            Html.span [
                                                prop.className (
                                                    "w-3 h-3 " +
                                                    if i <= stars then "text-yellow-400" else "text-base-content/20"
                                                )
                                                prop.children [ starSolid ]
                                            ]
                                    ]
                                ]
                            | None -> Html.none
                        ]
                    ]
                ]
            ]
        ]
    ]
