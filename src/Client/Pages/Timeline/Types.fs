module Pages.Timeline.Types

open System
open Common.Types
open Shared.Domain

/// Model for Timeline page
type Model = {
    /// Timeline entries, paged
    Entries: RemoteData<PagedResponse<TimelineEntry>>
    /// Current page
    Page: int
    /// Page size
    PageSize: int
    /// Start date filter (optional)
    StartDate: DateTime option
    /// End date filter (optional)
    EndDate: DateTime option
    /// Media type filter (optional)
    MediaType: MediaType option
    /// Whether date filter picker is open
    IsDateFilterOpen: bool
    /// Loading more entries
    IsLoadingMore: bool
}

/// Messages for Timeline page
type Msg =
    | LoadEntries
    | EntriesLoaded of Result<PagedResponse<TimelineEntry>, string>
    | LoadMoreEntries
    | MoreEntriesLoaded of Result<PagedResponse<TimelineEntry>, string>
    | SetStartDate of DateTime option
    | SetEndDate of DateTime option
    | SetMediaTypeFilter of MediaType option
    | ToggleDateFilter
    | ClearFilters
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string

/// External messages for parent to handle
type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string

module Model =
    let empty = {
        Entries = NotAsked
        Page = 1
        PageSize = 30
        StartDate = None
        EndDate = None
        MediaType = None
        IsDateFilterOpen = false
        IsLoadingMore = false
    }
