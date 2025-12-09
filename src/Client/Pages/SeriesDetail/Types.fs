module Pages.SeriesDetail.Types

open Common.Types
open Shared.Domain

/// Tabs for series detail view
type SeriesTab =
    | Overview
    | CastCrew
    | Episodes

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
    Collections: RemoteData<Collection list>
    Credits: RemoteData<TmdbCredits>
    TrackedPersonIds: Set<TmdbPersonId>
    Sessions: RemoteData<WatchSession list>
    SelectedSessionId: SessionId option
    EpisodeProgress: EpisodeProgress list
    SeasonDetails: Map<int, TmdbSeasonDetails>
    LoadingSeasons: Set<int>
    IsRatingOpen: bool
    IsFriendSelectorOpen: bool
    IsAddingFriend: bool
    ActiveTab: SeriesTab
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<LibraryEntry option, string>
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | LoadCredits of TmdbSeriesId
    | CreditsLoaded of Result<TmdbCredits, string>
    | LoadTrackedContributors
    | TrackedContributorsLoaded of TrackedContributor list
    | SetActiveTab of SeriesTab
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
    | UpdateEpisodeWatchedDate of seasonNum: int * episodeNum: int * date: System.DateTime option
    | EpisodeDateUpdateResult of Result<EpisodeProgress list, string>
    | ToggleRatingDropdown
    | SetRating of int
    | UpdateNotes of string
    | SaveNotes
    | OpenDeleteModal
    | OpenAddToCollectionModal
    | AddSeasonToCollection of seasonNumber: int
    | AddEpisodeToCollection of seasonNumber: int * episodeNumber: int
    | OpenNewSessionModal
    | DeleteSession of SessionId
    | SessionDeleteResult of Result<SessionId, string>
    | ToggleFriendSelector
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | ActionResult of Result<LibraryEntry, string>
    | EpisodeActionResult of Result<EpisodeProgress list, string>
    | ViewContributor of personId: TmdbPersonId * name: string
    | ViewFriendDetail of friendId: FriendId * name: string
    | ViewCollectionDetail of collectionId: CollectionId * name: string
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToContributor of personId: TmdbPersonId * name: string * isTracked: bool
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | RequestOpenAddToCollectionModal of CollectionItemRef * title: string
    | NavigateToCollectionDetail of collectionId: CollectionId * name: string
    | RequestOpenNewSessionModal of EntryId
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry
    | FriendCreatedInline of Friend
    | WatchSessionRemoved

module Model =
    let create entryId = {
        EntryId = entryId
        Entry = NotAsked
        Collections = NotAsked
        Credits = NotAsked
        TrackedPersonIds = Set.empty
        Sessions = NotAsked
        SelectedSessionId = None
        EpisodeProgress = []
        SeasonDetails = Map.empty
        LoadingSeasons = Set.empty
        IsRatingOpen = false
        IsFriendSelectorOpen = false
        IsAddingFriend = false
        ActiveTab = Overview
    }
