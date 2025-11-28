module App.Types

open Common.Types
open Common.Routing
open Shared.Domain

/// Active modal state
type ActiveModal =
    | NoModal
    | SearchModal of Components.SearchModal.Types.Model
    | QuickAddModal of Components.QuickAddModal.Types.Model
    | FriendModal of Components.FriendModal.Types.Model
    | TagModal of Components.TagModal.Types.Model
    | AbandonModal of Components.AbandonModal.Types.Model
    | ConfirmDeleteModal of Components.ConfirmModal.Types.Model

/// Main application model
type Model = {
    // Routing
    CurrentPage: Page

    // Global data (shared across pages)
    Friends: RemoteData<Friend list>
    Tags: RemoteData<Tag list>

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
    FriendsPage: Pages.Friends.Types.Model option
    FriendDetailPage: Pages.FriendDetail.Types.Model option
    TagsPage: Pages.Tags.Types.Model option
    TagDetailPage: Pages.TagDetail.Types.Model option
}

/// Main application messages
type Msg =
    // Navigation
    | NavigateTo of Page

    // Global data
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>
    | LoadTags
    | TagsLoaded of Result<Tag list, string>

    // Layout
    | LayoutMsg of Components.Layout.Types.Msg

    // Modal management
    | OpenSearchModal
    | CloseModal
    | SearchModalMsg of Components.SearchModal.Types.Msg
    | OpenQuickAddModal of TmdbSearchResult
    | QuickAddModalMsg of Components.QuickAddModal.Types.Msg
    | OpenFriendModal of Friend option
    | FriendModalMsg of Components.FriendModal.Types.Msg
    | OpenTagModal of Tag option
    | TagModalMsg of Components.TagModal.Types.Msg
    | OpenAbandonModal of EntryId
    | AbandonModalMsg of Components.AbandonModal.Types.Msg
    | OpenConfirmDeleteModal of Components.ConfirmModal.Types.DeleteTarget
    | ConfirmModalMsg of Components.ConfirmModal.Types.Msg

    // Notification
    | ShowNotification of message: string * isSuccess: bool
    | NotificationMsg of Components.Notification.Types.Msg

    // Page messages
    | HomeMsg of Pages.Home.Types.Msg
    | LibraryMsg of Pages.Library.Types.Msg
    | MovieDetailMsg of Pages.MovieDetail.Types.Msg
    | SeriesDetailMsg of Pages.SeriesDetail.Types.Msg
    | FriendsMsg of Pages.Friends.Types.Msg
    | FriendDetailMsg of Pages.FriendDetail.Types.Msg
    | TagsMsg of Pages.Tags.Types.Msg
    | TagDetailMsg of Pages.TagDetail.Types.Msg

    // API result handlers
    | FriendSaved of Friend
    | FriendDeleted of FriendId
    | TagSaved of Tag
    | TagDeleted of TagId
    | EntryAbandoned of LibraryEntry
    | EntryDeleted of EntryId
    | EntryAdded of LibraryEntry

module Model =
    let empty = {
        CurrentPage = HomePage
        Friends = NotAsked
        Tags = NotAsked
        Layout = Components.Layout.Types.Model.empty
        Modal = NoModal
        Notification = Components.Notification.Types.Model.empty
        HomePage = None
        LibraryPage = None
        MovieDetailPage = None
        SeriesDetailPage = None
        FriendsPage = None
        FriendDetailPage = None
        TagsPage = None
        TagDetailPage = None
    }
