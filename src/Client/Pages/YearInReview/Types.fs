module Pages.YearInReview.Types

open Common.Types
open Shared.Domain

type Model = {
    /// Currently selected year
    SelectedYear: int
    /// Available years with watch data
    AvailableYears: RemoteData<AvailableYears>
    /// Stats for the selected year
    Stats: RemoteData<YearInReviewStats>
}

type Msg =
    | LoadAvailableYears
    | AvailableYearsLoaded of Result<AvailableYears, string>
    | SelectYear of int
    | LoadStats of int
    | StatsLoaded of Result<YearInReviewStats, string>
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string

module Model =
    let empty = {
        SelectedYear = System.DateTime.UtcNow.Year
        AvailableYears = NotAsked
        Stats = NotAsked
    }
