module Components.SearchModal.State

open Elmish
open Common.Types
open Shared.Domain
open Types

/// Debounce delay for search (milliseconds)
let private searchDebounceDelay = 300

let init () : Model =
    Model.empty

let update (api: unit -> string -> Async<TmdbSearchResult list>) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | QueryChanged query ->
        let newModel = { model with Query = query }
        let cmd =
            if query.Length >= 2 then
                Cmd.OfAsync.perform
                    (fun () -> async { do! Async.Sleep searchDebounceDelay })
                    ()
                    (fun () -> SearchDebounced)
            else
                Cmd.none
        newModel, cmd, NoOp

    | SearchDebounced ->
        if model.Query.Length >= 2 then
            let cmd =
                Cmd.OfAsync.either
                    (api ())
                    model.Query
                    (Ok >> SearchResults)
                    (fun ex -> Error ex.Message |> SearchResults)
            { model with Results = Loading }, cmd, NoOp
        else
            model, Cmd.none, NoOp

    | SearchResults (Ok results) ->
        { model with Results = Success results }, Cmd.none, NoOp

    | SearchResults (Error err) ->
        { model with Results = Failure err }, Cmd.none, NoOp

    | SelectItem item ->
        model, Cmd.none, ItemSelected item

    | Close ->
        model, Cmd.none, CloseRequested
