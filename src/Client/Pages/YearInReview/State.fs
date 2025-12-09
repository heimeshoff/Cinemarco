module Pages.YearInReview.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type YearInReviewApi = {
    GetStats: int -> Async<YearInReviewStats>
    GetAvailableYears: unit -> Async<AvailableYears>
}

let init (initialYear: int option) : Model * Cmd<Msg> =
    let year = initialYear |> Option.defaultValue System.DateTime.UtcNow.Year
    { Model.empty with SelectedYear = year }, Cmd.ofMsg LoadAvailableYears

let update (api: YearInReviewApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadAvailableYears ->
        let cmd =
            Cmd.OfAsync.either
                api.GetAvailableYears
                ()
                (Ok >> AvailableYearsLoaded)
                (fun ex -> Error ex.Message |> AvailableYearsLoaded)
        { model with AvailableYears = Loading }, cmd, NoOp

    | AvailableYearsLoaded (Ok years) ->
        // If the selected year is not in the available years, pick the latest
        let selectedYear =
            if List.contains model.SelectedYear years.Years then
                model.SelectedYear
            else
                years.LatestYear |> Option.defaultValue model.SelectedYear
        let updatedModel = { model with AvailableYears = Success years; SelectedYear = selectedYear }
        // Load stats for the selected year
        updatedModel, Cmd.ofMsg (LoadStats selectedYear), NoOp

    | AvailableYearsLoaded (Error err) ->
        { model with AvailableYears = Failure err }, Cmd.none, NoOp

    | SelectYear year ->
        { model with SelectedYear = year; Stats = Loading }, Cmd.ofMsg (LoadStats year), NoOp

    | LoadStats year ->
        let cmd =
            Cmd.OfAsync.either
                api.GetStats
                year
                (Ok >> StatsLoaded)
                (fun ex -> Error ex.Message |> StatsLoaded)
        { model with Stats = Loading }, cmd, NoOp

    | StatsLoaded (Ok stats) ->
        { model with Stats = Success stats }, Cmd.none, NoOp

    | StatsLoaded (Error err) ->
        { model with Stats = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title)

    | ViewSeriesDetail (entryId, name) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name)
