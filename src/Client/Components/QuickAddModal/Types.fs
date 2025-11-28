module Components.QuickAddModal.Types

open Shared.Domain

type Model = {
    SelectedItem: TmdbSearchResult
    WhyAddedNote: string
    SelectedTags: TagId list
    SelectedFriends: FriendId list
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NoteChanged of string
    | ToggleTag of TagId
    | ToggleFriend of FriendId
    | Submit
    | SubmitResult of Result<LibraryEntry, string>
    | Close

type ExternalMsg =
    | NoOp
    | Added of LibraryEntry
    | CloseRequested

module Model =
    let create (item: TmdbSearchResult) = {
        SelectedItem = item
        WhyAddedNote = ""
        SelectedTags = []
        SelectedFriends = []
        IsSubmitting = false
        Error = None
    }
