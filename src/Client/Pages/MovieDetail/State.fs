module Pages.MovieDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type MovieApi = {
    GetEntry: EntryId -> Async<LibraryEntry option>
    MarkWatched: EntryId -> Async<Result<LibraryEntry, string>>
    MarkUnwatched: EntryId -> Async<Result<LibraryEntry, string>>
    Resume: EntryId -> Async<Result<LibraryEntry, string>>
    ToggleFavorite: EntryId -> Async<Result<LibraryEntry, string>>
    SetRating: EntryId * int option -> Async<Result<LibraryEntry, string>>
    UpdateNotes: EntryId * string option -> Async<Result<LibraryEntry, string>>
    ToggleTag: EntryId * TagId -> Async<Result<LibraryEntry, string>>
    ToggleFriend: EntryId * FriendId -> Async<Result<LibraryEntry, string>>
}

let init (entryId: EntryId) : Model * Cmd<Msg> =
    Model.create entryId, Cmd.ofMsg LoadEntry

let update (api: MovieApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntry ->
        let cmd =
            Cmd.OfAsync.either
                api.GetEntry
                model.EntryId
                (Ok >> EntryLoaded)
                (fun ex -> Error ex.Message |> EntryLoaded)
        { model with Entry = Loading }, cmd, NoOp

    | EntryLoaded (Ok (Some entry)) ->
        { model with Entry = Success entry }, Cmd.none, NoOp

    | EntryLoaded (Ok None) ->
        { model with Entry = Failure "Entry not found" }, Cmd.none, NoOp

    | EntryLoaded (Error err) ->
        { model with Entry = Failure err }, Cmd.none, NoOp

    | MarkWatched ->
        let cmd =
            Cmd.OfAsync.either
                api.MarkWatched
                model.EntryId
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | MarkUnwatched ->
        let cmd =
            Cmd.OfAsync.either
                api.MarkUnwatched
                model.EntryId
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | OpenAbandonModal ->
        model, Cmd.none, RequestOpenAbandonModal model.EntryId

    | ResumeEntry ->
        let cmd =
            Cmd.OfAsync.either
                api.Resume
                model.EntryId
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | ToggleFavorite ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleFavorite
                model.EntryId
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | SetRating rating ->
        // Rating of 0 means clear the rating
        let ratingValue = if rating = 0 then None else Some rating
        let cmd =
            Cmd.OfAsync.either
                api.SetRating
                (model.EntryId, ratingValue)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | UpdateNotes notes ->
        // Just update local state, will save on SaveNotes
        match model.Entry with
        | Success entry ->
            let updatedEntry = { entry with Notes = if notes = "" then None else Some notes }
            { model with Entry = Success updatedEntry }, Cmd.none, NoOp
        | _ -> model, Cmd.none, NoOp

    | SaveNotes ->
        match model.Entry with
        | Success entry ->
            let cmd =
                Cmd.OfAsync.either
                    api.UpdateNotes
                    (model.EntryId, entry.Notes)
                    ActionResult
                    (fun ex -> Error ex.Message |> ActionResult)
            model, cmd, NoOp
        | _ -> model, Cmd.none, NoOp

    | OpenDeleteModal ->
        model, Cmd.none, RequestOpenDeleteModal model.EntryId

    | ToggleTag tagId ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleTag
                (model.EntryId, tagId)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | ToggleFriend friendId ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleFriend
                (model.EntryId, friendId)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | ActionResult (Ok entry) ->
        { model with Entry = Success entry }, Cmd.none, EntryUpdated entry

    | ActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | GoBack ->
        model, Cmd.none, NavigateBack
