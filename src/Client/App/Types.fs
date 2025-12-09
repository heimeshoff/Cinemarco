module App.Types

open Common.Types
open Common.Routing
open Shared.Domain

/// Active modal state
type ActiveModal =
    | NoModal
    | SearchModal of Components.SearchModal.Types.Model
    | FriendModal of Components.FriendModal.Types.Model
    | AbandonModal of Components.AbandonModal.Types.Model
    | ConfirmDeleteModal of Components.ConfirmModal.Types.Model
    | WatchSessionModal of Components.WatchSessionModal.Types.Model
    | MovieWatchSessionModal of Components.MovieWatchSessionModal.Types.Model
    | CollectionModal of Components.CollectionModal.Types.Model
    | AddToCollectionModal of Components.AddToCollectionModal.Types.Model
    | ProfileImageModal of Components.ProfileImageEditor.Types.Model * FriendId

/// Main application model
type Model = {
    // Routing
    CurrentPage: Page

    // Global data (shared across pages)
    Friends: RemoteData<Friend list>

    // Layout state
    Layout: Components.Layout.Types.Model

    // Active modal
    Modal: ActiveModal

    // Notification
    Notification: Components.Notification.Types.Model

    // Page models (lazy loaded)
    HomePage: Pages.Home.Types.Model option
    LibraryPage: Pages.Library.Types.Model option
    MovieDetailPage: Pages.MovieDetail.Types.Model option
    SeriesDetailPage: Pages.SeriesDetail.Types.Model option
    SessionDetailPage: Pages.SessionDetail.Types.Model option
    FriendsPage: Pages.Friends.Types.Model option
    FriendDetailPage: Pages.FriendDetail.Types.Model option
    ContributorsPage: Pages.Contributors.Types.Model option
    ContributorDetailPage: Pages.ContributorDetail.Types.Model option
    CollectionsPage: Pages.Collections.Types.Model option
    CollectionDetailPage: Pages.CollectionDetail.Types.Model option
    CachePage: Pages.Cache.Types.Model option
}

/// Main application messages
type Msg =
    // Navigation
    | NavigateTo of Page
    | UrlChanged of Page  // Triggered by browser back/forward

    // Global data
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>

    // Layout
    | LayoutMsg of Components.Layout.Types.Msg

    // Modal management
    | OpenSearchModal
    | CloseModal
    | SearchModalMsg of Components.SearchModal.Types.Msg
    | AddTmdbItemDirectly of TmdbSearchResult
    | TmdbItemAddResult of Result<LibraryEntry, string> * MediaType
    | OpenFriendModal of Friend option
    | FriendModalMsg of Components.FriendModal.Types.Msg
    | OpenAbandonModal of EntryId
    | AbandonModalMsg of Components.AbandonModal.Types.Msg
    | OpenConfirmDeleteModal of Components.ConfirmModal.Types.DeleteTarget
    | ConfirmModalMsg of Components.ConfirmModal.Types.Msg
    | OpenWatchSessionModal of EntryId
    | WatchSessionModalMsg of Components.WatchSessionModal.Types.Msg
    | OpenMovieWatchSessionModal of EntryId
    | EditMovieWatchSessionModal of MovieWatchSession
    | MovieWatchSessionModalMsg of Components.MovieWatchSessionModal.Types.Msg
    | OpenCollectionModal of Collection option
    | CollectionModalMsg of Components.CollectionModal.Types.Msg
    | OpenAddToCollectionModal of CollectionItemRef * title: string
    | AddToCollectionModalMsg of Components.AddToCollectionModal.Types.Msg
    | OpenProfileImageModal of Friend
    | ProfileImageModalMsg of Components.ProfileImageEditor.Types.Msg
    | ProfileImageConfirmed of FriendId * base64Image: string

    // Notification
    | ShowNotification of message: string * isSuccess: bool
    | NotificationMsg of Components.Notification.Types.Msg

    // Page messages
    | HomeMsg of Pages.Home.Types.Msg
    | LibraryMsg of Pages.Library.Types.Msg
    | MovieDetailMsg of Pages.MovieDetail.Types.Msg
    | SeriesDetailMsg of Pages.SeriesDetail.Types.Msg
    | SessionDetailMsg of Pages.SessionDetail.Types.Msg
    | FriendsMsg of Pages.Friends.Types.Msg
    | FriendDetailMsg of Pages.FriendDetail.Types.Msg
    | ContributorsMsg of Pages.Contributors.Types.Msg
    | ContributorDetailMsg of Pages.ContributorDetail.Types.Msg
    | CollectionsMsg of Pages.Collections.Types.Msg
    | CollectionDetailMsg of Pages.CollectionDetail.Types.Msg
    | CacheMsg of Pages.Cache.Types.Msg

    // API result handlers
    | FriendSaved of Friend
    | FriendDeleted of FriendId
    | EntryAbandoned of LibraryEntry
    | EntryDeleted of EntryId
    | EntryAdded of LibraryEntry
    | SessionCreated of WatchSession
    | SessionDeleted of SessionId
    | CollectionSaved of Collection
    | CollectionDeleted of CollectionId
    | AddedToCollection of Collection

    // Slug-based entity loading (for URL navigation)
    | LoadEntryBySlug of slug: string * isMovie: bool
    | EntryBySlugLoaded of Result<LibraryEntry, string> * isMovie: bool
    | LoadFriendBySlug of slug: string
    | FriendBySlugLoaded of Result<Friend, string>
    | LoadSessionBySlug of slug: string
    | SessionBySlugLoaded of Result<WatchSessionWithProgress, string>
    | LoadCollectionBySlug of slug: string
    | CollectionBySlugLoaded of Result<CollectionWithItems, string>
    | LoadContributorBySlug of slug: string
    | ContributorBySlugLoaded of Result<TrackedContributor, string>

module Model =
    let empty = {
        CurrentPage = HomePage
        Friends = NotAsked
        Layout = Components.Layout.Types.Model.empty
        Modal = NoModal
        Notification = Components.Notification.Types.Model.empty
        HomePage = None
        LibraryPage = None
        MovieDetailPage = None
        SeriesDetailPage = None
        SessionDetailPage = None
        FriendsPage = None
        FriendDetailPage = None
        ContributorsPage = None
        ContributorDetailPage = None
        CollectionsPage = None
        CollectionDetailPage = None
        CachePage = None
    }
