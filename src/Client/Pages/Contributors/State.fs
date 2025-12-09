module Pages.Contributors.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type ContributorsApi = {
    GetAll: unit -> Async<TrackedContributor list>
    Untrack: TrackedContributorId -> Async<Result<unit, string>>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadContributors

let update (api: ContributorsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadContributors ->
        let cmd =
            Cmd.OfAsync.either
                api.GetAll
                ()
                (Ok >> ContributorsLoaded)
                (fun ex -> Error ex.Message |> ContributorsLoaded)
        { model with Contributors = Loading }, cmd, NoOp

    | ContributorsLoaded (Ok contributors) ->
        { model with Contributors = Success contributors }, Cmd.none, NoOp

    | ContributorsLoaded (Error err) ->
        { model with Contributors = Failure err }, Cmd.none, NoOp

    | SetDepartmentFilter filter ->
        { model with DepartmentFilter = filter }, Cmd.none, NoOp

    | SetSearchQuery query ->
        { model with SearchQuery = query }, Cmd.none, NoOp

    | ViewContributorDetail (personId, name) ->
        model, Cmd.none, NavigateToContributorDetail (personId, name)

    | UntrackContributor trackedId ->
        let cmd =
            Cmd.OfAsync.either
                api.Untrack
                trackedId
                UntrackResult
                (fun ex -> Error ex.Message |> UntrackResult)
        model, cmd, NoOp

    | UntrackResult (Ok ()) ->
        // Reload the list
        model, Cmd.ofMsg LoadContributors, ShowNotification ("Contributor untracked", true)

    | UntrackResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)
