module Pages.Collections.Types

open Common.Types
open Shared.Domain

type Model = {
    Collections: RemoteData<Collection list>
}

type Msg =
    | LoadCollections
    | CollectionsLoaded of Result<Collection list, string>
    | ViewCollectionDetail of CollectionId
    | OpenAddCollectionModal
    | OpenEditCollectionModal of Collection
    | OpenDeleteCollectionModal of Collection

type ExternalMsg =
    | NoOp
    | NavigateToCollectionDetail of CollectionId
    | RequestOpenAddModal
    | RequestOpenEditModal of Collection
    | RequestOpenDeleteModal of Collection

module Model =
    let empty = { Collections = NotAsked }
