module Pages.Timeline.State

open Elmish
open Common.Types
open Shared.Domain
open Types

/// API type for Timeline operations
type TimelineApi = {
    GetEntries: TimelineFilter * int * int -> Async<PagedResponse<TimelineEntry>>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadEntries

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
        { model with Entries = Loading; Page = 1 }, cmd, NoOp

    | EntriesLoaded (Ok response) ->
        { model with Entries = Success response; Page = response.Page }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | LoadMoreEntries ->
        match model.Entries with
        | Success response when response.HasNextPage && not model.IsLoadingMore ->
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
        | Success existingResponse ->
            let combinedItems = existingResponse.Items @ response.Items
            let updatedResponse = {
                response with
                    Items = combinedItems
                    Page = response.Page
            }
            { model with
                Entries = Success updatedResponse
                Page = response.Page
                IsLoadingMore = false
            }, Cmd.none, NoOp
        | _ ->
            { model with IsLoadingMore = false }, Cmd.none, NoOp

    | MoreEntriesLoaded (Error _) ->
        { model with IsLoadingMore = false }, Cmd.none, NoOp

    | SetStartDate date ->
        { model with StartDate = date }, Cmd.ofMsg LoadEntries, NoOp

    | SetEndDate date ->
        { model with EndDate = date }, Cmd.ofMsg LoadEntries, NoOp

    | SetMediaTypeFilter mediaType ->
        { model with MediaType = mediaType }, Cmd.ofMsg LoadEntries, NoOp

    | ToggleDateFilter ->
        { model with IsDateFilterOpen = not model.IsDateFilterOpen }, Cmd.none, NoOp

    | ClearFilters ->
        { model with
            StartDate = None
            EndDate = None
            MediaType = None
            IsDateFilterOpen = false
        }, Cmd.ofMsg LoadEntries, NoOp

    | ViewMovieDetail (entryId, title) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title)

    | ViewSeriesDetail (entryId, name) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name)
