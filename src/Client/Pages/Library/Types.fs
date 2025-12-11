module Pages.Library.Types

open System
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
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option

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
