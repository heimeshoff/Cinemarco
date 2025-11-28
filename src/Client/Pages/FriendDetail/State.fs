module Pages.FriendDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type EntriesApi = FriendId -> Async<LibraryEntry list>

let init (friendId: FriendId) : Model * Cmd<Msg> =
    Model.create friendId, Cmd.ofMsg LoadEntries

let update (api: EntriesApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntries ->
        let cmd =
            Cmd.OfAsync.either
                api
                model.FriendId
                (Ok >> EntriesLoaded)
                (fun ex -> Error ex.Message |> EntriesLoaded)
        { model with Entries = Loading }, cmd, NoOp

    | EntriesLoaded (Ok entries) ->
        { model with Entries = Success entries }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail entryId ->
        model, Cmd.none, NavigateToMovieDetail entryId

    | ViewSeriesDetail entryId ->
        model, Cmd.none, NavigateToSeriesDetail entryId

    | GoBack ->
        model, Cmd.none, NavigateBack
