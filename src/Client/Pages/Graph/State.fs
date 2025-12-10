module Pages.Graph.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type GraphApi = GraphFilter -> Async<RelationshipGraph>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadGraph

let update (api: GraphApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadGraph ->
        let cmd =
            Cmd.OfAsync.either
                api
                model.Filter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        { model with Graph = Loading }, cmd, NoOp

    | GraphLoaded (Ok graph) ->
        { model with Graph = Success graph }, Cmd.none, NoOp

    | GraphLoaded (Error err) ->
        { model with Graph = Failure err }, Cmd.none, NoOp

    | UpdateFilter filter ->
        // Update filter and reload graph
        let cmd =
            Cmd.OfAsync.either
                api
                filter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        { model with Filter = filter; Graph = Loading }, cmd, NoOp

    | SelectNode node ->
        { model with SelectedNode = node; IsPanelOpen = true }, Cmd.none, NoOp

    | DeselectNode ->
        { model with SelectedNode = NoSelection }, Cmd.none, NoOp

    | TogglePanel ->
        { model with IsPanelOpen = not model.IsPanelOpen }, Cmd.none, NoOp

    | SetZoom zoom ->
        { model with Zoom = zoom }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title)

    | ViewSeriesDetail (entryId, name) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name)

    | ViewFriendDetail (friendId, name) ->
        model, Cmd.none, NavigateToFriendDetail (friendId, name)

    | ViewContributor (contributorId, name) ->
        model, Cmd.none, NavigateToContributor (contributorId, name)
