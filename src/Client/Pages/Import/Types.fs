module Pages.Import.Types

open Common.Types
open Shared.Domain

/// Connection status with Trakt
type TraktConnectionStatus =
    | NotChecked
    | Checking
    | Connected
    | NotConnected

/// Import step in the wizard
type ImportStep =
    | Connect
    | SelectOptions
    | Preview
    | Importing
    | Complete

/// Model for the Import page
type Model = {
    /// Current step in the import wizard
    CurrentStep: ImportStep
    /// Connection status with Trakt
    ConnectionStatus: TraktConnectionStatus
    /// Auth URL for OAuth
    AuthUrl: RemoteData<TraktAuthUrl>
    /// User-entered auth code (for manual entry flow)
    AuthCode: string
    /// Import options selected by user
    ImportOptions: TraktImportOptions
    /// Preview of what will be imported
    Preview: RemoteData<TraktImportPreview>
    /// Current import status
    ImportStatus: ImportStatus
    /// Is polling for import status
    IsPollingStatus: bool
    /// Any error message
    Error: string option
}

type Msg =
    // Connection
    | CheckConnection
    | ConnectionChecked of bool
    | GetAuthUrl
    | AuthUrlReceived of Result<TraktAuthUrl, string>
    | UpdateAuthCode of string
    | SubmitAuthCode
    | AuthCodeSubmitted of Result<unit, string>
    | Logout
    | LoggedOut

    // Options
    | ToggleImportMovies
    | ToggleImportSeries
    | ToggleImportRatings
    | ToggleImportWatchlist
    | ProceedToPreview

    // Preview
    | LoadPreview
    | PreviewLoaded of Result<TraktImportPreview, string>
    | BackToOptions

    // Import
    | StartImport
    | ImportStarted of Result<unit, string>
    | PollImportStatus
    | ImportStatusReceived of ImportStatus
    | CancelImport

    // Navigation
    | GoToStep of ImportStep

type ExternalMsg =
    | NoOp
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let defaultOptions: TraktImportOptions = {
        ImportWatchedMovies = true
        ImportWatchedSeries = true
        ImportRatings = true
        ImportWatchlist = false
    }

    let emptyStatus: ImportStatus = {
        InProgress = false
        CurrentItem = None
        Completed = 0
        Total = 0
        Errors = []
    }

    let empty = {
        CurrentStep = Connect
        ConnectionStatus = NotChecked
        AuthUrl = NotAsked
        AuthCode = ""
        ImportOptions = defaultOptions
        Preview = NotAsked
        ImportStatus = emptyStatus
        IsPollingStatus = false
        Error = None
    }
