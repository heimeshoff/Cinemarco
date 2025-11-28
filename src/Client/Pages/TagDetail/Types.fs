module Pages.TagDetail.Types

open Common.Types
open Shared.Domain

type Model = {
    TagId: TagId
    Entries: RemoteData<LibraryEntry list>
}

type Msg =
    | LoadEntries
    | EntriesLoaded of Result<LibraryEntry list, string>
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId

module Model =
    let create tagId = { TagId = tagId; Entries = NotAsked }
