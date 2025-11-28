module Pages.Tags.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type TagsApi = unit -> Async<Tag list>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadTags

let update (api: TagsApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadTags ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> TagsLoaded)
                (fun ex -> Error ex.Message |> TagsLoaded)
        { model with Tags = Loading }, cmd, NoOp

    | TagsLoaded (Ok tags) ->
        { model with Tags = Success tags }, Cmd.none, NoOp

    | TagsLoaded (Error _) ->
        { model with Tags = Success [] }, Cmd.none, NoOp

    | ViewTagDetail tagId ->
        model, Cmd.none, NavigateToTagDetail tagId

    | OpenAddTagModal ->
        model, Cmd.none, RequestOpenAddModal

    | OpenEditTagModal tag ->
        model, Cmd.none, RequestOpenEditModal tag

    | OpenDeleteTagModal tag ->
        model, Cmd.none, RequestOpenDeleteModal tag
