module Pages.CollectionDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type CollectionApi = {
    GetCollection: CollectionId -> Async<Result<CollectionWithItems, string>>
    GetProgress: CollectionId -> Async<Result<CollectionProgress, string>>
    RemoveItem: CollectionId * EntryId -> Async<Result<CollectionWithItems, string>>
    ReorderItems: CollectionId * EntryId list -> Async<Result<CollectionWithItems, string>>
}

let init (collectionId: CollectionId) : Model * Cmd<Msg> =
    Model.init collectionId, Cmd.batch [ Cmd.ofMsg LoadCollection; Cmd.ofMsg LoadProgress ]

let update (api: CollectionApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCollection ->
        let cmd =
            Cmd.OfAsync.either
                api.GetCollection
                model.CollectionId
                CollectionLoaded
                (fun ex -> Error ex.Message |> CollectionLoaded)
        { model with Collection = Loading }, cmd, NoOp

    | CollectionLoaded (Ok cwi) ->
        { model with Collection = Success cwi }, Cmd.none, NoOp

    | CollectionLoaded (Error err) ->
        { model with Collection = Failure err }, Cmd.none, ShowNotification (err, false)

    | LoadProgress ->
        let cmd =
            Cmd.OfAsync.either
                api.GetProgress
                model.CollectionId
                ProgressLoaded
                (fun ex -> Error ex.Message |> ProgressLoaded)
        { model with Progress = Loading }, cmd, NoOp

    | ProgressLoaded (Ok progress) ->
        { model with Progress = Success progress }, Cmd.none, NoOp

    | ProgressLoaded (Error _) ->
        model, Cmd.none, NoOp

    | GoBack ->
        model, Cmd.none, NavigateBack

    | ViewMovieDetail entryId ->
        model, Cmd.none, NavigateToMovieDetail entryId

    | ViewSeriesDetail entryId ->
        model, Cmd.none, NavigateToSeriesDetail entryId

    | RemoveItem entryId ->
        let cmd =
            Cmd.OfAsync.either
                api.RemoveItem
                (model.CollectionId, entryId)
                ItemRemoved
                (fun ex -> Error ex.Message |> ItemRemoved)
        model, cmd, NoOp

    | ItemRemoved (Ok cwi) ->
        { model with Collection = Success cwi },
        Cmd.ofMsg LoadProgress,
        ShowNotification ("Item removed from collection", true)

    | ItemRemoved (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    // Drag and drop handling
    | StartDrag entryId ->
        { model with DraggingItem = Some entryId }, Cmd.none, NoOp

    | DragOver entryId ->
        { model with DragOverItem = Some entryId }, Cmd.none, NoOp

    | DragEnd ->
        { model with DraggingItem = None; DragOverItem = None }, Cmd.none, NoOp

    | Drop targetEntryId ->
        match model.DraggingItem, model.Collection with
        | Some draggedId, Success cwi when draggedId <> targetEntryId ->
            // Calculate new order
            let currentOrder = cwi.Items |> List.map (fun (item, _) -> item.EntryId)
            let draggedIndex = currentOrder |> List.findIndex ((=) draggedId)
            let targetIndex = currentOrder |> List.findIndex ((=) targetEntryId)

            // Remove dragged item and insert at target position
            let withoutDragged = currentOrder |> List.filter ((<>) draggedId)
            let newOrder =
                withoutDragged
                |> List.mapi (fun i id ->
                    if i = targetIndex then [draggedId; id]
                    elif i = targetIndex - 1 && targetIndex > draggedIndex then [id; draggedId]
                    else [id])
                |> List.concat
                |> List.distinct

            let cmd =
                Cmd.OfAsync.either
                    api.ReorderItems
                    (model.CollectionId, newOrder)
                    ReorderCompleted
                    (fun ex -> Error ex.Message |> ReorderCompleted)

            { model with DraggingItem = None; DragOverItem = None }, cmd, NoOp
        | _ ->
            { model with DraggingItem = None; DragOverItem = None }, Cmd.none, NoOp

    | ReorderCompleted (Ok cwi) ->
        { model with Collection = Success cwi }, Cmd.none, NoOp

    | ReorderCompleted (Error err) ->
        model, Cmd.ofMsg LoadCollection, ShowNotification (err, false)
