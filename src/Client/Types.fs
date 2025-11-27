module Types

/// Represents the state of a remote data fetch
type RemoteData<'T> =
    | NotAsked
    | Loading
    | Success of 'T
    | Failure of string

/// Page routes for navigation
type Page =
    | HomePage
    | LibraryPage
    | FriendsPage
    | TagsPage
    | CollectionsPage
    | StatsPage
    | TimelinePage
    | GraphPage
    | ImportPage
    | NotFoundPage

module Page =
    let toUrl = function
        | HomePage -> "/"
        | LibraryPage -> "/library"
        | FriendsPage -> "/friends"
        | TagsPage -> "/tags"
        | CollectionsPage -> "/collections"
        | StatsPage -> "/stats"
        | TimelinePage -> "/timeline"
        | GraphPage -> "/graph"
        | ImportPage -> "/import"
        | NotFoundPage -> "/404"

    let toString = function
        | HomePage -> "Home"
        | LibraryPage -> "Library"
        | FriendsPage -> "Friends"
        | TagsPage -> "Tags"
        | CollectionsPage -> "Collections"
        | StatsPage -> "Stats"
        | TimelinePage -> "Timeline"
        | GraphPage -> "Graph"
        | ImportPage -> "Import"
        | NotFoundPage -> "Not Found"
