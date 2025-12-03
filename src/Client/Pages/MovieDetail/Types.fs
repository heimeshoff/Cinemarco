module Pages.MovieDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
    Collections: RemoteData<Collection list>
    IsAddingFriend: bool
    IsRatingOpen: bool
    IsFriendSelectorOpen: bool
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<LibraryEntry option, string>
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | MarkWatched
    | MarkUnwatched
    | OpenAbandonModal
    | ResumeEntry
    | ToggleRatingDropdown
    | SetRating of int
    | UpdateNotes of string
    | SaveNotes
    | OpenDeleteModal
    | OpenAddToCollectionModal
    | ToggleTag of TagId
    | ToggleFriendSelector
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | ActionResult of Result<LibraryEntry, string>
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | RequestOpenAddToCollectionModal of EntryId * title: string
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry
    | FriendCreatedInline of Friend

module Model =
    let create entryId = { EntryId = entryId; Entry = NotAsked; Collections = NotAsked; IsAddingFriend = false; IsRatingOpen = false; IsFriendSelectorOpen = false }
