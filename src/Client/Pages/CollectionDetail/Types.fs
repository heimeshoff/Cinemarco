module Pages.CollectionDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    CollectionId: CollectionId
    Collection: RemoteData<CollectionWithItems>
    Progress: RemoteData<CollectionProgress>
    DraggingItem: EntryId option
    DragOverItem: EntryId option
}

type Msg =
    | LoadCollection
    | CollectionLoaded of Result<CollectionWithItems, string>
    | LoadProgress
    | ProgressLoaded of Result<CollectionProgress, string>
    | GoBack
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | RemoveItem of EntryId
    | ItemRemoved of Result<CollectionWithItems, string>
    // Drag and drop
    | StartDrag of EntryId
    | DragOver of EntryId
    | DragEnd
    | Drop of EntryId
    | ReorderCompleted of Result<CollectionWithItems, string>

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let init (collectionId: CollectionId) = {
        CollectionId = collectionId
        Collection = NotAsked
        Progress = NotAsked
        DraggingItem = None
        DragOverItem = None
    }
