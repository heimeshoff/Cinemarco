module Pages.CollectionDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type CollectionApi = {
    GetCollection: CollectionId -> Async<Result<CollectionWithItems, string>>
    GetProgress: CollectionId -> Async<Result<CollectionProgress, string>>
    RemoveItem: CollectionId * CollectionItemRef -> Async<Result<CollectionWithItems, string>>
    ReorderItems: CollectionId * CollectionItemRef list -> Async<Result<CollectionWithItems, string>>
    UpdateCollection: UpdateCollectionRequest -> Async<Result<Collection, string>>
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

    | ViewSeasonDetail (seriesId, _) ->
        model, Cmd.none, NavigateToSeriesBySeriesId seriesId

    | ViewEpisodeDetail (seriesId, _, _) ->
        model, Cmd.none, NavigateToSeriesBySeriesId seriesId

    | RemoveItem itemRef ->
        let cmd =
            Cmd.OfAsync.either
                api.RemoveItem
                (model.CollectionId, itemRef)
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
    | StartDrag itemRef ->
        { model with DraggingItem = Some itemRef }, Cmd.none, NoOp

    | DragOver dropPosition ->
        { model with DropTarget = Some dropPosition }, Cmd.none, NoOp

    | DragEnd ->
        { model with DraggingItem = None; DropTarget = None }, Cmd.none, NoOp

    | Drop ->
        match model.DraggingItem, model.DropTarget, model.Collection with
        | Some draggedRef, Some dropPosition, Success cwi ->
            // Get target ref and whether we're inserting before or after
            let targetRef, insertBefore =
                match dropPosition with
                | Before ref -> ref, true
                | After ref -> ref, false

            // Don't reorder if dropping on self
            if draggedRef = targetRef then
                { model with DraggingItem = None; DropTarget = None }, Cmd.none, NoOp
            else
                // Build new item order by removing dragged and inserting at correct position
                let reorderedItems =
                    cwi.Items
                    |> List.collect (fun (item, display) ->
                        if item.ItemRef = draggedRef then
                            [] // Remove from current position
                        elif item.ItemRef = targetRef then
                            let draggedItem = cwi.Items |> List.find (fun (i, _) -> i.ItemRef = draggedRef)
                            if insertBefore then [draggedItem; (item, display)]
                            else [(item, display); draggedItem]
                        else
                            [(item, display)])

                // Get new order of refs for API call
                let newOrder = reorderedItems |> List.map (fun (item, _) -> item.ItemRef)

                // Optimistic update: immediately show reordered items
                let updatedCwi = { cwi with Items = reorderedItems }

                let cmd =
                    Cmd.OfAsync.either
                        api.ReorderItems
                        (model.CollectionId, newOrder)
                        ReorderCompleted
                        (fun ex -> Error ex.Message |> ReorderCompleted)

                { model with
                    DraggingItem = None
                    DropTarget = None
                    Collection = Success updatedCwi }, cmd, NoOp
        | _ ->
            { model with DraggingItem = None; DropTarget = None }, Cmd.none, NoOp

    | ReorderCompleted (Ok cwi) ->
        { model with Collection = Success cwi }, Cmd.none, NoOp

    | ReorderCompleted (Error err) ->
        model, Cmd.ofMsg LoadCollection, ShowNotification (err, false)

    // Inline note editing
    | StartEditNote ->
        let currentNote =
            match model.Collection with
            | Success cwi -> cwi.Collection.Description |> Option.defaultValue ""
            | _ -> ""
        { model with EditingNote = true; NoteText = currentNote }, Cmd.none, NoOp

    | NoteChanged text ->
        { model with NoteText = text }, Cmd.none, NoOp

    | CancelEditNote ->
        { model with EditingNote = false; NoteText = "" }, Cmd.none, NoOp

    | SaveNote ->
        let request : UpdateCollectionRequest = {
            Id = model.CollectionId
            Name = None
            Description = Some (if System.String.IsNullOrWhiteSpace model.NoteText then "" else model.NoteText.Trim())
            LogoBase64 = None
        }
        let cmd =
            Cmd.OfAsync.either
                api.UpdateCollection
                request
                NoteSaved
                (fun ex -> Error ex.Message |> NoteSaved)
        { model with SavingNote = true }, cmd, NoOp

    | NoteSaved (Ok collection) ->
        // Update the collection in the model with new description
        let updatedCollection =
            match model.Collection with
            | Success cwi -> Success { cwi with Collection = collection }
            | other -> other
        { model with
            Collection = updatedCollection
            EditingNote = false
            NoteText = ""
            SavingNote = false }, Cmd.none, NoOp

    | NoteSaved (Error err) ->
        { model with SavingNote = false }, Cmd.none, ShowNotification (err, false)
