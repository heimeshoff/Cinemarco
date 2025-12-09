module Pages.Stats.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons

// Icon aliases for stats page (using existing icons)
let private barChart = stats
let private pieChart = circle
let private calendarDays = clock
let private listTodo = tags
let private folderOpen = collections

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

/// Format a large number with commas
let private formatNumber (n: int) =
    string n

/// Format percentage (no decimal places)
let private formatPercent (pct: float) =
    $"{int (System.Math.Round pct)}%%"

// =====================================
// Stat Card Components
// =====================================

/// Large stat card with icon and value
let private bigStatCard (icon: ReactElement) (label: string) (value: string) (subtitle: string option) (colorClass: string) =
    Html.div [
        prop.className "glass rounded-2xl p-6 flex flex-col items-center text-center space-y-3 hover:shadow-lg transition-shadow"
        prop.children [
            Html.div [
                prop.className $"w-14 h-14 rounded-xl bg-gradient-to-br {colorClass} flex items-center justify-center"
                prop.children [
                    Html.span [
                        prop.className "w-7 h-7 text-white"
                        prop.children [ icon ]
                    ]
                ]
            ]
            Html.div [
                prop.className "space-y-1"
                prop.children [
                    Html.div [
                        prop.className "text-3xl font-bold"
                        prop.text value
                    ]
                    Html.div [
                        prop.className "text-sm text-base-content/60"
                        prop.text label
                    ]
                    match subtitle with
                    | Some s ->
                        Html.div [
                            prop.className "text-xs text-base-content/40"
                            prop.text s
                        ]
                    | None -> Html.none
                ]
            ]
        ]
    ]

/// Small stat card for secondary metrics
let private smallStatCard (label: string) (value: string) =
    Html.div [
        prop.className "glass rounded-lg p-4 text-center"
        prop.children [
            Html.div [
                prop.className "text-xl font-bold"
                prop.text value
            ]
            Html.div [
                prop.className "text-xs text-base-content/60"
                prop.text label
            ]
        ]
    ]

// =====================================
// Section Components
// =====================================

/// Time breakdown section (movies vs series)
let private timeBreakdownSection (stats: WatchTimeStats) =
    let moviePct =
        if stats.TotalMinutes > 0 then
            float stats.MovieMinutes / float stats.TotalMinutes * 100.0
        else 0.0
    let seriesPct = 100.0 - moviePct

    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-primary"
                        prop.children [ pieChart ]
                    ]
                    Html.span [ prop.text "Time Breakdown" ]
                ]
            ]

            // Progress bar
            Html.div [
                prop.className "h-4 rounded-full overflow-hidden bg-base-300 flex"
                prop.children [
                    Html.div [
                        prop.className "h-full bg-primary transition-all duration-500"
                        prop.style [ style.width (length.percent moviePct) ]
                    ]
                    Html.div [
                        prop.className "h-full bg-secondary transition-all duration-500"
                        prop.style [ style.width (length.percent seriesPct) ]
                    ]
                ]
            ]

            // Legend
            Html.div [
                prop.className "flex justify-between text-sm"
                prop.children [
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.div [ prop.className "w-3 h-3 rounded-full bg-primary" ]
                            Html.span [ prop.text $"Movies: {formatDuration stats.MovieMinutes}" ]
                        ]
                    ]
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.div [ prop.className "w-3 h-3 rounded-full bg-secondary" ]
                            Html.span [ prop.text $"Series: {formatDuration stats.SeriesMinutes}" ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Year-by-year watch time chart
let private yearlyChartSection (byYear: Map<int, int>) =
    let years =
        byYear
        |> Map.toList
        |> List.sortByDescending fst
        |> List.truncate 6
        |> List.rev

    let maxMinutes =
        years
        |> List.map snd
        |> List.fold max 1

    // h-32 = 8rem = 128px, minus ~20px for the label = ~108px max bar height
    let maxBarHeight = 108.0
    // Minimum bar height so small values are still visible
    let minBarHeight = 12.0

    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-primary"
                        prop.children [ calendarDays ]
                    ]
                    Html.span [ prop.text "Watch Time by Year" ]
                ]
            ]

            if List.isEmpty years then
                Html.div [
                    prop.className "text-center py-8 text-base-content/50"
                    prop.text "No data yet"
                ]
            else
                Html.div [
                    prop.className "flex items-end gap-3 h-32"
                    prop.children [
                        for (year, minutes) in years do
                            let rawHeight = float minutes / float maxMinutes * maxBarHeight
                            let heightPx = max minBarHeight rawHeight |> int
                            Html.div [
                                prop.className "flex-1 flex flex-col items-center justify-end h-full"
                                prop.children [
                                    Html.div [
                                        prop.className "w-full rounded-t-lg transition-all duration-500 hover:opacity-80"
                                        prop.style [ 
                                            style.height (length.px heightPx)
                                            style.backgroundImage "linear-gradient(to top, #c9a227, #e8c547)"
                                        ]
                                        prop.title $"{formatDuration minutes}"
                                    ]
                                    Html.span [
                                        prop.className "text-xs text-base-content/60 mt-1"
                                        prop.text (string year)
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Rating distribution chart
let private ratingDistributionSection (byRating: Map<PersonalRating, int>) =
    let ratings = [
        (Outstanding, "Outstanding", "from-yellow-400 to-amber-500")
        (Entertaining, "Entertaining", "from-green-400 to-emerald-500")
        (Decent, "Decent", "from-blue-400 to-cyan-500")
        (Meh, "Meh", "from-gray-400 to-slate-500")
        (Waste, "Waste", "from-red-400 to-rose-500")
    ]

    let totalMinutes =
        byRating
        |> Map.toList
        |> List.sumBy snd
        |> max 1

    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-primary"
                        prop.children [ star ]
                    ]
                    Html.span [ prop.text "Time by Rating" ]
                ]
            ]

            Html.div [
                prop.className "space-y-3"
                prop.children [
                    for (rating, label, gradient) in ratings do
                        let minutes = byRating |> Map.tryFind rating |> Option.defaultValue 0
                        let pct = float minutes / float totalMinutes * 100.0
                        Html.div [
                            prop.className "space-y-1"
                            prop.children [
                                Html.div [
                                    prop.className "flex justify-between text-sm"
                                    prop.children [
                                        Html.span [ prop.text label ]
                                        Html.span [
                                            prop.className "text-base-content/60"
                                            prop.text (formatDuration minutes)
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "h-2 rounded-full bg-base-300"
                                    prop.children [
                                        Html.div [
                                            prop.className $"h-full rounded-full bg-gradient-to-r {gradient} transition-all duration-500"
                                            prop.style [ style.width (length.percent pct) ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]

/// Backlog section
let private backlogSection (backlog: BacklogStats) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-warning"
                        prop.children [ listTodo ]
                    ]
                    Html.span [ prop.text "Your Backlog" ]
                ]
            ]

            Html.div [
                prop.className "grid grid-cols-2 gap-4"
                prop.children [
                    Html.div [
                        prop.className "text-center"
                        prop.children [
                            Html.div [
                                prop.className "text-3xl font-bold text-warning"
                                prop.text (formatNumber backlog.TotalEntries)
                            ]
                            Html.div [
                                prop.className "text-sm text-base-content/60"
                                prop.text "Unwatched items"
                            ]
                        ]
                    ]
                    Html.div [
                        prop.className "text-center"
                        prop.children [
                            Html.div [
                                prop.className "text-3xl font-bold text-warning"
                                prop.text (formatDuration backlog.EstimatedMinutes)
                            ]
                            Html.div [
                                prop.className "text-sm text-base-content/60"
                                prop.text "Estimated time"
                            ]
                        ]
                    ]
                ]
            ]

            match backlog.OldestEntry with
            | Some entry ->
                let title =
                    match entry.Media with
                    | LibraryMovie m -> m.Title
                    | LibrarySeries s -> s.Name
                Html.div [
                    prop.className "pt-2 border-t border-base-content/10"
                    prop.children [
                        Html.div [
                            prop.className "text-sm text-base-content/60 mb-1"
                            prop.text "Oldest unwatched:"
                        ]
                        Html.button [
                            prop.className "text-primary hover:underline text-left"
                            prop.text title
                            prop.onClick (fun _ ->
                                match entry.Media with
                                | LibraryMovie _ -> dispatch (ViewMovieDetail (entry.Id, title))
                                | LibrarySeries _ -> dispatch (ViewSeriesDetail (entry.Id, title))
                            )
                        ]
                    ]
                ]
            | None -> Html.none
        ]
    ]

/// Top series by time investment
let private topSeriesSection (series: SeriesTimeInvestment list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-secondary"
                        prop.children [ tv ]
                    ]
                    Html.span [ prop.text "Top Series by Time" ]
                ]
            ]

            if List.isEmpty series then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No series watched yet"
                ]
            else
                Html.div [
                    prop.className "space-y-3"
                    prop.children [
                        for s in series |> List.truncate 5 do
                            Html.div [
                                prop.className "flex items-center gap-3"
                                prop.children [
                                    // Poster
                                    Html.div [
                                        prop.className "w-10 h-14 rounded overflow-hidden flex-shrink-0"
                                        prop.children [
                                            match s.Series.PosterPath with
                                            | Some path ->
                                                Html.img [
                                                    prop.className "w-full h-full object-cover"
                                                    prop.src $"/images/posters{path}"
                                                    prop.alt s.Series.Name
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-5 h-5 text-base-content/30"
                                                            prop.children [ tv ]
                                                        ]
                                                    ]
                                                ]
                                        ]
                                    ]

                                    // Info
                                    Html.div [
                                        prop.className "flex-1 min-w-0"
                                        prop.children [
                                            Html.button [
                                                prop.className "text-sm font-medium truncate w-full text-left hover:text-primary transition-colors"
                                                prop.text s.Series.Name
                                                prop.onClick (fun _ -> dispatch (ViewSeriesDetail (s.Entry.Id, s.Series.Name)))
                                            ]
                                            Html.div [
                                                prop.className "text-xs text-base-content/60"
                                                prop.text $"{formatDuration s.WatchedMinutes} watched"
                                            ]
                                        ]
                                    ]

                                    // Progress
                                    Html.div [
                                        prop.className "text-right flex-shrink-0"
                                        prop.children [
                                            Html.div [
                                                prop.className "text-sm font-medium"
                                                prop.text (formatPercent s.CompletionPercentage)
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Top collections by watched time
let private topCollectionsSection (collections: (Collection * CollectionProgress) list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-2xl p-6 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-lg font-semibold flex items-center gap-2"
                prop.children [
                    Html.span [
                        prop.className "w-5 h-5 text-accent"
                        prop.children [ folderOpen ]
                    ]
                    Html.span [ prop.text "Top Collections" ]
                ]
            ]

            if List.isEmpty collections then
                Html.div [
                    prop.className "text-center py-4 text-base-content/50"
                    prop.text "No collections with watched items"
                ]
            else
                Html.div [
                    prop.className "space-y-3"
                    prop.children [
                        for (collection, progress) in collections do
                            Html.div [
                                prop.className "flex items-center gap-3"
                                prop.children [
                                    // Icon
                                    Html.div [
                                        prop.className "w-10 h-10 rounded-lg bg-gradient-to-br from-accent/20 to-accent/5 flex items-center justify-center flex-shrink-0"
                                        prop.children [
                                            Html.span [
                                                prop.className "w-5 h-5 text-accent"
                                                prop.children [ folderOpen ]
                                            ]
                                        ]
                                    ]

                                    // Info
                                    Html.div [
                                        prop.className "flex-1 min-w-0"
                                        prop.children [
                                            Html.button [
                                                prop.className "text-sm font-medium truncate w-full text-left hover:text-accent transition-colors"
                                                prop.text collection.Name
                                                prop.onClick (fun _ -> dispatch (ViewCollection (collection.Id, collection.Name)))
                                            ]
                                            Html.div [
                                                prop.className "text-xs text-base-content/60"
                                                prop.text $"{formatDuration progress.WatchedMinutes} • {progress.CompletedItems}/{progress.TotalItems} items"
                                            ]
                                        ]
                                    ]

                                    // Progress
                                    Html.div [
                                        prop.className "text-right flex-shrink-0"
                                        prop.children [
                                            Html.div [
                                                prop.className "text-sm font-medium"
                                                prop.text (formatPercent progress.CompletionPercentage)
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
        prop.className "space-y-8"
        prop.children [
            // Header
            Html.div [
                prop.className "flex items-center gap-3"
                prop.children [
                    Html.div [
                        prop.className "w-12 h-12 rounded-xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center"
                        prop.children [
                            Html.span [
                                prop.className "w-6 h-6 text-primary"
                                prop.children [ barChart ]
                            ]
                        ]
                    ]
                    Html.div [
                        prop.children [
                            Html.h1 [
                                prop.className "text-2xl font-bold"
                                prop.text "Time Intelligence"
                            ]
                            Html.p [
                                prop.className "text-sm text-base-content/60"
                                prop.text "Your watch time statistics and insights"
                            ]
                        ]
                    ]
                ]
            ]

            match model.Stats with
            | NotAsked ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/50"
                    prop.text "Loading..."
                ]

            | Loading ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Skeleton for big stat cards
                        Html.div [
                            prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                            prop.children [
                                for _ in 1..4 do
                                    Html.div [
                                        prop.className "skeleton h-36 rounded-2xl"
                                    ]
                            ]
                        ]
                        // Skeleton for charts
                        Html.div [
                            prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                            prop.children [
                                Html.div [ prop.className "skeleton h-48 rounded-2xl" ]
                                Html.div [ prop.className "skeleton h-48 rounded-2xl" ]
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

            | Success stats ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Big stat cards
                        Html.div [
                            prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                            prop.children [
                                bigStatCard
                                    clock
                                    "Total Watch Time"
                                    (formatHours stats.LifetimeStats.TotalMinutes + "h")
                                    (Some (formatDuration stats.LifetimeStats.TotalMinutes))
                                    "from-primary to-primary/60"

                                bigStatCard
                                    film
                                    "Movie Time"
                                    (formatHours stats.LifetimeStats.MovieMinutes + "h")
                                    None
                                    "from-blue-500 to-blue-400"

                                bigStatCard
                                    tv
                                    "Series Time"
                                    (formatHours stats.LifetimeStats.SeriesMinutes + "h")
                                    None
                                    "from-purple-500 to-purple-400"

                                bigStatCard
                                    calendarDays
                                    "This Year"
                                    (formatDuration stats.ThisYearStats.TotalMinutes)
                                    None
                                    "from-green-500 to-green-400"
                            ]
                        ]

                        // Charts row
                        Html.div [
                            prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                            prop.children [
                                timeBreakdownSection stats.LifetimeStats
                                yearlyChartSection stats.LifetimeStats.ByYear
                            ]
                        ]

                        // Rating and backlog row
                        Html.div [
                            prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                            prop.children [
                                ratingDistributionSection stats.LifetimeStats.ByRating
                                backlogSection stats.Backlog dispatch
                            ]
                        ]

                        // Top series and collections
                        Html.div [
                            prop.className "grid grid-cols-1 md:grid-cols-2 gap-6"
                            prop.children [
                                topSeriesSection stats.TopSeriesByTime dispatch
                                topCollectionsSection stats.TopCollectionsByTime dispatch
                            ]
                        ]
                    ]
                ]
        ]
    ]
