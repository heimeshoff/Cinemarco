module Pages.Collections.Types

open Common.Types
open Shared.Domain

type Model = {
    Collections: RemoteData<Collection list>
    SearchQuery: string
}

type Msg =
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | SetSearchQuery of string
    | ViewCollectionDetail of collectionId: CollectionId * name: string
    | CreateNewCollection
    | CollectionCreated of Result<Collection, string>
    | OpenDeleteCollectionModal of Collection

type ExternalMsg =
    | NoOp
    | NavigateToCollectionDetail of slug: string
    | RequestOpenDeleteModal of Collection
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let empty = { Collections = NotAsked; SearchQuery = "" }
