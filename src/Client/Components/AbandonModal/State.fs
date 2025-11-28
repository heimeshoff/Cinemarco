module Components.AbandonModal.State

open Elmish
open Shared.Api
open Shared.Domain
open Types

type AbandonApi = EntryId * AbandonRequest -> Async<Result<LibraryEntry, string>>

let init (entryId: EntryId) : Model =
    Model.create entryId

let update (api: AbandonApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | ReasonChanged reason ->
        { model with Reason = reason }, Cmd.none, NoOp

    | SeasonChanged season ->
        { model with AbandonedAtSeason = season }, Cmd.none, NoOp

    | EpisodeChanged episode ->
        { model with AbandonedAtEpisode = episode }, Cmd.none, NoOp

    | Submit ->
        let request : AbandonRequest = {
            Reason = if String.length model.Reason > 0 then Some model.Reason else None
            AbandonedAtSeason = model.AbandonedAtSeason
            AbandonedAtEpisode = model.AbandonedAtEpisode
        }
        let cmd =
            Cmd.OfAsync.either
                api
                (model.EntryId, request)
                SubmitResult
                (fun ex -> Error ex.Message |> SubmitResult)
        { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok entry) ->
        { model with IsSubmitting = false }, Cmd.none, Abandoned entry

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
