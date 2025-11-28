module Components.Notification.State

open Elmish
open Types

let init () : Model =
    Model.empty

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | Show (message, isSuccess) ->
        let newModel = {
            Message = message
            IsSuccess = isSuccess
            IsVisible = true
        }
        // Auto-hide after 4 seconds
        let cmd =
            Cmd.OfAsync.perform
                (fun () -> async { do! Async.Sleep 4000 })
                ()
                (fun () -> Hide)
        newModel, cmd, NoOp

    | Hide ->
        { model with IsVisible = false }, Cmd.none, Dismissed
