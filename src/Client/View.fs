module View

open Feliz
open State
open Types
open Shared.Api
open Shared.Domain

/// TMDB image base URL
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Get poster URL with size
let private getPosterUrl (size: string) (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/{size}{p}"
    | None -> ""

/// Navigation item component
let private navItem (page: Page) (currentPage: Page) (dispatch: Msg -> unit) =
    let isActive = page = currentPage
    Html.li [
        Html.a [
            prop.className (
                "flex items-center gap-3 px-4 py-3 rounded-lg transition-colors " +
                if isActive then "bg-primary text-primary-content"
                else "hover:bg-base-200"
            )
            prop.onClick (fun _ -> dispatch (NavigateTo page))
            prop.children [
                Html.span [
                    prop.className "w-5 h-5 flex items-center justify-center text-sm"
                    prop.text (
                        match page with
                        | HomePage -> "H"
                        | LibraryPage -> "L"
                        | FriendsPage -> "F"
                        | TagsPage -> "T"
                        | CollectionsPage -> "C"
                        | StatsPage -> "S"
                        | TimelinePage -> "Ti"
                        | GraphPage -> "G"
                        | ImportPage -> "I"
                        | NotFoundPage -> "?"
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
            Html.div [
                prop.className "p-4 border-t border-base-300"
                prop.children [
                    match model.HealthCheck with
                    | Success health ->
                        Html.div [
                            prop.className "flex items-center gap-2 text-sm"
                            prop.children [
                                Html.span [ prop.className "w-2 h-2 bg-success rounded-full" ]
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
                                        | HomePage -> "H"
                                        | LibraryPage -> "L"
                                        | StatsPage -> "S"
                                        | _ -> "?"
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

/// Poster card component with shine effect
let private posterCard (item: TmdbSearchResult) (dispatch: Msg -> unit) =
    let year =
        item.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let mediaTypeLabel =
        match item.MediaType with
        | Movie -> "Movie"
        | Series -> "TV"

    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick (fun _ -> dispatch (OpenQuickAddModal item))
        prop.children [
            Html.div [
                prop.className "relative aspect-[2/3] rounded-lg overflow-hidden bg-base-300 shadow-md hover:shadow-xl transition-shadow"
                prop.children [
                    // Poster image
                    match item.PosterPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getPosterUrl "w342" item.PosterPath)
                            prop.alt item.Title
                            prop.className "w-full h-full object-cover"
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center bg-base-300"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/30"
                                    prop.text "?"
                                ]
                            ]
                        ]

                    // Shine effect overlay
                    Html.div [
                        prop.className "poster-shine absolute inset-0 pointer-events-none"
                    ]

                    // Media type badge
                    Html.div [
                        prop.className "absolute top-2 right-2 px-2 py-0.5 bg-base-100/90 rounded text-xs font-medium"
                        prop.text mediaTypeLabel
                    ]

                    // Hover overlay with + icon
                    Html.div [
                        prop.className "absolute inset-0 bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
                        prop.children [
                            Html.div [
                                prop.className "w-12 h-12 rounded-full bg-primary flex items-center justify-center"
                                prop.children [
                                    Html.span [
                                        prop.className "text-2xl text-primary-content font-bold"
                                        prop.text "+"
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
            // Title and year below poster
            Html.div [
                prop.className "mt-2"
                prop.children [
                    Html.p [
                        prop.className "font-medium text-sm truncate"
                        prop.title item.Title
                        prop.text item.Title
                    ]
                    if year <> "" then
                        Html.p [
                            prop.className "text-xs text-base-content/60"
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
            prop.className "absolute top-full left-0 right-0 mt-2 bg-base-100 rounded-lg shadow-xl border border-base-300 max-h-[70vh] overflow-y-auto z-50"
            prop.children [
                match model.Search.Results with
                | Loading ->
                    Html.div [
                        prop.className "p-8 flex items-center justify-center"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-md" ]
                        ]
                    ]
                | Success results when List.isEmpty results ->
                    Html.div [
                        prop.className "p-8 text-center text-base-content/60"
                        prop.text "No results found"
                    ]
                | Success results ->
                    Html.div [
                        prop.className "p-4"
                        prop.children [
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
                        prop.className "p-8 text-center text-error"
                        prop.text $"Error: {err}"
                    ]
                | NotAsked ->
                    Html.none
            ]
        ]

/// Search bar component
let private searchBar (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative flex-1 max-w-2xl"
        prop.children [
            Html.div [
                prop.className "relative"
                prop.children [
                    Html.span [
                        prop.className "absolute left-3 top-1/2 -translate-y-1/2 text-base-content/40"
                        prop.text "?"
                    ]
                    Html.input [
                        prop.className "input input-bordered w-full pl-10 pr-4"
                        prop.placeholder "Search movies and series..."
                        prop.value model.Search.Query
                        prop.onChange (fun (e: string) -> dispatch (SearchQueryChanged e))
                        prop.onBlur (fun _ ->
                            // Delay closing to allow click on results
                            Fable.Core.JS.setTimeout (fun () -> dispatch CloseSearchDropdown) 200 |> ignore
                        )
                    ]
                    if RemoteData.isLoading model.Search.Results then
                        Html.span [
                            prop.className "absolute right-3 top-1/2 -translate-y-1/2"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                            ]
                        ]
                ]
            ]
            searchResultsDropdown model dispatch
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
        | Series -> "TV Series"

    let yearSuffix = if year <> "" then sprintf "(%s)" year else ""
    let subtitle = sprintf "%s %s" mediaTypeLabel yearSuffix

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
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-lg max-h-[90vh] overflow-y-auto"
                prop.children [
                    // Header with poster
                    Html.div [
                        prop.className "relative h-48 bg-base-300 overflow-hidden rounded-t-xl"
                        prop.children [
                            match state.SelectedItem.PosterPath with
                            | Some _ ->
                                Html.img [
                                    prop.src (getPosterUrl "w500" state.SelectedItem.PosterPath)
                                    prop.alt state.SelectedItem.Title
                                    prop.className "w-full h-full object-cover opacity-30"
                                ]
                            | None -> Html.none

                            Html.div [
                                prop.className "absolute inset-0 bg-gradient-to-t from-base-100 to-transparent"
                            ]

                            // Close button
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch CloseModal)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    Html.span [ prop.text "X" ]
                                ]
                            ]

                            // Title
                            Html.div [
                                prop.className "absolute bottom-4 left-4 right-4"
                                prop.children [
                                    Html.h2 [
                                        prop.className "text-2xl font-bold"
                                        prop.text state.SelectedItem.Title
                                    ]
                                    Html.p [
                                        prop.className "text-sm text-base-content/70"
                                        prop.text subtitle
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
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            // Why I added note
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [
                                                prop.className "label-text"
                                                prop.text "Why are you adding this? (optional)"
                                            ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
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
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [
                                                    prop.className "label-text"
                                                    prop.text "Tags"
                                                ]
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
                                                            "badge badge-lg cursor-pointer transition-colors " +
                                                            if isSelected then "badge-primary" else "badge-outline"
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
                                    prop.className "form-control"
                                    prop.children [
                                        Html.label [
                                            prop.className "label"
                                            prop.children [
                                                Html.span [
                                                    prop.className "label-text"
                                                    prop.text "Watched with"
                                                ]
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
                                                            "badge badge-lg cursor-pointer transition-colors " +
                                                            if isSelected then "badge-secondary" else "badge-outline"
                                                        )
                                                        prop.onClick (fun _ -> dispatch (ToggleQuickAddFriend friend.Id))
                                                        prop.disabled state.IsSubmitting
                                                        prop.text friend.Name
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Submit button
                            Html.button [
                                prop.className "btn btn-primary w-full"
                                prop.onClick (fun _ -> dispatch SubmitQuickAdd)
                                prop.disabled state.IsSubmitting
                                prop.children [
                                    if state.IsSubmitting then
                                        Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                    else
                                        Html.span [ prop.text "Add to Library" ]
                                ]
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
        prop.className "toast toast-top toast-end z-50"
        prop.children [
            Html.div [
                prop.className (
                    "alert " +
                    if isSuccess then "alert-success" else "alert-error"
                )
                prop.children [
                    Html.span [ prop.text message ]
                    Html.button [
                        prop.className "btn btn-ghost btn-xs"
                        prop.onClick (fun _ -> dispatch ClearNotification)
                        prop.text "X"
                    ]
                ]
            ]
        ]
    ]

/// Library entry card for displaying items in library
let private libraryEntryCard (entry: LibraryEntry) =
    let (title, posterPath, year) =
        match entry.Media with
        | LibraryMovie m ->
            let y = m.ReleaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
            (m.Title, m.PosterPath, y)
        | LibrarySeries s ->
            let y = s.FirstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue ""
            (s.Name, s.PosterPath, y)

    Html.div [
        prop.className "poster-card group relative"
        prop.children [
            Html.div [
                prop.className "relative aspect-[2/3] rounded-lg overflow-hidden bg-base-300 shadow-md"
                prop.children [
                    match posterPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getPosterUrl "w342" posterPath)
                            prop.alt title
                            prop.className "w-full h-full object-cover"
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center bg-base-300"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/30"
                                    prop.text "?"
                                ]
                            ]
                        ]
                    // Shine effect overlay
                    Html.div [
                        prop.className "poster-shine absolute inset-0 pointer-events-none"
                    ]
                ]
            ]
            Html.div [
                prop.className "mt-2"
                prop.children [
                    Html.p [
                        prop.className "font-medium text-sm truncate"
                        prop.title title
                        prop.text title
                    ]
                    if year <> "" then
                        Html.p [
                            prop.className "text-xs text-base-content/60"
                            prop.text year
                        ]
                ]
            ]
        ]
    ]

/// Home page content
let private homePageContent (model: Model) =
    Html.div [
        prop.className "space-y-8"
        prop.children [
            // Hero section
            Html.div [
                prop.className "text-center py-12"
                prop.children [
                    Html.h2 [
                        prop.className "text-3xl font-bold mb-4"
                        prop.text "Your Cinema Memory Tracker"
                    ]
                    Html.p [
                        prop.className "text-base-content/70 max-w-xl mx-auto"
                        prop.text "Search for movies and series above to add them to your personal library. Track what you've watched, who you watched with, and your personal ratings."
                    ]
                ]
            ]

            // Recently added section
            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.children [
                        Html.h3 [
                            prop.className "text-xl font-bold mb-4"
                            prop.text "Recently Added"
                        ]
                        Html.div [
                            prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 gap-4"
                            prop.children [
                                for entry in entries |> List.truncate 6 do
                                    libraryEntryCard entry
                            ]
                        ]
                    ]
                ]
            | Success _ ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.text "Your library is empty. Search for movies and series to get started!" ]
                    ]
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
                    prop.text $"Error loading library: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]

/// Library page content
let private libraryPageContent (model: Model) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            Html.h2 [
                prop.className "text-2xl font-bold"
                prop.text "My Library"
            ]

            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-8 gap-4"
                    prop.children [
                        for entry in entries do
                            libraryEntryCard entry
                    ]
                ]
            | Success _ ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "Your library is empty" ]
                        Html.p [ prop.text "Use the search bar above to find movies and series to add." ]
                    ]
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
                    prop.text $"Error loading library: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]

/// Page placeholder content (for unimplemented pages)
let private pageContent (page: Page) (model: Model) =
    match page with
    | HomePage -> homePageContent model
    | LibraryPage -> libraryPageContent model
    | _ ->
        Html.div [
            prop.className "flex flex-col items-center justify-center min-h-[50vh] text-center"
            prop.children [
                Html.div [
                    prop.className "text-6xl mb-4"
                    prop.text (
                        match page with
                        | FriendsPage -> "F"
                        | TagsPage -> "T"
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
                        | FriendsPage -> "Manage friends you watch movies with."
                        | TagsPage -> "Organize your library with custom tags."
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
                                    // Search bar
                                    Html.div [
                                        prop.className "flex-1 hidden sm:block"
                                        prop.children [
                                            searchBar model dispatch
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Mobile search bar
            Html.div [
                prop.className "sm:hidden px-4 py-2 border-b border-base-300"
                prop.children [
                    searchBar model dispatch
                ]
            ]

            // Page content
            Html.div [
                prop.className "container mx-auto px-4 lg:px-8 py-8"
                prop.children [
                    pageContent model.CurrentPage model
                ]
            ]
        ]
    ]

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    let friends = RemoteData.defaultValue [] model.Friends
    let tags = RemoteData.defaultValue [] model.Tags

    Html.div [
        prop.className "min-h-screen bg-base-100"
        prop.children [
            sidebar model dispatch
            mobileNav model dispatch
            mainContent model dispatch

            // Modal
            match model.Modal with
            | QuickAddModal state ->
                quickAddModal state friends tags dispatch
            | NoModal -> Html.none

            // Notification
            match model.Notification with
            | Some (message, isSuccess) ->
                notificationToast message isSuccess dispatch
            | None -> Html.none
        ]
    ]
