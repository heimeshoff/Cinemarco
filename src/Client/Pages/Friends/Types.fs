module Pages.Friends.Types

open Common.Types
open Shared.Domain

type Model = {
    Friends: RemoteData<Friend list>
}

type Msg =
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>
    | ViewFriendDetail of FriendId
    | OpenAddFriendModal
    | OpenEditFriendModal of Friend
    | OpenDeleteFriendModal of Friend

type ExternalMsg =
    | NoOp
    | NavigateToFriendDetail of FriendId
    | RequestOpenAddModal
    | RequestOpenEditModal of Friend
    | RequestOpenDeleteModal of Friend

module Model =
    let empty = { Friends = NotAsked }
