module Pages.SeriesDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
    EpisodeProgress: EpisodeProgress list
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<(LibraryEntry * EpisodeProgress list) option, string>
    | MarkSeriesCompleted
    | OpenAbandonModal
    | ResumeEntry
    | ToggleEpisodeWatched of seasonNum: int * episodeNum: int * isWatched: bool
    | MarkSeasonWatched of seasonNum: int
    | ToggleFavorite
    | SetRating of int
    | UpdateNotes of string
    | SaveNotes
    | OpenDeleteModal
    | ToggleTag of TagId
    | ToggleFriend of FriendId
    | ActionResult of Result<LibraryEntry, string>
    | EpisodeActionResult of Result<EpisodeProgress list, string>
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let create entryId = {
        EntryId = entryId
        Entry = NotAsked
        EpisodeProgress = []
    }
