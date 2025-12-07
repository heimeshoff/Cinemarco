module Pages.CollectionDetail.Types

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
}

type Msg =
    | LoadCollection
    | CollectionLoaded of Result<CollectionWithItems, string>
    | LoadProgress
    | ProgressLoaded of Result<CollectionProgress, string>
    | GoBack
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | ViewSeasonDetail of SeriesId * seasonNumber: int
    | ViewEpisodeDetail of SeriesId * seasonNumber: int * episodeNumber: int
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

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId
    | NavigateToSeriesBySeriesId of SeriesId
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
    }
