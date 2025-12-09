module Pages.Friends.Types

open Common.Types
open Shared.Domain

type Model = {
    Friends: RemoteData<Friend list>
    SearchQuery: string
}

type Msg =
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>
    | SetSearchQuery of string
    | ViewFriendDetail of friendId: FriendId * name: string
    | OpenAddFriendModal
    | OpenDeleteFriendModal of Friend
    | OpenProfileImageModal of Friend

type ExternalMsg =
    | NoOp
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | RequestOpenAddModal
    | RequestOpenDeleteModal of Friend
    | RequestOpenProfileImageModal of Friend

module Model =
    let empty = { Friends = NotAsked; SearchQuery = "" }
