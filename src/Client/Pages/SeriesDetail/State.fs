module Pages.SeriesDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type SeriesApi = {
    GetEntry: EntryId -> Async<LibraryEntry option>
    GetCollections: EntryId -> Async<Collection list>
    GetCredits: TmdbSeriesId -> Async<Result<TmdbCredits, string>>
    GetTrackedContributors: unit -> Async<TrackedContributor list>
    GetSessions: EntryId -> Async<WatchSession list>
    GetSessionProgress: SessionId -> Async<EpisodeProgress list>
    GetSeasonDetails: TmdbSeriesId * int -> Async<Result<TmdbSeasonDetails, string>>
    MarkCompleted: EntryId -> Async<Result<LibraryEntry, string>>
    Resume: EntryId -> Async<Result<LibraryEntry, string>>
    SetRating: EntryId * int option -> Async<Result<LibraryEntry, string>>
    UpdateNotes: EntryId * string option -> Async<Result<LibraryEntry, string>>
    ToggleTag: EntryId * TagId -> Async<Result<LibraryEntry, string>>
    ToggleFriend: EntryId * FriendId -> Async<Result<LibraryEntry, string>>
    ToggleEpisode: SessionId * int * int * bool -> Async<Result<EpisodeProgress list, string>>
    MarkSeasonWatched: SessionId * int -> Async<Result<EpisodeProgress list, string>>
    DeleteSession: SessionId -> Async<Result<unit, string>>
}

let init (entryId: EntryId) : Model * Cmd<Msg> =
    Model.create entryId, Cmd.batch [ Cmd.ofMsg LoadEntry; Cmd.ofMsg LoadCollections; Cmd.ofMsg LoadSessions; Cmd.ofMsg LoadTrackedContributors ]

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

    | EntryLoaded (Ok (Some entry)) ->
        // Trigger loading season details for all seasons and credits
        let seasonCmds, creditsCmd =
            match entry.Media with
            | LibrarySeries series ->
                let sCmd =
                    [ 1 .. series.NumberOfSeasons ]
                    |> List.map (fun seasonNum -> Cmd.ofMsg (LoadSeasonDetails seasonNum))
                    |> Cmd.batch
                let cCmd = Cmd.ofMsg (LoadCredits series.TmdbId)
                sCmd, cCmd
            | _ -> Cmd.none, Cmd.none
        { model with Entry = Success entry }, Cmd.batch [seasonCmds; creditsCmd], NoOp

    | EntryLoaded (Ok None) ->
        { model with Entry = Failure "Entry not found" }, Cmd.none, NoOp

    | EntryLoaded (Error err) ->
        { model with Entry = Failure err }, Cmd.none, NoOp

    | LoadCollections ->
        let cmd =
            Cmd.OfAsync.either
                api.GetCollections
                model.EntryId
                (Ok >> CollectionsLoaded)
                (fun ex -> Error ex.Message |> CollectionsLoaded)
        { model with Collections = Loading }, cmd, NoOp

    | CollectionsLoaded (Ok collections) ->
        { model with Collections = Success collections }, Cmd.none, NoOp

    | CollectionsLoaded (Error _) ->
        { model with Collections = Success [] }, Cmd.none, NoOp

    | LoadCredits tmdbId ->
        let cmd =
            Cmd.OfAsync.either
                api.GetCredits
                tmdbId
                CreditsLoaded
                (fun ex -> Error ex.Message |> CreditsLoaded)
        { model with Credits = Loading }, cmd, NoOp

    | CreditsLoaded (Ok credits) ->
        { model with Credits = Success credits }, Cmd.none, NoOp

    | CreditsLoaded (Error _) ->
        { model with Credits = Failure "Could not load credits" }, Cmd.none, NoOp

    | LoadTrackedContributors ->
        let cmd =
            Cmd.OfAsync.perform
                api.GetTrackedContributors
                ()
                TrackedContributorsLoaded
        model, cmd, NoOp

    | TrackedContributorsLoaded trackedContributors ->
        let trackedIds =
            trackedContributors
            |> List.map (fun tc -> tc.TmdbPersonId)
            |> Set.ofList
        { model with TrackedPersonIds = trackedIds }, Cmd.none, NoOp

    | LoadSeasonDetails seasonNum ->
        match model.Entry with
        | Success entry ->
            match entry.Media with
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

    | LoadSessions ->
        let cmd =
            Cmd.OfAsync.either
                api.GetSessions
                model.EntryId
                (Ok >> SessionsLoaded)
                (fun ex -> Error ex.Message |> SessionsLoaded)
        { model with Sessions = Loading }, cmd, NoOp

    | SessionsLoaded (Ok sessions) ->
        // Preserve existing selection if valid, otherwise auto-select the default session
        let existingSelectionValid =
            model.SelectedSessionId
            |> Option.bind (fun id -> sessions |> List.tryFind (fun s -> s.Id = id))
            |> Option.map (fun s -> s.Id)
        let defaultSession = sessions |> List.tryFind (fun s -> s.IsDefault)
        let selectedId =
            match existingSelectionValid with
            | Some id -> Some id
            | None -> defaultSession |> Option.map (fun s -> s.Id)
        let loadProgressCmd =
            match selectedId with
            | Some id -> Cmd.ofMsg (LoadSessionProgress id)
            | None -> Cmd.none
        { model with Sessions = Success sessions; SelectedSessionId = selectedId }, loadProgressCmd, NoOp

    | SessionsLoaded (Error _) ->
        { model with Sessions = Success [] }, Cmd.none, NoOp

    | SelectSession sessionId ->
        { model with SelectedSessionId = Some sessionId }, Cmd.ofMsg (LoadSessionProgress sessionId), NoOp

    | LoadSessionProgress sessionId ->
        let cmd =
            Cmd.OfAsync.either
                api.GetSessionProgress
                sessionId
                (Ok >> SessionProgressLoaded)
                (fun ex -> Error ex.Message |> SessionProgressLoaded)
        model, cmd, NoOp

    | SessionProgressLoaded (Ok progress) ->
        { model with EpisodeProgress = progress }, Cmd.none, NoOp

    | SessionProgressLoaded (Error _) ->
        { model with EpisodeProgress = [] }, Cmd.none, NoOp

    | OpenNewSessionModal ->
        model, Cmd.none, RequestOpenNewSessionModal model.EntryId

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
        match model.SelectedSessionId with
        | Some sessionId ->
            let cmd =
                Cmd.OfAsync.either
                    api.ToggleEpisode
                    (sessionId, seasonNum, episodeNum, isWatched)
                    EpisodeActionResult
                    (fun ex -> Error ex.Message |> EpisodeActionResult)
            model, cmd, NoOp
        | None -> model, Cmd.none, NoOp

    | MarkEpisodesUpTo (seasonNum, episodeNum, isWatched) ->
        match model.SelectedSessionId with
        | Some sessionId ->
            // Mark all episodes from 1 to episodeNum in this season
            let cmds =
                [ 1 .. episodeNum ]
                |> List.map (fun epNum ->
                    Cmd.OfAsync.either
                        api.ToggleEpisode
                        (sessionId, seasonNum, epNum, isWatched)
                        EpisodeActionResult
                        (fun ex -> Error ex.Message |> EpisodeActionResult))
                |> Cmd.batch
            model, cmds, NoOp
        | None -> model, Cmd.none, NoOp

    | MarkSeasonWatched seasonNum ->
        match model.SelectedSessionId with
        | Some sessionId ->
            let cmd =
                Cmd.OfAsync.either
                    api.MarkSeasonWatched
                    (sessionId, seasonNum)
                    EpisodeActionResult
                    (fun ex -> Error ex.Message |> EpisodeActionResult)
            model, cmd, NoOp
        | None -> model, Cmd.none, NoOp

    | ToggleRatingDropdown ->
        { model with IsRatingOpen = not model.IsRatingOpen }, Cmd.none, NoOp

    | SetRating rating ->
        // Rating of 0 means clear the rating
        let ratingValue = if rating = 0 then None else Some rating
        let cmd =
            Cmd.OfAsync.either
                api.SetRating
                (model.EntryId, ratingValue)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        { model with IsRatingOpen = false }, cmd, NoOp

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

    | OpenAddToCollectionModal ->
        match model.Entry with
        | Success entry ->
            let title =
                match entry.Media with
                | LibraryMovie m -> m.Title
                | LibrarySeries s -> s.Name
            model, Cmd.none, RequestOpenAddToCollectionModal (model.EntryId, title)
        | _ -> model, Cmd.none, NoOp

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

    | EpisodeActionResult (Ok progress) ->
        { model with EpisodeProgress = progress }, Cmd.none, NoOp

    | EpisodeActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | DeleteSession sessionId ->
        let cmd =
            Cmd.OfAsync.either
                api.DeleteSession
                sessionId
                (fun result -> result |> Result.map (fun () -> sessionId) |> SessionDeleteResult)
                (fun ex -> Error ex.Message |> SessionDeleteResult)
        model, cmd, NoOp

    | SessionDeleteResult (Ok deletedId) ->
        // Remove session from list and select default if we deleted the selected one
        let updatedSessions =
            model.Sessions |> RemoteData.map (List.filter (fun s -> s.Id <> deletedId))
        let newSelectedId =
            if model.SelectedSessionId = Some deletedId then
                match updatedSessions with
                | Success sessions -> sessions |> List.tryFind (fun s -> s.IsDefault) |> Option.map (fun s -> s.Id)
                | _ -> None
            else
                model.SelectedSessionId
        let loadProgressCmd =
            match newSelectedId with
            | Some id when model.SelectedSessionId = Some deletedId -> Cmd.ofMsg (LoadSessionProgress id)
            | _ -> Cmd.none
        { model with Sessions = updatedSessions; SelectedSessionId = newSelectedId }, loadProgressCmd, ShowNotification ("Session deleted", true)

    | SessionDeleteResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | ViewContributor personId ->
        model, Cmd.none, NavigateToContributor personId

    | GoBack ->
        model, Cmd.none, NavigateBack
