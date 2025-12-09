module Components.AddToCollectionModal.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type Api = {
    GetCollections: unit -> Async<Collection list>
    GetCollectionsForItem: CollectionItemRef -> Async<Collection list>
    AddToCollection: CollectionId * CollectionItemRef * string option -> Async<Result<CollectionWithItems, string>>
    RemoveFromCollection: CollectionId * CollectionItemRef -> Async<Result<CollectionWithItems, string>>
    CreateCollection: CreateCollectionRequest -> Async<Result<Collection, string>>
}

let init (itemRef: CollectionItemRef) (title: string) : Model * Cmd<Msg> =
    Model.create itemRef title, Cmd.ofMsg LoadCollections

let private loadCollectionsAsync (api: Api) (itemRef: CollectionItemRef) = async {
    let! allCollections = api.GetCollections()
    let! itemCollections = api.GetCollectionsForItem itemRef
    return (allCollections, itemCollections)
}

let update (api: Api) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCollections ->
        { model with Collections = Loading },
        Cmd.OfAsync.either
            (loadCollectionsAsync api)
            model.ItemRef
            (Ok >> CollectionsLoaded)
            (fun ex -> CollectionsLoaded (Error ex.Message)),
        NoOp

    | CollectionsLoaded (Ok (allCollections, itemCollections)) ->
        let initialIds = itemCollections |> List.map (fun c -> c.Id) |> Set.ofList
        { model with
            Collections = Success allCollections
            InitialCollectionIds = initialIds
            SelectedCollectionIds = initialIds }, Cmd.none, NoOp

    | CollectionsLoaded (Error err) ->
        { model with Collections = Failure err }, Cmd.none, NoOp

    | ToggleCollection collectionId ->
        let newSelection =
            if model.SelectedCollectionIds.Contains collectionId then
                model.SelectedCollectionIds.Remove collectionId
            else
                model.SelectedCollectionIds.Add collectionId
        { model with SelectedCollectionIds = newSelection; Error = None }, Cmd.none, NoOp

    | SearchTextChanged text ->
        { model with SearchText = text; NewCollectionName = text }, Cmd.none, NoOp

    | NewCollectionNameChanged name ->
        { model with NewCollectionName = name }, Cmd.none, NoOp

    | CreateAndAddCollection ->
        let name = model.NewCollectionName.Trim()
        if name.Length = 0 then
            { model with Error = Some "Collection name cannot be empty" }, Cmd.none, NoOp
        else
            let request: CreateCollectionRequest = {
                Name = name
                Description = None
                LogoBase64 = None
            }
            { model with IsCreatingCollection = true; Error = None },
            Cmd.OfAsync.either
                api.CreateCollection
                request
                CollectionCreated
                (fun ex -> CollectionCreated (Error ex.Message)),
            NoOp

    | CollectionCreated (Ok collection) ->
        // Add the new collection to the list and select it
        let updatedCollections =
            match model.Collections with
            | Success collections -> Success (collection :: collections |> List.sortBy (fun c -> c.Name.ToLowerInvariant()))
            | other -> other
        { model with
            Collections = updatedCollections
            SelectedCollectionIds = model.SelectedCollectionIds.Add collection.Id
            IsCreatingCollection = false
            SearchText = ""
            NewCollectionName = "" }, Cmd.none, NoOp

    | CollectionCreated (Error err) ->
        { model with IsCreatingCollection = false; Error = Some err }, Cmd.none, NoOp

    | Submit ->
        // Calculate what needs to be added and removed
        let toAdd = model.SelectedCollectionIds - model.InitialCollectionIds
        let toRemove = model.InitialCollectionIds - model.SelectedCollectionIds

        if Set.isEmpty toAdd && Set.isEmpty toRemove then
            // No changes
            model, Cmd.none, CloseRequested
        else
            // Execute all operations sequentially
            let allOperations = async {
                // Add to new collections
                for collId in toAdd do
                    let! _ = api.AddToCollection(collId, model.ItemRef, None)
                    ()
                // Remove from deselected collections
                for collId in toRemove do
                    let! _ = api.RemoveFromCollection(collId, model.ItemRef)
                    ()
                return ()
            }

            { model with IsSubmitting = true; Error = None },
            Cmd.OfAsync.either
                (fun () -> allOperations)
                ()
                (fun () -> SubmitResult (Ok ()))
                (fun ex -> SubmitResult (Error ex.Message)),
            NoOp

    | SubmitResult (Ok ()) ->
        { model with IsSubmitting = false }, Cmd.none, CollectionsUpdated

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
