module Common.Routing

open Shared.Domain

/// Page routes for navigation
type Page =
    | HomePage
    | LibraryPage
    | MovieDetailPage of EntryId
    | SeriesDetailPage of EntryId
    | SessionDetailPage of SessionId
    | FriendsPage
    | FriendDetailPage of FriendId
    | TagsPage
    | TagDetailPage of TagId
    | CollectionsPage
    | CollectionDetailPage of CollectionId
    | StatsPage
    | TimelinePage
    | GraphPage
    | ImportPage
    | CachePage
    | NotFoundPage

module Page =
    let toUrl = function
        | HomePage -> "/"
        | LibraryPage -> "/library"
        | MovieDetailPage (EntryId id) -> $"/movie/{id}"
        | SeriesDetailPage (EntryId id) -> $"/series/{id}"
        | SessionDetailPage (SessionId id) -> $"/session/{id}"
        | FriendsPage -> "/friends"
        | FriendDetailPage (FriendId id) -> $"/friend/{id}"
        | TagsPage -> "/tags"
        | TagDetailPage (TagId id) -> $"/tag/{id}"
        | CollectionsPage -> "/collections"
        | CollectionDetailPage (CollectionId id) -> $"/collection/{id}"
        | StatsPage -> "/stats"
        | TimelinePage -> "/timeline"
        | GraphPage -> "/graph"
        | ImportPage -> "/import"
        | CachePage -> "/cache"
        | NotFoundPage -> "/404"

    let toString = function
        | HomePage -> "Home"
        | LibraryPage -> "Library"
        | MovieDetailPage _ -> "Movie"
        | SeriesDetailPage _ -> "Series"
        | SessionDetailPage _ -> "Session"
        | FriendsPage -> "Friends"
        | FriendDetailPage _ -> "Friend"
        | TagsPage -> "Tags"
        | TagDetailPage _ -> "Tag"
        | CollectionsPage -> "Collections"
        | CollectionDetailPage _ -> "Collection"
        | StatsPage -> "Stats"
        | TimelinePage -> "Timeline"
        | GraphPage -> "Graph"
        | ImportPage -> "Import"
        | CachePage -> "Cache"
        | NotFoundPage -> "Not Found"
