module Components.AbandonModal.Types

open Shared.Domain

type Model = {
    EntryId: EntryId
    Reason: string
    AbandonedAtSeason: int option
    AbandonedAtEpisode: int option
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | ReasonChanged of string
    | SeasonChanged of int option
    | EpisodeChanged of int option
    | Submit
    | SubmitResult of Result<LibraryEntry, string>
    | Close

type ExternalMsg =
    | NoOp
    | Abandoned of LibraryEntry
    | CloseRequested

module Model =
    let create entryId = {
        EntryId = entryId
        Reason = ""
        AbandonedAtSeason = None
        AbandonedAtEpisode = None
        IsSubmitting = false
        Error = None
    }
