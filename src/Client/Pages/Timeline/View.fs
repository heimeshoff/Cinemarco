module Pages.Timeline.View

open System
open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons

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

/// Format full date (e.g., "December 15, 2024")
let private formatFullDate (date: DateTime) =
    date.ToString("MMMM d, yyyy")

/// Get poster URL from entry
let private getPosterUrl (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.PosterPath
    | LibrarySeries s -> s.PosterPath

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

/// Timeline entry card
let private timelineCard (entry: TimelineEntry) (dispatch: Msg -> unit) =
    let posterUrl = getPosterUrl entry.Entry
    let title = getTitle entry.Entry
    let mediaType = getMediaType entry.Entry
    let detailText = getDetailText entry.Detail entry.Entry entry.EpisodeName

    Html.div [
        prop.className "flex gap-4 items-start group cursor-pointer hover:bg-base-200/50 rounded-lg p-3 -m-3 transition-colors"
        prop.onClick (fun _ ->
            match entry.Entry.Media with
            | LibraryMovie m -> dispatch (ViewMovieDetail (entry.Entry.Id, title, m.ReleaseDate))
            | LibrarySeries s -> dispatch (ViewSeriesDetail (entry.Entry.Id, title, s.FirstAirDate))
        )
        prop.children [
            // Poster
            Html.div [
                prop.className "w-16 h-24 flex-shrink-0 rounded-lg overflow-hidden poster-shadow"
                prop.children [
                    match posterUrl with
                    | Some path ->
                        Html.img [
                            prop.className "w-full h-full object-cover"
                            prop.src $"/images/posters{path}"
                            prop.alt title
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full bg-base-300 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-8 h-8 text-base-content/30"
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

            // Info
            Html.div [
                prop.className "flex-1 min-w-0 py-1"
                prop.children [
                    // Title with media type badge
                    Html.div [
                        prop.className "flex items-center gap-2 flex-wrap"
                        prop.children [
                            Html.h3 [
                                prop.className "font-medium truncate group-hover:text-primary transition-colors"
                                prop.text title
                            ]
                            Html.span [
                                prop.className "badge badge-sm"
                                prop.text (
                                    match mediaType with
                                    | MediaType.Movie -> "Movie"
                                    | MediaType.Series -> "Series"
                                )
                            ]
                        ]
                    ]
                    // Detail (episode info or "Watched")
                    Html.span [
                        prop.className "text-sm text-base-content/60 mt-1 block"
                        prop.text detailText
                    ]
                    // Friends watched with
                    if not (List.isEmpty entry.WatchedWithFriends) then
                        Html.div [
                            prop.className "flex items-center gap-1 mt-2 flex-wrap"
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
                    // Rating if present (only for movies, not episode watches)
                    match entry.Detail, entry.Entry.PersonalRating with
                    | MovieWatched, Some rating ->
                        let ratingValue = PersonalRating.toInt rating
                        let (_, label, _, icon, colorClass) = getRatingDisplay ratingValue
                        Html.div [
                            prop.className "flex items-center gap-1.5 mt-2"
                            prop.children [
                                Html.span [
                                    prop.className $"w-4 h-4 {colorClass}"
                                    prop.children [ icon ]
                                ]
                                Html.span [
                                    prop.className $"text-sm {colorClass}"
                                    prop.text label
                                ]
                            ]
                        ]
                    | _ -> Html.none
                ]
            ]
        ]
    ]

/// Day group within month
let private dayGroup (date: DateTime) (entries: TimelineEntry list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative pl-6 pb-6"
        prop.children [
            // Timeline dot
            Html.div [
                prop.className "absolute left-0 top-1 w-3 h-3 rounded-full bg-primary"
            ]
            // Timeline line (connecting to next)
            Html.div [
                prop.className "absolute left-[5px] top-4 w-0.5 h-full bg-base-300"
            ]
            // Day label
            Html.div [
                prop.className "text-sm font-medium text-base-content/60 mb-3"
                prop.text (formatDay date)
            ]
            // Entries for this day
            Html.div [
                prop.className "space-y-4"
                prop.children [
                    for entry in entries do
                        timelineCard entry dispatch
                ]
            ]
        ]
    ]

/// Month section
let private monthSection (monthDate: DateTime) (entries: TimelineEntry list) (dispatch: Msg -> unit) =
    // Group entries by day
    let byDay =
        entries
        |> List.groupBy (fun e -> e.WatchedDate.Date)
        |> List.sortByDescending fst

    Html.div [
        prop.className "mb-8"
        prop.children [
            // Month header
            Html.h2 [
                prop.className "text-xl font-bold mb-4 sticky top-0 bg-base-100/95 backdrop-blur-sm py-2 z-10"
                prop.text (formatMonthHeader monthDate)
            ]
            // Days within month
            Html.div [
                prop.className "ml-2"
                prop.children [
                    for (dayDate, dayEntries) in byDay do
                        dayGroup dayDate dayEntries dispatch
                ]
            ]
        ]
    ]

/// Filter bar
let private filterBar (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-xl p-4 mb-6"
        prop.children [
            Html.div [
                prop.className "flex flex-wrap items-center gap-3"
                prop.children [
                    // Media type filter chips
                    Html.div [
                        prop.className "flex gap-2"
                        prop.children [
                            Html.button [
                                prop.className (
                                    if model.MediaType.IsNone
                                    then "btn btn-sm btn-primary"
                                    else "btn btn-sm btn-ghost"
                                )
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter None))
                                prop.text "All"
                            ]
                            Html.button [
                                prop.className (
                                    if model.MediaType = Some MediaType.Movie
                                    then "btn btn-sm btn-primary"
                                    else "btn btn-sm btn-ghost"
                                )
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter (Some MediaType.Movie)))
                                prop.children [
                                    Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ film ] ]
                                    Html.span [ prop.text "Movies" ]
                                ]
                            ]
                            Html.button [
                                prop.className (
                                    if model.MediaType = Some MediaType.Series
                                    then "btn btn-sm btn-primary"
                                    else "btn btn-sm btn-ghost"
                                )
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter (Some MediaType.Series)))
                                prop.children [
                                    Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ tv ] ]
                                    Html.span [ prop.text "Series" ]
                                ]
                            ]
                        ]
                    ]

                    // Spacer
                    Html.div [ prop.className "flex-1" ]

                    // Date range filter toggle
                    Html.button [
                        prop.className (
                            if model.IsDateFilterOpen || model.StartDate.IsSome || model.EndDate.IsSome
                            then "btn btn-sm btn-accent"
                            else "btn btn-sm btn-ghost"
                        )
                        prop.onClick (fun _ -> dispatch ToggleDateFilter)
                        prop.children [
                            Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ clock ] ]
                            Html.span [ prop.text "Date Range" ]
                        ]
                    ]

                    // Clear filters
                    if model.StartDate.IsSome || model.EndDate.IsSome || model.MediaType.IsSome then
                        Html.button [
                            prop.className "btn btn-sm btn-ghost text-error"
                            prop.onClick (fun _ -> dispatch ClearFilters)
                            prop.children [
                                Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ close ] ]
                                Html.span [ prop.text "Clear" ]
                            ]
                        ]
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

/// Loading skeleton
let private loadingSkeleton =
    Html.div [
        prop.className "space-y-8"
        prop.children [
            for _ in 1..3 do
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        Html.div [ prop.className "skeleton h-8 w-40" ]
                        for _ in 1..4 do
                            Html.div [
                                prop.className "flex gap-4"
                                prop.children [
                                    Html.div [ prop.className "skeleton w-16 h-24 rounded-lg" ]
                                    Html.div [
                                        prop.className "flex-1 space-y-2"
                                        prop.children [
                                            Html.div [ prop.className "skeleton h-5 w-48" ]
                                            Html.div [ prop.className "skeleton h-4 w-24" ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Load more button
let private loadMoreButton (response: PagedResponse<TimelineEntry>) (isLoading: bool) (dispatch: Msg -> unit) =
    if response.HasNextPage then
        Html.div [
            prop.className "text-center py-6"
            prop.children [
                Html.button [
                    prop.className "btn btn-primary btn-wide"
                    prop.disabled isLoading
                    prop.onClick (fun _ -> dispatch LoadMoreEntries)
                    prop.children [
                        if isLoading then
                            Html.span [ prop.className "loading loading-spinner loading-sm mr-2" ]
                        Html.span [ prop.text (if isLoading then "Loading..." else "Load More") ]
                    ]
                ]
            ]
        ]
    else
        Html.none

// =====================================
// Main View
// =====================================

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
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

            | Success response ->
                if List.isEmpty response.Items then
                    emptyState
                else
                    Html.div [
                        prop.children [
                            // Group by month and render
                            let grouped = groupByMonth response.Items
                            for (monthDate, entries) in grouped do
                                monthSection monthDate entries dispatch

                            // Load more button
                            loadMoreButton response model.IsLoadingMore dispatch

                            // Summary
                            Html.div [
                                prop.className "text-center text-sm text-base-content/50 py-4"
                                prop.text $"Showing {List.length response.Items} of {response.TotalCount} entries"
                            ]
                        ]
                    ]
        ]
    ]
