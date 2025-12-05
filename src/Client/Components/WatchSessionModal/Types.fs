module Components.WatchSessionModal.Types

open Shared.Domain

type Model = {
    EntryId: EntryId
    SelectedFriends: FriendId list
    IsSubmitting: bool
    IsAddingFriend: bool
    Error: string option
}

type Msg =
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | Submit
    | SubmitResult of Result<WatchSession, string>
    | Close

type ExternalMsg =
    | NoOp
    | Created of WatchSession
    | CloseRequested

module Model =
    let create (entryId: EntryId) = {
        EntryId = entryId
        SelectedFriends = []
        IsSubmitting = false
        IsAddingFriend = false
        Error = None
    }
