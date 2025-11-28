module Pages.TagDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type EntriesApi = TagId -> Async<LibraryEntry list>

let init (tagId: TagId) : Model * Cmd<Msg> =
    Model.create tagId, Cmd.ofMsg LoadEntries

let update (api: EntriesApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntries ->
        let cmd =
            Cmd.OfAsync.either
                api
                model.TagId
                (Ok >> EntriesLoaded)
                (fun ex -> Error ex.Message |> EntriesLoaded)
        { model with Entries = Loading }, cmd, NoOp

    | EntriesLoaded (Ok entries) ->
        { model with Entries = Success entries }, Cmd.none, NoOp

    | EntriesLoaded (Error err) ->
        { model with Entries = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail entryId ->
        model, Cmd.none, NavigateToMovieDetail entryId

    | ViewSeriesDetail entryId ->
        model, Cmd.none, NavigateToSeriesDetail entryId

    | GoBack ->
        model, Cmd.none, NavigateBack
