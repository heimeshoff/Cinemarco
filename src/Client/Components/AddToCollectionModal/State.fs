module Components.AddToCollectionModal.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type Api = {
    GetCollections: unit -> Async<Collection list>
    AddToCollection: CollectionId * EntryId * string option -> Async<Result<CollectionWithItems, string>>
}

let init (entryId: EntryId) (title: string) : Model * Cmd<Msg> =
    Model.create entryId title, Cmd.ofMsg LoadCollections

let update (api: Api) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadCollections ->
        { model with Collections = Loading },
        Cmd.OfAsync.either
            api.GetCollections
            ()
            (Ok >> CollectionsLoaded)
            (fun ex -> CollectionsLoaded (Error ex.Message)),
        NoOp

    | CollectionsLoaded (Ok collections) ->
        { model with Collections = Success collections }, Cmd.none, NoOp

    | CollectionsLoaded (Error err) ->
        { model with Collections = Failure err }, Cmd.none, NoOp

    | SelectCollection collectionId ->
        { model with SelectedCollectionId = Some collectionId; Error = None }, Cmd.none, NoOp

    | NotesChanged notes ->
        { model with Notes = notes }, Cmd.none, NoOp

    | Submit ->
        match model.SelectedCollectionId with
        | None ->
            { model with Error = Some "Please select a collection" }, Cmd.none, NoOp
        | Some collectionId ->
            let notes = if model.Notes.Trim().Length > 0 then Some (model.Notes.Trim()) else None
            { model with IsSubmitting = true; Error = None },
            Cmd.OfAsync.either
                api.AddToCollection
                (collectionId, model.EntryId, notes)
                SubmitResult
                (fun ex -> SubmitResult (Error ex.Message)),
            NoOp

    | SubmitResult (Ok cwi) ->
        { model with IsSubmitting = false }, Cmd.none, AddedToCollection cwi.Collection

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
