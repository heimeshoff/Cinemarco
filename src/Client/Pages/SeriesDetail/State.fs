module Pages.SeriesDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type SeriesApi = {
    GetEntry: EntryId -> Async<(LibraryEntry * EpisodeProgress list) option>
    MarkCompleted: EntryId -> Async<Result<LibraryEntry, string>>
    Resume: EntryId -> Async<Result<LibraryEntry, string>>
    ToggleFavorite: EntryId -> Async<Result<LibraryEntry, string>>
    SetRating: EntryId * int option -> Async<Result<LibraryEntry, string>>
    UpdateNotes: EntryId * string option -> Async<Result<LibraryEntry, string>>
    ToggleTag: EntryId * TagId -> Async<Result<LibraryEntry, string>>
    ToggleFriend: EntryId * FriendId -> Async<Result<LibraryEntry, string>>
    ToggleEpisode: EntryId * int * int * bool -> Async<Result<EpisodeProgress list, string>>
    MarkSeasonWatched: EntryId * int -> Async<Result<EpisodeProgress list, string>>
}

let init (entryId: EntryId) : Model * Cmd<Msg> =
    Model.create entryId, Cmd.ofMsg LoadEntry

let update (api: SeriesApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadEntry ->
        let cmd =
            Cmd.OfAsync.either
                api.GetEntry
                model.EntryId
                (Ok >> EntryLoaded)
                (fun ex -> Error ex.Message |> EntryLoaded)
        { model with Entry = Loading }, cmd, NoOp

    | EntryLoaded (Ok (Some (entry, progress))) ->
        { model with Entry = Success entry; EpisodeProgress = progress }, Cmd.none, NoOp

    | EntryLoaded (Ok None) ->
        { model with Entry = Failure "Entry not found" }, Cmd.none, NoOp

    | EntryLoaded (Error err) ->
        { model with Entry = Failure err }, Cmd.none, NoOp

    | MarkSeriesCompleted ->
        let cmd =
            Cmd.OfAsync.either
                api.MarkCompleted
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

    | ToggleEpisodeWatched (seasonNum, episodeNum, isWatched) ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleEpisode
                (model.EntryId, seasonNum, episodeNum, isWatched)
                EpisodeActionResult
                (fun ex -> Error ex.Message |> EpisodeActionResult)
        model, cmd, NoOp

    | MarkSeasonWatched seasonNum ->
        let cmd =
            Cmd.OfAsync.either
                api.MarkSeasonWatched
                (model.EntryId, seasonNum)
                EpisodeActionResult
                (fun ex -> Error ex.Message |> EpisodeActionResult)
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
        let cmd =
            Cmd.OfAsync.either
                api.SetRating
                (model.EntryId, Some rating)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | UpdateNotes notes ->
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
        { model with Entry = Success entry }, Cmd.none, ShowNotification ("Updated successfully", true)

    | ActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | EpisodeActionResult (Ok progress) ->
        { model with EpisodeProgress = progress }, Cmd.none, NoOp

    | EpisodeActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | GoBack ->
        model, Cmd.none, NavigateBack
