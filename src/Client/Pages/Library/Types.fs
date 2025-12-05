module Pages.Library.Types

open Common.Types
open Shared.Domain

type WatchStatusFilter =
    | AllStatuses
    | FilterNotStarted
    | FilterInProgress
    | FilterCompleted
    | FilterAbandoned

type LibrarySortBy =
    | SortByDateAdded
    | SortByTitle
    | SortByYear
    | SortByRating

type SortDirection =
    | Ascending
    | Descending

type LibraryFilters = {
    SearchQuery: string
    WatchStatus: WatchStatusFilter
    MinRating: int option
    SortBy: LibrarySortBy
    SortDirection: SortDirection
}

type Model = {
    Entries: RemoteData<LibraryEntry list>
    Filters: LibraryFilters
}

type Msg =
    | LoadEntries
    | EntriesLoaded of Result<LibraryEntry list, string>
    | SetSearchQuery of string
    | SetWatchStatusFilter of WatchStatusFilter
    | SetMinRatingFilter of int option
    | SetSortBy of LibrarySortBy
    | ToggleSortDirection
    | ClearFilters
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId

module LibraryFilters =
    let empty = {
        SearchQuery = ""
        WatchStatus = AllStatuses
        MinRating = None
        SortBy = SortByDateAdded
        SortDirection = Descending
    }

module Model =
    let empty = {
        Entries = NotAsked
        Filters = LibraryFilters.empty
    }
