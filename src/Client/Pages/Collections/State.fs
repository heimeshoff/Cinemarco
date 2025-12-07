module Pages.Collections.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type CollectionsApi = unit -> Async<Collection list>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadCollections

let update (api: CollectionsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCollections ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> CollectionsLoaded)
                (fun ex -> Error ex.Message |> CollectionsLoaded)
        { model with Collections = Loading }, cmd, NoOp

    | CollectionsLoaded (Ok collections) ->
        { model with Collections = Success collections }, Cmd.none, NoOp

    | CollectionsLoaded (Error _) ->
        { model with Collections = Success [] }, Cmd.none, NoOp

    | SetSearchQuery query ->
        { model with SearchQuery = query }, Cmd.none, NoOp

    | ViewCollectionDetail collectionId ->
        model, Cmd.none, NavigateToCollectionDetail collectionId

    | OpenAddCollectionModal ->
        model, Cmd.none, RequestOpenAddModal

    | OpenEditCollectionModal collection ->
        model, Cmd.none, RequestOpenEditModal collection

    | OpenDeleteCollectionModal collection ->
        model, Cmd.none, RequestOpenDeleteModal collection
