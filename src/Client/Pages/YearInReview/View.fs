module Pages.YearInReview.View

open Feliz
open Common.Types
open Common.Routing
open Shared.Domain
open Types
open Components.Icons

// =====================================
// Helper Functions
// =====================================

/// Format minutes as human-readable duration
let private formatDuration (minutes: int) =
    let days = minutes / (60 * 24)
    let hours = (minutes % (60 * 24)) / 60
    let mins = minutes % 60

    if days > 0 then
        $"{days}d {hours}h"
    elif hours > 0 then
        $"{hours}h {mins}m"
    else
        $"{mins}m"

/// Format minutes as hours only (for large numbers)
let private formatHours (minutes: int) =
    let hours = minutes / 60
    string hours

/// Get poster URL for an entry
let private getPosterUrl (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.PosterPath |> Option.map (fun p -> $"/images/posters{p}")
    | LibrarySeries s -> s.PosterPath |> Option.map (fun p -> $"/images/posters{p}")

/// Get title for an entry
let private getTitle (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.Title
    | LibrarySeries s -> s.Name

// =====================================
// Year Selector Component
// =====================================

let private yearSelector (currentYear: int) (availableYears: AvailableYears) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex items-center gap-4 justify-center"
        prop.children [
            // Previous year button
            let prevYear = currentYear - 1
            let canGoPrev = List.contains prevYear availableYears.Years
            let prevBtnClass = if canGoPrev then "btn btn-circle btn-ghost" else "btn btn-circle btn-ghost btn-disabled opacity-30"
            Html.button [
                prop.className prevBtnClass
                prop.disabled (not canGoPrev)
                prop.onClick (fun _ -> if canGoPrev then dispatch (SelectYear prevYear))
                prop.children [
                    Html.span [
                        prop.className "w-6 h-6"
                        prop.children [ chevronLeft ]
                    ]
                ]
            ]

            // Year display with dropdown
            Html.div [
                prop.className "dropdown"
                prop.children [
                    Html.label [
                        prop.tabIndex 0
                        prop.className "text-6xl font-black tracking-tight cursor-pointer hover:text-primary transition-colors"
                        prop.text (string currentYear)
                    ]
                    Html.ul [
                        prop.tabIndex 0
                        prop.className "dropdown-content menu p-2 shadow-lg bg-base-200 rounded-box w-32 max-h-60 overflow-y-auto"
                        prop.children [
                            for year in availableYears.Years |> List.sortDescending do
                                let yearBtnClass = if year = currentYear then "w-full text-center bg-primary text-primary-content" else "w-full text-center"
                                Html.li [
                                    Html.button [
                                        prop.className yearBtnClass
                                        prop.text (string year)
                                        prop.onClick (fun _ -> dispatch (SelectYear year))
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

            // Next year button
            let nextYear = currentYear + 1
            let canGoNext = List.contains nextYear availableYears.Years
            let nextBtnClass = if canGoNext then "btn btn-circle btn-ghost" else "btn btn-circle btn-ghost btn-disabled opacity-30"
            Html.button [
                prop.className nextBtnClass
                prop.disabled (not canGoNext)
                prop.onClick (fun _ -> if canGoNext then dispatch (SelectYear nextYear))
                prop.children [
                    Html.span [
                        prop.className "w-6 h-6"
                        prop.children [ chevronRight ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Stats Cards
// =====================================

let private heroStatCard (value: string) (label: string) (icon: ReactElement) (gradientFrom: string) (gradientTo: string) (onClick: (unit -> unit) option) =
    let baseClass = "relative overflow-hidden rounded-3xl p-6 min-h-[140px] flex flex-col justify-between"
    let className =
        match onClick with
        | Some _ -> baseClass + " cursor-pointer hover:scale-[1.02] transition-transform duration-200"
        | None -> baseClass

    Html.div [
        prop.className className
        prop.style [
            style.backgroundImage $"linear-gradient(135deg, {gradientFrom}, {gradientTo})"
        ]
        match onClick with
        | Some handler -> prop.onClick (fun _ -> handler())
        | None -> ()
        prop.children [
            Html.div [
                prop.className "absolute top-4 right-4 w-10 h-10 opacity-30"
                prop.children [ icon ]
            ]
            Html.div [
                prop.children [
                    Html.div [
                        prop.className "text-4xl md:text-5xl font-black text-white/95"
                        prop.text value
                    ]
                    Html.div [
                        prop.className "text-sm text-white/70 font-medium mt-1"
                        prop.text label
                    ]
                ]
            ]
        ]
    ]

let private smallStatCard (value: string) (label: string) =
    Html.div [
        prop.className "glass rounded-xl p-4 text-center"
        prop.children [
            Html.div [
                prop.className "text-2xl font-bold"
                prop.text value
            ]
            Html.div [
                prop.className "text-xs text-base-content/60 mt-1"
                prop.text label
            ]
        ]
    ]

// =====================================
// Rating Distribution Chart
// =====================================

let private ratingDistributionChart (dist: RatingDistribution) =
    let total = RatingDistribution.total dist |> max 1
    let ratings = [
        ("Outstanding", dist.Outstanding, "#fbbf24", "#f59e0b")  // amber
        ("Entertaining", dist.Entertaining, "#4ade80", "#22c55e")  // green
        ("Decent", dist.Decent, "#60a5fa", "#3b82f6")  // blue
        ("Meh", dist.Meh, "#9ca3af", "#6b7280")  // gray
        ("Waste", dist.Waste, "#f87171", "#ef4444")  // red
    ]

    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-amber-400"
                        prop.children [ star ]
                    ]
                    Html.span [ prop.text "Rating Distribution" ]
                ]
            ]

            Html.div [
                prop.className "space-y-3"
                prop.children [
                    for (label, count, colorLight, colorDark) in ratings do
                        let pct = (float count / float total) * 100.0
                        Html.div [
                            prop.className "space-y-1"
                            prop.children [
                                Html.div [
                                    prop.className "flex justify-between text-sm"
                                    prop.children [
                                        Html.span [ prop.text label ]
                                        Html.span [
                                            prop.className "text-base-content/60 font-medium"
                                            prop.text (string count)
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "h-2.5 rounded-full bg-base-300 overflow-hidden"
                                    prop.children [
                                        Html.div [
                                            prop.className "h-full rounded-full transition-all duration-700"
                                            prop.style [
                                                style.width (length.percent pct)
                                                style.backgroundImage $"linear-gradient(to right, {colorLight}, {colorDark})"
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
// Top Friends Section
// =====================================

let private topFriendsSection (friends: FriendWatchCount list) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-pink-400"
                        prop.children [ users ]
                    ]
                    Html.span [ prop.text "Watched With" ]
                ]
            ]

            if List.isEmpty friends then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No watch sessions with friends this year"
                ]
            else
                Html.div [
                    prop.className "space-y-3"
                    prop.children [
                        for fwc in friends do
                            Html.div [
                                prop.className "flex items-center gap-3"
                                prop.children [
                                    // Avatar
                                    Html.div [
                                        prop.className "w-10 h-10 rounded-full bg-gradient-to-br from-pink-400 to-purple-500 flex items-center justify-center text-white font-bold text-sm flex-shrink-0"
                                        prop.text (fwc.Friend.Name.Substring(0, 1).ToUpper())
                                    ]
                                    // Name and count
                                    Html.div [
                                        prop.className "flex-1"
                                        prop.children [
                                            Html.div [
                                                prop.className "font-medium"
                                                prop.text fwc.Friend.Name
                                            ]
                                            Html.div [
                                                prop.className "text-sm text-base-content/60"
                                                prop.text $"{fwc.WatchCount} titles"
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
// Top Rated Section
// =====================================

let private topRatedSection (entries: LibraryEntry list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-amber-400"
                        prop.children [ trophy ]
                    ]
                    Html.span [ prop.text "Top Rated This Year" ]
                ]
            ]

            if List.isEmpty entries then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No highly rated titles this year"
                ]
            else
                Html.div [
                    prop.className "grid grid-cols-4 sm:grid-cols-5 md:grid-cols-6 gap-3"
                    prop.children [
                        for entry in entries |> List.truncate 12 do
                            let title = getTitle entry
                            Html.div [
                                prop.className "cursor-pointer group"
                                prop.onClick (fun _ ->
                                    match entry.Media with
                                    | LibraryMovie m -> dispatch (ViewMovieDetail (entry.Id, title, m.ReleaseDate))
                                    | LibrarySeries s -> dispatch (ViewSeriesDetail (entry.Id, title, s.FirstAirDate))
                                )
                                prop.children [
                                    Html.div [
                                        prop.className "aspect-[2/3] rounded-lg overflow-hidden poster-shadow relative"
                                        prop.children [
                                            match getPosterUrl entry with
                                            | Some url ->
                                                Html.img [
                                                    prop.className "w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                                                    prop.src url
                                                    prop.alt title
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-6 h-6 text-base-content/30"
                                                            prop.children [ film ]
                                                        ]
                                                    ]
                                                ]
                                            Html.div [ prop.className "poster-shine" ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

// =====================================
// All Movies Section
// =====================================

let private allMoviesSection (movies: LibraryEntry list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-blue-400"
                        prop.children [ film ]
                    ]
                    Html.span [ prop.text "All Movies" ]
                    if not (List.isEmpty movies) then
                        Html.span [
                            prop.className "ml-auto text-sm text-base-content/50 font-normal"
                            prop.text $"{List.length movies} movies"
                        ]
                ]
            ]

            if List.isEmpty movies then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No movies watched this year"
                ]
            else
                Html.div [
                    prop.className "grid grid-cols-4 sm:grid-cols-5 md:grid-cols-6 lg:grid-cols-8 gap-3"
                    prop.children [
                        for entry in movies do
                            let title = getTitle entry
                            Html.div [
                                prop.className "cursor-pointer group"
                                prop.onClick (fun _ ->
                                    match entry.Media with
                                    | LibraryMovie m -> dispatch (ViewMovieDetail (entry.Id, title, m.ReleaseDate))
                                    | _ -> ()
                                )
                                prop.children [
                                    Html.div [
                                        prop.className "aspect-[2/3] rounded-lg overflow-hidden poster-shadow relative"
                                        prop.children [
                                            match getPosterUrl entry with
                                            | Some url ->
                                                Html.img [
                                                    prop.className "w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                                                    prop.src url
                                                    prop.alt title
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-6 h-6 text-base-content/30"
                                                            prop.children [ film ]
                                                        ]
                                                    ]
                                                ]
                                            Html.div [ prop.className "poster-shine" ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

// =====================================
// All Series Section
// =====================================

let private allSeriesSection (seriesWithFlags: SeriesWithFinishedFlag list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-purple-400"
                        prop.children [ tv ]
                    ]
                    Html.span [ prop.text "All Series" ]
                    if not (List.isEmpty seriesWithFlags) then
                        Html.span [
                            prop.className "ml-auto text-sm text-base-content/50 font-normal"
                            prop.text $"{List.length seriesWithFlags} series"
                        ]
                ]
            ]

            if List.isEmpty seriesWithFlags then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No series watched this year"
                ]
            else
                Html.div [
                    prop.className "grid grid-cols-4 sm:grid-cols-5 md:grid-cols-6 lg:grid-cols-8 gap-3"
                    prop.children [
                        for swf in seriesWithFlags do
                            let entry = swf.Entry
                            let title = getTitle entry
                            Html.div [
                                prop.className "cursor-pointer group"
                                prop.onClick (fun _ ->
                                    match entry.Media with
                                    | LibrarySeries s -> dispatch (ViewSeriesDetail (entry.Id, title, s.FirstAirDate))
                                    | _ -> ()
                                )
                                prop.children [
                                    Html.div [
                                        prop.className "aspect-[2/3] rounded-lg overflow-hidden poster-shadow relative"
                                        prop.children [
                                            match getPosterUrl entry with
                                            | Some url ->
                                                Html.img [
                                                    prop.className "w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                                                    prop.src url
                                                    prop.alt title
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-6 h-6 text-base-content/30"
                                                            prop.children [ tv ]
                                                        ]
                                                    ]
                                                ]
                                            Html.div [ prop.className "poster-shine" ]
                                            // "Finished" badge for series completed this year
                                            if swf.FinishedThisYear then
                                                Html.div [
                                                    prop.className "absolute bottom-0 left-0 right-0 bg-gradient-to-t from-emerald-900/95 via-emerald-900/80 to-transparent pt-6 pb-2 px-2"
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
                                            // "Abandoned" badge for series abandoned this year
                                            elif swf.AbandonedThisYear then
                                                Html.div [
                                                    prop.className "absolute bottom-0 left-0 right-0 bg-gradient-to-t from-red-900/95 via-red-900/80 to-transparent pt-6 pb-2 px-2"
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
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

// =====================================
// Movies vs Series Breakdown
// =====================================

let private breakdownSection (movieMinutes: int) (seriesMinutes: int) (moviesWatched: int) (seriesWatched: int) =
    let total = movieMinutes + seriesMinutes |> max 1
    let moviePct = (float movieMinutes / float total) * 100.0
    let seriesPct = 100.0 - moviePct

    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-5"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-primary"
                        prop.children [ circle ]
                    ]
                    Html.span [ prop.text "Time Breakdown" ]
                ]
            ]

            // Visual bar
            Html.div [
                prop.className "h-6 rounded-full overflow-hidden flex shadow-inner"
                prop.style [ style.backgroundColor "#1e293b" ]
                prop.children [
                    if movieMinutes > 0 then
                        Html.div [
                            prop.className "h-full transition-all duration-700 flex items-center justify-center text-xs font-semibold text-white"
                            prop.style [
                                style.width (length.percent moviePct)
                                style.backgroundImage "linear-gradient(135deg, #3b82f6, #2563eb)"
                            ]
                            prop.text (if moviePct > 15.0 then $"{int moviePct}%%" else "")
                        ]
                    if seriesMinutes > 0 then
                        Html.div [
                            prop.className "h-full transition-all duration-700 flex items-center justify-center text-xs font-semibold text-white"
                            prop.style [
                                style.width (length.percent seriesPct)
                                style.backgroundImage "linear-gradient(135deg, #a855f7, #7c3aed)"
                            ]
                            prop.text (if seriesPct > 15.0 then $"{int seriesPct}%%" else "")
                        ]
                ]
            ]

            // Legend with counts
            Html.div [
                prop.className "grid grid-cols-2 gap-4"
                prop.children [
                    Html.div [
                        prop.className "flex items-center gap-3"
                        prop.children [
                            Html.div [
                                prop.className "w-4 h-4 rounded"
                                prop.style [ style.backgroundImage "linear-gradient(135deg, #3b82f6, #2563eb)" ]
                            ]
                            Html.div [
                                prop.children [
                                    Html.div [
                                        prop.className "font-medium"
                                        prop.text "Movies"
                                    ]
                                    Html.div [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text $"{moviesWatched} watched, {formatDuration movieMinutes}"
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "flex items-center gap-3"
                        prop.children [
                            Html.div [
                                prop.className "w-4 h-4 rounded"
                                prop.style [ style.backgroundImage "linear-gradient(135deg, #a855f7, #7c3aed)" ]
                            ]
                            Html.div [
                                prop.children [
                                    Html.div [
                                        prop.className "font-medium"
                                        prop.text "Series"
                                    ]
                                    Html.div [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text $"{seriesWatched} watched, {formatDuration seriesMinutes}"
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
// Empty State
// =====================================

let private emptyState (year: int) =
    Html.div [
        prop.className "text-center py-16 space-y-4"
        prop.children [
            Html.div [
                prop.className "w-24 h-24 mx-auto rounded-full bg-base-200 flex items-center justify-center"
                prop.children [
                    Html.span [
                        prop.className "w-12 h-12 text-base-content/30"
                        prop.children [ film ]
                    ]
                ]
            ]
            Html.h2 [
                prop.className "text-2xl font-bold"
                prop.text $"No watch data for {year}"
            ]
            Html.p [
                prop.className "text-base-content/60 max-w-md mx-auto"
                prop.text "Start watching and logging movies and series to see your year-in-review stats here."
            ]
        ]
    ]

// =====================================
// Main View
// =====================================

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-8 pb-8"
        prop.children [
            // Header with year
            Html.div [
                prop.className "text-center space-y-2 pt-4"
                prop.children [
                    Html.div [
                        prop.className "text-sm uppercase tracking-widest text-base-content/50 font-semibold"
                        prop.text "Year in Review"
                    ]

                    match model.AvailableYears with
                    | Success years when not (List.isEmpty years.Years) ->
                        yearSelector model.SelectedYear years dispatch
                    | Success _ ->
                        Html.div [
                            prop.className "text-6xl font-black tracking-tight"
                            prop.text (string model.SelectedYear)
                        ]
                    | Loading ->
                        Html.div [
                            prop.className "text-6xl font-black tracking-tight"
                            prop.text (string model.SelectedYear)
                        ]
                    | NotAsked ->
                        Html.div [
                            prop.className "text-6xl font-black tracking-tight"
                            prop.text (string model.SelectedYear)
                        ]
                    | Failure _ ->
                        Html.div [
                            prop.className "text-6xl font-black tracking-tight"
                            prop.text (string model.SelectedYear)
                        ]
                ]
            ]

            // Stats content
            match model.Stats with
            | NotAsked
            | Loading ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Skeleton for hero cards
                        Html.div [
                            prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                            prop.children [
                                for _ in 1..4 do
                                    Html.div [ prop.className "skeleton h-36 rounded-3xl" ]
                            ]
                        ]
                        // Skeleton for sections
                        Html.div [
                            prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                            prop.children [
                                Html.div [ prop.className "skeleton h-64 rounded-2xl" ]
                                Html.div [ prop.className "skeleton h-64 rounded-2xl" ]
                            ]
                        ]
                    ]
                ]

            | Failure err ->
                Html.div [
                    prop.className "glass rounded-2xl p-8 text-center"
                    prop.children [
                        Html.span [
                            prop.className "w-12 h-12 text-error mx-auto block mb-4"
                            prop.children [ error ]
                        ]
                        Html.p [
                            prop.className "text-error"
                            prop.text $"Failed to load stats: {err}"
                        ]
                    ]
                ]

            | Success stats when not stats.HasData ->
                emptyState stats.Year

            | Success stats ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Back button for detail views
                        match model.ViewMode with
                        | YearInReviewViewMode.Overview -> ()
                        | YearInReviewViewMode.MoviesOnly | YearInReviewViewMode.SeriesOnly ->
                            Html.button [
                                prop.className "btn btn-ghost btn-sm gap-2"
                                prop.onClick (fun _ -> dispatch (SetViewMode YearInReviewViewMode.Overview))
                                prop.children [
                                    Html.span [
                                        prop.className "w-4 h-4"
                                        prop.children [ chevronLeft ]
                                    ]
                                    Html.span [ prop.text "Back to Overview" ]
                                ]
                            ]

                        match model.ViewMode with
                        | YearInReviewViewMode.Overview ->
                            // Hero stat cards
                            Html.div [
                                prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                                prop.children [
                                    heroStatCard
                                        (formatHours stats.TotalMinutes + "h")
                                        "Total Watch Time"
                                        clock
                                        "#c9a227"
                                        "#8b6914"
                                        None

                                    heroStatCard
                                        (string stats.MoviesWatched)
                                        "Movies Watched"
                                        film
                                        "#3b82f6"
                                        "#1d4ed8"
                                        (if stats.MoviesWatched > 0 then Some (fun () -> dispatch (SetViewMode YearInReviewViewMode.MoviesOnly)) else None)

                                    heroStatCard
                                        (string stats.SeriesWatched)
                                        "Series Watched"
                                        tv
                                        "#a855f7"
                                        "#7c3aed"
                                        (if stats.SeriesWatched > 0 then Some (fun () -> dispatch (SetViewMode YearInReviewViewMode.SeriesOnly)) else None)

                                    heroStatCard
                                        (string stats.EpisodesWatched)
                                        "Episodes Watched"
                                        playCircle
                                        "#22c55e"
                                        "#15803d"
                                        None
                                ]
                            ]

                            // Secondary stats
                            Html.div [
                                prop.className "grid grid-cols-2 sm:grid-cols-4 gap-3"
                                prop.children [
                                    smallStatCard (formatDuration stats.MovieMinutes) "Movie Time"
                                    smallStatCard (formatDuration stats.SeriesMinutes) "Series Time"
                                    smallStatCard
                                        (stats.AverageRating
                                            |> Option.map (fun r -> $"%.1f{r}")
                                            |> Option.defaultValue "-")
                                        "Avg Rating"
                                    smallStatCard
                                        (string (RatingDistribution.total stats.RatingDistribution))
                                        "Titles Rated"
                                ]
                            ]

                            // Charts row
                            Html.div [
                                prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                                prop.children [
                                    breakdownSection stats.MovieMinutes stats.SeriesMinutes stats.MoviesWatched stats.SeriesWatched
                                    ratingDistributionChart stats.RatingDistribution
                                ]
                            ]

                            // Top rated and friends
                            Html.div [
                                prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                                prop.children [
                                    topRatedSection stats.TopRated dispatch
                                    topFriendsSection stats.TopFriends
                                ]
                            ]

                        | YearInReviewViewMode.MoviesOnly ->
                            // Show only movies
                            allMoviesSection stats.AllMovies dispatch

                        | YearInReviewViewMode.SeriesOnly ->
                            // Show only series
                            allSeriesSection stats.AllSeries dispatch
                    ]
                ]
        ]
    ]
