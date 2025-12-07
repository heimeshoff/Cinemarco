module Pages.Friends.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type FriendsApi = unit -> Async<Friend list>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadFriends

let update (api: FriendsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadFriends ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> FriendsLoaded)
                (fun ex -> Error ex.Message |> FriendsLoaded)
        { model with Friends = Loading }, cmd, NoOp

    | FriendsLoaded (Ok friends) ->
        { model with Friends = Success friends }, Cmd.none, NoOp

    | FriendsLoaded (Error _) ->
        { model with Friends = Success [] }, Cmd.none, NoOp

    | SetSearchQuery query ->
        { model with SearchQuery = query }, Cmd.none, NoOp

    | ViewFriendDetail friendId ->
        model, Cmd.none, NavigateToFriendDetail friendId

    | OpenAddFriendModal ->
        model, Cmd.none, RequestOpenAddModal

    | OpenEditFriendModal friend ->
        model, Cmd.none, RequestOpenEditModal friend

    | OpenDeleteFriendModal friend ->
        model, Cmd.none, RequestOpenDeleteModal friend
