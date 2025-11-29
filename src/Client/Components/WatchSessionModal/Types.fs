module Components.WatchSessionModal.Types

open Shared.Domain

type Model = {
    EntryId: EntryId
    Name: string
    SelectedTags: TagId list
    SelectedFriends: FriendId list
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NameChanged of string
    | ToggleTag of TagId
    | ToggleFriend of FriendId
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
        Name = ""
        SelectedTags = []
        SelectedFriends = []
        IsSubmitting = false
        Error = None
    }
