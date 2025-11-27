module State

open Elmish
open Shared.Api
open Types

/// Application model
type Model = {
    CurrentPage: Page
    HealthCheck: RemoteData<HealthCheckResponse>
}

/// Application messages
type Msg =
    | NavigateTo of Page
    | CheckHealth
    | HealthCheckResult of Result<HealthCheckResponse, string>

/// Initialize the model
let init () : Model * Cmd<Msg> =
    let model = {
        CurrentPage = HomePage
        HealthCheck = NotAsked
    }
    model, Cmd.ofMsg CheckHealth

/// Update function following the MVU pattern
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | NavigateTo page ->
        { model with CurrentPage = page }, Cmd.none

    | CheckHealth ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.healthCheck
                ()
                (Ok >> HealthCheckResult)
                (fun ex -> Error ex.Message |> HealthCheckResult)
        { model with HealthCheck = Loading }, cmd

    | HealthCheckResult (Ok response) ->
        { model with HealthCheck = Success response }, Cmd.none

    | HealthCheckResult (Error err) ->
        { model with HealthCheck = Failure err }, Cmd.none
