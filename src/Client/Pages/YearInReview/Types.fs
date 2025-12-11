module Pages.YearInReview.Types

open System
open Common.Types
open Common.Routing
open Shared.Domain

type Model = {
    /// Currently selected year
    SelectedYear: int
    /// Available years with watch data
    AvailableYears: RemoteData<AvailableYears>
    /// Stats for the selected year
    Stats: RemoteData<YearInReviewStats>
    /// Current view mode (overview, movies only, or series only)
    ViewMode: YearInReviewViewMode
}

type Msg =
    | LoadAvailableYears
    | AvailableYearsLoaded of Result<AvailableYears, string>
    | SelectYear of int
    | LoadStats of int
    | StatsLoaded of Result<YearInReviewStats, string>
    | SetViewMode of YearInReviewViewMode
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | NavigateToYearView of year: int * viewMode: YearInReviewViewMode

module Model =
    let empty = {
        SelectedYear = System.DateTime.UtcNow.Year
        AvailableYears = NotAsked
        Stats = NotAsked
        ViewMode = YearInReviewViewMode.Overview
    }
