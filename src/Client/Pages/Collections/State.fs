module Pages.Collections.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type CollectionsApi = {
    GetAll: unit -> Async<Collection list>
    Create: CreateCollectionRequest -> Async<Result<Collection, string>>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadCollections

let update (api: CollectionsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCollections ->
        let cmd =
            Cmd.OfAsync.either
                api.GetAll
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

    | ViewCollectionDetail (collectionId, name) ->
        // Calculate unique slug, handling duplicates
        let slug =
            match model.Collections with
            | Success collections ->
                let baseSlug = Slug.generate name
                // Find all collections with the same base slug, sorted by ID
                let duplicates =
                    collections
                    |> List.filter (fun c -> Slug.matches baseSlug (Slug.generate c.Name))
                    |> List.sortBy (fun c -> CollectionId.value c.Id)
                // Find the index of this collection among duplicates
                let index = duplicates |> List.tryFindIndex (fun c -> c.Id = collectionId) |> Option.defaultValue 0
                Slug.slugForIndex baseSlug index
            | _ -> Slug.generate name
        model, Cmd.none, NavigateToCollectionDetail slug

    | CreateNewCollection ->
        let request : CreateCollectionRequest = {
            Name = "New Collection"
            Description = None
            LogoBase64 = None
        }
        let cmd =
            Cmd.OfAsync.either
                api.Create
                request
                CollectionCreated
                (fun ex -> Error ex.Message |> CollectionCreated)
        model, cmd, NoOp

    | CollectionCreated (Ok collection) ->
        // Calculate unique slug for the newly created collection
        let slug =
            match model.Collections with
            | Success collections ->
                let baseSlug = Slug.generate collection.Name
                // Count existing collections with the same base slug
                let existingDuplicates =
                    collections
                    |> List.filter (fun c -> Slug.matches baseSlug (Slug.generate c.Name))
                    |> List.length
                // The new collection is the last one (highest index)
                Slug.slugForIndex baseSlug existingDuplicates
            | _ -> Slug.generate collection.Name
        model, Cmd.none, NavigateToCollectionDetail slug

    | CollectionCreated (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | OpenDeleteCollectionModal collection ->
        model, Cmd.none, RequestOpenDeleteModal collection
