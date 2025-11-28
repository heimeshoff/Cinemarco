module Components.ConfirmModal.State

open Elmish
open Types

let init (target: DeleteTarget) : Model =
    Model.create target

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | Confirm ->
        { model with IsSubmitting = true }, Cmd.none, Confirmed model.Target

    | Cancel ->
        model, Cmd.none, Cancelled
