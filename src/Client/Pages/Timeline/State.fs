module Pages.Timeline.State

open System
open Elmish
open Common.Types
open Shared.Domain
open Types

/// API type for Timeline operations
type TimelineApi = {
    GetEntries: TimelineFilter * int * int -> Async<PagedResponse<TimelineEntry>>
    GetDateRange: TimelineFilter -> Async<TimelineDateRange option>
    GetYearStats: TimelineFilter -> Async<TimelineYearStats list>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.batch [
        Cmd.ofMsg LoadEntries
        Cmd.ofMsg LoadDateRange
        Cmd.ofMsg LoadYearStats
    ]

let private buildFilter (model: Model) : TimelineFilter =
    {
        StartDate = model.StartDate
        EndDate = model.EndDate
        MediaType = model.MediaType
        EntryId = None
    }

let update (api: TimelineApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntries ->
        let filter = buildFilter model
        let cmd =
            Cmd.OfAsync.either
                api.GetEntries
                (filter, 1, model.PageSize)
                (Ok >> EntriesLoaded)
                (fun ex -> Error ex.Message |> EntriesLoaded)
        { model with Entries = Loading; Page = 1; TotalCount = 0; HasNextPage = false }, cmd, NoOp

    | EntriesLoaded (Ok response) ->
        { model with
            Entries = Success response.Items
            TotalCount = response.TotalCount
            HasNextPage = response.HasNextPage
            Page = response.Page
        }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | LoadMoreEntries ->
        match model.Entries with
        | Success _ when model.HasNextPage && not model.IsLoadingMore ->
            let filter = buildFilter model
            let nextPage = model.Page + 1
            let cmd =
                Cmd.OfAsync.either
                    api.GetEntries
                    (filter, nextPage, model.PageSize)
                    (Ok >> MoreEntriesLoaded)
                    (fun ex -> Error ex.Message |> MoreEntriesLoaded)
            { model with IsLoadingMore = true }, cmd, NoOp
        | _ -> model, Cmd.none, NoOp

    | MoreEntriesLoaded (Ok response) ->
        match model.Entries with
        | Success existingItems ->
            let combinedItems = existingItems @ response.Items
            { model with
                Entries = Success combinedItems
                TotalCount = response.TotalCount
                HasNextPage = response.HasNextPage
                Page = response.Page
                IsLoadingMore = false
            }, Cmd.none, NoOp
        | _ ->
            { model with IsLoadingMore = false }, Cmd.none, NoOp

    | MoreEntriesLoaded (Error _) ->
        { model with IsLoadingMore = false }, Cmd.none, NoOp

    | LoadDateRange ->
        // Always load full range for year scale (ignore date filters)
        let filter = { buildFilter model with StartDate = None; EndDate = None }
        let cmd =
            Cmd.OfAsync.either
                api.GetDateRange
                filter
                (Ok >> DateRangeLoaded)
                (fun ex -> Error ex.Message |> DateRangeLoaded)
        { model with DateRange = Loading }, cmd, NoOp

    | DateRangeLoaded (Ok dateRange) ->
        { model with DateRange = Success dateRange }, Cmd.none, NoOp

    | DateRangeLoaded (Error err) ->
        { model with DateRange = Failure err }, Cmd.none, NoOp

    | LoadYearStats ->
        // Always load full range for year scale navigation (ignore date filters)
        let filter = { buildFilter model with StartDate = None; EndDate = None }
        let cmd =
            Cmd.OfAsync.either
                api.GetYearStats
                filter
                (Ok >> YearStatsLoaded)
                (fun ex -> Error ex.Message |> YearStatsLoaded)
        { model with YearStats = Loading }, cmd, NoOp

    | YearStatsLoaded (Ok stats) ->
        { model with YearStats = Success stats }, Cmd.none, NoOp

    | YearStatsLoaded (Error err) ->
        { model with YearStats = Failure err }, Cmd.none, NoOp

    | UpdateVisibleDate date ->
        { model with CurrentVisibleDate = Some date }, Cmd.none, NoOp

    | JumpToDate _ ->
        // Handled in view via scroll behavior
        model, Cmd.none, NoOp

    | JumpToYear year ->
        // Filter entries to show only the selected year
        let startDate = Some (DateTime(year, 1, 1))
        let endDate = Some (DateTime(year, 12, 31))
        { model with
            StartDate = startDate
            EndDate = endDate
            CurrentVisibleDate = Some (DateTime(year, 1, 1))
        }, Cmd.ofMsg LoadEntries, NoOp

    | SetStartDate date ->
        { model with StartDate = date }, Cmd.batch [Cmd.ofMsg LoadEntries; Cmd.ofMsg LoadDateRange; Cmd.ofMsg LoadYearStats], NoOp

    | SetEndDate date ->
        { model with EndDate = date }, Cmd.batch [Cmd.ofMsg LoadEntries; Cmd.ofMsg LoadDateRange; Cmd.ofMsg LoadYearStats], NoOp

    | SetMediaTypeFilter mediaType ->
        { model with MediaType = mediaType }, Cmd.batch [Cmd.ofMsg LoadEntries; Cmd.ofMsg LoadDateRange; Cmd.ofMsg LoadYearStats], NoOp

    | ToggleDateFilter ->
        { model with IsDateFilterOpen = not model.IsDateFilterOpen }, Cmd.none, NoOp

    | ClearFilters ->
        { model with
            StartDate = None
            EndDate = None
            MediaType = None
            IsDateFilterOpen = false
        }, Cmd.batch [Cmd.ofMsg LoadEntries; Cmd.ofMsg LoadDateRange; Cmd.ofMsg LoadYearStats], NoOp

    | ViewMovieDetail (entryId, title, releaseDate) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title, releaseDate)

    | ViewSeriesDetail (entryId, name, firstAirDate) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name, firstAirDate)
