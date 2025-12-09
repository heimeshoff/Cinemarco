module Pages.Stats.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type StatsApi = unit -> Async<TimeIntelligenceStats>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadStats

let update (api: StatsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadStats ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
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

    | ViewCollection (collectionId, name) ->
        model, Cmd.none, NavigateToCollection (collectionId, name)
