module Pages.MovieDetail.Types

open System
open Common.Types
open Shared.Domain

/// Tabs for movie detail view
type MovieTab =
    | Overview
    | CastCrew
    | Friends

type Model = {
    EntryId: EntryId
    Entry: RemoteData<LibraryEntry>
    Collections: RemoteData<Collection list>
    Credits: RemoteData<TmdbCredits>
    TrackedPersonIds: Set<TmdbPersonId>
    WatchSessions: RemoteData<MovieWatchSession list>
    IsAddingFriend: bool
    IsAddingWatchSession: bool
    IsRatingOpen: bool
    IsFriendSelectorOpen: bool
    IsWatchSessionModalOpen: bool
    ActiveTab: MovieTab
    /// The session ID and date currently being edited (None when not editing)
    EditingSessionDate: (SessionId * DateTime) option
}

type Msg =
    | LoadEntry
    | EntryLoaded of Result<LibraryEntry option, string>
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | LoadCredits of TmdbMovieId
    | CreditsLoaded of Result<TmdbCredits, string>
    | LoadTrackedContributors
    | TrackedContributorsLoaded of TrackedContributor list
    | LoadWatchSessions
    | WatchSessionsLoaded of Result<MovieWatchSession list, string>
    | SetActiveTab of MovieTab
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
    | ToggleFriendSelector
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | OpenWatchSessionModal
    | CloseWatchSessionModal
    | CreateWatchSession of DateTime * FriendId list * string option
    | WatchSessionCreated of Result<MovieWatchSession, string>
    | DeleteWatchSession of SessionId
    | WatchSessionDeleted of SessionId * Result<unit, string>
    | StartEditingSessionDate of SessionId * DateTime
    | UpdateEditingDate of DateTime
    | SaveSessionDate
    | CancelEditingSessionDate
    | SessionDateUpdated of Result<MovieWatchSession, string>
    | ActionResult of Result<LibraryEntry, string>
    | ViewContributor of personId: TmdbPersonId * name: string
    | ViewFriendDetail of friendId: FriendId * name: string
    | ViewCollectionDetail of collectionId: CollectionId * name: string
    | EditWatchSession of MovieWatchSession
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToContributor of personId: TmdbPersonId * name: string * isTracked: bool
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | RequestOpenAddToCollectionModal of CollectionItemRef * title: string
    | RequestOpenMovieWatchSessionModal of EntryId
    | RequestEditMovieWatchSession of MovieWatchSession
    | NavigateToCollectionDetail of collectionId: CollectionId * name: string
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry
    | FriendCreatedInline of Friend
    | MovieWatchSessionRemoved

module Model =
    let create entryId = {
        EntryId = entryId
        Entry = NotAsked
        Collections = NotAsked
        Credits = NotAsked
        TrackedPersonIds = Set.empty
        WatchSessions = NotAsked
        IsAddingFriend = false
        IsAddingWatchSession = false
        IsRatingOpen = false
        IsFriendSelectorOpen = false
        IsWatchSessionModalOpen = false
        ActiveTab = Overview
        EditingSessionDate = None
    }
