module Types

open Shared.Domain

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
    | MovieDetailPage of EntryId
    | SeriesDetailPage of EntryId
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
        | MovieDetailPage (EntryId id) -> $"/movie/{id}"
        | SeriesDetailPage (EntryId id) -> $"/series/{id}"
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
        | MovieDetailPage _ -> "Movie"
        | SeriesDetailPage _ -> "Series"
        | FriendsPage -> "Friends"
        | TagsPage -> "Tags"
        | CollectionsPage -> "Collections"
        | StatsPage -> "Stats"
        | TimelinePage -> "Timeline"
        | GraphPage -> "Graph"
        | ImportPage -> "Import"
        | NotFoundPage -> "Not Found"

/// Filter options for watch status
type WatchStatusFilter =
    | AllStatuses
    | FilterNotStarted
    | FilterInProgress
    | FilterCompleted
    | FilterAbandoned

/// Sort options for library
type LibrarySortBy =
    | SortByDateAdded
    | SortByTitle
    | SortByYear
    | SortByRating

type SortDirection =
    | Ascending
    | Descending

/// Library filter and sort state
type LibraryFilters = {
    SearchQuery: string
    WatchStatus: WatchStatusFilter
    SelectedTags: TagId list
    MinRating: int option
    SortBy: LibrarySortBy
    SortDirection: SortDirection
}

module LibraryFilters =
    let empty = {
        SearchQuery = ""
        WatchStatus = AllStatuses
        SelectedTags = []
        MinRating = None
        SortBy = SortByDateAdded
        SortDirection = Descending
    }

/// State for the search component
type SearchState = {
    Query: string
    Results: RemoteData<TmdbSearchResult list>
    IsDropdownOpen: bool
}

module SearchState =
    let empty = {
        Query = ""
        Results = NotAsked
        IsDropdownOpen = false
    }

/// State for the quick add modal
type QuickAddModalState = {
    SelectedItem: TmdbSearchResult
    WhyAddedNote: string
    SelectedTags: TagId list
    SelectedFriends: FriendId list
    IsSubmitting: bool
    Error: string option
}

/// Modal display states
type ModalState =
    | NoModal
    | QuickAddModal of QuickAddModalState

/// Helper functions for RemoteData
module RemoteData =
    let isLoading = function
        | Loading -> true
        | _ -> false

    let isSuccess = function
        | Success _ -> true
        | _ -> false

    let toOption = function
        | Success x -> Some x
        | _ -> None

    let defaultValue def = function
        | Success x -> x
        | _ -> def
