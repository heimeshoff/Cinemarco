module Pages.FriendDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type FriendDetailApi = {
    GetEntries: FriendId -> Async<LibraryEntry list>
    UpdateFriend: UpdateFriendRequest -> Async<Result<Friend, string>>
}

let init (friendId: FriendId) : Model * Cmd<Msg> =
    Model.create friendId, Cmd.ofMsg LoadEntries

let update (api: FriendDetailApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntries ->
        let cmd =
            Cmd.OfAsync.either
                api.GetEntries
                model.FriendId
                (Ok >> EntriesLoaded)
                (fun ex -> Error ex.Message |> EntriesLoaded)
        { model with Entries = Loading }, cmd, NoOp

    | EntriesLoaded (Ok entries) ->
        { model with Entries = Success entries }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title, releaseDate) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title, releaseDate)

    | ViewSeriesDetail (entryId, name, firstAirDate) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name, firstAirDate)

    | GoBack ->
        model, Cmd.none, NavigateBack

    | OpenProfileImageModal friend ->
        model, Cmd.none, RequestOpenProfileImageModal friend

    // Inline name editing
    | StartEditingName currentName ->
        { model with
            IsEditingName = true
            EditingName = currentName
            IsSaving = false }, Cmd.none, NoOp

    | UpdateEditingName name ->
        { model with EditingName = name }, Cmd.none, NoOp

    | SaveFriendName ->
        let trimmedName = model.EditingName.Trim()
        if trimmedName.Length = 0 then
            // Don't save empty names, just cancel
            { model with IsEditingName = false; EditingName = ""; IsSaving = false }, Cmd.none, NoOp
        else
            let request : UpdateFriendRequest = {
                Id = model.FriendId
                Name = Some trimmedName
                Nickname = None
                AvatarBase64 = None
            }
            let cmd =
                Cmd.OfAsync.either
                    api.UpdateFriend
                    request
                    FriendNameSaved
                    (fun ex -> Error ex.Message |> FriendNameSaved)
            { model with IsSaving = true }, cmd, NoOp

    | CancelEditing ->
        { model with IsEditingName = false; EditingName = ""; IsSaving = false }, Cmd.none, NoOp

    | FriendNameSaved (Ok friend) ->
        { model with
            IsEditingName = false
            EditingName = ""
            IsSaving = false }, Cmd.none, FriendUpdated friend

    | FriendNameSaved (Error _) ->
        // On error, just cancel editing
        { model with IsEditingName = false; EditingName = ""; IsSaving = false }, Cmd.none, NoOp
