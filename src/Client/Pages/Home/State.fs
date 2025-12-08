module Pages.Home.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type LibraryApi = unit -> Async<LibraryEntry list>

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadLibrary

let update (api: LibraryApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadLibrary ->
        let cmd =
            Cmd.OfAsync.either
                api
                ()
                (Ok >> LibraryLoaded)
                (fun ex -> Error ex.Message |> LibraryLoaded)
        { model with Library = Loading }, cmd, NoOp

    | LibraryLoaded (Ok entries) ->
        { model with Library = Success entries }, Cmd.none, NoOp

    | LibraryLoaded (Error err) ->
        { model with Library = Failure err }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title)

    | ViewSeriesDetail (entryId, name) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name)

    | ViewLibrary ->
        model, Cmd.none, NavigateToLibrary
