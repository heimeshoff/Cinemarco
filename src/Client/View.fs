module View

open Feliz
open State
open Types
open Shared.Api
open Shared.Domain
open Browser.Types

/// TMDB image base URL (for search results)
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Get poster URL from TMDB CDN (for search results)
let private getTmdbPosterUrl (size: string) (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/{size}{p}"
    | None -> ""

/// Get local cached poster URL (for library items)
let private getLocalPosterUrl (path: string option) =
    match path with
    | Some p ->
        // Remove leading slash from TMDB path
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/posters/{filename}"
    | None -> ""

/// Get local cached backdrop URL (for library items)
let private getLocalBackdropUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/backdrops/{filename}"
    | None -> ""

// =====================================
// Library Filtering & Sorting
// =====================================

/// Get title from library entry
let private getEntryTitle (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.Title
    | LibrarySeries s -> s.Name

/// Get year from library entry
let private getEntryYear (entry: LibraryEntry) =
    match entry.Media with
    | LibraryMovie m -> m.ReleaseDate |> Option.map (fun d -> d.Year)
    | LibrarySeries s -> s.FirstAirDate |> Option.map (fun d -> d.Year)

/// Get rating from library entry
let private getEntryRating (entry: LibraryEntry) =
    entry.PersonalRating |> Option.map PersonalRating.toInt

/// Check if entry matches watch status filter
let private matchesWatchStatus (filter: WatchStatusFilter) (entry: LibraryEntry) =
    match filter with
    | AllStatuses -> true
    | FilterNotStarted ->
        match entry.WatchStatus with
        | NotStarted -> true
        | _ -> false
    | FilterInProgress ->
        match entry.WatchStatus with
        | InProgress _ -> true
        | _ -> false
    | FilterCompleted ->
        match entry.WatchStatus with
        | Completed -> true
        | _ -> false
    | FilterAbandoned ->
        match entry.WatchStatus with
        | Abandoned _ -> true
        | _ -> false

/// Check if entry matches search query
let private matchesSearch (query: string) (entry: LibraryEntry) =
    if System.String.IsNullOrWhiteSpace(query) then true
    else
        let title = getEntryTitle entry
        title.ToLowerInvariant().Contains(query.ToLowerInvariant())

/// Check if entry matches tag filter
let private matchesTags (tagIds: TagId list) (entry: LibraryEntry) =
    if List.isEmpty tagIds then true
    else
        tagIds |> List.exists (fun t -> List.contains t entry.Tags)

/// Check if entry matches minimum rating filter
let private matchesRating (minRating: int option) (entry: LibraryEntry) =
    match minRating with
    | None -> true
    | Some min ->
        match getEntryRating entry with
        | Some rating -> rating >= min
        | None -> false

/// Apply all filters to library entries
let private filterEntries (filters: LibraryFilters) (entries: LibraryEntry list) =
    entries
    |> List.filter (matchesSearch filters.SearchQuery)
    |> List.filter (matchesWatchStatus filters.WatchStatus)
    |> List.filter (matchesTags filters.SelectedTags)
    |> List.filter (matchesRating filters.MinRating)

/// Sort library entries
let private sortEntries (sortBy: LibrarySortBy) (direction: SortDirection) (entries: LibraryEntry list) =
    let sorted =
        match sortBy with
        | SortByDateAdded ->
            entries |> List.sortBy (fun e -> e.DateAdded)
        | SortByTitle ->
            entries |> List.sortBy getEntryTitle
        | SortByYear ->
            entries |> List.sortBy (fun e -> getEntryYear e |> Option.defaultValue 0)
        | SortByRating ->
            entries |> List.sortBy (fun e -> getEntryRating e |> Option.defaultValue 0)
    match direction with
    | Ascending -> sorted
    | Descending -> List.rev sorted

/// Apply filters and sorting to library
let private applyFiltersAndSort (filters: LibraryFilters) (entries: LibraryEntry list) =
    entries
    |> filterEntries filters
    |> sortEntries filters.SortBy filters.SortDirection

/// Get icon for a page
let private getPageIcon (page: Page) =
    match page with
    | HomePage -> Icons.home
    | LibraryPage -> Icons.library
    | MovieDetailPage _ -> Icons.film
    | SeriesDetailPage _ -> Icons.tv
    | FriendsPage -> Icons.friends
    | FriendDetailPage _ -> Icons.friends
    | TagsPage -> Icons.tags
    | TagDetailPage _ -> Icons.tags
    | CollectionsPage -> Icons.collections
    | StatsPage -> Icons.stats
    | TimelinePage -> Icons.timeline
    | GraphPage -> Icons.graph
    | ImportPage -> Icons.import
    | NotFoundPage -> Icons.warning

/// Navigation item component
let private navItem (page: Page) (currentPage: Page) (dispatch: Msg -> unit) =
    let isActive = page = currentPage
    Html.li [
        Html.a [
            prop.className (
                "nav-item cursor-pointer " +
                if isActive then "nav-item-active" else ""
            )
            prop.onClick (fun _ -> dispatch (NavigateTo page))
            prop.children [
                Html.span [
                    prop.className "nav-icon"
                    prop.children [ getPageIcon page ]
                ]
                Html.span [
                    prop.className "font-medium text-sm"
                    prop.text (Page.toString page)
                ]
            ]
        ]
    ]

/// Sidebar navigation component
let private sidebar (model: Model) (dispatch: Msg -> unit) =
    Html.aside [
        prop.className "sidebar fixed left-0 top-0 h-full w-64 hidden lg:flex lg:flex-col z-40"
        prop.children [
            // Logo section
            Html.div [
                prop.className "p-6 border-b border-white/5"
                prop.children [
                    Html.div [
                        prop.className "flex items-center gap-3"
                        prop.children [
                            Html.span [
                                prop.className "text-primary"
                                prop.children [ Icons.clapperboard ]
                            ]
                            Html.div [
                                prop.children [
                                    Html.h1 [
                                        prop.className "text-xl font-bold text-gradient"
                                        prop.text "Cinemarco"
                                    ]
                                    Html.p [
                                        prop.className "text-xs text-base-content/50"
                                        prop.text "Your Cinema Memories"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Navigation items
            Html.nav [
                prop.className "flex-1 p-4 overflow-y-auto"
                prop.children [
                    Html.ul [
                        prop.className "space-y-1"
                        prop.children [
                            navItem HomePage model.CurrentPage dispatch
                            navItem LibraryPage model.CurrentPage dispatch

                            // Search button
                            Html.li [
                                Html.a [
                                    prop.className "nav-item cursor-pointer"
                                    prop.onClick (fun _ -> dispatch OpenSearchModal)
                                    prop.children [
                                        Html.span [
                                            prop.className "nav-icon"
                                            prop.children [ Icons.search ]
                                        ]
                                        Html.span [
                                            prop.className "font-medium text-sm"
                                            prop.text "Search"
                                        ]
                                    ]
                                ]
                            ]

                            // Divider
                            Html.li [
                                prop.className "my-4 border-t border-white/5"
                            ]

                            navItem FriendsPage model.CurrentPage dispatch
                            navItem TagsPage model.CurrentPage dispatch
                            navItem CollectionsPage model.CurrentPage dispatch

                            // Divider
                            Html.li [
                                prop.className "my-4 border-t border-white/5"
                            ]

                            navItem StatsPage model.CurrentPage dispatch
                            navItem TimelinePage model.CurrentPage dispatch
                            navItem GraphPage model.CurrentPage dispatch

                            // Divider
                            Html.li [
                                prop.className "my-4 border-t border-white/5"
                            ]

                            navItem ImportPage model.CurrentPage dispatch
                        ]
                    ]
                ]
            ]

            // Status footer
            Html.div [
                prop.className "p-4 border-t border-white/5"
                prop.children [
                    match model.HealthCheck with
                    | Success health ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-xs"
                            prop.children [
                                Html.span [
                                    prop.className "w-2 h-2 bg-success rounded-full animate-pulse-subtle"
                                ]
                                Html.span [
                                    prop.className "text-base-content/50"
                                    prop.text $"Connected  v{health.Version}"
                                ]
                            ]
                        ]
                    | Loading ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-xs text-base-content/40"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                Html.span [ prop.text "Connecting..." ]
                            ]
                        ]
                    | Failure _ ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-xs text-error/80"
                            prop.children [
                                Html.span [ prop.className "w-2 h-2 bg-error rounded-full" ]
                                Html.span [ prop.text "Offline" ]
                            ]
                        ]
                    | NotAsked -> Html.none
                ]
            ]
        ]
    ]

/// Mobile menu drawer
let private mobileMenuDrawer (model: Model) (dispatch: Msg -> unit) =
    if not model.IsMobileMenuOpen then Html.none
    else
        Html.div [
            prop.className "fixed inset-0 z-50 lg:hidden"
            prop.children [
                // Backdrop
                Html.div [
                    prop.className "fixed inset-0 bg-black/60 backdrop-blur-sm"
                    prop.onClick (fun _ -> dispatch CloseMobileMenu)
                ]
                // Drawer panel (slides from bottom)
                Html.div [
                    prop.className "fixed bottom-0 left-0 right-0 bg-base-100 rounded-t-2xl max-h-[70vh] overflow-y-auto safe-area-bottom animate-slide-up"
                    prop.children [
                        // Handle bar
                        Html.div [
                            prop.className "flex justify-center pt-3 pb-2"
                            prop.children [
                                Html.div [
                                    prop.className "w-10 h-1 bg-base-content/20 rounded-full"
                                ]
                            ]
                        ]
                        // Menu items
                        Html.nav [
                            prop.className "px-4 pb-6"
                            prop.children [
                                Html.ul [
                                    prop.className "space-y-1"
                                    prop.children [
                                        // Primary navigation
                                        for page in [ FriendsPage; TagsPage; CollectionsPage ] do
                                            Html.li [
                                                Html.button [
                                                    prop.className (
                                                        "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                        if model.CurrentPage = page then "bg-primary/10 text-primary" else "text-base-content/70 hover:bg-base-200"
                                                    )
                                                    prop.onClick (fun _ -> dispatch (NavigateTo page))
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-5 h-5"
                                                            prop.children [ getPageIcon page ]
                                                        ]
                                                        Html.span [
                                                            prop.className "font-medium"
                                                            prop.text (Page.toString page)
                                                        ]
                                                    ]
                                                ]
                                            ]

                                        // Divider
                                        Html.li [ prop.className "my-3 border-t border-base-300" ]

                                        // Secondary navigation
                                        for page in [ StatsPage; TimelinePage; GraphPage ] do
                                            Html.li [
                                                Html.button [
                                                    prop.className (
                                                        "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                        if model.CurrentPage = page then "bg-primary/10 text-primary" else "text-base-content/70 hover:bg-base-200"
                                                    )
                                                    prop.onClick (fun _ -> dispatch (NavigateTo page))
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-5 h-5"
                                                            prop.children [ getPageIcon page ]
                                                        ]
                                                        Html.span [
                                                            prop.className "font-medium"
                                                            prop.text (Page.toString page)
                                                        ]
                                                    ]
                                                ]
                                            ]

                                        // Divider
                                        Html.li [ prop.className "my-3 border-t border-base-300" ]

                                        // Import
                                        Html.li [
                                            Html.button [
                                                prop.className (
                                                    "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                    if model.CurrentPage = ImportPage then "bg-primary/10 text-primary" else "text-base-content/70 hover:bg-base-200"
                                                )
                                                prop.onClick (fun _ -> dispatch (NavigateTo ImportPage))
                                                prop.children [
                                                    Html.span [
                                                        prop.className "w-5 h-5"
                                                        prop.children [ getPageIcon ImportPage ]
                                                    ]
                                                    Html.span [
                                                        prop.className "font-medium"
                                                        prop.text "Import"
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

/// Mobile bottom navigation
let private mobileNav (model: Model) (dispatch: Msg -> unit) =
    Html.nav [
        prop.className "fixed bottom-0 left-0 right-0 glass-strong lg:hidden z-40 safe-area-bottom"
        prop.children [
            Html.div [
                prop.className "flex justify-around items-center h-16"
                prop.children [
                    // Home button
                    let isHomeActive = model.CurrentPage = HomePage
                    Html.button [
                        prop.className (
                            "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 " +
                            if isHomeActive then "text-primary scale-105" else "text-base-content/50 hover:text-base-content/80"
                        )
                        prop.onClick (fun _ -> dispatch (NavigateTo HomePage))
                        prop.children [
                            Html.span [
                                prop.className (if isHomeActive then "scale-110 transition-transform" else "transition-transform")
                                prop.children [ getPageIcon HomePage ]
                            ]
                            Html.span [
                                prop.className "text-xs font-medium"
                                prop.text "Home"
                            ]
                        ]
                    ]

                    // Library button
                    let isLibraryActive = model.CurrentPage = LibraryPage
                    Html.button [
                        prop.className (
                            "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 " +
                            if isLibraryActive then "text-primary scale-105" else "text-base-content/50 hover:text-base-content/80"
                        )
                        prop.onClick (fun _ -> dispatch (NavigateTo LibraryPage))
                        prop.children [
                            Html.span [
                                prop.className (if isLibraryActive then "scale-110 transition-transform" else "transition-transform")
                                prop.children [ getPageIcon LibraryPage ]
                            ]
                            Html.span [
                                prop.className "text-xs font-medium"
                                prop.text "Library"
                            ]
                        ]
                    ]

                    // Search button
                    Html.button [
                        prop.className "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 text-base-content/50 hover:text-base-content/80"
                        prop.onClick (fun _ -> dispatch OpenSearchModal)
                        prop.children [
                            Html.span [
                                prop.className "transition-transform"
                                prop.children [ Icons.search ]
                            ]
                            Html.span [
                                prop.className "text-xs font-medium"
                                prop.text "Search"
                            ]
                        ]
                    ]

                    // Menu button
                    Html.button [
                        prop.className (
                            "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 " +
                            if model.IsMobileMenuOpen then "text-primary scale-105" else "text-base-content/50 hover:text-base-content/80"
                        )
                        prop.onClick (fun _ -> dispatch ToggleMobileMenu)
                        prop.children [
                            Html.span [
                                prop.className (if model.IsMobileMenuOpen then "scale-110 transition-transform" else "transition-transform")
                                prop.children [ Icons.menu ]
                            ]
                            Html.span [
                                prop.className "text-xs font-medium"
                                prop.text "Menu"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Poster card component with shine effect (for search results)
let private posterCard (item: TmdbSearchResult) (dispatch: Msg -> unit) =
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
        prop.onClick (fun _ -> dispatch (OpenQuickAddModal item))
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
                                    prop.children [ Icons.film ]
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
                                            | Movie -> Icons.film
                                            | Series -> Icons.tv
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
                                        prop.children [ Icons.plus ]
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
                        prop.className "font-medium text-sm truncate text-base-content/90 group-hover:text-white transition-colors"
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

/// Search results dropdown
let private searchResultsDropdown (model: Model) (dispatch: Msg -> unit) =
    if not model.Search.IsDropdownOpen then Html.none
    else
        Html.div [
            prop.className "search-dropdown max-h-[70vh] overflow-y-auto"
            prop.children [
                match model.Search.Results with
                | Loading ->
                    Html.div [
                        prop.className "p-12 flex flex-col items-center justify-center gap-3"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-lg text-primary" ]
                            Html.span [ prop.className "text-sm text-base-content/50"; prop.text "Searching..." ]
                        ]
                    ]
                | Success results when List.isEmpty results ->
                    Html.div [
                        prop.className "p-12 text-center"
                        prop.children [
                            Html.span [
                                prop.className "text-4xl opacity-30 mb-3 block"
                                prop.children [ Icons.film ]
                            ]
                            Html.p [
                                prop.className "text-base-content/60"
                                prop.text "No results found"
                            ]
                            Html.p [
                                prop.className "text-sm text-base-content/40 mt-1"
                                prop.text "Try a different search term"
                            ]
                        ]
                    ]
                | Success results ->
                    Html.div [
                        prop.className "p-4"
                        prop.children [
                            Html.p [
                                prop.className "text-xs text-base-content/50 mb-3 px-1"
                                prop.text $"Found {List.length results} results"
                            ]
                            Html.div [
                                prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-4"
                                prop.children [
                                    for item in results |> List.truncate 12 do
                                        posterCard item dispatch
                                ]
                            ]
                        ]
                    ]
                | Failure err ->
                    Html.div [
                        prop.className "p-8 text-center"
                        prop.children [
                            Html.span [
                                prop.className "text-3xl mb-2 block"
                                prop.children [ Icons.error ]
                            ]
                            Html.p [
                                prop.className "text-error"
                                prop.text $"Error: {err}"
                            ]
                        ]
                    ]
                | NotAsked ->
                    Html.none
            ]
        ]

/// Search modal component
let private searchModal (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-start justify-center pt-[10vh] p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "modal-backdrop"
                prop.onClick (fun _ -> dispatch CloseModal)
            ]
            // Modal content
            Html.div [
                prop.className "modal-content relative w-full max-w-2xl"
                prop.children [
                    // Search input
                    Html.div [
                        prop.className "p-4 border-b border-base-300/50"
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.input [
                                        prop.className "w-full pl-12 pr-4 py-3 bg-transparent text-lg placeholder:text-base-content/30 focus:outline-none"
                                        prop.placeholder "Search movies and series..."
                                        prop.value model.Search.Query
                                        prop.autoFocus true
                                        prop.onChange (fun (e: string) -> dispatch (SearchQueryChanged e))
                                        prop.onKeyDown (fun e ->
                                            if e.key = "Escape" then dispatch CloseModal
                                        )
                                    ]
                                    Html.span [
                                        prop.className "absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-base-content/40"
                                        prop.children [ Icons.search ]
                                    ]
                                    if RemoteData.isLoading model.Search.Results then
                                        Html.span [
                                            prop.className "absolute right-4 top-1/2 -translate-y-1/2"
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner loading-sm text-primary" ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                    // Results area
                    Html.div [
                        prop.className "max-h-[60vh] overflow-y-auto"
                        prop.children [
                            match model.Search.Results with
                            | Loading ->
                                Html.div [
                                    prop.className "p-12 flex flex-col items-center justify-center gap-3"
                                    prop.children [
                                        Html.span [ prop.className "loading loading-spinner loading-lg text-primary" ]
                                        Html.span [ prop.className "text-sm text-base-content/50"; prop.text "Searching..." ]
                                    ]
                                ]
                            | Success results when List.isEmpty results ->
                                Html.div [
                                    prop.className "p-12 text-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-4xl opacity-30 mb-3 block"
                                            prop.children [ Icons.film ]
                                        ]
                                        Html.p [
                                            prop.className "text-base-content/60"
                                            prop.text "No results found"
                                        ]
                                        Html.p [
                                            prop.className "text-sm text-base-content/40 mt-1"
                                            prop.text "Try a different search term"
                                        ]
                                    ]
                                ]
                            | Success results ->
                                Html.div [
                                    prop.className "p-4"
                                    prop.children [
                                        Html.p [
                                            prop.className "text-xs text-base-content/50 mb-3 px-1"
                                            prop.text $"Found {List.length results} results"
                                        ]
                                        Html.div [
                                            prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-4"
                                            prop.children [
                                                for item in results |> List.truncate 15 do
                                                    posterCard item dispatch
                                            ]
                                        ]
                                    ]
                                ]
                            | Failure err ->
                                Html.div [
                                    prop.className "p-8 text-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-3xl mb-2 block"
                                            prop.children [ Icons.error ]
                                        ]
                                        Html.p [
                                            prop.className "text-error"
                                            prop.text $"Error: {err}"
                                        ]
                                    ]
                                ]
                            | NotAsked ->
                                Html.div [
                                    prop.className "p-8 text-center text-base-content/40"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-12 h-12 mx-auto mb-3 opacity-30 block"
                                            prop.children [ Icons.search ]
                                        ]
                                        Html.p [ prop.text "Start typing to search" ]
                                    ]
                                ]
                        ]
                    ]
                    // Footer with keyboard hints
                    Html.div [
                        prop.className "p-3 border-t border-base-300/50 flex items-center justify-end gap-4 text-xs text-base-content/40"
                        prop.children [
                            Html.span [
                                prop.className "flex items-center gap-1"
                                prop.children [
                                    Html.kbd [ prop.className "px-1.5 py-0.5 bg-base-300/50 rounded text-[10px]"; prop.text "ESC" ]
                                    Html.span [ prop.text "to close" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Quick add modal
let private quickAddModal (state: QuickAddModalState) (friends: Friend list) (tags: Tag list) (dispatch: Msg -> unit) =
    let year =
        state.SelectedItem.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let mediaTypeLabel =
        match state.SelectedItem.MediaType with
        | Movie -> "Movie"
        | Series -> "Series"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "modal-backdrop"
                prop.onClick (fun _ -> if not state.IsSubmitting then dispatch CloseModal)
            ]
            // Modal content
            Html.div [
                prop.className "modal-content relative w-full max-w-lg max-h-[90vh] overflow-y-auto"
                prop.children [
                    // Header with poster background
                    Html.div [
                        prop.className "relative h-52 overflow-hidden"
                        prop.children [
                            // Background poster
                            match state.SelectedItem.PosterPath with
                            | Some _ ->
                                Html.img [
                                    prop.src (getTmdbPosterUrl "w500" state.SelectedItem.PosterPath)
                                    prop.alt state.SelectedItem.Title
                                    prop.className "w-full h-full object-cover opacity-40 blur-sm scale-110"
                                ]
                            | None ->
                                Html.div [
                                    prop.className "w-full h-full bg-gradient-to-br from-primary/20 to-secondary/20"
                                ]

                            // Gradient overlay
                            Html.div [
                                prop.className "absolute inset-0 bg-gradient-to-t from-base-100 via-base-100/80 to-transparent"
                            ]

                            // Close button
                            Html.button [
                                prop.className "absolute top-4 right-4 w-8 h-8 rounded-full glass flex items-center justify-center hover:bg-white/10 transition-colors"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    Html.span [
                                        prop.className "w-4 h-4 text-base-content/70"
                                        prop.children [ Icons.close ]
                                    ]
                                ]
                            ]

                            // Title and info
                            Html.div [
                                prop.className "absolute bottom-4 left-4 right-4 flex gap-4"
                                prop.children [
                                    // Small poster thumbnail
                                    match state.SelectedItem.PosterPath with
                                    | Some _ ->
                                        Html.img [
                                            prop.src (getTmdbPosterUrl "w185" state.SelectedItem.PosterPath)
                                            prop.alt state.SelectedItem.Title
                                            prop.className "w-16 h-24 rounded-lg object-cover shadow-lg"
                                        ]
                                    | None -> Html.none

                                    Html.div [
                                        prop.className "flex-1"
                                        prop.children [
                                            Html.h2 [
                                                prop.className "text-xl font-bold"
                                                prop.text state.SelectedItem.Title
                                            ]
                                            Html.div [
                                                prop.className "flex items-center gap-2 mt-1 text-sm text-base-content/60"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "flex items-center gap-1"
                                                        prop.children [
                                                            Html.span [
                                                                prop.className "w-4 h-4"
                                                                prop.children [
                                                                    match state.SelectedItem.MediaType with
                                                                    | Movie -> Icons.film
                                                                    | Series -> Icons.tv
                                                                ]
                                                            ]
                                                            Html.span [ prop.text mediaTypeLabel ]
                                                        ]
                                                    ]
                                                    if year <> "" then
                                                        Html.span [
                                                            prop.className "w-1 h-1 rounded-full bg-base-content/30"
                                                        ]
                                                        Html.span [ prop.text year ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-6"
                        prop.children [
                            // Error message
                            match state.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "flex items-center gap-3 p-4 rounded-lg bg-error/10 border border-error/30"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-5 h-5 text-error"
                                            prop.children [ Icons.error ]
                                        ]
                                        Html.span [
                                            prop.className "text-sm text-error"
                                            prop.text err
                                        ]
                                    ]
                                ]
                            | None -> Html.none

                            // Why I added note
                            Html.div [
                                prop.className "space-y-2"
                                prop.children [
                                    Html.label [
                                        prop.className "text-sm font-medium text-base-content/70"
                                        prop.text "Why are you adding this?"
                                    ]
                                    Html.textarea [
                                        prop.className "w-full px-4 py-3 bg-base-200/50 border border-white/5 rounded-lg text-sm placeholder:text-base-content/30 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 transition-all resize-none"
                                        prop.rows 3
                                        prop.placeholder "e.g., Recommended by Sarah, saw the trailer..."
                                        prop.value state.WhyAddedNote
                                        prop.onChange (fun (e: string) -> dispatch (QuickAddNoteChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Tags selection
                            if not (List.isEmpty tags) then
                                Html.div [
                                    prop.className "space-y-3"
                                    prop.children [
                                        Html.label [
                                            prop.className "flex items-center gap-2 text-sm font-medium text-base-content/70"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-4 h-4"
                                                    prop.children [ Icons.tags ]
                                                ]
                                                Html.span [ prop.text "Tags" ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "flex flex-wrap gap-2"
                                            prop.children [
                                                for tag in tags do
                                                    let isSelected = List.contains tag.Id state.SelectedTags
                                                    Html.button [
                                                        prop.type' "button"
                                                        prop.className (
                                                            "px-3 py-1.5 rounded-full text-sm font-medium transition-all " +
                                                            if isSelected then "bg-primary text-primary-content shadow-glow-primary"
                                                            else "bg-base-200/50 text-base-content/60 hover:bg-base-200 hover:text-base-content"
                                                        )
                                                        prop.onClick (fun _ -> dispatch (ToggleQuickAddTag tag.Id))
                                                        prop.disabled state.IsSubmitting
                                                        prop.text tag.Name
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Friends selection
                            if not (List.isEmpty friends) then
                                Html.div [
                                    prop.className "space-y-3"
                                    prop.children [
                                        Html.label [
                                            prop.className "flex items-center gap-2 text-sm font-medium text-base-content/70"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-4 h-4"
                                                    prop.children [ Icons.friends ]
                                                ]
                                                Html.span [ prop.text "Watch with" ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "flex flex-wrap gap-2"
                                            prop.children [
                                                for friend in friends do
                                                    let isSelected = List.contains friend.Id state.SelectedFriends
                                                    Html.button [
                                                        prop.type' "button"
                                                        prop.className (
                                                            "px-3 py-1.5 rounded-full text-sm font-medium transition-all " +
                                                            if isSelected then "bg-secondary text-secondary-content shadow-glow-secondary"
                                                            else "bg-base-200/50 text-base-content/60 hover:bg-base-200 hover:text-base-content"
                                                        )
                                                        prop.onClick (fun _ -> dispatch (ToggleQuickAddFriend friend.Id))
                                                        prop.disabled state.IsSubmitting
                                                        prop.text friend.Name
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Divider
                            Html.div [ prop.className "border-t border-white/5" ]

                            // Submit button
                            Html.button [
                                prop.className "btn-gradient w-full py-3 rounded-lg font-semibold transition-all disabled:opacity-50"
                                prop.onClick (fun _ -> dispatch SubmitQuickAdd)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    if state.IsSubmitting then
                                        Html.span [
                                            prop.className "flex items-center justify-center gap-2"
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                Html.span [ prop.text "Adding..." ]
                                            ]
                                        ]
                                    else
                                        Html.span [
                                            prop.className "flex items-center justify-center gap-2"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-5 h-5"
                                                    prop.children [ Icons.plus ]
                                                ]
                                                Html.span [ prop.text "Add to Library" ]
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
// Filter Bar Component
// =====================================

/// Filter bar for library view
let private filterBar (filters: LibraryFilters) (tags: Tag list) (dispatch: Msg -> unit) =
    let hasActiveFilters =
        filters.SearchQuery <> "" ||
        filters.WatchStatus <> AllStatuses ||
        not (List.isEmpty filters.SelectedTags) ||
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
                                prop.onChange (fun (e: string) -> dispatch (SetLibrarySearchQuery e))
                            ]
                            Html.span [
                                prop.className "absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-base-content/40"
                                prop.children [ Icons.filter ]
                            ]
                        ]
                    ]

                    // Sort controls
                    Html.div [
                        prop.className "flex items-center gap-2"
                        prop.children [
                            Html.span [
                                prop.className "w-4 h-4 text-base-content/40"
                                prop.children [ Icons.sort ]
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
                                        prop.children [ Icons.chevronUp ]
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
                                Html.span [
                                    prop.className "w-4 h-4"
                                    prop.children [ Icons.close ]
                                ]
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

                    // Tag filters (if tags exist)
                    if not (List.isEmpty tags) then
                        Html.div [
                            prop.className "flex flex-wrap items-center gap-2"
                            prop.children [
                                Html.span [
                                    prop.className "text-xs uppercase tracking-wider text-base-content/40 font-medium"
                                    prop.text "Tags"
                                ]
                                for tag in tags do
                                    let isSelected = List.contains tag.Id filters.SelectedTags
                                    Html.button [
                                        prop.type' "button"
                                        prop.className (
                                            "px-3 py-1 rounded-full text-xs font-medium transition-all " +
                                            if isSelected then "bg-secondary/20 text-secondary border border-secondary/30"
                                            else "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70"
                                        )
                                        prop.onClick (fun _ -> dispatch (ToggleTagFilter tag.Id))
                                        prop.text tag.Name
                                    ]
                            ]
                        ]

                    // Rating filter
                    Html.div [
                        prop.className "flex flex-wrap items-center gap-2"
                        prop.children [
                            Html.span [
                                prop.className "text-xs uppercase tracking-wider text-base-content/40 font-medium"
                                prop.text "Rating"
                            ]
                            for rating in [ None; Some 1; Some 2; Some 3; Some 4; Some 5 ] do
                                let label =
                                    match rating with
                                    | None -> "Any"
                                    | Some r -> String.replicate r "\u2605"
                                let isSelected = filters.MinRating = rating
                                Html.button [
                                    prop.type' "button"
                                    prop.className (
                                        "px-3 py-1 rounded-full text-xs font-medium transition-all " +
                                        if isSelected then "bg-accent/20 text-accent border border-accent/30"
                                        else "bg-base-100/30 text-base-content/50 border border-white/5 hover:bg-base-100/50 hover:text-base-content/70"
                                    )
                                    prop.onClick (fun _ -> dispatch (SetMinRatingFilter rating))
                                    prop.text label
                                ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Detail Page Components
// =====================================

/// Helper to format watch status for display
let private formatWatchStatus (status: WatchStatus) =
    match status with
    | NotStarted -> "Not Started"
    | InProgress progress ->
        match progress.CurrentSeason, progress.CurrentEpisode with
        | Some s, Some e -> $"In Progress (S{s}E{e})"
        | Some s, None -> $"In Progress (Season {s})"
        | None, Some e -> $"In Progress (Episode {e})"
        | None, None -> "In Progress"
    | Completed -> "Completed"
    | Abandoned info ->
        match info.Reason with
        | Some reason -> $"Abandoned: {reason}"
        | None -> "Abandoned"

/// Helper to format date
let private formatDate (dt: System.DateTime option) =
    dt |> Option.map (fun d -> d.ToString("MMM d, yyyy")) |> Option.defaultValue "Unknown"

// =====================================
// Watch Status Components
// =====================================

/// Watch status badge component
let private watchStatusBadge (status: WatchStatus) =
    let (text, badgeClass) =
        match status with
        | NotStarted -> "Not Started", "badge-ghost"
        | InProgress _ -> "In Progress", "badge-info"
        | Completed -> "Completed", "badge-success"
        | Abandoned _ -> "Abandoned", "badge-error"

    Html.span [
        prop.className $"badge {badgeClass}"
        prop.text text
    ]

/// Progress bar component
let private progressBar (current: int) (total: int) =
    let percentage = if total > 0 then float current / float total * 100.0 else 0.0
    Html.div [
        prop.className "w-full"
        prop.children [
            Html.div [
                prop.className "flex justify-between text-sm mb-1"
                prop.children [
                    Html.span [ prop.text $"{current} / {total}" ]
                    Html.span [ prop.text $"{percentage:F0}%%" ]
                ]
            ]
            Html.div [
                prop.className "w-full bg-base-300 rounded-full h-2"
                prop.children [
                    Html.div [
                        prop.className "bg-primary h-2 rounded-full transition-all duration-300"
                        prop.style [ Feliz.style.width (Feliz.length.percent (int percentage)) ]
                    ]
                ]
            ]
        ]
    ]

/// Movie watch status controls
let private movieWatchControls (entry: LibraryEntry) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-3"
        prop.children [
            match entry.WatchStatus with
            | NotStarted ->
                Html.button [
                    prop.className "btn btn-primary btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (MarkMovieWatched entry.Id))
                    prop.text "Mark as Watched"
                ]
                Html.button [
                    prop.className "btn btn-outline btn-error btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (OpenAbandonModal entry.Id))
                    prop.text "Abandon"
                ]
            | InProgress _ ->
                Html.button [
                    prop.className "btn btn-primary btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (MarkMovieWatched entry.Id))
                    prop.text "Mark as Completed"
                ]
                Html.button [
                    prop.className "btn btn-outline btn-error btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (OpenAbandonModal entry.Id))
                    prop.text "Abandon"
                ]
            | Completed ->
                Html.button [
                    prop.className "btn btn-outline btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (MarkMovieUnwatched entry.Id))
                    prop.text "Mark as Unwatched"
                ]
            | Abandoned _ ->
                Html.button [
                    prop.className "btn btn-primary btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (ResumeEntry entry.Id))
                    prop.text "Resume"
                ]
                Html.button [
                    prop.className "btn btn-outline btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (MarkMovieWatched entry.Id))
                    prop.text "Mark as Completed"
                ]
        ]
    ]

/// Series watch status controls (includes progress bar)
let private seriesWatchControls (entry: LibraryEntry) (series: Series) (watchedCount: int) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-3"
        prop.children [
            // Progress bar
            progressBar watchedCount series.NumberOfEpisodes

            // Action buttons based on status
            match entry.WatchStatus with
            | NotStarted | InProgress _ ->
                if watchedCount >= series.NumberOfEpisodes then
                    Html.button [
                        prop.className "btn btn-primary btn-sm w-full"
                        prop.onClick (fun _ -> dispatch (MarkSeriesCompleted entry.Id))
                        prop.text "Mark as Completed"
                    ]
                Html.button [
                    prop.className "btn btn-outline btn-error btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (OpenAbandonModal entry.Id))
                    prop.text "Abandon"
                ]
            | Completed ->
                Html.div [
                    prop.className "text-center text-success text-sm"
                    prop.text "Series Completed"
                ]
            | Abandoned _ ->
                Html.button [
                    prop.className "btn btn-primary btn-sm w-full"
                    prop.onClick (fun _ -> dispatch (ResumeEntry entry.Id))
                    prop.text "Resume Watching"
                ]
        ]
    ]

/// Episode checkbox component
let private episodeCheckbox (entryId: EntryId) (seasonNum: int) (epNum: int) (isWatched: bool) (dispatch: Msg -> unit) =
    let stateClass = if isWatched then "bg-success/20 border-success" else "bg-base-200 border-base-300 hover:border-primary"
    Html.label [
        prop.className $"cursor-pointer p-2 rounded border transition-colors {stateClass}"
        prop.children [
            Html.input [
                prop.type' "checkbox"
                prop.className "hidden"
                prop.isChecked isWatched
                prop.onChange (fun (checked': bool) ->
                    dispatch (ToggleEpisodeWatched (entryId, seasonNum, epNum, checked')))
            ]
            Html.span [
                prop.className "text-xs font-medium"
                prop.text $"E{epNum}"
            ]
        ]
    ]

/// Episode grid for a single season
let private seasonEpisodeGrid (entryId: EntryId) (seasonNum: int) (episodeCount: int) (progress: EpisodeProgress list) (dispatch: Msg -> unit) =
    let watchedInSeason =
        progress
        |> List.filter (fun p -> p.SeasonNumber = seasonNum && p.IsWatched)
        |> List.length

    Html.div [
        prop.className "card bg-base-200"
        prop.children [
            Html.div [
                prop.className "card-body p-4"
                prop.children [
                    // Season header
                    Html.div [
                        prop.className "flex justify-between items-center mb-3"
                        prop.children [
                            Html.h3 [
                                prop.className "font-semibold"
                                prop.text $"Season {seasonNum}"
                            ]
                            Html.div [
                                prop.className "flex items-center gap-2"
                                prop.children [
                                    Html.span [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text $"{watchedInSeason}/{episodeCount}"
                                    ]
                                    Html.button [
                                        prop.className "btn btn-xs btn-ghost"
                                        prop.onClick (fun _ -> dispatch (MarkSeasonWatched (entryId, seasonNum)))
                                        prop.text "Mark All"
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Episode grid
                    Html.div [
                        prop.className "grid grid-cols-5 sm:grid-cols-8 md:grid-cols-10 gap-1"
                        prop.children [
                            for epNum in 1 .. episodeCount do
                                let isWatched =
                                    progress
                                    |> List.exists (fun p -> p.SeasonNumber = seasonNum && p.EpisodeNumber = epNum && p.IsWatched)
                                episodeCheckbox entryId seasonNum epNum isWatched dispatch
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Movie detail page
let private movieDetailPage (entry: LibraryEntry) (movie: Movie) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    let entryTags = tags |> List.filter (fun t -> List.contains t.Id entry.Tags)
    let entryFriends = friends |> List.filter (fun f -> List.contains f.Id entry.Friends)

    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm"
                prop.onClick (fun _ -> dispatch (NavigateTo LibraryPage))
                prop.text "< Back to Library"
            ]

            // Hero section with backdrop
            Html.div [
                prop.className "relative rounded-xl overflow-hidden"
                prop.children [
                    // Backdrop image
                    match movie.BackdropPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getLocalBackdropUrl movie.BackdropPath)
                            prop.alt movie.Title
                            prop.className "w-full h-64 md:h-80 object-cover"
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-64 md:h-80 bg-gradient-to-r from-primary/20 to-secondary/20"
                        ]

                    // Gradient overlay
                    Html.div [
                        prop.className "absolute inset-0 bg-gradient-to-t from-base-100 via-base-100/80 to-transparent"
                    ]

                    // Content overlay
                    Html.div [
                        prop.className "absolute bottom-0 left-0 right-0 p-6 flex gap-6"
                        prop.children [
                            // Poster
                            Html.div [
                                prop.className "hidden md:block w-32 lg:w-40 flex-shrink-0"
                                prop.children [
                                    match movie.PosterPath with
                                    | Some _ ->
                                        Html.img [
                                            prop.src (getLocalPosterUrl movie.PosterPath)
                                            prop.alt movie.Title
                                            prop.className "w-full rounded-lg shadow-xl"
                                        ]
                                    | None ->
                                        Html.div [
                                            prop.className "w-full aspect-[2/3] bg-base-300 rounded-lg flex items-center justify-center"
                                            prop.children [
                                                Html.span [ prop.className "text-4xl text-base-content/30"; prop.text "?" ]
                                            ]
                                        ]
                                ]
                            ]

                            // Title and meta info
                            Html.div [
                                prop.className "flex-1"
                                prop.children [
                                    Html.h1 [
                                        prop.className "text-3xl md:text-4xl font-bold"
                                        prop.text movie.Title
                                    ]
                                    Html.div [
                                        prop.className "flex flex-wrap gap-2 mt-2 text-base-content/70"
                                        prop.children [
                                            match movie.ReleaseDate with
                                            | Some d ->
                                                Html.span [ prop.text (d.Year.ToString()) ]
                                            | None -> Html.none

                                            match movie.RuntimeMinutes with
                                            | Some mins ->
                                                Html.span [ prop.text $"{mins} min" ]
                                            | None -> Html.none

                                            if not (List.isEmpty movie.Genres) then
                                                Html.span [ prop.text (String.concat ", " movie.Genres) ]
                                        ]
                                    ]
                                    match movie.Tagline with
                                    | Some tagline ->
                                        Html.p [
                                            prop.className "italic text-base-content/60 mt-2"
                                            prop.text $"\"{tagline}\""
                                        ]
                                    | None -> Html.none
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Main content grid
            Html.div [
                prop.className "grid md:grid-cols-3 gap-6"
                prop.children [
                    // Left column - Overview and details
                    Html.div [
                        prop.className "md:col-span-2 space-y-6"
                        prop.children [
                            // Overview
                            match movie.Overview with
                            | Some overview ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Overview" ]
                                                Html.p [ prop.className "text-base-content/80"; prop.text overview ]
                                            ]
                                        ]
                                    ]
                                ]
                            | None -> Html.none

                            // Why added
                            match entry.WhyAdded with
                            | Some whyAdded when whyAdded.Context.IsSome || whyAdded.RecommendedByName.IsSome || whyAdded.Source.IsSome ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Why I Added This" ]
                                                match whyAdded.Context with
                                                | Some context -> Html.p [ prop.text context ]
                                                | None -> Html.none
                                                match whyAdded.RecommendedByName with
                                                | Some name -> Html.p [ prop.className "text-sm text-base-content/60"; prop.text $"Recommended by: {name}" ]
                                                | None -> Html.none
                                                match whyAdded.Source with
                                                | Some source -> Html.p [ prop.className "text-sm text-base-content/60"; prop.text $"Source: {source}" ]
                                                | None -> Html.none
                                            ]
                                        ]
                                    ]
                                ]
                            | _ -> Html.none

                            // Notes
                            match entry.Notes with
                            | Some notes ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Notes" ]
                                                Html.p [ prop.text notes ]
                                            ]
                                        ]
                                    ]
                                ]
                            | None -> Html.none
                        ]
                    ]

                    // Right column - Status and metadata
                    Html.div [
                        prop.className "space-y-4"
                        prop.children [
                            // Watch status card
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h2 [ prop.className "card-title"; prop.text "Status" ]

                                            Html.div [
                                                prop.className "space-y-3"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex justify-between items-center"
                                                        prop.children [
                                                            Html.span [ prop.className "text-base-content/70"; prop.text "Watch Status" ]
                                                            watchStatusBadge entry.WatchStatus
                                                        ]
                                                    ]

                                                    match entry.PersonalRating with
                                                    | Some rating ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.className "text-base-content/70"; prop.text "My Rating" ]
                                                                Html.span [ prop.className "font-medium text-yellow-400"; prop.text (String.replicate (PersonalRating.toInt rating) "*") ]
                                                            ]
                                                        ]
                                                    | None -> Html.none

                                                    Html.div [
                                                        prop.className "flex justify-between"
                                                        prop.children [
                                                            Html.span [ prop.className "text-base-content/70"; prop.text "Added" ]
                                                            Html.span [ prop.className "font-medium"; prop.text (entry.DateAdded.ToString("MMM d, yyyy")) ]
                                                        ]
                                                    ]

                                                    if entry.IsFavorite then
                                                        Html.div [
                                                            prop.className "badge badge-warning"
                                                            prop.text "Favorite"
                                                        ]

                                                    // Watch controls
                                                    Html.div [
                                                        prop.className "divider my-2"
                                                    ]
                                                    movieWatchControls entry dispatch
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Tags
                            if not (List.isEmpty entryTags) then
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Tags" ]
                                                Html.div [
                                                    prop.className "flex flex-wrap gap-2"
                                                    prop.children [
                                                        for tag in entryTags do
                                                            Html.span [ prop.className "badge badge-secondary"; prop.text tag.Name ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Watched with
                            if not (List.isEmpty entryFriends) then
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Watched With" ]
                                                Html.div [
                                                    prop.className "flex flex-wrap gap-2"
                                                    prop.children [
                                                        for friend in entryFriends do
                                                            Html.span [ prop.className "badge badge-primary"; prop.text friend.Name ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]

                            // TMDB info
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h2 [ prop.className "card-title"; prop.text "Info" ]
                                            Html.div [
                                                prop.className "space-y-1 text-sm"
                                                prop.children [
                                                    match movie.VoteAverage with
                                                    | Some avg ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.text "TMDB Rating" ]
                                                                Html.span [ prop.text $"{avg:F1}/10" ]
                                                            ]
                                                        ]
                                                    | None -> Html.none

                                                    match movie.OriginalLanguage with
                                                    | Some lang ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.text "Language" ]
                                                                Html.span [ prop.text (lang.ToUpperInvariant()) ]
                                                            ]
                                                        ]
                                                    | None -> Html.none
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Delete button
                            Html.button [
                                prop.className "btn btn-error btn-outline w-full"
                                prop.onClick (fun _ -> dispatch (OpenDeleteEntryModal entry.Id))
                                prop.text "Remove from Library"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Series detail page
let private seriesDetailPage (entry: LibraryEntry) (series: Series) (tags: Tag list) (friends: Friend list) (progress: EpisodeProgress list) (dispatch: Msg -> unit) =
    let entryTags = tags |> List.filter (fun t -> List.contains t.Id entry.Tags)
    let entryFriends = friends |> List.filter (fun f -> List.contains f.Id entry.Friends)
    let watchedCount = progress |> List.filter (fun p -> p.IsWatched) |> List.length

    let seriesStatusText =
        match series.Status with
        | Returning -> "Returning Series"
        | Ended -> "Ended"
        | Canceled -> "Canceled"
        | InProduction -> "In Production"
        | Planned -> "Planned"
        | Unknown -> "Unknown"

    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm"
                prop.onClick (fun _ -> dispatch (NavigateTo LibraryPage))
                prop.text "< Back to Library"
            ]

            // Hero section with backdrop
            Html.div [
                prop.className "relative rounded-xl overflow-hidden"
                prop.children [
                    // Backdrop image
                    match series.BackdropPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getLocalBackdropUrl series.BackdropPath)
                            prop.alt series.Name
                            prop.className "w-full h-64 md:h-80 object-cover"
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-64 md:h-80 bg-gradient-to-r from-primary/20 to-secondary/20"
                        ]

                    // Gradient overlay
                    Html.div [
                        prop.className "absolute inset-0 bg-gradient-to-t from-base-100 via-base-100/80 to-transparent"
                    ]

                    // Content overlay
                    Html.div [
                        prop.className "absolute bottom-0 left-0 right-0 p-6 flex gap-6"
                        prop.children [
                            // Poster
                            Html.div [
                                prop.className "hidden md:block w-32 lg:w-40 flex-shrink-0"
                                prop.children [
                                    match series.PosterPath with
                                    | Some _ ->
                                        Html.img [
                                            prop.src (getLocalPosterUrl series.PosterPath)
                                            prop.alt series.Name
                                            prop.className "w-full rounded-lg shadow-xl"
                                        ]
                                    | None ->
                                        Html.div [
                                            prop.className "w-full aspect-[2/3] bg-base-300 rounded-lg flex items-center justify-center"
                                            prop.children [
                                                Html.span [ prop.className "text-4xl text-base-content/30"; prop.text "?" ]
                                            ]
                                        ]
                                ]
                            ]

                            // Title and meta info
                            Html.div [
                                prop.className "flex-1"
                                prop.children [
                                    Html.h1 [
                                        prop.className "text-3xl md:text-4xl font-bold"
                                        prop.text series.Name
                                    ]
                                    Html.div [
                                        prop.className "flex flex-wrap gap-2 mt-2 text-base-content/70"
                                        prop.children [
                                            match series.FirstAirDate with
                                            | Some d ->
                                                Html.span [ prop.text $"{d.Year}" ]
                                            | None -> Html.none

                                            Html.span [ prop.text $"{series.NumberOfSeasons} Seasons" ]
                                            Html.span [ prop.text $"{series.NumberOfEpisodes} Episodes" ]
                                            Html.span [ prop.className "badge badge-outline badge-sm"; prop.text seriesStatusText ]
                                        ]
                                    ]
                                    if not (List.isEmpty series.Genres) then
                                        Html.p [
                                            prop.className "text-base-content/60 mt-2"
                                            prop.text (String.concat ", " series.Genres)
                                        ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Main content grid
            Html.div [
                prop.className "grid md:grid-cols-3 gap-6"
                prop.children [
                    // Left column - Overview and details
                    Html.div [
                        prop.className "md:col-span-2 space-y-6"
                        prop.children [
                            // Overview
                            match series.Overview with
                            | Some overview ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Overview" ]
                                                Html.p [ prop.className "text-base-content/80"; prop.text overview ]
                                            ]
                                        ]
                                    ]
                                ]
                            | None -> Html.none

                            // Why added
                            match entry.WhyAdded with
                            | Some whyAdded when whyAdded.Context.IsSome || whyAdded.RecommendedByName.IsSome || whyAdded.Source.IsSome ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Why I Added This" ]
                                                match whyAdded.Context with
                                                | Some context -> Html.p [ prop.text context ]
                                                | None -> Html.none
                                                match whyAdded.RecommendedByName with
                                                | Some name -> Html.p [ prop.className "text-sm text-base-content/60"; prop.text $"Recommended by: {name}" ]
                                                | None -> Html.none
                                                match whyAdded.Source with
                                                | Some source -> Html.p [ prop.className "text-sm text-base-content/60"; prop.text $"Source: {source}" ]
                                                | None -> Html.none
                                            ]
                                        ]
                                    ]
                                ]
                            | _ -> Html.none

                            // Notes
                            match entry.Notes with
                            | Some notes ->
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Notes" ]
                                                Html.p [ prop.text notes ]
                                            ]
                                        ]
                                    ]
                                ]
                            | None -> Html.none

                            // Episode Progress Grid
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex justify-between items-center mb-4"
                                                prop.children [
                                                    Html.h2 [ prop.className "card-title"; prop.text "Episode Progress" ]
                                                    Html.span [
                                                        prop.className "text-sm text-base-content/60"
                                                        prop.text $"{watchedCount} / {series.NumberOfEpisodes} episodes"
                                                    ]
                                                ]
                                            ]
                                            // Overall progress bar
                                            Html.div [
                                                prop.className "mb-6"
                                                prop.children [ progressBar watchedCount series.NumberOfEpisodes ]
                                            ]
                                            // Season grids
                                            Html.div [
                                                prop.className "space-y-4"
                                                prop.children [
                                                    // Generate episode counts per season (estimate based on total)
                                                    let episodesPerSeason =
                                                        if series.NumberOfSeasons > 0 then
                                                            series.NumberOfEpisodes / series.NumberOfSeasons
                                                        else 10
                                                    for season in 1 .. series.NumberOfSeasons do
                                                        // Get actual episode count for this season from progress data if available
                                                        let seasonEpisodes =
                                                            progress
                                                            |> List.filter (fun p -> p.SeasonNumber = season)
                                                            |> List.length
                                                            |> fun count -> if count > 0 then count else episodesPerSeason
                                                        seasonEpisodeGrid entry.Id season seasonEpisodes progress dispatch
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Right column - Status and metadata
                    Html.div [
                        prop.className "space-y-4"
                        prop.children [
                            // Watch status card
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h2 [ prop.className "card-title"; prop.text "Status" ]

                                            Html.div [
                                                prop.className "space-y-3"
                                                prop.children [
                                                    Html.div [
                                                        prop.className "flex justify-between items-center"
                                                        prop.children [
                                                            Html.span [ prop.className "text-base-content/70"; prop.text "Watch Status" ]
                                                            watchStatusBadge entry.WatchStatus
                                                        ]
                                                    ]

                                                    match entry.PersonalRating with
                                                    | Some rating ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.className "text-base-content/70"; prop.text "My Rating" ]
                                                                Html.span [ prop.className "font-medium text-yellow-400"; prop.text (String.replicate (PersonalRating.toInt rating) "*") ]
                                                            ]
                                                        ]
                                                    | None -> Html.none

                                                    Html.div [
                                                        prop.className "flex justify-between"
                                                        prop.children [
                                                            Html.span [ prop.className "text-base-content/70"; prop.text "Added" ]
                                                            Html.span [ prop.className "font-medium"; prop.text (entry.DateAdded.ToString("MMM d, yyyy")) ]
                                                        ]
                                                    ]

                                                    if entry.IsFavorite then
                                                        Html.div [
                                                            prop.className "badge badge-warning"
                                                            prop.text "Favorite"
                                                        ]

                                                    // Watch controls
                                                    Html.div [
                                                        prop.className "divider my-2"
                                                    ]
                                                    seriesWatchControls entry series watchedCount dispatch
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Tags
                            if not (List.isEmpty entryTags) then
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Tags" ]
                                                Html.div [
                                                    prop.className "flex flex-wrap gap-2"
                                                    prop.children [
                                                        for tag in entryTags do
                                                            Html.span [ prop.className "badge badge-secondary"; prop.text tag.Name ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Watched with
                            if not (List.isEmpty entryFriends) then
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body"
                                            prop.children [
                                                Html.h2 [ prop.className "card-title"; prop.text "Watched With" ]
                                                Html.div [
                                                    prop.className "flex flex-wrap gap-2"
                                                    prop.children [
                                                        for friend in entryFriends do
                                                            Html.span [ prop.className "badge badge-primary"; prop.text friend.Name ]
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]

                            // TMDB info
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h2 [ prop.className "card-title"; prop.text "Info" ]
                                            Html.div [
                                                prop.className "space-y-1 text-sm"
                                                prop.children [
                                                    match series.VoteAverage with
                                                    | Some avg ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.text "TMDB Rating" ]
                                                                Html.span [ prop.text $"{avg:F1}/10" ]
                                                            ]
                                                        ]
                                                    | None -> Html.none

                                                    match series.EpisodeRunTimeMinutes with
                                                    | Some mins ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.text "Episode Runtime" ]
                                                                Html.span [ prop.text $"{mins} min" ]
                                                            ]
                                                        ]
                                                    | None -> Html.none

                                                    match series.OriginalLanguage with
                                                    | Some lang ->
                                                        Html.div [
                                                            prop.className "flex justify-between"
                                                            prop.children [
                                                                Html.span [ prop.text "Language" ]
                                                                Html.span [ prop.text (lang.ToUpperInvariant()) ]
                                                            ]
                                                        ]
                                                    | None -> Html.none
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Delete button
                            Html.button [
                                prop.className "btn btn-error btn-outline w-full"
                                prop.onClick (fun _ -> dispatch (OpenDeleteEntryModal entry.Id))
                                prop.text "Remove from Library"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Notification toast
let private notificationToast (message: string) (isSuccess: bool) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "toast-container"
        prop.children [
            Html.div [
                prop.className (
                    "toast " +
                    if isSuccess then "toast-success" else "toast-error"
                )
                prop.children [
                    Html.span [
                        prop.className (
                            "w-5 h-5 " +
                            if isSuccess then "text-success" else "text-error"
                        )
                        prop.children [
                            if isSuccess then Icons.success else Icons.error
                        ]
                    ]
                    Html.span [
                        prop.className "flex-1 text-sm"
                        prop.text message
                    ]
                    Html.button [
                        prop.className "w-6 h-6 rounded-full hover:bg-white/10 flex items-center justify-center transition-colors"
                        prop.onClick (fun _ -> dispatch ClearNotification)
                        prop.children [
                            Html.span [
                                prop.className "w-4 h-4 text-base-content/50"
                                prop.children [ Icons.close ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Friend Modal Component
// =====================================

let private friendModal (state: FriendModalState) (dispatch: Msg -> unit) =
    let title = match state.EditingFriend with | Some _ -> "Edit Friend" | None -> "Add Friend"
    let buttonText = match state.EditingFriend with | Some _ -> "Save Changes" | None -> "Add Friend"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not state.IsSubmitting then dispatch CloseModal)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-md"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "p-6 border-b border-base-300"
                        prop.children [
                            Html.h2 [
                                prop.className "text-xl font-bold"
                                prop.text title
                            ]
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.disabled state.IsSubmitting
                                prop.text "X"
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-4"
                        prop.children [
                            // Error message
                            match state.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            // Name field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Name *" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "Friend's name"
                                        prop.value state.Name
                                        prop.onChange (fun (e: string) -> dispatch (FriendModalNameChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Nickname field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Nickname (optional)" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "Optional nickname"
                                        prop.value state.Nickname
                                        prop.onChange (fun (e: string) -> dispatch (FriendModalNicknameChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Notes field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Notes (optional)" ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
                                        prop.placeholder "Notes about this friend..."
                                        prop.value state.Notes
                                        prop.onChange (fun (e: string) -> dispatch (FriendModalNotesChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Submit button
                            Html.button [
                                prop.className "btn btn-primary w-full"
                                prop.onClick (fun _ -> dispatch SubmitFriendModal)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    if state.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    else
                                        Html.span [ prop.text buttonText ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Tag Modal Component
// =====================================

let private tagModal (state: TagModalState) (dispatch: Msg -> unit) =
    let title = match state.EditingTag with | Some _ -> "Edit Tag" | None -> "Add Tag"
    let buttonText = match state.EditingTag with | Some _ -> "Save Changes" | None -> "Add Tag"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not state.IsSubmitting then dispatch CloseModal)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-md"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "p-6 border-b border-base-300"
                        prop.children [
                            Html.h2 [
                                prop.className "text-xl font-bold"
                                prop.text title
                            ]
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.disabled state.IsSubmitting
                                prop.text "X"
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-4"
                        prop.children [
                            // Error message
                            match state.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            // Name field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Name *" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "Tag name"
                                        prop.value state.Name
                                        prop.onChange (fun (e: string) -> dispatch (TagModalNameChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Color field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Color (optional)" ]
                                        ]
                                    ]
                                    Html.input [
                                        prop.className "input input-bordered"
                                        prop.placeholder "e.g., #3B82F6 or blue"
                                        prop.value state.Color
                                        prop.onChange (fun (e: string) -> dispatch (TagModalColorChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Description field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Description (optional)" ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
                                        prop.placeholder "Description of this tag..."
                                        prop.value state.Description
                                        prop.onChange (fun (e: string) -> dispatch (TagModalDescriptionChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Submit button
                            Html.button [
                                prop.className "btn btn-primary w-full"
                                prop.onClick (fun _ -> dispatch SubmitTagModal)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    if state.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    else
                                        Html.span [ prop.text buttonText ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Confirm Delete Modal
// =====================================

let private confirmDeleteFriendModal (friend: Friend) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> dispatch CloseModal)
            ]
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-sm p-6"
                prop.children [
                    Html.h2 [
                        prop.className "text-xl font-bold mb-4"
                        prop.text "Delete Friend?"
                    ]
                    Html.p [
                        prop.className "text-base-content/70 mb-6"
                        prop.text $"Are you sure you want to delete \"{friend.Name}\"? This cannot be undone."
                    ]
                    Html.div [
                        prop.className "flex gap-2 justify-end"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.text "Cancel"
                            ]
                            Html.button [
                                prop.className "btn btn-error"
                                prop.onClick (fun _ -> dispatch (ConfirmDeleteFriend friend.Id))
                                prop.text "Delete"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let private confirmDeleteTagModal (tag: Tag) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> dispatch CloseModal)
            ]
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-sm p-6"
                prop.children [
                    Html.h2 [
                        prop.className "text-xl font-bold mb-4"
                        prop.text "Delete Tag?"
                    ]
                    Html.p [
                        prop.className "text-base-content/70 mb-6"
                        prop.text $"Are you sure you want to delete \"{tag.Name}\"? This cannot be undone."
                    ]
                    Html.div [
                        prop.className "flex gap-2 justify-end"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.text "Cancel"
                            ]
                            Html.button [
                                prop.className "btn btn-error"
                                prop.onClick (fun _ -> dispatch (ConfirmDeleteTag tag.Id))
                                prop.text "Delete"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Abandon Modal Component
// =====================================

let private abandonModal (state: AbandonModalState) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not state.IsSubmitting then dispatch CloseModal)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-md"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "p-6 border-b border-base-300"
                        prop.children [
                            Html.h2 [
                                prop.className "text-xl font-bold"
                                prop.text "Abandon Entry"
                            ]
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.disabled state.IsSubmitting
                                prop.text "X"
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-4"
                        prop.children [
                            // Error message
                            match state.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            Html.p [
                                prop.className "text-base-content/70 text-sm"
                                prop.text "Mark this entry as abandoned. You can optionally specify where you stopped and why."
                            ]

                            // Reason field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Reason (optional)" ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
                                        prop.placeholder "Why did you stop watching?"
                                        prop.value state.Reason
                                        prop.onChange (fun (e: string) -> dispatch (AbandonModalReasonChanged e))
                                        prop.disabled state.IsSubmitting
                                    ]
                                ]
                            ]

                            // Abandoned at season/episode (for series)
                            Html.div [
                                prop.className "grid grid-cols-2 gap-4"
                                prop.children [
                                    Html.div [
                                        prop.className "form-control"
                                        prop.children [
                                            Html.label [
                                                prop.className "label"
                                                prop.children [
                                                    Html.span [ prop.className "label-text"; prop.text "Stopped at Season" ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.className "input input-bordered"
                                                prop.type' "number"
                                                prop.min 1
                                                prop.placeholder "Season"
                                                prop.value (state.AbandonedAtSeason |> Option.map string |> Option.defaultValue "")
                                                prop.onChange (fun (e: string) ->
                                                    let v = match System.Int32.TryParse(e) with | true, n -> Some n | _ -> None
                                                    dispatch (AbandonModalSeasonChanged v))
                                                prop.disabled state.IsSubmitting
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "form-control"
                                        prop.children [
                                            Html.label [
                                                prop.className "label"
                                                prop.children [
                                                    Html.span [ prop.className "label-text"; prop.text "Episode" ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.className "input input-bordered"
                                                prop.type' "number"
                                                prop.min 1
                                                prop.placeholder "Episode"
                                                prop.value (state.AbandonedAtEpisode |> Option.map string |> Option.defaultValue "")
                                                prop.onChange (fun (e: string) ->
                                                    let v = match System.Int32.TryParse(e) with | true, n -> Some n | _ -> None
                                                    dispatch (AbandonModalEpisodeChanged v))
                                                prop.disabled state.IsSubmitting
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Submit button
                            Html.div [
                                prop.className "flex gap-2 justify-end mt-4"
                                prop.children [
                                    Html.button [
                                        prop.className "btn btn-ghost"
                                        prop.onClick (fun _ -> dispatch CloseModal)
                                        prop.disabled state.IsSubmitting
                                        prop.text "Cancel"
                                    ]
                                    Html.button [
                                        prop.className "btn btn-error"
                                        prop.onClick (fun _ -> dispatch SubmitAbandonModal)
                                        prop.disabled state.IsSubmitting
                                        prop.children [
                                            if state.IsSubmitting then
                                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                            else
                                                Html.span [ prop.text "Abandon" ]
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
// Delete Entry Confirmation Modal
// =====================================

let private confirmDeleteEntryModal (entryId: EntryId) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> dispatch CloseModal)
            ]
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-sm p-6"
                prop.children [
                    Html.h2 [
                        prop.className "text-xl font-bold mb-4"
                        prop.text "Remove from Library?"
                    ]
                    Html.p [
                        prop.className "text-base-content/70 mb-6"
                        prop.text "Are you sure you want to remove this entry from your library? This cannot be undone."
                    ]
                    Html.div [
                        prop.className "flex gap-2 justify-end"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.text "Cancel"
                            ]
                            Html.button [
                                prop.className "btn btn-error"
                                prop.onClick (fun _ -> dispatch (ConfirmDeleteEntry entryId))
                                prop.text "Remove"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

// =====================================
// Friends Page Component
// =====================================

let private friendsPageContent (friends: RemoteData<Friend list>) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with add button
            Html.div [
                prop.className "flex justify-between items-center"
                prop.children [
                    Html.h2 [
                        prop.className "text-2xl font-bold"
                        prop.text "Friends"
                    ]
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ -> dispatch OpenAddFriendModal)
                        prop.children [
                            Html.span [ prop.text "+ Add Friend" ]
                        ]
                    ]
                ]
            ]

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Track who you watch movies and series with. Click on a friend to see what you've watched together."
            ]

            match friends with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success friendList when List.isEmpty friendList ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "No friends yet" ]
                        Html.p [ prop.text "Add friends to track who you watch with!" ]
                    ]
                ]
            | Success friendList ->
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
                    prop.children [
                        for friend in friendList do
                            Html.div [
                                prop.className "card bg-base-200 hover:shadow-lg transition-shadow cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewFriendDetail friend.Id))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center gap-3"
                                                prop.children [
                                                    // Avatar placeholder
                                                    Html.div [
                                                        prop.className "avatar placeholder"
                                                        prop.children [
                                                            Html.div [
                                                                prop.className "bg-primary text-primary-content rounded-full w-12"
                                                                prop.children [
                                                                    Html.span [
                                                                        prop.className "text-xl"
                                                                        prop.text (friend.Name.Substring(0, 1).ToUpperInvariant())
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex-1"
                                                        prop.children [
                                                            Html.h3 [
                                                                prop.className "font-bold"
                                                                prop.text friend.Name
                                                            ]
                                                            match friend.Nickname with
                                                            | Some nick ->
                                                                Html.p [
                                                                    prop.className "text-sm text-base-content/60"
                                                                    prop.text nick
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            // Action buttons
                                            Html.div [
                                                prop.className "card-actions justify-end mt-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenEditFriendModal friend)
                                                        )
                                                        prop.text "Edit"
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenDeleteFriendModal friend)
                                                        )
                                                        prop.text "Delete"
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading friends: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]

// =====================================
// Friend Detail Page Component
// =====================================

let private friendDetailPageContent (friendId: FriendId) (friends: Friend list) (entries: RemoteData<LibraryEntry list>) (dispatch: Msg -> unit) =
    let friend = friends |> List.tryFind (fun f -> f.Id = friendId)

    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm"
                prop.onClick (fun _ -> dispatch (NavigateTo FriendsPage))
                prop.text "< Back to Friends"
            ]

            match friend with
            | None ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Friend not found"
                ]
            | Some f ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Friend header
                        Html.div [
                            prop.className "flex items-center gap-4"
                            prop.children [
                                Html.div [
                                    prop.className "avatar placeholder"
                                    prop.children [
                                        Html.div [
                                            prop.className "bg-primary text-primary-content rounded-full w-20"
                                            prop.children [
                                                Html.span [
                                                    prop.className "text-3xl"
                                                    prop.text (f.Name.Substring(0, 1).ToUpperInvariant())
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.children [
                                        Html.h2 [
                                            prop.className "text-2xl font-bold"
                                            prop.text f.Name
                                        ]
                                        match f.Nickname with
                                        | Some nick ->
                                            Html.p [
                                                prop.className "text-base-content/60"
                                                prop.text nick
                                            ]
                                        | None -> Html.none
                                    ]
                                ]
                                Html.div [
                                    prop.className "flex-1"
                                ]
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.onClick (fun _ -> dispatch (OpenEditFriendModal f))
                                    prop.text "Edit"
                                ]
                            ]
                        ]

                        // Notes
                        match f.Notes with
                        | Some notes ->
                            Html.div [
                                prop.className "card bg-base-200"
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.h3 [ prop.className "font-bold"; prop.text "Notes" ]
                                            Html.p [ prop.text notes ]
                                        ]
                                    ]
                                ]
                            ]
                        | None -> Html.none

                        // Watched together section
                        Html.h3 [
                            prop.className "text-xl font-bold mt-6"
                            prop.text "Watched Together"
                        ]

                        match entries with
                        | Loading ->
                            Html.div [
                                prop.className "flex justify-center py-8"
                                prop.children [
                                    Html.span [ prop.className "loading loading-spinner loading-md" ]
                                ]
                            ]
                        | Success entryList when List.isEmpty entryList ->
                            Html.div [
                                prop.className "text-center py-8 text-base-content/60"
                                prop.text "No entries watched with this friend yet."
                            ]
                        | Success entryList ->
                            Html.div [
                                prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-4"
                                prop.children [
                                    for entry in entryList do
                                        let (title, posterPath, isMovie) =
                                            match entry.Media with
                                            | LibraryMovie m -> (m.Title, m.PosterPath, true)
                                            | LibrarySeries s -> (s.Name, s.PosterPath, false)
                                        Html.div [
                                            prop.className "cursor-pointer"
                                            prop.onClick (fun _ ->
                                                if isMovie then dispatch (ViewMovieDetail entry.Id)
                                                else dispatch (ViewSeriesDetail entry.Id)
                                            )
                                            prop.children [
                                                Html.div [
                                                    prop.className "aspect-[2/3] rounded-lg overflow-hidden bg-base-300 shadow-md hover:shadow-xl transition-shadow"
                                                    prop.children [
                                                        match posterPath with
                                                        | Some path ->
                                                            Html.img [
                                                                prop.src (getLocalPosterUrl posterPath)
                                                                prop.alt title
                                                                prop.className "w-full h-full object-cover"
                                                            ]
                                                        | None ->
                                                            Html.div [
                                                                prop.className "w-full h-full flex items-center justify-center text-base-content/40"
                                                                prop.text title
                                                            ]
                                                    ]
                                                ]
                                                Html.p [
                                                    prop.className "text-sm mt-1 truncate"
                                                    prop.text title
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        | Failure err ->
                            Html.div [
                                prop.className "alert alert-error"
                                prop.text $"Error: {err}"
                            ]
                        | NotAsked -> Html.none
                    ]
                ]
        ]
    ]

// =====================================
// Tags Page Component
// =====================================

let private tagsPageContent (tags: RemoteData<Tag list>) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with add button
            Html.div [
                prop.className "flex justify-between items-center"
                prop.children [
                    Html.h2 [
                        prop.className "text-2xl font-bold"
                        prop.text "Tags"
                    ]
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ -> dispatch OpenAddTagModal)
                        prop.children [
                            Html.span [ prop.text "+ Add Tag" ]
                        ]
                    ]
                ]
            ]

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Organize your library with custom tags. Click on a tag to see all tagged entries."
            ]

            match tags with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success tagList when List.isEmpty tagList ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "No tags yet" ]
                        Html.p [ prop.text "Create tags to organize your library!" ]
                    ]
                ]
            | Success tagList ->
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
                    prop.children [
                        for tag in tagList do
                            Html.div [
                                prop.className "card bg-base-200 hover:shadow-lg transition-shadow cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewTagDetail tag.Id))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center gap-3"
                                                prop.children [
                                                    // Color indicator
                                                    Html.div [
                                                        prop.className "w-4 h-4 rounded-full"
                                                        prop.style [
                                                            style.backgroundColor (tag.Color |> Option.defaultValue "#6366F1")
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex-1"
                                                        prop.children [
                                                            Html.h3 [
                                                                prop.className "font-bold"
                                                                prop.text tag.Name
                                                            ]
                                                            match tag.Description with
                                                            | Some desc ->
                                                                Html.p [
                                                                    prop.className "text-sm text-base-content/60 truncate"
                                                                    prop.text desc
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            // Action buttons
                                            Html.div [
                                                prop.className "card-actions justify-end mt-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenEditTagModal tag)
                                                        )
                                                        prop.text "Edit"
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenDeleteTagModal tag)
                                                        )
                                                        prop.text "Delete"
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading tags: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]

// =====================================
// Tag Detail Page Component
// =====================================

let private tagDetailPageContent (tagId: TagId) (tags: Tag list) (entries: RemoteData<LibraryEntry list>) (dispatch: Msg -> unit) =
    let tag = tags |> List.tryFind (fun t -> t.Id = tagId)

    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm"
                prop.onClick (fun _ -> dispatch (NavigateTo TagsPage))
                prop.text "< Back to Tags"
            ]

            match tag with
            | None ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Tag not found"
                ]
            | Some t ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Tag header
                        Html.div [
                            prop.className "flex items-center gap-4"
                            prop.children [
                                Html.div [
                                    prop.className "w-8 h-8 rounded-full"
                                    prop.style [
                                        style.backgroundColor (t.Color |> Option.defaultValue "#6366F1")
                                    ]
                                ]
                                Html.h2 [
                                    prop.className "text-2xl font-bold flex-1"
                                    prop.text t.Name
                                ]
                                Html.button [
                                    prop.className "btn btn-ghost btn-sm"
                                    prop.onClick (fun _ -> dispatch (OpenEditTagModal t))
                                    prop.text "Edit"
                                ]
                            ]
                        ]

                        // Description
                        match t.Description with
                        | Some desc ->
                            Html.p [
                                prop.className "text-base-content/70"
                                prop.text desc
                            ]
                        | None -> Html.none

                        // Tagged entries section
                        Html.h3 [
                            prop.className "text-xl font-bold mt-6"
                            prop.text "Tagged Entries"
                        ]

                        match entries with
                        | Loading ->
                            Html.div [
                                prop.className "flex justify-center py-8"
                                prop.children [
                                    Html.span [ prop.className "loading loading-spinner loading-md" ]
                                ]
                            ]
                        | Success entryList when List.isEmpty entryList ->
                            Html.div [
                                prop.className "text-center py-8 text-base-content/60"
                                prop.text "No entries with this tag yet."
                            ]
                        | Success entryList ->
                            Html.div [
                                prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-4"
                                prop.children [
                                    for entry in entryList do
                                        let (title, posterPath, isMovie) =
                                            match entry.Media with
                                            | LibraryMovie m -> (m.Title, m.PosterPath, true)
                                            | LibrarySeries s -> (s.Name, s.PosterPath, false)
                                        Html.div [
                                            prop.className "cursor-pointer"
                                            prop.onClick (fun _ ->
                                                if isMovie then dispatch (ViewMovieDetail entry.Id)
                                                else dispatch (ViewSeriesDetail entry.Id)
                                            )
                                            prop.children [
                                                Html.div [
                                                    prop.className "aspect-[2/3] rounded-lg overflow-hidden bg-base-300 shadow-md hover:shadow-xl transition-shadow"
                                                    prop.children [
                                                        match posterPath with
                                                        | Some path ->
                                                            Html.img [
                                                                prop.src (getLocalPosterUrl posterPath)
                                                                prop.alt title
                                                                prop.className "w-full h-full object-cover"
                                                            ]
                                                        | None ->
                                                            Html.div [
                                                                prop.className "w-full h-full flex items-center justify-center text-base-content/40"
                                                                prop.text title
                                                            ]
                                                    ]
                                                ]
                                                Html.p [
                                                    prop.className "text-sm mt-1 truncate"
                                                    prop.text title
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        | Failure err ->
                            Html.div [
                                prop.className "alert alert-error"
                                prop.text $"Error: {err}"
                            ]
                        | NotAsked -> Html.none
                    ]
                ]
        ]
    ]

/// Library entry card for displaying items in library (uses local cached images)
let private libraryEntryCard (entry: LibraryEntry) (dispatch: Msg -> unit) =
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
        prop.onClick (fun _ ->
            if isMovie then dispatch (ViewMovieDetail entry.Id)
            else dispatch (ViewSeriesDetail entry.Id)
        )
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
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/20"
                                    prop.children [ if isMovie then Icons.film else Icons.tv ]
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
                                    prop.children [ Icons.heartSolid ]
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
                                        prop.children [ Icons.eye ]
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
                        prop.className "font-medium text-sm truncate text-base-content/90 group-hover:text-white transition-colors"
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
                                                prop.children [ Icons.starSolid ]
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

/// Home page content
let private homePageContent (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-10"
        prop.children [
            // Hero section
            Html.div [
                prop.className "text-center py-16 space-y-6"
                prop.children [
                    // Logo icon
                    Html.div [
                        prop.className "mx-auto w-20 h-20 rounded-2xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center mb-6 shadow-glow-primary"
                        prop.children [
                            Html.span [
                                prop.className "w-10 h-10 text-primary"
                                prop.children [ Icons.clapperboard ]
                            ]
                        ]
                    ]

                    Html.h2 [
                        prop.className "text-4xl font-bold"
                        prop.children [
                            Html.span [ prop.text "Your " ]
                            Html.span [ prop.className "text-gradient"; prop.text "Cinema" ]
                            Html.span [ prop.text " Memory Tracker" ]
                        ]
                    ]

                    Html.p [
                        prop.className "text-base-content/60 max-w-xl mx-auto text-lg leading-relaxed"
                        prop.text "Search for movies and series to add them to your personal library. Track what you've watched, who you watched with, and capture your thoughts."
                    ]

                    // Quick tips
                    Html.div [
                        prop.className "flex flex-wrap justify-center gap-4 mt-8"
                        prop.children [
                            for (icon, tip) in [
                                (Icons.search, "Search above to find titles")
                                (Icons.plus, "Click any result to add")
                                (Icons.friends, "Track who you watch with")
                            ] do
                                Html.div [
                                    prop.className "flex items-center gap-2 px-4 py-2 glass rounded-full text-sm text-base-content/60"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-4 h-4"
                                            prop.children [ icon ]
                                        ]
                                        Html.span [ prop.text tip ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

            // Recently added section
            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        Html.div [
                            prop.className "flex justify-between items-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "text-xl font-bold"
                                    prop.text "Recently Added"
                                ]
                                Html.button [
                                    prop.className "flex items-center gap-2 text-sm text-primary hover:underline"
                                    prop.onClick (fun _ -> dispatch (NavigateTo LibraryPage))
                                    prop.children [
                                        Html.span [ prop.text "View All" ]
                                        Html.span [
                                            prop.className "w-4 h-4"
                                            prop.children [ Icons.arrowRight ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                            prop.children [
                                for entry in entries |> List.sortByDescending (fun e -> e.DateAdded) |> List.truncate 12 do
                                    libraryEntryCard entry dispatch
                            ]
                        ]
                    ]
                ]
            | Success _ ->
                Html.div [
                    prop.className "text-center py-16"
                    prop.children [
                        Html.div [
                            prop.className "w-20 h-20 mx-auto mb-6 rounded-full bg-base-200 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-10 h-10 text-base-content/30"
                                    prop.children [ Icons.library ]
                                ]
                            ]
                        ]
                        Html.h3 [
                            prop.className "text-xl font-semibold mb-2 text-base-content/70"
                            prop.text "Your library is empty"
                        ]
                        Html.p [
                            prop.className "text-base-content/50"
                            prop.text "Search for a movie or series above to get started"
                        ]
                    ]
                ]
            | Loading ->
                Html.div [
                    prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                    prop.children [
                        for _ in 1..6 do
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
            | Failure err ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.children [
                        Html.span [
                            prop.className "w-12 h-12 mx-auto mb-4 text-error/50 block"
                            prop.children [ Icons.error ]
                        ]
                        Html.p [
                            prop.className "text-error"
                            prop.text $"Error loading library: {err}"
                        ]
                    ]
                ]
            | NotAsked -> Html.none
        ]
    ]

/// Library page content
let private libraryPageContent (model: Model) (tags: Tag list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with title and stats
            Html.div [
                prop.className "flex items-center justify-between"
                prop.children [
                    Html.h2 [
                        prop.className "text-2xl font-bold flex items-center gap-3"
                        prop.children [
                            Html.span [
                                prop.className "w-8 h-8 text-primary"
                                prop.children [ Icons.library ]
                            ]
                            Html.span [ prop.text "My Library" ]
                        ]
                    ]
                    match model.Library with
                    | Success entries ->
                        Html.div [
                            prop.className "text-sm text-base-content/50"
                            prop.text $"{List.length entries} titles"
                        ]
                    | _ -> Html.none
                ]
            ]

            // Filter bar
            filterBar model.LibraryFilters tags dispatch

            match model.Library with
            | Success entries ->
                let filteredEntries = applyFiltersAndSort model.LibraryFilters entries
                let totalCount = List.length entries
                let filteredCount = List.length filteredEntries

                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        // Results count
                        if filteredCount <> totalCount then
                            Html.div [
                                prop.className "text-sm text-base-content/50"
                                prop.text $"Showing {filteredCount} of {totalCount} items"
                            ]

                        if List.isEmpty filteredEntries then
                            Html.div [
                                prop.className "text-center py-20"
                                prop.children [
                                    if totalCount = 0 then
                                        Html.div [
                                            prop.className "space-y-4"
                                            prop.children [
                                                Html.div [
                                                    prop.className "w-20 h-20 mx-auto rounded-full bg-base-200 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-10 h-10 text-base-content/30"
                                                            prop.children [ Icons.library ]
                                                        ]
                                                    ]
                                                ]
                                                Html.h3 [
                                                    prop.className "text-xl font-semibold text-base-content/70"
                                                    prop.text "Your library is empty"
                                                ]
                                                Html.p [
                                                    prop.className "text-base-content/50 max-w-md mx-auto"
                                                    prop.text "Use the search bar above to find movies and series to add to your collection."
                                                ]
                                            ]
                                        ]
                                    else
                                        Html.div [
                                            prop.className "space-y-4"
                                            prop.children [
                                                Html.div [
                                                    prop.className "w-20 h-20 mx-auto rounded-full bg-base-200 flex items-center justify-center"
                                                    prop.children [
                                                        Html.span [
                                                            prop.className "w-10 h-10 text-base-content/30"
                                                            prop.children [ Icons.filter ]
                                                        ]
                                                    ]
                                                ]
                                                Html.h3 [
                                                    prop.className "text-xl font-semibold text-base-content/70"
                                                    prop.text "No items match your filters"
                                                ]
                                                Html.button [
                                                    prop.className "px-4 py-2 rounded-lg bg-primary/10 text-primary hover:bg-primary/20 transition-colors"
                                                    prop.onClick (fun _ -> dispatch ClearFilters)
                                                    prop.text "Clear All Filters"
                                                ]
                                            ]
                                        ]
                                ]
                            ]
                        else
                            Html.div [
                                prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4 animate-fade-in"
                                prop.children [
                                    for entry in filteredEntries do
                                        libraryEntryCard entry dispatch
                                ]
                            ]
                    ]
                ]
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
            | Failure err ->
                Html.div [
                    prop.className "flex items-center gap-3 p-4 rounded-lg bg-error/10 border border-error/30"
                    prop.children [
                        Html.span [
                            prop.className "w-5 h-5 text-error"
                            prop.children [ Icons.error ]
                        ]
                        Html.span [
                            prop.className "text-sm text-error"
                            prop.text $"Error loading library: {err}"
                        ]
                    ]
                ]
            | NotAsked -> Html.none
        ]
    ]

/// Page placeholder content (for unimplemented pages)
let private pageContent (page: Page) (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    match page with
    | HomePage -> homePageContent model dispatch
    | LibraryPage -> libraryPageContent model tags dispatch

    // Movie detail page
    | MovieDetailPage _ ->
        match model.DetailEntry with
        | Success entry ->
            match entry.Media with
            | LibraryMovie movie -> movieDetailPage entry movie tags friends dispatch
            | LibrarySeries _ ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Invalid entry type"
                ]
        | Loading ->
            Html.div [
                prop.className "flex justify-center py-12"
                prop.children [
                    Html.span [ prop.className "loading loading-spinner loading-lg" ]
                ]
            ]
        | Failure err ->
            Html.div [
                prop.className "alert alert-error"
                prop.text $"Error loading movie: {err}"
            ]
        | NotAsked ->
            Html.div [
                prop.className "flex justify-center py-12"
                prop.children [
                    Html.span [ prop.className "loading loading-spinner loading-lg" ]
                ]
            ]

    // Series detail page
    | SeriesDetailPage _ ->
        match model.DetailEntry with
        | Success entry ->
            let episodeProgress = RemoteData.defaultValue [] model.EpisodeProgress
            match entry.Media with
            | LibrarySeries series -> seriesDetailPage entry series tags friends episodeProgress dispatch
            | LibraryMovie _ ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Invalid entry type"
                ]
        | Loading ->
            Html.div [
                prop.className "flex justify-center py-12"
                prop.children [
                    Html.span [ prop.className "loading loading-spinner loading-lg" ]
                ]
            ]
        | Failure err ->
            Html.div [
                prop.className "alert alert-error"
                prop.text $"Error loading series: {err}"
            ]
        | NotAsked ->
            Html.div [
                prop.className "flex justify-center py-12"
                prop.children [
                    Html.span [ prop.className "loading loading-spinner loading-lg" ]
                ]
            ]

    // Friends pages
    | FriendsPage ->
        friendsPageContent model.Friends dispatch

    | FriendDetailPage friendId ->
        friendDetailPageContent friendId friends model.FriendDetailEntries dispatch

    // Tags pages
    | TagsPage ->
        tagsPageContent model.Tags dispatch

    | TagDetailPage tagId ->
        tagDetailPageContent tagId tags model.TagDetailEntries dispatch

    | _ ->
        Html.div [
            prop.className "flex flex-col items-center justify-center min-h-[50vh] text-center"
            prop.children [
                Html.div [
                    prop.className "text-6xl mb-4"
                    prop.text (
                        match page with
                        | CollectionsPage -> "C"
                        | StatsPage -> "S"
                        | TimelinePage -> "Ti"
                        | GraphPage -> "G"
                        | ImportPage -> "I"
                        | NotFoundPage -> "?"
                        | _ -> "?"
                    )
                ]
                Html.h2 [
                    prop.className "text-2xl font-bold mb-2"
                    prop.text (Page.toString page)
                ]
                Html.p [
                    prop.className "text-base-content/60"
                    prop.text (
                        match page with
                        | CollectionsPage -> "Create and manage franchises and custom lists."
                        | StatsPage -> "View your watching statistics and insights."
                        | TimelinePage -> "See your watching history chronologically."
                        | GraphPage -> "Explore relationships between your movies, friends, and tags."
                        | ImportPage -> "Import your watching history from other services."
                        | NotFoundPage -> "The page you're looking for doesn't exist."
                        | _ -> ""
                    )
                ]
            ]
        ]

/// Main content area
let private mainContent (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.main [
        prop.className "lg:ml-64 min-h-screen pb-20 lg:pb-0"
        prop.children [
            // Page content
            Html.div [
                prop.className "container mx-auto px-4 lg:px-8 py-8"
                prop.children [
                    pageContent model.CurrentPage model tags friends dispatch
                ]
            ]
        ]
    ]

/// Global keyboard shortcut handler component
[<ReactComponent>]
let private KeyboardShortcuts (model: Model) (dispatch: Msg -> unit) (children: Fable.React.ReactElement) =
    React.useEffect(fun () ->
        let handler (e: Event) =
            let ke = e :?> KeyboardEvent
            // Ctrl+K or Cmd+K to open search
            if (ke.ctrlKey || ke.metaKey) && ke.key = "k" then
                ke.preventDefault()
                // Only open if no modal is currently open
                if model.Modal = NoModal then
                    dispatch OpenSearchModal

        Browser.Dom.document.addEventListener("keydown", handler)

        // Cleanup
        React.createDisposable(fun () ->
            Browser.Dom.document.removeEventListener("keydown", handler)
        )
    , [| box model.Modal |])

    children

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    let friends = RemoteData.defaultValue [] model.Friends
    let tags = RemoteData.defaultValue [] model.Tags

    let modalElement =
        match model.Modal with
        | SearchModal ->
            searchModal model dispatch
        | QuickAddModal state ->
            quickAddModal state friends tags dispatch
        | FriendModal state ->
            friendModal state dispatch
        | TagModal state ->
            tagModal state dispatch
        | ConfirmDeleteFriendModal friend ->
            confirmDeleteFriendModal friend dispatch
        | ConfirmDeleteTagModal tag ->
            confirmDeleteTagModal tag dispatch
        | AbandonModal state ->
            abandonModal state dispatch
        | ConfirmDeleteEntryModal entryId ->
            confirmDeleteEntryModal entryId dispatch
        | NoModal -> Html.none

    let notificationElement =
        match model.Notification with
        | Some (message, isSuccess) ->
            notificationToast message isSuccess dispatch
        | None -> Html.none

    KeyboardShortcuts model dispatch (
        Html.div [
            prop.className "min-h-screen bg-base-100"
            prop.children [
                sidebar model dispatch
                mobileNav model dispatch
                mobileMenuDrawer model dispatch
                mainContent model tags friends dispatch
                modalElement
                notificationElement
            ]
        ]
    )
