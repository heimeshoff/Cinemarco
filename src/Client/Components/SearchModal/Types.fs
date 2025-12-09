module Components.SearchModal.Types

open Common.Types
open Shared.Domain

type Model = {
    Query: string
    Results: RemoteData<TmdbSearchResult list>
    LibraryEntries: LibraryEntry list
}

type Msg =
    | QueryChanged of string
    | SearchDebounced of string  // Carries the query that triggered it
    | SearchResults of Result<TmdbSearchResult list, string>
    | SelectTmdbItem of TmdbSearchResult
    | SelectLibraryItem of EntryId * MediaType * title: string
    | Close

type ExternalMsg =
    | NoOp
    | TmdbItemSelected of TmdbSearchResult
    | LibraryItemSelected of EntryId * MediaType * title: string
    | CloseRequested

module Model =
    let empty = {
        Query = ""
        Results = NotAsked
        LibraryEntries = []
    }
