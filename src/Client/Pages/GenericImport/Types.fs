module Pages.GenericImport.Types

open Common.Types
open Shared.Domain

/// Import step in the wizard
type ImportStep =
    | SelectFile
    | MatchingPreview
    | ResolveAmbiguous
    | Importing
    | Complete

/// Model for the Generic Import page
type Model = {
    /// Current step in the import wizard
    CurrentStep: ImportStep
    /// Name of the selected JSON file
    SelectedFileName: string option
    /// Content read from the file
    FileContent: string option
    /// Parsed import items
    ParsedItems: RemoteData<GenericImportItem list>
    /// Preview with TMDB matches
    Preview: RemoteData<GenericImportPreview>
    /// Items being edited (for resolving ambiguous matches)
    EditingItems: GenericImportItemWithMatch list
    /// Current item being resolved (index)
    ResolvingIndex: int option
    /// Import progress
    Progress: GenericImportProgress
    /// Is polling for progress
    IsPollingProgress: bool
    /// Final result
    Result: GenericImportResult option
    /// Any error message
    Error: string option
}

type Msg =
    // File selection step
    | FileSelected of fileName: string * content: string
    | ParseFile
    | FileParsed of Result<GenericImportItem list, string>
    | ClearFile
    | ProceedToMatching

    // Matching preview step
    | LoadPreview
    | PreviewLoaded of Result<GenericImportPreview, string>
    | BackToFile

    // Resolve ambiguous step
    | StartResolving
    | ConfirmMatch of index: int * TmdbSearchResult
    | MatchConfirmationReceived of Result<GenericImportItemWithMatch, string>
    | SkipItem of int
    | NextAmbiguous
    | ProceedToImport

    // Import step
    | StartImport
    | ImportStarted of Result<unit, string>
    | PollProgress
    | ProgressReceived of GenericImportProgress
    | CancelImport

    // Navigation
    | GoToStep of ImportStep

type ExternalMsg =
    | NoOp
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let emptyProgress: GenericImportProgress = {
        InProgress = false
        CurrentItem = None
        CurrentIndex = 0
        TotalItems = 0
        CompletedSuccessfully = 0
        Skipped = 0
        Errors = []
    }

    let empty = {
        CurrentStep = SelectFile
        SelectedFileName = None
        FileContent = None
        ParsedItems = NotAsked
        Preview = NotAsked
        EditingItems = []
        ResolvingIndex = None
        Progress = emptyProgress
        IsPollingProgress = false
        Result = None
        Error = None
    }
