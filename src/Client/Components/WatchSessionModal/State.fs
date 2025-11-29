module Components.WatchSessionModal.State

open Elmish
open Shared.Domain
open Types

type CreateApi = CreateSessionRequest -> Async<Result<WatchSession, string>>

let init (entryId: EntryId) : Model =
    Model.create entryId

let update (api: CreateApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | NameChanged name ->
        { model with Name = name }, Cmd.none, NoOp

    | ToggleTag tagId ->
        let newTags =
            if List.contains tagId model.SelectedTags then
                List.filter ((<>) tagId) model.SelectedTags
            else
                tagId :: model.SelectedTags
        { model with SelectedTags = newTags }, Cmd.none, NoOp

    | ToggleFriend friendId ->
        let newFriends =
            if List.contains friendId model.SelectedFriends then
                List.filter ((<>) friendId) model.SelectedFriends
            else
                friendId :: model.SelectedFriends
        { model with SelectedFriends = newFriends }, Cmd.none, NoOp

    | Submit ->
        if model.Name.Trim().Length = 0 then
            { model with Error = Some "Session name is required" }, Cmd.none, NoOp
        else
            let request : CreateSessionRequest = {
                EntryId = model.EntryId
                Name = model.Name.Trim()
                Tags = model.SelectedTags
                Friends = model.SelectedFriends
            }
            let cmd =
                Cmd.OfAsync.either
                    api
                    request
                    SubmitResult
                    (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok session) ->
        { model with IsSubmitting = false }, Cmd.none, Created session

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
