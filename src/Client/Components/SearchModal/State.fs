module Components.SearchModal.State

open System
open Elmish
open Common.Types
open Shared.Domain
open Types

/// Debounce delay for TMDB search (milliseconds)
let private searchDebounceDelay = 300

/// Filter library entries by title (case-insensitive)
let filterLibraryEntries (query: string) (entries: LibraryEntry list) =
    if String.IsNullOrWhiteSpace query then []
    else
        entries
        |> List.filter (fun entry ->
            let title =
                match entry.Media with
                | LibraryMovie m -> m.Title
                | LibrarySeries s -> s.Name
            title.ToLowerInvariant().Contains(query.ToLowerInvariant()))
        |> List.truncate 15

let init (libraryEntries: LibraryEntry list) : Model =
    { Model.empty with LibraryEntries = libraryEntries }

let update (api: unit -> string -> Async<TmdbSearchResult list>) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | QueryChanged query ->
        let newModel = { model with Query = query }
        let cmd =
            if query.Length >= 2 then
                Cmd.OfAsync.perform
                    (fun q -> async {
                        do! Async.Sleep searchDebounceDelay
                        return q
                    })
                    query
                    SearchDebounced
            else
                Cmd.none
        newModel, cmd, NoOp

    | SearchDebounced debouncedQuery ->
        // Only search if the query hasn't changed since the timer started
        if debouncedQuery = model.Query && model.Query.Length >= 2 then
            let cmd =
                Cmd.OfAsync.either
                    (api ())
                    model.Query
                    (Ok >> SearchResults)
                    (fun ex -> Error ex.Message |> SearchResults)
            { model with Results = Loading }, cmd, NoOp
        else
            // Query changed, ignore this debounce
            model, Cmd.none, NoOp

    | SearchResults (Ok results) ->
        { model with Results = Success results }, Cmd.none, NoOp

    | SearchResults (Error err) ->
        { model with Results = Failure err }, Cmd.none, NoOp

    | SelectTmdbItem item ->
        model, Cmd.none, TmdbItemSelected item

    | SelectLibraryItem (entryId, mediaType, title) ->
        model, Cmd.none, LibraryItemSelected (entryId, mediaType, title)

    | Close ->
        model, Cmd.none, CloseRequested
