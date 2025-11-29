module Components.AddToCollectionModal.Types

open Common.Types
open Shared.Domain

type Model = {
    EntryId: EntryId
    EntryTitle: string
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
    let create (entryId: EntryId) (title: string) : Model = {
        EntryId = entryId
        EntryTitle = title
        Collections = NotAsked
        SelectedCollectionId = None
        Notes = ""
        IsSubmitting = false
        Error = None
    }
