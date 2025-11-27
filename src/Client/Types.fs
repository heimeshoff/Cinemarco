module Types

open Shared.Domain

/// Represents the state of a remote data fetch
type RemoteData<'T> =
    | NotAsked
    | Loading
    | Success of 'T
    | Failure of string

/// Page routes for navigation
type Page =
    | HomePage
    | LibraryPage
    | MovieDetailPage of EntryId
    | SeriesDetailPage of EntryId
    | FriendsPage
    | FriendDetailPage of FriendId
    | TagsPage
    | TagDetailPage of TagId
    | CollectionsPage
    | StatsPage
    | TimelinePage
    | GraphPage
    | ImportPage
    | NotFoundPage

module Page =
    let toUrl = function
        | HomePage -> "/"
        | LibraryPage -> "/library"
        | MovieDetailPage (EntryId id) -> $"/movie/{id}"
        | SeriesDetailPage (EntryId id) -> $"/series/{id}"
        | FriendsPage -> "/friends"
        | FriendDetailPage (FriendId id) -> $"/friend/{id}"
        | TagsPage -> "/tags"
        | TagDetailPage (TagId id) -> $"/tag/{id}"
        | CollectionsPage -> "/collections"
        | StatsPage -> "/stats"
        | TimelinePage -> "/timeline"
        | GraphPage -> "/graph"
        | ImportPage -> "/import"
        | NotFoundPage -> "/404"

    let toString = function
        | HomePage -> "Home"
        | LibraryPage -> "Library"
        | MovieDetailPage _ -> "Movie"
        | SeriesDetailPage _ -> "Series"
        | FriendsPage -> "Friends"
        | FriendDetailPage _ -> "Friend"
        | TagsPage -> "Tags"
        | TagDetailPage _ -> "Tag"
        | CollectionsPage -> "Collections"
        | StatsPage -> "Stats"
        | TimelinePage -> "Timeline"
        | GraphPage -> "Graph"
        | ImportPage -> "Import"
        | NotFoundPage -> "Not Found"

/// Filter options for watch status
type WatchStatusFilter =
    | AllStatuses
    | FilterNotStarted
    | FilterInProgress
    | FilterCompleted
    | FilterAbandoned

/// Sort options for library
type LibrarySortBy =
    | SortByDateAdded
    | SortByTitle
    | SortByYear
    | SortByRating

type SortDirection =
    | Ascending
    | Descending

/// Library filter and sort state
type LibraryFilters = {
    SearchQuery: string
    WatchStatus: WatchStatusFilter
    SelectedTags: TagId list
    MinRating: int option
    SortBy: LibrarySortBy
    SortDirection: SortDirection
}

module LibraryFilters =
    let empty = {
        SearchQuery = ""
        WatchStatus = AllStatuses
        SelectedTags = []
        MinRating = None
        SortBy = SortByDateAdded
        SortDirection = Descending
    }

/// State for the search component
type SearchState = {
    Query: string
    Results: RemoteData<TmdbSearchResult list>
    IsDropdownOpen: bool
}

module SearchState =
    let empty = {
        Query = ""
        Results = NotAsked
        IsDropdownOpen = false
    }

/// State for the quick add modal
type QuickAddModalState = {
    SelectedItem: TmdbSearchResult
    WhyAddedNote: string
    SelectedTags: TagId list
    SelectedFriends: FriendId list
    IsSubmitting: bool
    Error: string option
}

/// State for the friend add/edit modal
type FriendModalState = {
    EditingFriend: Friend option  // None = creating new, Some = editing existing
    Name: string
    Nickname: string
    Notes: string
    IsSubmitting: bool
    Error: string option
}

module FriendModalState =
    let empty = {
        EditingFriend = None
        Name = ""
        Nickname = ""
        Notes = ""
        IsSubmitting = false
        Error = None
    }

    let fromFriend (friend: Friend) = {
        EditingFriend = Some friend
        Name = friend.Name
        Nickname = friend.Nickname |> Option.defaultValue ""
        Notes = friend.Notes |> Option.defaultValue ""
        IsSubmitting = false
        Error = None
    }

/// State for the tag add/edit modal
type TagModalState = {
    EditingTag: Tag option  // None = creating new, Some = editing existing
    Name: string
    Color: string
    Description: string
    IsSubmitting: bool
    Error: string option
}

module TagModalState =
    let empty = {
        EditingTag = None
        Name = ""
        Color = ""
        Description = ""
        IsSubmitting = false
        Error = None
    }

    let fromTag (tag: Tag) = {
        EditingTag = Some tag
        Name = tag.Name
        Color = tag.Color |> Option.defaultValue ""
        Description = tag.Description |> Option.defaultValue ""
        IsSubmitting = false
        Error = None
    }

/// State for the abandon modal
type AbandonModalState = {
    EntryId: EntryId
    Reason: string
    AbandonedAtSeason: int option
    AbandonedAtEpisode: int option
    IsSubmitting: bool
    Error: string option
}

module AbandonModalState =
    let create entryId = {
        EntryId = entryId
        Reason = ""
        AbandonedAtSeason = None
        AbandonedAtEpisode = None
        IsSubmitting = false
        Error = None
    }

/// Modal display states
type ModalState =
    | NoModal
    | QuickAddModal of QuickAddModalState
    | FriendModal of FriendModalState
    | TagModal of TagModalState
    | ConfirmDeleteFriendModal of Friend
    | ConfirmDeleteTagModal of Tag
    | AbandonModal of AbandonModalState
    | ConfirmDeleteEntryModal of EntryId

/// Helper functions for RemoteData
module RemoteData =
    let isLoading = function
        | Loading -> true
        | _ -> false

    let isSuccess = function
        | Success _ -> true
        | _ -> false

    let toOption = function
        | Success x -> Some x
        | _ -> None

    let defaultValue def = function
        | Success x -> x
        | _ -> def
