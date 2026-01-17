module Pages.Timeline.Types

open System
open Common.Types
open Shared.Domain

/// Model for Timeline page
type Model = {
    /// Timeline entries (accumulated for infinite scroll)
    Entries: RemoteData<TimelineEntry list>
    /// Total count of entries
    TotalCount: int
    /// Whether there are more entries to load
    HasNextPage: bool
    /// Current page (for pagination)
    Page: int
    /// Page size
    PageSize: int
    /// Date range for time axis
    DateRange: RemoteData<TimelineDateRange option>
    /// Currently visible date (for time axis position indicator)
    CurrentVisibleDate: DateTime option
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
    | LoadDateRange
    | DateRangeLoaded of Result<TimelineDateRange option, string>
    | UpdateVisibleDate of DateTime
    | JumpToDate of DateTime
    | SetStartDate of DateTime option
    | SetEndDate of DateTime option
    | SetMediaTypeFilter of MediaType option
    | ToggleDateFilter
    | ClearFilters
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option

/// External messages for parent to handle
type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option

module Model =
    let empty = {
        Entries = NotAsked
        TotalCount = 0
        HasNextPage = false
        Page = 1
        PageSize = 30
        DateRange = NotAsked
        CurrentVisibleDate = None
        StartDate = None
        EndDate = None
        MediaType = None
        IsDateFilterOpen = false
        IsLoadingMore = false
    }
