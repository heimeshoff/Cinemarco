module Components.SearchModal.Types

open Common.Types
open Shared.Domain

type Model = {
    Query: string
    Results: RemoteData<TmdbSearchResult list>
}

type Msg =
    | QueryChanged of string
    | SearchDebounced
    | SearchResults of Result<TmdbSearchResult list, string>
    | SelectItem of TmdbSearchResult
    | Close

type ExternalMsg =
    | NoOp
    | ItemSelected of TmdbSearchResult
    | CloseRequested

module Model =
    let empty = {
        Query = ""
        Results = NotAsked
    }
