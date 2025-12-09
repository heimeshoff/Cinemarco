module Pages.MovieDetail.Types

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
    IsAddingFriend: bool
    IsRatingOpen: bool
    IsFriendSelectorOpen: bool
    ActiveTab: MovieTab
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
    | ActionResult of Result<LibraryEntry, string>
    | ViewContributor of personId: TmdbPersonId * name: string
    | ViewFriendDetail of friendId: FriendId * name: string
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToContributor of personId: TmdbPersonId * name: string * isTracked: bool
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | RequestOpenAbandonModal of EntryId
    | RequestOpenDeleteModal of EntryId
    | RequestOpenAddToCollectionModal of CollectionItemRef * title: string
    | ShowNotification of message: string * isSuccess: bool
    | EntryUpdated of LibraryEntry
    | FriendCreatedInline of Friend

module Model =
    let create entryId = {
        EntryId = entryId
        Entry = NotAsked
        Collections = NotAsked
        Credits = NotAsked
        TrackedPersonIds = Set.empty
        IsAddingFriend = false
        IsRatingOpen = false
        IsFriendSelectorOpen = false
        ActiveTab = Overview
    }
