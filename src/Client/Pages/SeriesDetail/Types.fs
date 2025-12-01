module Pages.SeriesDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
    Collections: RemoteData<Collection list>
    Sessions: RemoteData<WatchSession list>
    SelectedSessionId: SessionId option
    EpisodeProgress: EpisodeProgress list
    SeasonDetails: Map<int, TmdbSeasonDetails>
    LoadingSeasons: Set<int>
    IsRatingOpen: bool
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<LibraryEntry option, string>
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | LoadSessions
    | SessionsLoaded of Result<WatchSession list, string>
    | SelectSession of SessionId
    | LoadSessionProgress of SessionId
    | SessionProgressLoaded of Result<EpisodeProgress list, string>
    | LoadSeasonDetails of int
    | SeasonDetailsLoaded of int * Result<TmdbSeasonDetails, string>
    | MarkSeriesCompleted
    | OpenAbandonModal
    | ResumeEntry
    | ToggleEpisodeWatched of seasonNum: int * episodeNum: int * isWatched: bool
    | MarkEpisodesUpTo of seasonNum: int * episodeNum: int * isWatched: bool
    | MarkSeasonWatched of seasonNum: int
    | ToggleFavorite
    | ToggleRatingDropdown
    | SetRating of int
    | UpdateNotes of string
    | SaveNotes
    | OpenDeleteModal
    | OpenAddToCollectionModal
    | OpenNewSessionModal
    | DeleteSession of SessionId
    | SessionDeleteResult of Result<SessionId, string>
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
    | RequestOpenAddToCollectionModal of EntryId * title: string
    | RequestOpenNewSessionModal of EntryId
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry

module Model =
    let create entryId = {
        EntryId = entryId
        Entry = NotAsked
        Collections = NotAsked
        Sessions = NotAsked
        SelectedSessionId = None
        EpisodeProgress = []
        SeasonDetails = Map.empty
        LoadingSeasons = Set.empty
        IsRatingOpen = false
    }
