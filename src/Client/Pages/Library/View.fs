module Pages.Library.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

/// Filter bar component
let private filterBar (filters: LibraryFilters) (dispatch: Msg -> unit) =
    let hasActiveFilters =
        filters.SearchQuery <> "" ||
        filters.WatchStatus <> AllStatuses ||
        filters.MinRating.IsSome

    Html.div [
        prop.className "glass rounded-xl p-4 mb-6 space-y-4"
        prop.children [
            // Top row: Search, Sort, Clear
            Html.div [
                prop.className "flex flex-wrap gap-3 items-center"
                prop.children [
                    // Search input with icon
                    Html.div [
                        prop.className "relative flex-1 min-w-[200px]"
                        prop.children [
                            Html.input [
                                prop.className "w-full pl-10 pr-4 py-2.5 bg-base-100/50 border border-white/5 rounded-lg text-sm placeholder:text-base-content/30 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 transition-all"
                                prop.placeholder "Filter by title..."
                                prop.value filters.SearchQuery
                                prop.onChange (fun (e: string) -> dispatch (SetSearchQuery e))
                            ]
                            Html.span [
                                prop.className "absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40"
                                prop.children [ filter ]
                            ]
                        ]
                    ]

                    // Sort controls
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.span [
                                prop.className "w-4 h-4 text-base-content/40"
                                prop.children [ sort ]
                            ]
                            Html.select [
                                prop.className "bg-base-100/50 border border-white/5 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-primary/30 cursor-pointer"
                                prop.value (
                                    match filters.SortBy with
                                    | SortByDateAdded -> "dateAdded"
                                    | SortByTitle -> "title"
                                    | SortByYear -> "year"
                                    | SortByRating -> "rating"
                                )
                                prop.onChange (fun (e: string) ->
                                    let sortBy =
                                        match e with
                                        | "title" -> SortByTitle
                                        | "year" -> SortByYear
                                        | "rating" -> SortByRating
                                        | _ -> SortByDateAdded
                                    dispatch (SetSortBy sortBy)
                                )
                                prop.children [
                                    Html.option [ prop.value "dateAdded"; prop.text "Date Added" ]
                                    Html.option [ prop.value "title"; prop.text "Title" ]
                                    Html.option [ prop.value "year"; prop.text "Year" ]
                                    Html.option [ prop.value "rating"; prop.text "Rating" ]
                                ]
                            ]
                            Html.button [
                                prop.className "w-9 h-9 rounded-lg bg-base-100/50 border border-white/5 flex items-center justify-center hover:bg-base-100 transition-colors"
                                prop.onClick (fun _ -> dispatch ToggleSortDirection)
                                prop.title (if filters.SortDirection = Ascending then "Ascending" else "Descending")
                                prop.children [
                                    Html.span [
                                        prop.className (
                                            "w-4 h-4 text-base-content/60 transition-transform " +
                                            if filters.SortDirection = Ascending then "" else "rotate-180"
                                        )
                                        prop.children [ chevronUp ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Clear filters (if active)
                    if hasActiveFilters then
                        Html.button [
                            prop.className "flex items-center gap-1.5 px-3 py-2 text-sm text-base-content/60 hover:text-error transition-colors"
                            prop.onClick (fun _ -> dispatch ClearFilters)
                            prop.children [
                                Html.span [ prop.className "w-4 h-4"; prop.children [ close ] ]
                                Html.span [ prop.text "Clear" ]
                            ]
                        ]
                ]
            ]

            // Divider
            Html.div [ prop.className "border-t border-white/5" ]

            // Filter chips section
            Html.div [
                prop.className "flex flex-wrap gap-4"
                prop.children [
                    // Watch status filter
                    Html.div [
                        prop.className "flex flex-wrap items-center gap-2"
                        prop.children [
                            Html.span [
                                prop.className "text-xs uppercase tracking-wider text-base-content/40 font-medium"
                                prop.text "Status"
                            ]
                            for (status, label) in [
                                (AllStatuses, "All")
                                (FilterNotStarted, "Unwatched")
                                (FilterInProgress, "Watching")
                                (FilterCompleted, "Watched")
                                (FilterAbandoned, "Dropped")
                            ] do
                                let isActive = filters.WatchStatus = status
                                Html.button [
                                    prop.type' "button"
                                    prop.className (
                                        "px-3 py-1 rounded-full text-xs font-medium transition-all " +
                                        if isActive then "bg-primary/20 text-primary border border-primary/30"
                                        else "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70"
                                    )
                                    prop.onClick (fun _ -> dispatch (SetWatchStatusFilter status))
                                    prop.text label
                                ]
                        ]
                    ]

                    // Rating filter
                    Html.div [
                        prop.className "flex flex-wrap items-center gap-2"
                        prop.children [
                            Html.span [
                                prop.className "text-xs uppercase tracking-wider text-base-content/40 font-medium"
                                prop.text "Min Rating"
                            ]
                            // All ratings button
                            Html.button [
                                prop.type' "button"
                                prop.className (
                                    "px-3 py-1 rounded-full text-xs font-medium transition-all " +
                                    if filters.MinRating.IsNone then "bg-yellow-500/20 text-yellow-400 border border-yellow-500/30"
                                    else "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70"
                                )
                                prop.onClick (fun _ -> dispatch (SetMinRatingFilter None))
                                prop.text "Any"
                            ]
                            // Rating icon filter buttons
                            let ratingFilters = [
                                (1, "1+", thumbsDown, "text-red-400", "bg-red-500/20", "border-red-500/30")
                                (2, "2+", minusCircle, "text-orange-400", "bg-orange-500/20", "border-orange-500/30")
                                (3, "3+", handOkay, "text-yellow-400", "bg-yellow-500/20", "border-yellow-500/30")
                                (4, "4+", thumbsUp, "text-lime-400", "bg-lime-500/20", "border-lime-500/30")
                                (5, "5", trophy, "text-amber-400", "bg-amber-500/20", "border-amber-500/30")
                            ]
                            for (rating, label, icon, textColor, bgColor, borderColor) in ratingFilters do
                                let isSelected = filters.MinRating = Some rating
                                Html.button [
                                    prop.type' "button"
                                    prop.className (
                                        "px-3 py-1 rounded-full text-xs font-medium transition-all flex items-center gap-1 " +
                                        if isSelected then $"{bgColor} {textColor} border {borderColor}"
                                        else "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70"
                                    )
                                    prop.onClick (fun _ -> dispatch (SetMinRatingFilter (Some rating)))
                                    prop.children [
                                        Html.span [
                                            prop.className ("w-3 h-3 " + if isSelected then textColor else "")
                                            prop.children [ icon ]
                                        ]
                                        Html.span [ prop.text label ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header
            Html.h2 [
                prop.className "text-2xl font-bold"
                prop.text "Library"
            ]

            // Filter bar
            filterBar model.Filters dispatch

            // Entries grid
            match model.Entries with
            | Loading ->
                Html.div [
                    prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                    prop.children [
                        for _ in 1..12 do
                            Html.div [
                                prop.className "space-y-3"
                                prop.children [
                                    Html.div [ prop.className "skeleton aspect-[2/3] rounded-lg" ]
                                    Html.div [ prop.className "skeleton h-4 w-3/4 rounded" ]
                                    Html.div [ prop.className "skeleton h-3 w-1/2 rounded" ]
                                ]
                            ]
                    ]
                ]
            | Success entries ->
                let filteredEntries = State.filterAndSortEntries model.Filters entries
                if List.isEmpty filteredEntries then
                    Html.div [
                        prop.className "text-center py-16"
                        prop.children [
                            Html.span [
                                prop.className "w-16 h-16 mx-auto mb-4 text-base-content/20 block"
                                prop.children [ library ]
                            ]
                            Html.p [
                                prop.className "text-lg text-base-content/60"
                                prop.text (if List.isEmpty entries then "Your library is empty" else "No entries match your filters")
                            ]
                        ]
                    ]
                else
                    Html.div [
                        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                        prop.children [
                            for entry in filteredEntries do
                                Html.div [
                                    prop.key (EntryId.value entry.Id)
                                    prop.children [
                                        libraryEntryCard entry (fun id isMovie ->
                                            if isMovie then dispatch (ViewMovieDetail id)
                                            else dispatch (ViewSeriesDetail id))
                                    ]
                                ]
                        ]
                    ]
            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading library: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]
