module Components.Layout.State

open Elmish
open Common.Types
open Types

type HealthApi = unit -> Async<Shared.Api.HealthCheckResponse>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg CheckHealth

let update (api: HealthApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | ToggleMobileMenu ->
        { model with IsMobileMenuOpen = not model.IsMobileMenuOpen }, Cmd.none, NoOp

    | CloseMobileMenu ->
        { model with IsMobileMenuOpen = false }, Cmd.none, NoOp

    | CheckHealth ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> HealthCheckResult)
                (fun ex -> Error ex.Message |> HealthCheckResult)
        { model with HealthCheck = Loading }, cmd, NoOp

    | HealthCheckResult (Ok response) ->
        { model with HealthCheck = Success response }, Cmd.none, NoOp

    | HealthCheckResult (Error err) ->
        { model with HealthCheck = Failure err }, Cmd.none, NoOp
