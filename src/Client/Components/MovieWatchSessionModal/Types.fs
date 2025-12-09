module Components.MovieWatchSessionModal.Types

open System
open Shared.Domain

/// Mode for the modal: creating a new session or editing an existing one
type ModalMode =
    | Create of EntryId
    | Edit of MovieWatchSession

type Model = {
    Mode: ModalMode
    WatchedDate: DateTime
    SelectedFriends: FriendId list
    SessionName: string
    IsSubmitting: bool
    IsAddingFriend: bool
    Error: string option
}

type Msg =
    | SetWatchedDate of DateTime
    | SetSessionName of string
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | Submit
    | SubmitResult of Result<MovieWatchSession, string>
    | Close

type ExternalMsg =
    | NoOp
    | Created of MovieWatchSession
    | Updated of MovieWatchSession
    | FriendCreatedInline of Friend
    | CloseRequested

module Model =
    /// Create model for adding a new session
    let create entryId = {
        Mode = Create entryId
        WatchedDate = DateTime.UtcNow.Date
        SelectedFriends = []
        SessionName = ""
        IsSubmitting = false
        IsAddingFriend = false
        Error = None
    }

    /// Create model for editing an existing session
    let edit (session: MovieWatchSession) = {
        Mode = Edit session
        WatchedDate = session.WatchedDate
        SelectedFriends = session.Friends
        SessionName = session.Name |> Option.defaultValue ""
        IsSubmitting = false
        IsAddingFriend = false
        Error = None
    }
