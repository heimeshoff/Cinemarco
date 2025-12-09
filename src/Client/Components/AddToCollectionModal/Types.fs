module Components.AddToCollectionModal.Types

open Common.Types
open Shared.Domain

type Model = {
    ItemRef: CollectionItemRef
    ItemTitle: string
    Collections: RemoteData<Collection list>
    InitialCollectionIds: Set<CollectionId>  // Collections the item was already in
    SelectedCollectionIds: Set<CollectionId>  // Currently selected collections
    SearchText: string
    NewCollectionName: string  // For creating a new collection on the fly
    IsCreatingCollection: bool
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | LoadCollections
    | CollectionsLoaded of Result<Collection list * Collection list, string>  // (all collections, item's current collections)
    | ToggleCollection of CollectionId
    | SearchTextChanged of string
    | NewCollectionNameChanged of string
    | CreateAndAddCollection
    | CollectionCreated of Result<Collection, string>
    | Submit
    | SubmitResult of Result<unit, string>
    | Close

type ExternalMsg =
    | NoOp
    | CloseRequested
    | CollectionsUpdated  // Notify parent that collections were modified

module Model =
    let create (itemRef: CollectionItemRef) (title: string) : Model = {
        ItemRef = itemRef
        ItemTitle = title
        Collections = NotAsked
        InitialCollectionIds = Set.empty
        SelectedCollectionIds = Set.empty
        SearchText = ""
        NewCollectionName = ""
        IsCreatingCollection = false
        IsSubmitting = false
        Error = None
    }
