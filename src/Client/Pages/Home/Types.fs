module Pages.Home.Types

open Common.Types
open Shared.Domain

type Model = {
    Library: RemoteData<LibraryEntry list>
}

type Msg =
    | LoadLibrary
    | LibraryLoaded of Result<LibraryEntry list, string>
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string
    | ViewLibrary
    | ViewYearInReview

type ExternalMsg =
    | NoOp
    | NavigateToLibrary
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string
    | NavigateToYearInReview

module Model =
    let empty = {
        Library = NotAsked
    }
