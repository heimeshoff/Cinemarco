module Components.FriendModal.State

open Elmish
open Shared.Api
open Shared.Domain
open Types

type SaveApi = {
    Create: CreateFriendRequest -> Async<Result<Friend, string>>
    Update: UpdateFriendRequest -> Async<Result<Friend, string>>
}

let init (friend: Friend option) : Model =
    match friend with
    | Some f -> Model.fromFriend f
    | None -> Model.empty

let update (api: SaveApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | NameChanged name ->
        { model with Name = name }, Cmd.none, NoOp

    | NicknameChanged nickname ->
        { model with Nickname = nickname }, Cmd.none, NoOp

    | Submit ->
        if model.Name.Trim().Length = 0 then
            { model with Error = Some "Name is required" }, Cmd.none, NoOp
        else
            let cmd =
                match model.EditingFriend with
                | None ->
                    let request : CreateFriendRequest = {
                        Name = model.Name.Trim()
                        Nickname = if model.Nickname.Trim().Length > 0 then Some (model.Nickname.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        api.Create
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
                | Some existing ->
                    let request : UpdateFriendRequest = {
                        Id = existing.Id
                        Name = Some (model.Name.Trim())
                        Nickname = if model.Nickname.Trim().Length > 0 then Some (model.Nickname.Trim()) else None
                        AvatarBase64 = None  // Don't change avatar from here
                    }
                    Cmd.OfAsync.either
                        api.Update
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok friend) ->
        { model with IsSubmitting = false }, Cmd.none, Saved friend

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
