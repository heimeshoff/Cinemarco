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
    /// Parsed import result (items + collections)
    ParsedResult: RemoteData<GenericImportParseResult>
    /// Preview with TMDB matches
    Preview: RemoteData<GenericImportPreview>
    /// Items being edited (for resolving ambiguous matches)
    EditingItems: GenericImportItemWithMatch list
    /// Collection suggestions with user selection state
    CollectionSuggestions: GenericImportCollectionSuggestion list
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
    /// Search query for manual TMDB search
    SearchQuery: string
    /// Search results from manual TMDB search
    SearchResults: RemoteData<TmdbSearchResult list>
}

type Msg =
    // File selection step
    | FileSelected of fileName: string * content: string
    | ParseFile
    | FileParsed of Result<GenericImportParseResult, string>
    | ClearFile
    | ProceedToMatching

    // Matching preview step
    | LoadPreview
    | PreviewLoaded of Result<GenericImportPreview, string>
    | BackToFile

    // Collection suggestions
    | ToggleCollectionSelection of collectionIndex: int
    | SelectAllCollections
    | DeselectAllCollections

    // Resolve ambiguous step
    | StartResolving
    | ConfirmMatch of index: int * TmdbSearchResult
    | MatchConfirmationReceived of Result<GenericImportItemWithMatch, string>
    | SkipItem of int
    | NextAmbiguous
    | ProceedToImport

    // Manual search
    | SetSearchQuery of string
    | SearchTmdb
    | SearchResultsReceived of Result<TmdbSearchResult list, string>
    | ClearSearch

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
        ImportedItems = []
        SkippedItems = []
    }

    let empty = {
        CurrentStep = SelectFile
        SelectedFileName = None
        FileContent = None
        ParsedResult = NotAsked
        Preview = NotAsked
        EditingItems = []
        CollectionSuggestions = []
        ResolvingIndex = None
        Progress = emptyProgress
        IsPollingProgress = false
        Result = None
        Error = None
        SearchQuery = ""
        SearchResults = NotAsked
    }
