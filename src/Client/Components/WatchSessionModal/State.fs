module Components.WatchSessionModal.State

open Elmish
open Shared.Domain
open Types

type Api = {
    CreateSession: CreateSessionRequest -> Async<Result<WatchSession, string>>
    CreateFriend: CreateFriendRequest -> Async<Result<Friend, string>>
}

let init (entryId: EntryId) : Model =
    Model.create entryId

let update (api: Api) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
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

    | AddNewFriend name ->
        let request : CreateFriendRequest = {
            Name = name
            Nickname = None
            Notes = None
        }
        let cmd =
            Cmd.OfAsync.either
                api.CreateFriend
                request
                FriendCreated
                (fun ex -> Error ex.Message |> FriendCreated)
        { model with IsAddingFriend = true; Error = None }, cmd, NoOp

    | FriendCreated (Ok friend) ->
        // Add the new friend to the selected list
        { model with
            IsAddingFriend = false
            SelectedFriends = friend.Id :: model.SelectedFriends }, Cmd.none, NoOp

    | FriendCreated (Error err) ->
        { model with IsAddingFriend = false; Error = Some err }, Cmd.none, NoOp

    | Submit ->
        if List.isEmpty model.SelectedFriends then
            { model with Error = Some "Please select at least one friend to watch with" }, Cmd.none, NoOp
        else
            let request : CreateSessionRequest = {
                EntryId = model.EntryId
                Tags = model.SelectedTags
                Friends = model.SelectedFriends
            }
            let cmd =
                Cmd.OfAsync.either
                    api.CreateSession
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
