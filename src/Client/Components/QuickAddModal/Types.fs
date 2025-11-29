module Components.QuickAddModal.Types

open Shared.Domain

type Model = {
    SelectedItem: TmdbSearchResult
    WhyAddedNote: string
    SelectedTags: TagId list
    SelectedFriends: FriendId list
    IsSubmitting: bool
    IsAddingFriend: bool
    Error: string option
}

type Msg =
    | NoteChanged of string
    | ToggleTag of TagId
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | Submit
    | SubmitResult of Result<LibraryEntry, string>
    | Close

type ExternalMsg =
    | NoOp
    | Added of LibraryEntry
    | CloseRequested
    | FriendCreatedInline of Friend

module Model =
    let create (item: TmdbSearchResult) = {
        SelectedItem = item
        WhyAddedNote = ""
        SelectedTags = []
        SelectedFriends = []
        IsSubmitting = false
        IsAddingFriend = false
        Error = None
    }
