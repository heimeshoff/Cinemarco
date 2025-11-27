module View

open Feliz
open State
open Types
open Shared.Api

/// Navigation item component
let private navItem (page: Page) (currentPage: Page) (dispatch: Msg -> unit) =
    let isActive = page = currentPage
    let icon =
        match page with
        | HomePage -> "home"
        | LibraryPage -> "library"
        | FriendsPage -> "users"
        | TagsPage -> "tag"
        | CollectionsPage -> "folder"
        | StatsPage -> "bar-chart-2"
        | TimelinePage -> "calendar"
        | GraphPage -> "git-branch"
        | ImportPage -> "download"
        | NotFoundPage -> "alert-circle"

    Html.li [
        Html.a [
            prop.className (
                "flex items-center gap-3 px-4 py-3 rounded-lg transition-colors " +
                if isActive then "bg-primary text-primary-content"
                else "hover:bg-base-200"
            )
            prop.onClick (fun _ -> dispatch (NavigateTo page))
            prop.children [
                // Icon placeholder - using text for now
                Html.span [
                    prop.className "w-5 h-5 flex items-center justify-center text-sm"
                    prop.text (
                        match page with
                        | HomePage -> "🏠"
                        | LibraryPage -> "📚"
                        | FriendsPage -> "👥"
                        | TagsPage -> "🏷️"
                        | CollectionsPage -> "📁"
                        | StatsPage -> "📊"
                        | TimelinePage -> "📅"
                        | GraphPage -> "🕸️"
                        | ImportPage -> "⬇️"
                        | NotFoundPage -> "❌"
                    )
                ]
                Html.span [
                    prop.className "font-medium"
                    prop.text (Page.toString page)
                ]
            ]
        ]
    ]

/// Sidebar navigation component
let private sidebar (model: Model) (dispatch: Msg -> unit) =
    Html.aside [
        prop.className "fixed left-0 top-0 h-full w-64 bg-base-200 border-r border-base-300 hidden lg:flex lg:flex-col z-40"
        prop.children [
            // Logo section
            Html.div [
                prop.className "p-6 border-b border-base-300"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl font-bold text-primary"
                        prop.text "Cinemarco"
                    ]
                    Html.p [
                        prop.className "text-sm text-base-content/60 mt-1"
                        prop.text "Your Cinema Memories"
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
                            navItem FriendsPage model.CurrentPage dispatch
                            navItem TagsPage model.CurrentPage dispatch
                            navItem CollectionsPage model.CurrentPage dispatch
                            navItem StatsPage model.CurrentPage dispatch
                            navItem TimelinePage model.CurrentPage dispatch
                            navItem GraphPage model.CurrentPage dispatch
                            navItem ImportPage model.CurrentPage dispatch
                        ]
                    ]
                ]
            ]

            // Health status footer
            Html.div [
                prop.className "p-4 border-t border-base-300"
                prop.children [
                    match model.HealthCheck with
                    | Success health ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-sm"
                            prop.children [
                                Html.span [
                                    prop.className "w-2 h-2 bg-success rounded-full"
                                ]
                                Html.span [
                                    prop.className "text-base-content/60"
                                    prop.text $"v{health.Version}"
                                ]
                            ]
                        ]
                    | Loading ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-sm text-base-content/40"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                Html.span [ prop.text "Connecting..." ]
                            ]
                        ]
                    | Failure _ ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-sm text-error"
                            prop.children [
                                Html.span [
                                    prop.className "w-2 h-2 bg-error rounded-full"
                                ]
                                Html.span [ prop.text "Offline" ]
                            ]
                        ]
                    | NotAsked -> Html.none
                ]
            ]
        ]
    ]

/// Mobile bottom navigation
let private mobileNav (model: Model) (dispatch: Msg -> unit) =
    Html.nav [
        prop.className "fixed bottom-0 left-0 right-0 bg-base-200 border-t border-base-300 lg:hidden z-40"
        prop.children [
            Html.div [
                prop.className "flex justify-around items-center h-16"
                prop.children [
                    for page in [ HomePage; LibraryPage; StatsPage ] do
                        Html.button [
                            prop.className (
                                "flex flex-col items-center gap-1 px-4 py-2 " +
                                if model.CurrentPage = page then "text-primary" else "text-base-content/60"
                            )
                            prop.onClick (fun _ -> dispatch (NavigateTo page))
                            prop.children [
                                Html.span [
                                    prop.className "text-lg"
                                    prop.text (
                                        match page with
                                        | HomePage -> "🏠"
                                        | LibraryPage -> "📚"
                                        | StatsPage -> "📊"
                                        | _ -> "•"
                                    )
                                ]
                                Html.span [
                                    prop.className "text-xs"
                                    prop.text (Page.toString page)
                                ]
                            ]
                        ]
                ]
            ]
        ]
    ]

/// Page placeholder content
let private pageContent (page: Page) =
    Html.div [
        prop.className "flex flex-col items-center justify-center min-h-[50vh] text-center"
        prop.children [
            Html.div [
                prop.className "text-6xl mb-4"
                prop.text (
                    match page with
                    | HomePage -> "🎬"
                    | LibraryPage -> "📚"
                    | FriendsPage -> "👥"
                    | TagsPage -> "🏷️"
                    | CollectionsPage -> "📁"
                    | StatsPage -> "📊"
                    | TimelinePage -> "📅"
                    | GraphPage -> "🕸️"
                    | ImportPage -> "⬇️"
                    | NotFoundPage -> "🔍"
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
                    | HomePage -> "Welcome to Cinemarco. Your personal cinema memory tracker."
                    | LibraryPage -> "Your movie and series collection will appear here."
                    | FriendsPage -> "Manage friends you watch movies with."
                    | TagsPage -> "Organize your library with custom tags."
                    | CollectionsPage -> "Create and manage franchises and custom lists."
                    | StatsPage -> "View your watching statistics and insights."
                    | TimelinePage -> "See your watching history chronologically."
                    | GraphPage -> "Explore relationships between your movies, friends, and tags."
                    | ImportPage -> "Import your watching history from other services."
                    | NotFoundPage -> "The page you're looking for doesn't exist."
                )
            ]
        ]
    ]

/// Main content area
let private mainContent (model: Model) (dispatch: Msg -> unit) =
    Html.main [
        prop.className "lg:ml-64 min-h-screen pb-20 lg:pb-0"
        prop.children [
            // Header with search
            Html.header [
                prop.className "sticky top-0 z-30 bg-base-100/80 backdrop-blur-lg border-b border-base-300"
                prop.children [
                    Html.div [
                        prop.className "container mx-auto px-4 lg:px-8 py-4"
                        prop.children [
                            Html.div [
                                prop.className "flex items-center gap-4"
                                prop.children [
                                    // Mobile logo
                                    Html.h1 [
                                        prop.className "text-xl font-bold text-primary lg:hidden"
                                        prop.text "Cinemarco"
                                    ]
                                    // Search placeholder
                                    Html.div [
                                        prop.className "flex-1 hidden sm:block"
                                        prop.children [
                                            Html.div [
                                                prop.className "relative max-w-md"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "absolute left-3 top-1/2 -translate-y-1/2 text-base-content/40"
                                                        prop.text "🔍"
                                                    ]
                                                    Html.input [
                                                        prop.className "input input-bordered w-full pl-10"
                                                        prop.placeholder "Search movies and series..."
                                                        prop.disabled true
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

            // Page content
            Html.div [
                prop.className "container mx-auto px-4 lg:px-8 py-8"
                prop.children [
                    pageContent model.CurrentPage
                ]
            ]
        ]
    ]

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "min-h-screen bg-base-100"
        prop.children [
            sidebar model dispatch
            mobileNav model dispatch
            mainContent model dispatch
        ]
    ]
