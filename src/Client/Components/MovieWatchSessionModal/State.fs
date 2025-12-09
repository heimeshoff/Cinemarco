module Components.MovieWatchSessionModal.State

open System
open Elmish
open Shared.Domain
open Types

type Api = {
    CreateSession: CreateMovieWatchSessionRequest -> Async<Result<MovieWatchSession, string>>
    UpdateSession: UpdateMovieWatchSessionRequest -> Async<Result<MovieWatchSession, string>>
    CreateFriend: CreateFriendRequest -> Async<Result<Friend, string>>
}

let init (entryId: EntryId) : Model =
    Model.create entryId

let initEdit (session: MovieWatchSession) : Model =
    Model.edit session

let update (api: Api) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | SetWatchedDate date ->
        { model with WatchedDate = date }, Cmd.none, NoOp

    | SetSessionName name ->
        { model with SessionName = name }, Cmd.none, NoOp

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
        }
        let cmd =
            Cmd.OfAsync.either
                api.CreateFriend
                request
                FriendCreated
                (fun ex -> Error ex.Message |> FriendCreated)
        { model with IsAddingFriend = true }, cmd, NoOp

    | FriendCreated (Ok friend) ->
        { model with
            IsAddingFriend = false
            SelectedFriends = friend.Id :: model.SelectedFriends }, Cmd.none, FriendCreatedInline friend

    | FriendCreated (Error err) ->
        { model with IsAddingFriend = false; Error = Some err }, Cmd.none, NoOp

    | Submit ->
        let nameOption = if String.IsNullOrWhiteSpace(model.SessionName) then None else Some model.SessionName
        match model.Mode with
        | Create entryId ->
            let request : CreateMovieWatchSessionRequest = {
                EntryId = entryId
                WatchedDate = model.WatchedDate
                Friends = model.SelectedFriends
                Name = nameOption
            }
            let cmd =
                Cmd.OfAsync.either
                    api.CreateSession
                    request
                    SubmitResult
                    (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp
        | Edit session ->
            let request : UpdateMovieWatchSessionRequest = {
                SessionId = session.Id
                WatchedDate = model.WatchedDate
                Friends = model.SelectedFriends
                Name = nameOption
            }
            let cmd =
                Cmd.OfAsync.either
                    api.UpdateSession
                    request
                    SubmitResult
                    (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok session) ->
        let externalMsg =
            match model.Mode with
            | Create _ -> Created session
            | Edit _ -> Updated session
        { model with IsSubmitting = false }, Cmd.none, externalMsg

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
