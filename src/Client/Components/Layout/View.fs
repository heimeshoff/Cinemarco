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
    | TagsPage -> tags
    | TagDetailPage _ -> tags
    | CollectionsPage -> collections
    | StatsPage -> stats
    | TimelinePage -> timeline
    | GraphPage -> graph
    | ImportPage -> import
    | CachePage -> cache
    | NotFoundPage -> warning

/// Get icon color class for a page - Cinema-inspired color palette
let private getIconColor (page: Page) =
    match page with
    | HomePage -> "text-nav-home"           // Amber - theater marquee
    | LibraryPage -> "text-nav-library"     // Blue - film archives
    | MovieDetailPage _ -> "text-nav-library"
    | SeriesDetailPage _ -> "text-nav-library"
    | FriendsPage -> "text-nav-friends"     // Emerald - social
    | FriendDetailPage _ -> "text-nav-friends"
    | TagsPage -> "text-nav-tags"           // Teal - organization
    | TagDetailPage _ -> "text-nav-tags"
    | CollectionsPage -> "text-nav-collections" // Pink - curated
    | StatsPage -> "text-nav-stats"         // Orange - metrics
    | TimelinePage -> "text-nav-timeline"   // Sky - time
    | GraphPage -> "text-nav-graph"         // Violet - connections
    | ImportPage -> "text-nav-import"       // Lime - fresh data
    | CachePage -> "text-nav-cache"         // Slate - system
    | NotFoundPage -> "text-error"

/// Navigation item component
let private navItem (page: Page) (currentPage: Page) (onNavigate: Page -> unit) =
    let isActive = page = currentPage
    let iconColor = getIconColor page
    Html.li [
        Html.a [
            prop.className (
                "nav-item cursor-pointer " +
                if isActive then "nav-item-active" else ""
            )
            prop.onClick (fun _ -> onNavigate page)
            prop.children [
                Html.span [
                    prop.className $"nav-icon {iconColor}"
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
let sidebar (model: Model) (currentPage: Page) (onNavigate: Page -> unit) (onSearch: unit -> unit) =
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
                                prop.children [ clapperboard ]
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
                            navItem HomePage currentPage onNavigate
                            navItem LibraryPage currentPage onNavigate

                            // Search button
                            Html.li [
                                Html.a [
                                    prop.className "nav-item cursor-pointer"
                                    prop.onClick (fun _ -> onSearch ())
                                    prop.children [
                                        Html.span [
                                            prop.className "nav-icon text-nav-search"
                                            prop.children [ search ]
                                        ]
                                        Html.span [
                                            prop.className "font-medium text-sm"
                                            prop.text "Search"
                                        ]
                                    ]
                                ]
                            ]

                            // Divider
                            Html.li [ prop.className "my-4 border-t border-white/5" ]

                            navItem FriendsPage currentPage onNavigate
                            navItem TagsPage currentPage onNavigate
                            navItem CollectionsPage currentPage onNavigate

                            // Divider
                            Html.li [ prop.className "my-4 border-t border-white/5" ]

                            navItem StatsPage currentPage onNavigate
                            navItem TimelinePage currentPage onNavigate
                            navItem GraphPage currentPage onNavigate

                            // Divider
                            Html.li [ prop.className "my-4 border-t border-white/5" ]

                            navItem ImportPage currentPage onNavigate
                            navItem CachePage currentPage onNavigate
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

/// Mobile bottom navigation
let mobileNav (model: Model) (currentPage: Page) (onNavigate: Page -> unit) (onSearch: unit -> unit) (dispatch: Msg -> unit) =
    Html.nav [
        prop.className "fixed bottom-0 left-0 right-0 glass-strong lg:hidden z-40 safe-area-bottom"
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
                                        for page in [ StatsPage; TimelinePage; GraphPage ] do
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
