module Pages.Timeline.View

open System
open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser.Types
open Common.Types
open Shared.Domain
open Types
open Components.Icons

module GlassButton = Common.Components.GlassButton.View

// =====================================
// JS Interop for IntersectionObserver
// =====================================

module private IntersectionObserverInterop =
    [<Emit("new IntersectionObserver($0, { rootMargin: '200px', threshold: 0 })")>]
    let create (callback: obj -> unit) : obj = jsNative

    // Observer for tracking visible section (triggers when element enters top 20% of viewport)
    [<Emit("new IntersectionObserver($0, { rootMargin: '-10% 0px -80% 0px', threshold: 0 })")>]
    let createVisibilityTracker (callback: obj -> unit) : obj = jsNative

    [<Emit("$0.observe($1)")>]
    let observe (observer: obj) (element: Element) : unit = jsNative

    [<Emit("$0.disconnect()")>]
    let disconnect (observer: obj) : unit = jsNative

    [<Emit("$0.scrollIntoView({ behavior: 'smooth', block: 'start' })")>]
    let scrollIntoViewSmooth (element: Element) : unit = jsNative

// =====================================
// Rating Options (same as MovieDetail)
// =====================================

/// Rating labels, descriptions and icons
let private ratingOptions = [
    (0, "Unrated", "No rating yet", questionCircle, "text-base-content/50")
    (1, "Waste", "Waste of time", thumbsDown, "text-red-400")
    (2, "Meh", "Didn't click, uninspiring", minusCircle, "text-orange-400")
    (3, "Decent", "Watchable, even if not life-changing", handOkay, "text-yellow-400")
    (4, "Entertaining", "Strong craft, enjoyable", thumbsUp, "text-lime-400")
    (5, "Outstanding", "Absolutely brilliant, stays with you", trophy, "text-amber-400")
]

/// Get rating display info for a rating value
let private getRatingDisplay (rating: int) =
    ratingOptions |> List.find (fun (n, _, _, _, _) -> n = rating)

// =====================================
// Helper Functions
// =====================================

/// Group timeline entries by month
let private groupByMonth (entries: TimelineEntry list) : (DateTime * TimelineEntry list) list =
    entries
    |> List.groupBy (fun e -> DateTime(e.WatchedDate.Year, e.WatchedDate.Month, 1))
    |> List.sortByDescending fst

/// Format month header (e.g., "December 2024")
let private formatMonthHeader (date: DateTime) =
    date.ToString("MMMM yyyy")

/// Format day within timeline (e.g., "15 Dec")
let private formatDay (date: DateTime) =
    date.ToString("d MMM")

/// Generate month range from earliest to now
let private generateMonthRange (earliest: DateTime) (latest: DateTime) : DateTime list =
    let mutable months = []
    let mutable current = DateTime(earliest.Year, earliest.Month, 1)
    let endMonth = DateTime(latest.Year, latest.Month, 1)
    while current <= endMonth do
        months <- current :: months
        current <- current.AddMonths(1)
    months |> List.rev

/// Get image URL from timeline entry with fallback logic
/// For movies: use poster
/// For series: episode still -> season poster -> series poster
let private getImageUrl (timelineEntry: TimelineEntry) =
    match timelineEntry.Entry.Media with
    | LibraryMovie m -> (m.PosterPath, false)  // (path, isStill)
    | LibrarySeries s ->
        // Try episode still first, then season poster, then series poster
        match timelineEntry.EpisodeStillPath with
        | Some stillPath -> (Some stillPath, true)
        | None ->
            match timelineEntry.SeasonPosterPath with
            | Some seasonPath -> (Some seasonPath, false)
            | None -> (s.PosterPath, false)

/// Get title from entry
let private getTitle (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.Title
    | LibrarySeries s -> s.Name

/// Get media type from entry
let private getMediaType (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie _ -> MediaType.Movie
    | LibrarySeries _ -> MediaType.Series

/// Get detail description
let private getDetailText (detail: TimelineDetail) (entry: LibraryEntry) (episodeName: string option) =
    match detail, entry.Media with
    | MovieWatched, LibraryMovie _ -> "Watched"
    | EpisodeWatched (season, episode), LibrarySeries _ ->
        match episodeName with
        | Some name -> $"S{season}E{episode}: {name}"
        | None -> $"S{season}E{episode}"
    | SeasonCompleted season, LibrarySeries _ -> $"Completed Season {season}"
    | SeriesCompleted, LibrarySeries _ -> "Completed Series"
    | _ -> "Watched"

// =====================================
// Components
// =====================================

/// Infinite scroll sentinel - triggers load when visible
[<ReactComponent>]
let private InfiniteScrollSentinel (hasMore: bool) (isLoading: bool) (onLoadMore: unit -> unit) =
    let sentinelRef = React.useRef<Element option>(None)

    React.useEffect((fun () ->
        match sentinelRef.current, hasMore, isLoading with
        | Some el, true, false ->
            let callback (entries: obj) =
                let entry = entries?(0)
                if entry?isIntersecting then onLoadMore()
            let observer = IntersectionObserverInterop.create callback
            IntersectionObserverInterop.observe observer el
            { new IDisposable with member _.Dispose() = IntersectionObserverInterop.disconnect observer }
        | _ -> { new IDisposable with member _.Dispose() = () }
    ), [| box hasMore; box isLoading |])

    Html.div [
        prop.ref (fun el -> sentinelRef.current <- if isNull el then None else Some el)
        prop.className "h-4"
        prop.children [
            if isLoading then
                Html.div [
                    prop.className "text-center py-4"
                    prop.children [ Html.span [ prop.className "loading loading-spinner loading-md" ] ]
                ]
        ]
    ]

/// Year scale component - fixed on right side (desktop only)
/// Shows all years from earliest to today with density indicators
[<ReactComponent>]
let private YearScale
    (yearStats: TimelineYearStats list)
    (dateRange: TimelineDateRange)
    (currentDate: DateTime option)
    (onJumpToYear: int -> unit) =

    // Generate all years from earliest to now
    let years =
        let earliest = dateRange.EarliestDate.Year
        let latest = DateTime.Now.Year
        [ earliest .. latest ] |> List.rev

    let totalYears = max 1 (List.length years - 1)

    // Get entry count for a specific year
    let getYearCount year =
        yearStats
        |> List.tryFind (fun s -> s.Year = year)
        |> Option.map (fun s -> s.EntryCount)
        |> Option.defaultValue 0

    // Calculate max count for density scaling
    let maxCount =
        if List.isEmpty yearStats then 1
        else yearStats |> List.map (fun s -> s.EntryCount) |> List.max |> max 1

    // Determine dot size based on entry count (quartile-based)
    // Minimum size is visible, scales up with content density
    let getDotSize count =
        if count = 0 then "w-2 h-2"
        elif count <= maxCount / 4 then "w-2.5 h-2.5"
        elif count <= maxCount / 2 then "w-3 h-3"
        elif count <= maxCount * 3 / 4 then "w-3.5 h-3.5"
        else "w-4 h-4"

    let currentYear = currentDate |> Option.map (fun d -> d.Year)

    // Determine label visibility based on total years
    // With many years, only show labels for decade markers
    let yearCount = List.length years
    let showLabel year i =
        if yearCount <= 20 then true  // Show all if 20 or fewer years
        elif yearCount <= 40 then i % 2 = 0  // Every other year for 21-40 years
        else year % 10 = 0 || i = 0  // Only decades + current year for 40+ years

    Html.div [
        prop.className "fixed right-6 top-1/2 -translate-y-1/2 h-[85vh] hidden lg:flex flex-col items-end z-30"
        prop.children [
            // "Now" label
            Html.span [
                prop.className "text-xs text-base-content/40 mb-2 font-medium"
                prop.text "Now"
            ]
            // Year scale track with labels to the left
            Html.div [
                prop.className "relative flex-1 flex items-center"
                prop.children [
                    // Labels column (to the left of the track)
                    Html.div [
                        prop.className "relative h-full mr-2"
                        prop.style [ style.width (length.rem 3) ]
                        prop.children [
                            for (i, year) in years |> List.indexed do
                                let pos = float i / float totalYears * 100.0
                                let isActive = currentYear = Some year

                                if showLabel year i then
                                    Html.span [
                                        prop.key $"year-label-{year}"
                                        prop.className (
                                            "absolute right-0 text-xs whitespace-nowrap transition-colors cursor-pointer hover:text-primary " +
                                            if isActive then "text-primary font-semibold"
                                            else "text-base-content/40"
                                        )
                                        prop.style [
                                            style.top (length.percent pos)
                                            style.transform.translateY (length.percent -50)
                                        ]
                                        prop.onClick (fun _ -> onJumpToYear year)
                                        prop.text (string year)
                                    ]
                        ]
                    ]
                    // Dots track
                    Html.div [
                        prop.className "relative h-full w-1 bg-base-content/10 rounded-full"
                        prop.children [
                            for (i, year) in years |> List.indexed do
                                let pos = float i / float totalYears * 100.0
                                let count = getYearCount year
                                let isActive = currentYear = Some year
                                let dotSize = getDotSize count

                                Html.div [
                                    prop.key $"year-{year}"
                                    prop.className (
                                        $"absolute left-1/2 -translate-x-1/2 rounded-full cursor-pointer transition-all hover:scale-150 {dotSize} " +
                                        if isActive then
                                            "bg-primary ring-2 ring-primary/30 shadow-lg shadow-primary/50"
                                        elif count > 0 then
                                            "bg-amber-400/80 hover:bg-primary shadow-sm shadow-amber-400/30"
                                        else
                                            "bg-base-content/30 hover:bg-primary/60"
                                    )
                                    prop.style [ style.top (length.percent pos) ]
                                    prop.onClick (fun _ -> onJumpToYear year)
                                    prop.title $"{year} ({count} entries)"
                                ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Render image for timeline entry (responsive sizes)
/// Mobile: smaller, md+: larger
let private renderImage (imagePath: string option) (isStill: bool) (title: string) (mediaType: MediaType) =
    Html.div [
        prop.className (
            if isStill then
                // Stills: landscape aspect ratio - mobile: w-20 h-12, md+: w-36 h-20
                "w-20 h-12 md:w-36 md:h-20 flex-shrink-0 rounded-lg overflow-hidden poster-shadow"
            else
                // Posters: portrait aspect ratio - mobile: w-12 h-18, md+: w-20 h-28
                "w-12 h-[4.5rem] md:w-20 md:h-28 flex-shrink-0 rounded-lg overflow-hidden poster-shadow"
        )
        prop.children [
            match imagePath with
            | Some path ->
                let imageUrl =
                    if isStill then $"/images/stills{path}"
                    else $"/images/posters{path}"
                Html.img [
                    prop.className "w-full h-full object-cover"
                    prop.src imageUrl
                    prop.alt title
                ]
            | None ->
                Html.div [
                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                    prop.children [
                        Html.span [
                            prop.className "w-5 h-5 md:w-8 md:h-8 text-base-content/30"
                            prop.children [
                                match mediaType with
                                | MediaType.Movie -> film
                                | MediaType.Series -> tv
                            ]
                        ]
                    ]
                ]
        ]
    ]

/// Mobile card - unified layout for both movies and series (left-aligned)
let private mobileCard (entry: TimelineEntry) (dispatch: Msg -> unit) =
    let (imagePath, isStill) = getImageUrl entry
    let title = getTitle entry.Entry
    let mediaType = getMediaType entry.Entry
    let detailText = getDetailText entry.Detail entry.Entry entry.EpisodeName

    Html.div [
        prop.className "flex gap-3 items-center group cursor-pointer hover:bg-base-200/30 rounded-lg p-2 transition-colors"
        prop.onClick (fun _ ->
            match entry.Entry.Media with
            | LibraryMovie m -> dispatch (ViewMovieDetail (entry.Entry.Id, title, m.ReleaseDate))
            | LibrarySeries s -> dispatch (ViewSeriesDetail (entry.Entry.Id, title, s.FirstAirDate))
        )
        prop.children [
            // Image
            renderImage imagePath isStill title mediaType
            // Info
            Html.div [
                prop.className "flex-1 min-w-0"
                prop.children [
                    Html.h3 [
                        prop.className "font-medium truncate group-hover:text-primary transition-colors text-sm"
                        prop.text title
                    ]
                    Html.span [
                        prop.className "text-xs text-base-content/60 block"
                        prop.text detailText
                    ]
                    // Rating if present (movies only)
                    match entry.Detail, entry.Entry.PersonalRating with
                    | MovieWatched, Some rating ->
                        let ratingValue = PersonalRating.toInt rating
                        let (_, label, _, icon, colorClass) = getRatingDisplay ratingValue
                        Html.div [
                            prop.className "flex items-center gap-1 mt-1"
                            prop.children [
                                Html.span [
                                    prop.className $"w-3 h-3 {colorClass}"
                                    prop.children [ icon ]
                                ]
                                Html.span [
                                    prop.className $"text-xs {colorClass}"
                                    prop.text label
                                ]
                            ]
                        ]
                    | _ -> Html.none
                    // Friends watched with
                    if not (List.isEmpty entry.WatchedWithFriends) then
                        Html.div [
                            prop.className "flex items-center gap-1 mt-1 flex-wrap"
                            prop.children [
                                Html.span [
                                    prop.className "w-3 h-3 text-base-content/40"
                                    prop.children [ userPlus ]
                                ]
                                for friend in entry.WatchedWithFriends do
                                    Html.span [
                                        prop.className "badge badge-xs badge-outline"
                                        prop.text (friend.Nickname |> Option.defaultValue friend.Name)
                                    ]
                            ]
                        ]
                ]
            ]
        ]
    ]

/// Movie card (left side, md+ only) - info on left, poster on right
let private movieCard (entry: TimelineEntry) (dispatch: Msg -> unit) =
    let movie = match entry.Entry.Media with LibraryMovie m -> m | _ -> failwith "Expected movie"
    let title = movie.Title
    let detailText = getDetailText entry.Detail entry.Entry entry.EpisodeName

    Html.div [
        prop.className "flex gap-4 items-center group cursor-pointer hover:bg-base-200/30 rounded-lg p-3 transition-colors justify-end"
        prop.onClick (fun _ -> dispatch (ViewMovieDetail (entry.Entry.Id, title, movie.ReleaseDate)))
        prop.children [
            // Info (right-aligned text)
            Html.div [
                prop.className "flex-1 min-w-0 text-right"
                prop.children [
                    Html.h3 [
                        prop.className "font-medium truncate group-hover:text-primary transition-colors text-base md:text-lg"
                        prop.text title
                    ]
                    Html.span [
                        prop.className "text-sm text-base-content/60 block"
                        prop.text detailText
                    ]
                    // Rating if present
                    match entry.Entry.PersonalRating with
                    | Some rating ->
                        let ratingValue = PersonalRating.toInt rating
                        let (_, label, _, icon, colorClass) = getRatingDisplay ratingValue
                        Html.div [
                            prop.className "flex items-center gap-1.5 mt-1.5 justify-end"
                            prop.children [
                                Html.span [
                                    prop.className $"text-sm {colorClass}"
                                    prop.text label
                                ]
                                Html.span [
                                    prop.className $"w-4 h-4 {colorClass}"
                                    prop.children [ icon ]
                                ]
                            ]
                        ]
                    | None -> Html.none
                    // Friends watched with
                    if not (List.isEmpty entry.WatchedWithFriends) then
                        Html.div [
                            prop.className "flex items-center gap-1.5 mt-1.5 flex-wrap justify-end"
                            prop.children [
                                for friend in entry.WatchedWithFriends do
                                    Html.span [
                                        prop.className "badge badge-sm badge-outline"
                                        prop.text (friend.Nickname |> Option.defaultValue friend.Name)
                                    ]
                                Html.span [
                                    prop.className "w-4 h-4 text-base-content/40"
                                    prop.children [ userPlus ]
                                ]
                            ]
                        ]
                ]
            ]
            // Poster
            renderImage movie.PosterPath false title MediaType.Movie
        ]
    ]

/// Series card (right side, md+ only) - image on left, info on right
let private seriesCard (entry: TimelineEntry) (dispatch: Msg -> unit) =
    let series = match entry.Entry.Media with LibrarySeries s -> s | _ -> failwith "Expected series"
    let (imagePath, isStill) = getImageUrl entry
    let title = series.Name
    let detailText = getDetailText entry.Detail entry.Entry entry.EpisodeName

    Html.div [
        prop.className "flex gap-4 items-center group cursor-pointer hover:bg-base-200/30 rounded-lg p-3 transition-colors"
        prop.onClick (fun _ -> dispatch (ViewSeriesDetail (entry.Entry.Id, title, series.FirstAirDate)))
        prop.children [
            // Image (still or poster)
            renderImage imagePath isStill title MediaType.Series
            // Info
            Html.div [
                prop.className "flex-1 min-w-0"
                prop.children [
                    Html.h3 [
                        prop.className "font-medium truncate group-hover:text-primary transition-colors text-base md:text-lg"
                        prop.text title
                    ]
                    Html.span [
                        prop.className "text-sm text-base-content/60 block"
                        prop.text detailText
                    ]
                    // Friends watched with
                    if not (List.isEmpty entry.WatchedWithFriends) then
                        Html.div [
                            prop.className "flex items-center gap-1.5 mt-1.5 flex-wrap"
                            prop.children [
                                Html.span [
                                    prop.className "w-4 h-4 text-base-content/40"
                                    prop.children [ userPlus ]
                                ]
                                for friend in entry.WatchedWithFriends do
                                    Html.span [
                                        prop.className "badge badge-sm badge-outline"
                                        prop.text (friend.Nickname |> Option.defaultValue friend.Name)
                                    ]
                            ]
                        ]
                ]
            ]
        ]
    ]

/// Timeline row - responsive: mobile (left-aligned) vs md+ (split)
let private timelineRow (entry: TimelineEntry) (dispatch: Msg -> unit) =
    let isMovie = match entry.Entry.Media with LibraryMovie _ -> true | _ -> false

    Html.div [
        prop.children [
            // Mobile layout (shown on small screens, hidden on md+)
            Html.div [
                prop.className "md:hidden flex items-center gap-3 min-h-[4rem]"
                prop.children [
                    // Timeline dot (left)
                    Html.div [
                        prop.className "w-3 h-3 rounded-full bg-primary/80 ring-2 ring-base-100 flex-shrink-0"
                    ]
                    // Card
                    Html.div [
                        prop.className "flex-1"
                        prop.children [ mobileCard entry dispatch ]
                    ]
                ]
            ]

            // Desktop split layout (hidden on small screens, shown on md+)
            Html.div [
                prop.className "hidden md:grid grid-cols-[1fr_auto_1fr] gap-0 items-center min-h-[5rem]"
                prop.children [
                    // Left column (movies)
                    Html.div [
                        prop.className "pr-6"
                        prop.children [
                            if isMovie then
                                movieCard entry dispatch
                        ]
                    ]
                    // Center timeline dot
                    Html.div [
                        prop.className "flex flex-col items-center"
                        prop.children [
                            Html.div [
                                prop.className "w-4 h-4 rounded-full bg-primary/80 ring-4 ring-base-100 z-10"
                            ]
                        ]
                    ]
                    // Right column (series)
                    Html.div [
                        prop.className "pl-6"
                        prop.children [
                            if not isMovie then
                                seriesCard entry dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Day group with date label - responsive positioning
let private dayGroup (date: DateTime) (entries: TimelineEntry list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative"
        prop.children [
            // Date label - left on mobile, centered on md+
            Html.div [
                prop.className "flex justify-start md:justify-center mb-2"
                prop.children [
                    Html.span [
                        prop.className "text-xs md:text-sm font-medium text-base-content/50 bg-base-100 px-3 py-1 rounded-full z-10 ml-6 md:ml-0"
                        prop.text (formatDay date)
                    ]
                ]
            ]
            // Entries for this day
            Html.div [
                prop.className "space-y-1 md:space-y-2"
                prop.children [
                    for entry in entries do
                        timelineRow entry dispatch
                ]
            ]
        ]
    ]

/// Month section with timeline - responsive
[<ReactComponent>]
let private MonthSection (monthDate: DateTime) (entries: TimelineEntry list) (dispatch: Msg -> unit) =
    let elementRef = React.useRef<HTMLDivElement option>(None)

    // Set up visibility observer to track current visible month
    React.useEffect((fun () ->
        match elementRef.current with
        | Some el ->
            let callback (observerEntries: obj) =
                let arr: obj array = observerEntries :?> obj array
                for entry in arr do
                    let isIntersecting: bool = entry?isIntersecting
                    if isIntersecting then
                        dispatch (UpdateVisibleDate monthDate)

            let observer = IntersectionObserverInterop.createVisibilityTracker callback
            IntersectionObserverInterop.observe observer el
            { new IDisposable with member _.Dispose() = IntersectionObserverInterop.disconnect observer }
        | None ->
            { new IDisposable with member _.Dispose() = () }
    ), [| box monthDate |])

    // Group entries by day
    let byDay =
        entries
        |> List.groupBy (fun e -> e.WatchedDate.Date)
        |> List.sortByDescending fst

    Html.div [
        prop.id $"timeline-month-{monthDate.Year}-{monthDate.Month}"
        prop.ref elementRef
        prop.className "mb-8 relative scroll-mt-20"
        prop.children [
            // Vertical timeline line - left on mobile, center on md+
            Html.div [
                prop.className "absolute left-[5px] md:left-1/2 top-0 bottom-0 w-0.5 bg-base-content/20 md:-translate-x-1/2"
            ]
            // Month header - left on mobile, centered on md+
            Html.div [
                prop.className "flex justify-start md:justify-center mb-4 md:mb-6 sticky top-0 z-20"
                prop.children [
                    Html.h2 [
                        prop.className "text-base md:text-xl font-bold bg-base-100/95 backdrop-blur-sm px-4 md:px-6 py-1.5 md:py-2 rounded-full border border-base-content/10 ml-6 md:ml-0"
                        prop.text (formatMonthHeader monthDate)
                    ]
                ]
            ]
            // Days within month
            Html.div [
                prop.className "space-y-4 md:space-y-6"
                prop.children [
                    for (dayDate, dayEntries) in byDay do
                        dayGroup dayDate dayEntries dispatch
                ]
            ]
        ]
    ]

/// Filter bar
let private filterBar (model: Model) (dispatch: Msg -> unit) =
    let hasDateFilter = model.IsDateFilterOpen || model.StartDate.IsSome || model.EndDate.IsSome

    Html.div [
        prop.className "glass rounded-xl p-4 mb-6"
        prop.children [
            Html.div [
                prop.className "flex flex-wrap items-center gap-3"
                prop.children [
                    // Date range filter toggle (first)
                    if hasDateFilter then
                        GlassButton.primaryWithLabel clock "Date Range" "Toggle date range filter" (fun () -> dispatch ToggleDateFilter)
                    else
                        GlassButton.withLabel clock "Date Range" "Toggle date range filter" (fun () -> dispatch ToggleDateFilter)

                    // Media type filter chips
                    Html.div [
                        prop.className "flex gap-2"
                        prop.children [
                            if model.MediaType.IsNone then
                                GlassButton.primaryWithLabel list "All" "Show all" (fun () -> dispatch (SetMediaTypeFilter None))
                            else
                                GlassButton.withLabel list "All" "Show all" (fun () -> dispatch (SetMediaTypeFilter None))

                            if model.MediaType = Some MediaType.Movie then
                                GlassButton.primaryWithLabel film "Movies" "Show movies" (fun () -> dispatch (SetMediaTypeFilter (Some MediaType.Movie)))
                            else
                                GlassButton.withLabel film "Movies" "Show movies" (fun () -> dispatch (SetMediaTypeFilter (Some MediaType.Movie)))

                            if model.MediaType = Some MediaType.Series then
                                GlassButton.primaryWithLabel tv "Series" "Show series" (fun () -> dispatch (SetMediaTypeFilter (Some MediaType.Series)))
                            else
                                GlassButton.withLabel tv "Series" "Show series" (fun () -> dispatch (SetMediaTypeFilter (Some MediaType.Series)))
                        ]
                    ]

                    // Spacer
                    Html.div [ prop.className "flex-1" ]

                    // Clear filters
                    if model.StartDate.IsSome || model.EndDate.IsSome || model.MediaType.IsSome then
                        GlassButton.dangerWithLabel close "Clear" "Clear all filters" (fun () -> dispatch ClearFilters)
                ]
            ]

            // Date range inputs (expandable)
            if model.IsDateFilterOpen then
                Html.div [
                    prop.className "flex flex-wrap gap-4 mt-4 pt-4 border-t border-base-content/10"
                    prop.children [
                        Html.div [
                            prop.className "flex items-center gap-2"
                            prop.children [
                                Html.label [
                                    prop.className "text-sm text-base-content/60"
                                    prop.text "From:"
                                ]
                                Html.input [
                                    prop.className "input input-sm input-bordered w-40"
                                    prop.type' "date"
                                    prop.value (
                                        model.StartDate
                                        |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
                                        |> Option.defaultValue ""
                                    )
                                    prop.onChange (fun (value: string) ->
                                        if String.IsNullOrEmpty(value) then
                                            dispatch (SetStartDate None)
                                        else
                                            match DateTime.TryParse(value) with
                                            | true, date -> dispatch (SetStartDate (Some date))
                                            | _ -> ()
                                    )
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "flex items-center gap-2"
                            prop.children [
                                Html.label [
                                    prop.className "text-sm text-base-content/60"
                                    prop.text "To:"
                                ]
                                Html.input [
                                    prop.className "input input-sm input-bordered w-40"
                                    prop.type' "date"
                                    prop.value (
                                        model.EndDate
                                        |> Option.map (fun d -> d.ToString("yyyy-MM-dd"))
                                        |> Option.defaultValue ""
                                    )
                                    prop.onChange (fun (value: string) ->
                                        if String.IsNullOrEmpty(value) then
                                            dispatch (SetEndDate None)
                                        else
                                            match DateTime.TryParse(value) with
                                            | true, date -> dispatch (SetEndDate (Some date))
                                            | _ -> ()
                                    )
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]

/// Empty state
let private emptyState =
    Html.div [
        prop.className "glass rounded-xl p-12 text-center"
        prop.children [
            Html.div [
                prop.className "w-16 h-16 mx-auto mb-4 text-base-content/30"
                prop.children [ clock ]
            ]
            Html.h3 [
                prop.className "text-lg font-medium mb-2"
                prop.text "No watch history yet"
            ]
            Html.p [
                prop.className "text-base-content/60"
                prop.text "Start watching movies and series to build your timeline"
            ]
        ]
    ]

/// Loading skeleton - responsive
let private loadingSkeleton =
    Html.div [
        prop.className "relative"
        prop.children [
            // Timeline line - left on mobile, center on md+
            Html.div [
                prop.className "absolute left-[5px] md:left-1/2 top-0 bottom-0 w-0.5 bg-base-content/10 md:-translate-x-1/2"
            ]
            Html.div [
                prop.className "space-y-8"
                prop.children [
                    for _ in 1..2 do
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                // Month header skeleton - left on mobile, centered on md+
                                Html.div [
                                    prop.className "flex justify-start md:justify-center"
                                    prop.children [
                                        Html.div [ prop.className "skeleton h-8 md:h-10 w-32 md:w-40 rounded-full ml-6 md:ml-0" ]
                                    ]
                                ]
                                // Entry skeletons
                                for i in 1..4 do
                                    Html.div [
                                        prop.children [
                                            // Mobile skeleton
                                            Html.div [
                                                prop.className "md:hidden flex items-center gap-3 min-h-[4rem]"
                                                prop.children [
                                                    Html.div [ prop.className "skeleton w-3 h-3 rounded-full flex-shrink-0" ]
                                                    Html.div [ prop.className "skeleton w-12 h-[4.5rem] rounded-lg" ]
                                                    Html.div [
                                                        prop.className "flex-1 space-y-2"
                                                        prop.children [
                                                            Html.div [ prop.className "skeleton h-4 w-32" ]
                                                            Html.div [ prop.className "skeleton h-3 w-20" ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            // Desktop skeleton
                                            Html.div [
                                                prop.className "hidden md:grid grid-cols-[1fr_auto_1fr] gap-0 items-center min-h-[5rem]"
                                                prop.children [
                                                    // Left side (alternating)
                                                    Html.div [
                                                        prop.className "pr-6 flex justify-end gap-4"
                                                        prop.children [
                                                            if i % 2 = 1 then
                                                                Html.div [
                                                                    prop.className "space-y-2 text-right"
                                                                    prop.children [
                                                                        Html.div [ prop.className "skeleton h-5 w-40 ml-auto" ]
                                                                        Html.div [ prop.className "skeleton h-4 w-24 ml-auto" ]
                                                                    ]
                                                                ]
                                                                Html.div [ prop.className "skeleton w-20 h-28 rounded-lg" ]
                                                        ]
                                                    ]
                                                    // Center dot
                                                    Html.div [
                                                        prop.className "flex flex-col items-center"
                                                        prop.children [
                                                            Html.div [ prop.className "skeleton w-4 h-4 rounded-full" ]
                                                        ]
                                                    ]
                                                    // Right side (alternating)
                                                    Html.div [
                                                        prop.className "pl-6 flex gap-4"
                                                        prop.children [
                                                            if i % 2 = 0 then
                                                                Html.div [ prop.className "skeleton w-36 h-20 rounded-lg" ]
                                                                Html.div [
                                                                    prop.className "space-y-2"
                                                                    prop.children [
                                                                        Html.div [ prop.className "skeleton h-5 w-40" ]
                                                                        Html.div [ prop.className "skeleton h-4 w-28" ]
                                                                    ]
                                                                ]
                                                        ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                ]
            ]
        ]
    ]

// =====================================
// Main View
// =====================================

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative"
        prop.children [
            // Main content with right padding for time axis on lg+
            Html.div [
                prop.className "space-y-6 lg:pr-16"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "flex items-center gap-3 mb-6"
                        prop.children [
                            Html.div [
                                prop.className "w-12 h-12 rounded-xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center"
                                prop.children [
                                    Html.span [
                                        prop.className "w-6 h-6 text-primary"
                                        prop.children [ clock ]
                                    ]
                                ]
                            ]
                            Html.div [
                                prop.children [
                                    Html.h1 [
                                        prop.className "text-2xl font-bold"
                                        prop.text "Timeline"
                                    ]
                                    Html.p [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text "Your chronological viewing history"
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Filter bar
                    filterBar model dispatch

                    // Content
                    match model.Entries with
                    | NotAsked -> Html.none

                    | Loading -> loadingSkeleton

                    | Failure err ->
                        Html.div [
                            prop.className "glass rounded-xl p-8 text-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-12 h-12 text-error mx-auto block mb-4"
                                    prop.children [ error ]
                                ]
                                Html.p [
                                    prop.className "text-error"
                                    prop.text $"Failed to load timeline: {err}"
                                ]
                                Html.button [
                                    prop.className "btn btn-primary mt-4"
                                    prop.onClick (fun _ -> dispatch LoadEntries)
                                    prop.text "Try Again"
                                ]
                            ]
                        ]

                    | Success entries ->
                        if List.isEmpty entries then
                            emptyState
                        else
                            Html.div [
                                prop.children [
                                    // Column labels header (md+ only)
                                    Html.div [
                                        prop.className "hidden md:grid grid-cols-[1fr_auto_1fr] gap-0 items-center mb-6"
                                        prop.children [
                                            // Movies label (left)
                                            Html.div [
                                                prop.className "flex items-center justify-end gap-2 pr-6 text-base-content/60"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "text-base font-medium"
                                                        prop.text "Movies"
                                                    ]
                                                    Html.span [
                                                        prop.className "w-6 h-6"
                                                        prop.children [ film ]
                                                    ]
                                                ]
                                            ]
                                            // Center spacer
                                            Html.div [
                                                prop.className "w-4"
                                            ]
                                            // Series label (right)
                                            Html.div [
                                                prop.className "flex items-center gap-2 pl-6 text-base-content/60"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "w-6 h-6"
                                                        prop.children [ tv ]
                                                    ]
                                                    Html.span [
                                                        prop.className "text-base font-medium"
                                                        prop.text "Series"
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    // Group by month and render
                                    let grouped = groupByMonth entries
                                    for (monthDate, monthEntries) in grouped do
                                        MonthSection monthDate monthEntries dispatch

                                    // Infinite scroll sentinel (replaces load more button)
                                    InfiniteScrollSentinel model.HasNextPage model.IsLoadingMore (fun () -> dispatch LoadMoreEntries)

                                    // Summary
                                    Html.div [
                                        prop.className "text-center text-sm text-base-content/50 py-4"
                                        prop.text $"Showing {List.length entries} of {model.TotalCount} entries"
                                    ]
                                ]
                            ]
                ]
            ]

            // Year scale (desktop only, lg+)
            match model.DateRange, model.YearStats with
            | Success (Some range), Success yearStats ->
                YearScale yearStats range model.CurrentVisibleDate (fun year ->
                    dispatch (JumpToYear year)
                )
            | _ -> Html.none
        ]
    ]
