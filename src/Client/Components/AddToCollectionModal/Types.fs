module Components.AddToCollectionModal.Types

open Common.Types
open Shared.Domain

type Model = {
    ItemRef: CollectionItemRef
    ItemTitle: string
    Collections: RemoteData<Collection list>
    SelectedCollectionId: CollectionId option
    Notes: string
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | SelectCollection of CollectionId
    | NotesChanged of string
    | Submit
    | SubmitResult of Result<CollectionWithItems, string>
    | Close

type ExternalMsg =
    | NoOp
    | CloseRequested
    | AddedToCollection of Collection

module Model =
    let create (itemRef: CollectionItemRef) (title: string) : Model = {
        ItemRef = itemRef
        ItemTitle = title
        Collections = NotAsked
        SelectedCollectionId = None
        Notes = ""
        IsSubmitting = false
        Error = None
    }
