module Pages.Library.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type LibraryApi = unit -> Async<LibraryEntry list>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadEntries

let update (api: LibraryApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntries ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> EntriesLoaded)
                (fun ex -> Error ex.Message |> EntriesLoaded)
        { model with Entries = Loading }, cmd, NoOp

    | EntriesLoaded (Ok entries) ->
        { model with Entries = Success entries }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | SetSearchQuery query ->
        { model with Filters = { model.Filters with SearchQuery = query } }, Cmd.none, NoOp

    | SetWatchStatusFilter status ->
        { model with Filters = { model.Filters with WatchStatus = status } }, Cmd.none, NoOp

    | SetMinRatingFilter rating ->
        { model with Filters = { model.Filters with MinRating = rating } }, Cmd.none, NoOp

    | SetSortBy sortBy ->
        { model with Filters = { model.Filters with SortBy = sortBy } }, Cmd.none, NoOp

    | ToggleSortDirection ->
        let newDir =
            match model.Filters.SortDirection with
            | Ascending -> Descending
            | Descending -> Ascending
        { model with Filters = { model.Filters with SortDirection = newDir } }, Cmd.none, NoOp

    | ClearFilters ->
        { model with Filters = LibraryFilters.empty }, Cmd.none, NoOp

    | ViewMovieDetail entryId ->
        model, Cmd.none, NavigateToMovieDetail entryId

    | ViewSeriesDetail entryId ->
        model, Cmd.none, NavigateToSeriesDetail entryId

/// Apply filters and sorting to entries
let filterAndSortEntries (filters: LibraryFilters) (entries: LibraryEntry list) =
    entries
    |> List.filter (fun entry ->
        // Search filter
        let title =
            match entry.Media with
            | LibraryMovie m -> m.Title
            | LibrarySeries s -> s.Name
        let matchesSearch =
            filters.SearchQuery = "" ||
            title.ToLowerInvariant().Contains(filters.SearchQuery.ToLowerInvariant())

        // Watch status filter
        let matchesStatus =
            match filters.WatchStatus with
            | AllStatuses -> true
            | FilterNotStarted -> match entry.WatchStatus with NotStarted -> true | _ -> false
            | FilterInProgress -> match entry.WatchStatus with InProgress _ -> true | _ -> false
            | FilterCompleted -> match entry.WatchStatus with Completed -> true | _ -> false
            | FilterAbandoned -> match entry.WatchStatus with Abandoned _ -> true | _ -> false

        // Rating filter
        let matchesRating =
            match filters.MinRating with
            | None -> true
            | Some minRating ->
                match entry.PersonalRating with
                | Some rating -> PersonalRating.toInt rating >= minRating
                | None -> false

        matchesSearch && matchesStatus && matchesRating
    )
    |> List.sortBy (fun entry ->
        let sortKey =
            match filters.SortBy with
            | SortByDateAdded -> entry.DateAdded.Ticks.ToString("D20")
            | SortByTitle ->
                match entry.Media with
                | LibraryMovie m -> m.Title.ToLowerInvariant()
                | LibrarySeries s -> s.Name.ToLowerInvariant()
            | SortByYear ->
                match entry.Media with
                | LibraryMovie m -> m.ReleaseDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue "0000"
                | LibrarySeries s -> s.FirstAirDate |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue "0000"
            | SortByRating ->
                entry.PersonalRating |> Option.map PersonalRating.toInt |> Option.defaultValue 0 |> sprintf "%02d"
        match filters.SortDirection with
        | Ascending -> sortKey
        | Descending -> "~" + (sortKey |> Seq.map (fun c -> char (255 - int c)) |> System.String.Concat)
    )
