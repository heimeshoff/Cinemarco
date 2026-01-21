module Pages.Styleguide.State

open Elmish
open Types

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.none

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | Msg.NoOp ->
        model, Cmd.none, ExternalMsg.NoOp
