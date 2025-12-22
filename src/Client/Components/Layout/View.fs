module Components.Layout.View

open Feliz
open Common.Routing
open Common.Types
open Types
open Components.Icons

/// Get icon for a page
let private getPageIcon (page: Page) =
    match page with
    | HomePage -> home
    | LibraryPage -> library
    | MovieDetailPage _ -> film
    | SeriesDetailPage _ -> tv
    | SessionDetailPage _ -> tv
    | FriendsPage -> friends
    | FriendDetailPage _ -> friends
    | ContributorsPage -> userPlus
    | ContributorDetailPage _ -> userPlus
    | CollectionsPage -> collections
    | CollectionDetailPage _ -> collections
    | StatsPage -> stats
    | YearInReviewPage _ -> sparkles
    | TimelinePage -> timeline
    | GraphPage _ -> graph
    | ImportPage -> import
    | CachePage -> cache
    | NotFoundPage -> warning

/// Get icon color class for a page - Velvet Cinema color palette
let private getIconColor (page: Page) =
    match page with
    | HomePage -> "text-nav-home"           // Gold - theater marquee
    | LibraryPage -> "text-nav-library"     // Champagne - film archives
    | MovieDetailPage _ -> "text-nav-library"
    | SeriesDetailPage _ -> "text-nav-library"
    | SessionDetailPage _ -> "text-nav-library"
    | FriendsPage -> "text-nav-friends"     // Emerald - social
    | FriendDetailPage _ -> "text-nav-friends"
    | ContributorsPage -> "text-nav-contributors" // Violet/Purple - people you follow
    | ContributorDetailPage _ -> "text-nav-contributors"
    | CollectionsPage -> "text-nav-collections" // Pink - curated
    | CollectionDetailPage _ -> "text-nav-collections"
    | StatsPage -> "text-nav-stats"         // Orange - metrics
    | YearInReviewPage _ -> "text-amber-400" // Amber - celebration/year
    | TimelinePage -> "text-nav-timeline"   // Sky - time
    | GraphPage _ -> "text-nav-graph"         // Violet - connections
    | ImportPage -> "text-nav-import"       // Lime - fresh data
    | CachePage -> "text-nav-cache"         // Slate - system
    | NotFoundPage -> "text-error"

/// Navigation item component
let private navItem (page: Page) (currentPage: Page) (onNavigate: Page -> unit) (isExpanded: bool) =
    let isActive = page = currentPage
    let iconColor = getIconColor page
    Html.li [
        Html.a [
            prop.className (
                "nav-item cursor-pointer " +
                (if isActive then "nav-item-active " else "")
            )
            prop.onClick (fun _ -> onNavigate page)
            prop.title (if isExpanded then "" else Page.toString page)
            prop.children [
                Html.span [
                    prop.className $"nav-icon {iconColor}"
                    prop.children [ getPageIcon page ]
                ]
                // Always render text, CSS handles visibility
                Html.span [
                    prop.className (
                        "font-medium text-sm nav-item-text " +
                        (if isExpanded then "opacity-100" else "opacity-0 w-0")
                    )
                    prop.text (Page.toString page)
                ]
            ]
        ]
    ]

/// Sidebar navigation component
let sidebar (model: Model) (currentPage: Page) (onNavigate: Page -> unit) (onSearch: unit -> unit) (dispatch: Msg -> unit) =
    let isExpanded = model.IsSidebarExpanded
    Html.aside [
        prop.className (
            "sidebar fixed left-0 top-0 h-full hidden md:flex md:flex-col z-40 transition-[width] duration-300 ease-out " +
            (if isExpanded then "w-64 sidebar-expanded" else "w-[72px] sidebar-collapsed")
        )
        prop.children [
            // Logo section - clickable to toggle
            Html.div [
                prop.className (
                    "border-b border-[#d4a574]/10 cursor-pointer transition-[padding,background] duration-300 " +
                    (if isExpanded then "p-6" else "p-4 flex justify-center")
                )
                prop.onClick (fun _ -> dispatch ToggleSidebar)
                prop.title (if isExpanded then "Collapse sidebar" else "Expand sidebar")
                prop.children [
                    Html.div [
                        prop.className (
                            "flex items-center transition-[gap] duration-300 " +
                            (if isExpanded then "gap-3" else "justify-center")
                        )
                        prop.children [
                            Html.span [
                                prop.className "text-primary flex-shrink-0 transition-transform duration-300 hover:scale-110"
                                prop.children [ clapperboard ]
                            ]
                            if isExpanded then
                                Html.div [
                                    prop.className "sidebar-text-container overflow-hidden"
                                    prop.children [
                                        Html.h1 [
                                            prop.className "text-xl font-bold text-gradient whitespace-nowrap"
                                            prop.text "Cinemarco"
                                        ]
                                        Html.p [
                                            prop.className "text-xs text-base-content/50 whitespace-nowrap"
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
                prop.className (
                    "flex-1 overflow-y-auto transition-[padding] duration-300 " +
                    (if isExpanded then "p-4" else "p-2")
                )
                prop.children [
                    Html.ul [
                        prop.className "space-y-1"
                        prop.children [
                            navItem HomePage currentPage onNavigate isExpanded
                            navItem LibraryPage currentPage onNavigate isExpanded

                            // Search button
                            Html.li [
                                Html.a [
                                    prop.className "nav-item cursor-pointer"
                                    prop.onClick (fun _ -> onSearch ())
                                    prop.title (if isExpanded then "" else "Search")
                                    prop.children [
                                        Html.span [
                                            prop.className "nav-icon text-nav-search"
                                            prop.children [ search ]
                                        ]
                                        // Always render text, CSS handles visibility
                                        Html.span [
                                            prop.className (
                                                "font-medium text-sm nav-item-text " +
                                                (if isExpanded then "opacity-100" else "opacity-0 w-0")
                                            )
                                            prop.text "Search"
                                        ]
                                    ]
                                ]
                            ]

                            // Divider
                            Html.li [
                                prop.className (
                                    "sidebar-divider border-t border-[#d4a574]/8 " +
                                    (if isExpanded then "my-4" else "my-2 mx-2")
                                )
                            ]

                            navItem FriendsPage currentPage onNavigate isExpanded
                            navItem ContributorsPage currentPage onNavigate isExpanded
                            navItem CollectionsPage currentPage onNavigate isExpanded

                            // Divider
                            Html.li [
                                prop.className (
                                    "sidebar-divider border-t border-[#d4a574]/8 " +
                                    (if isExpanded then "my-4" else "my-2 mx-2")
                                )
                            ]

                            navItem StatsPage currentPage onNavigate isExpanded
                            navItem (YearInReviewPage (None, YearInReviewViewMode.Overview)) currentPage onNavigate isExpanded
                            navItem TimelinePage currentPage onNavigate isExpanded
                            navItem (GraphPage None) currentPage onNavigate isExpanded

                            // Divider
                            Html.li [
                                prop.className (
                                    "sidebar-divider border-t border-[#d4a574]/8 " +
                                    (if isExpanded then "my-4" else "my-2 mx-2")
                                )
                            ]

                            navItem ImportPage currentPage onNavigate isExpanded
                            navItem CachePage currentPage onNavigate isExpanded
                        ]
                    ]
                ]
            ]

            // Status footer
            Html.div [
                prop.className (
                    "border-t border-[#d4a574]/10 transition-[padding,background] duration-300 " +
                    (if isExpanded then "p-4" else "p-2 flex justify-center")
                )
                prop.children [
                    match model.HealthCheck with
                    | Success health ->
                        Html.div [
                            prop.className (
                                "flex items-center text-xs " +
                                (if isExpanded then "gap-2" else "")
                            )
                            prop.title (if isExpanded then "" else $"Connected v{health.Version}")
                            prop.children [
                                Html.span [
                                    prop.className "w-2 h-2 bg-success rounded-full animate-pulse-subtle flex-shrink-0"
                                ]
                                if isExpanded then
                                    Html.span [
                                        prop.className "text-base-content/50 whitespace-nowrap"
                                        prop.text $"Connected  v{health.Version}"
                                    ]
                            ]
                        ]
                    | Loading ->
                        Html.div [
                            prop.className (
                                "flex items-center text-xs text-base-content/40 " +
                                (if isExpanded then "gap-2" else "")
                            )
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                if isExpanded then
                                    Html.span [ prop.text "Connecting..." ]
                            ]
                        ]
                    | Failure _ ->
                        Html.div [
                            prop.className (
                                "flex items-center text-xs text-error/80 " +
                                (if isExpanded then "gap-2" else "")
                            )
                            prop.title (if isExpanded then "" else "Offline")
                            prop.children [
                                Html.span [ prop.className "w-2 h-2 bg-error rounded-full flex-shrink-0" ]
                                if isExpanded then
                                    Html.span [ prop.text "Offline" ]
                            ]
                        ]
                    | NotAsked -> Html.none
                ]
            ]
        ]
    ]

/// Mobile bottom navigation
let mobileNav (model: Model) (currentPage: Page) (onNavigate: Page -> unit) (onSearch: unit -> unit) (dispatch: Msg -> unit) =
    Html.nav [
        prop.className "fixed bottom-0 left-0 right-0 glass-strong md:hidden z-40 safe-area-bottom"
        prop.children [
            Html.div [
                prop.className "flex justify-around items-center h-16"
                prop.children [
                    // Home button
                    let isHomeActive = currentPage = HomePage
                    Html.button [
                        prop.className (
                            "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 " +
                            if isHomeActive then "text-nav-home scale-105" else "text-base-content/50 hover:text-nav-home/80"
                        )
                        prop.onClick (fun _ -> onNavigate HomePage)
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
                    let isLibraryActive = currentPage = LibraryPage
                    Html.button [
                        prop.className (
                            "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 " +
                            if isLibraryActive then "text-nav-library scale-105" else "text-base-content/50 hover:text-nav-library/80"
                        )
                        prop.onClick (fun _ -> onNavigate LibraryPage)
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
                        prop.className "flex flex-col items-center gap-1 px-4 py-2 transition-all duration-200 text-base-content/50 hover:text-nav-search/80"
                        prop.onClick (fun _ -> onSearch ())
                        prop.children [
                            Html.span [
                                prop.className "transition-transform text-nav-search/70 hover:text-nav-search"
                                prop.children [ search ]
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
                                prop.children [ menu ]
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

/// Mobile menu drawer
let mobileMenuDrawer (model: Model) (currentPage: Page) (onNavigate: Page -> unit) (dispatch: Msg -> unit) =
    if not model.IsMobileMenuOpen then Html.none
    else
        Html.div [
            prop.className "fixed inset-0 z-50 md:hidden"
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
                                        for page in [ FriendsPage; ContributorsPage; CollectionsPage ] do
                                            let iconColor = getIconColor page
                                            Html.li [
                                                Html.button [
                                                    prop.className (
                                                        "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                        if currentPage = page then "bg-base-200" else "text-base-content/70 hover:bg-base-200"
                                                    )
                                                    prop.onClick (fun _ ->
                                                        dispatch CloseMobileMenu
                                                        onNavigate page)
                                                    prop.children [
                                                        Html.span [
                                                            prop.className $"w-5 h-5 {iconColor}"
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
                                        for page in [ StatsPage; YearInReviewPage (None, YearInReviewViewMode.Overview); TimelinePage; GraphPage None ] do
                                            let iconColor = getIconColor page
                                            Html.li [
                                                Html.button [
                                                    prop.className (
                                                        "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                        if currentPage = page then "bg-base-200" else "text-base-content/70 hover:bg-base-200"
                                                    )
                                                    prop.onClick (fun _ ->
                                                        dispatch CloseMobileMenu
                                                        onNavigate page)
                                                    prop.children [
                                                        Html.span [
                                                            prop.className $"w-5 h-5 {iconColor}"
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

                                        // Import and Cache
                                        for page in [ ImportPage; CachePage ] do
                                            let iconColor = getIconColor page
                                            Html.li [
                                                Html.button [
                                                    prop.className (
                                                        "flex items-center gap-3 w-full px-4 py-3 rounded-xl transition-all " +
                                                        if currentPage = page then "bg-base-200" else "text-base-content/70 hover:bg-base-200"
                                                    )
                                                    prop.onClick (fun _ ->
                                                        dispatch CloseMobileMenu
                                                        onNavigate page)
                                                    prop.children [
                                                        Html.span [
                                                            prop.className $"w-5 h-5 {iconColor}"
                                                            prop.children [ getPageIcon page ]
                                                        ]
                                                        Html.span [
                                                            prop.className "font-medium"
                                                            prop.text (Page.toString page)
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
