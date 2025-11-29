module Pages.MovieDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<LibraryEntry option, string>
    | MarkWatched
    | MarkUnwatched
    | OpenAbandonModal
    | ResumeEntry
    | ToggleFavorite
    | SetRating of int
    | UpdateNotes of string
    | SaveNotes
    | OpenDeleteModal
    | ToggleTag of TagId
    | ToggleFriend of FriendId
    | ActionResult of Result<LibraryEntry, string>
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry

module Model =
    let create entryId = { EntryId = entryId; Entry = NotAsked }
