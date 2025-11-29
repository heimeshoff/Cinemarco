module Pages.SessionDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type SessionApi = {
    GetSession: SessionId -> Async<Result<WatchSessionWithProgress, string>>
    UpdateSession: UpdateSessionRequest -> Async<Result<WatchSession, string>>
    DeleteSession: SessionId -> Async<Result<unit, string>>
    ToggleTag: SessionId * TagId -> Async<Result<WatchSession, string>>
    ToggleFriend: SessionId * FriendId -> Async<Result<WatchSession, string>>
    ToggleEpisode: SessionId * int * int * bool -> Async<Result<EpisodeProgress list, string>>
    MarkSeasonWatched: SessionId * int -> Async<Result<EpisodeProgress list, string>>
    GetSeasonDetails: TmdbSeriesId * int -> Async<Result<TmdbSeasonDetails, string>>
}

let init (sessionId: SessionId) : Model * Cmd<Msg> =
    Model.create sessionId, Cmd.ofMsg LoadSession

let update (api: SessionApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadSession ->
        let cmd =
            Cmd.OfAsync.either
                api.GetSession
                model.SessionId
                SessionLoaded
                (fun ex -> Error ex.Message |> SessionLoaded)
        { model with SessionData = Loading }, cmd, NoOp

    | SessionLoaded (Ok data) ->
        // Trigger loading season details for all seasons
        let seasonCmds =
            match data.Entry.Media with
            | LibrarySeries series ->
                [ 1 .. series.NumberOfSeasons ]
                |> List.map (fun seasonNum -> Cmd.ofMsg (LoadSeasonDetails seasonNum))
                |> Cmd.batch
            | _ -> Cmd.none
        { model with SessionData = Success data }, seasonCmds, NoOp

    | SessionLoaded (Error err) ->
        { model with SessionData = Failure err }, Cmd.none, NoOp

    | LoadSeasonDetails seasonNum ->
        match model.SessionData with
        | Success data ->
            match data.Entry.Media with
            | LibrarySeries series ->
                // Skip if already loaded or loading
                if Map.containsKey seasonNum model.SeasonDetails || Set.contains seasonNum model.LoadingSeasons then
                    model, Cmd.none, NoOp
                else
                    let cmd =
                        Cmd.OfAsync.either
                            api.GetSeasonDetails
                            (series.TmdbId, seasonNum)
                            (fun result -> SeasonDetailsLoaded (seasonNum, result))
                            (fun ex -> SeasonDetailsLoaded (seasonNum, Error ex.Message))
                    { model with LoadingSeasons = Set.add seasonNum model.LoadingSeasons }, cmd, NoOp
            | _ -> model, Cmd.none, NoOp
        | _ -> model, Cmd.none, NoOp

    | SeasonDetailsLoaded (seasonNum, Ok details) ->
        { model with
            SeasonDetails = Map.add seasonNum details model.SeasonDetails
            LoadingSeasons = Set.remove seasonNum model.LoadingSeasons
        }, Cmd.none, NoOp

    | SeasonDetailsLoaded (seasonNum, Error _) ->
        { model with LoadingSeasons = Set.remove seasonNum model.LoadingSeasons }, Cmd.none, NoOp

    | ToggleEpisodeWatched (seasonNum, episodeNum, isWatched) ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleEpisode
                (model.SessionId, seasonNum, episodeNum, isWatched)
                EpisodeActionResult
                (fun ex -> Error ex.Message |> EpisodeActionResult)
        model, cmd, NoOp

    | MarkEpisodesUpTo (seasonNum, episodeNum, isWatched) ->
        // Mark all episodes from 1 to episodeNum in this season
        let cmds =
            [ 1 .. episodeNum ]
            |> List.map (fun epNum ->
                Cmd.OfAsync.either
                    api.ToggleEpisode
                    (model.SessionId, seasonNum, epNum, isWatched)
                    EpisodeActionResult
                    (fun ex -> Error ex.Message |> EpisodeActionResult))
            |> Cmd.batch
        model, cmds, NoOp

    | MarkSeasonWatched seasonNum ->
        let cmd =
            Cmd.OfAsync.either
                api.MarkSeasonWatched
                (model.SessionId, seasonNum)
                EpisodeActionResult
                (fun ex -> Error ex.Message |> EpisodeActionResult)
        model, cmd, NoOp

    | UpdateStatus status ->
        let request : UpdateSessionRequest = {
            Id = model.SessionId
            Notes = None
            Status = Some status
        }
        let cmd =
            Cmd.OfAsync.either
                api.UpdateSession
                request
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | ToggleTag tagId ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleTag
                (model.SessionId, tagId)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | ToggleFriend friendId ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleFriend
                (model.SessionId, friendId)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        model, cmd, NoOp

    | DeleteSession ->
        let cmd =
            Cmd.OfAsync.either
                api.DeleteSession
                model.SessionId
                DeleteResult
                (fun ex -> Error ex.Message |> DeleteResult)
        model, cmd, NoOp

    | ActionResult (Ok session) ->
        // Reload the session to get updated data
        model, Cmd.ofMsg LoadSession, ShowNotification ("Updated successfully", true)

    | ActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | EpisodeActionResult (Ok progress) ->
        // Update the episode progress in the model
        match model.SessionData with
        | Success data ->
            let watchedCount = progress |> List.filter (fun p -> p.IsWatched) |> List.length
            let completionPct =
                if data.TotalEpisodes > 0 then
                    float watchedCount / float data.TotalEpisodes * 100.0
                else 0.0
            let newData = {
                data with
                    EpisodeProgress = progress
                    WatchedEpisodes = watchedCount
                    CompletionPercentage = completionPct
            }
            { model with SessionData = Success newData }, Cmd.none, NoOp
        | _ -> model, Cmd.none, NoOp

    | EpisodeActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | DeleteResult (Ok ()) ->
        model, Cmd.none, SessionDeleted model.SessionId

    | DeleteResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | GoBack ->
        match model.SessionData with
        | Success data -> model, Cmd.none, NavigateToSeries data.Session.EntryId
        | _ -> model, Cmd.none, NavigateBack
