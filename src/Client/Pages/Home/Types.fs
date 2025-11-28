module Pages.Home.Types

open Common.Types
open Shared.Domain

type Model = {
    Library: RemoteData<LibraryEntry list>
}

type Msg =
    | LoadLibrary
    | LibraryLoaded of Result<LibraryEntry list, string>
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | ViewLibrary

type ExternalMsg =
    | NoOp
    | NavigateToLibrary
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId

module Model =
    let empty = {
        Library = NotAsked
    }
