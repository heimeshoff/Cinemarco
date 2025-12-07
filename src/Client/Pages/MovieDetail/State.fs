module Pages.MovieDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type MovieApi = {
    GetEntry: EntryId -> Async<LibraryEntry option>
    GetCollections: EntryId -> Async<Collection list>
    GetCredits: TmdbMovieId -> Async<Result<TmdbCredits, string>>
    GetTrackedContributors: unit -> Async<TrackedContributor list>
    MarkWatched: EntryId -> Async<Result<LibraryEntry, string>>
    MarkUnwatched: EntryId -> Async<Result<LibraryEntry, string>>
    Resume: EntryId -> Async<Result<LibraryEntry, string>>
    SetRating: EntryId * int option -> Async<Result<LibraryEntry, string>>
    UpdateNotes: EntryId * string option -> Async<Result<LibraryEntry, string>>
    ToggleFriend: EntryId * FriendId -> Async<Result<LibraryEntry, string>>
    CreateFriend: CreateFriendRequest -> Async<Result<Friend, string>>
}

let init (entryId: EntryId) : Model * Cmd<Msg> =
    Model.create entryId, Cmd.batch [ Cmd.ofMsg LoadEntry; Cmd.ofMsg LoadCollections; Cmd.ofMsg LoadTrackedContributors ]

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
        // Also load credits when entry is loaded
        let creditsCmd =
            match entry.Media with
            | LibraryMovie m -> Cmd.ofMsg (LoadCredits m.TmdbId)
            | LibrarySeries _ -> Cmd.none
        { model with Entry = Success entry }, creditsCmd, NoOp

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

    | SetActiveTab tab ->
        { model with ActiveTab = tab }, Cmd.none, NoOp

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

    | OpenAddToCollectionModal ->
        match model.Entry with
        | Success entry ->
            let title =
                match entry.Media with
                | LibraryMovie m -> m.Title
                | LibrarySeries s -> s.Name
            model, Cmd.none, RequestOpenAddToCollectionModal (LibraryEntryRef model.EntryId, title)
        | _ -> model, Cmd.none, NoOp

    | ToggleFriendSelector ->
        { model with IsFriendSelectorOpen = not model.IsFriendSelectorOpen }, Cmd.none, NoOp

    | ToggleFriend friendId ->
        let cmd =
            Cmd.OfAsync.either
                api.ToggleFriend
                (model.EntryId, friendId)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        { model with IsFriendSelectorOpen = false }, cmd, NoOp

    | AddNewFriend name ->
        let request : CreateFriendRequest = {
            Name = name
            Nickname = None
            Notes = None
        }
        let cmd =
            Cmd.OfAsync.either
                api.CreateFriend
                request
                FriendCreated
                (fun ex -> Error ex.Message |> FriendCreated)
        { model with IsAddingFriend = true; IsFriendSelectorOpen = false }, cmd, NoOp

    | FriendCreated (Ok friend) ->
        // Toggle the friend on this entry (to add them) and notify the app
        let toggleCmd =
            Cmd.OfAsync.either
                api.ToggleFriend
                (model.EntryId, friend.Id)
                ActionResult
                (fun ex -> Error ex.Message |> ActionResult)
        { model with IsAddingFriend = false }, toggleCmd, FriendCreatedInline friend

    | FriendCreated (Error err) ->
        { model with IsAddingFriend = false }, Cmd.none, ShowNotification (err, false)

    | ActionResult (Ok entry) ->
        { model with Entry = Success entry }, Cmd.none, EntryUpdated entry

    | ActionResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | ViewContributor personId ->
        model, Cmd.none, NavigateToContributor personId

    | GoBack ->
        model, Cmd.none, NavigateBack
