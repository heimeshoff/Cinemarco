module Pages.FriendDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    FriendId: FriendId
    Entries: RemoteData<LibraryEntry list>
    /// Whether we're currently editing the name
    IsEditingName: bool
    /// The current value in the name input while editing
    EditingName: string
    /// Whether we're currently saving
    IsSaving: bool
}

type Msg =
    | LoadEntries
    | EntriesLoaded of Result<LibraryEntry list, string>
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string
    | GoBack
    | OpenProfileImageModal of Friend
    // Inline name editing
    | StartEditingName of string
    | UpdateEditingName of string
    | SaveFriendName
    | CancelEditing
    | FriendNameSaved of Result<Friend, string>

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string
    | RequestOpenProfileImageModal of Friend
    | FriendUpdated of Friend

module Model =
    let create friendId = {
        FriendId = friendId
        Entries = NotAsked
        IsEditingName = false
        EditingName = ""
        IsSaving = false
    }
