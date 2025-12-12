module Pages.CollectionDetail.Types

open System
open Common.Types
open Shared.Domain

type DropPosition =
    | Before of CollectionItemRef
    | After of CollectionItemRef

type Model = {
    CollectionId: CollectionId
    Collection: RemoteData<CollectionWithItems>
    Progress: RemoteData<CollectionProgress>
    DraggingItem: CollectionItemRef option
    DropTarget: DropPosition option
    // Inline note editing
    EditingNote: bool
    NoteText: string
    SavingNote: bool
    // Inline name editing
    EditingName: bool
    NameText: string
    SavingName: bool
    // Inline logo editing
    UploadingLogo: bool
}

type Msg =
    | LoadCollection
    | CollectionLoaded of Result<CollectionWithItems, string>
    | LoadProgress
    | ProgressLoaded of Result<CollectionProgress, string>
    | GoBack
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | ViewSeasonDetail of seriesName: string
    | ViewEpisodeDetail of seriesName: string
    | RemoveItem of CollectionItemRef
    | ItemRemoved of Result<CollectionWithItems, string>
    // Drag and drop
    | StartDrag of CollectionItemRef
    | DragOver of DropPosition
    | DragEnd
    | Drop
    | ReorderCompleted of Result<CollectionWithItems, string>
    // Inline note editing
    | StartEditNote
    | NoteChanged of string
    | SaveNote
    | CancelEditNote
    | NoteSaved of Result<Collection, string>
    // Inline name editing
    | StartEditName
    | NameChanged of string
    | SaveName
    | CancelEditName
    | NameSaved of Result<Collection, string>
    // Inline logo editing
    | LogoSelected of string  // Base64 encoded image
    | LogoSaved of Result<Collection, string>
    // Graph navigation
    | ViewInGraph

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | NavigateToSeriesByName of seriesName: string * firstAirDate: DateTime option
    | NavigateToGraphWithFocus of CollectionId
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let init (collectionId: CollectionId) = {
        CollectionId = collectionId
        Collection = NotAsked
        Progress = NotAsked
        DraggingItem = None
        DropTarget = None
        EditingNote = false
        NoteText = ""
        SavingNote = false
        EditingName = false
        NameText = ""
        SavingName = false
        UploadingLogo = false
    }
