module Pages.ContributorDetail.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type ContributorApi = {
    GetPersonDetails: TmdbPersonId -> Async<Result<TmdbPersonDetails, string>>
    GetFilmography: TmdbPersonId -> Async<Result<TmdbFilmography, string>>
    GetLibrary: unit -> Async<LibraryEntry list>
    AddMovie: AddMovieRequest -> Async<Result<LibraryEntry, string>>
    AddSeries: AddSeriesRequest -> Async<Result<LibraryEntry, string>>
    GetTrackedByTmdbId: TmdbPersonId -> Async<TrackedContributor option>
    Track: TrackContributorRequest -> Async<Result<TrackedContributor, string>>
    Untrack: TrackedContributorId -> Async<Result<unit, string>>
}

let init (personId: TmdbPersonId) : Model * Cmd<Msg> =
    let model = Model.create personId
    let cmds = Cmd.batch [
        Cmd.ofMsg LoadPersonDetails
        Cmd.ofMsg LoadFilmography
        Cmd.ofMsg CheckIsTracked
    ]
    model, cmds

/// Build a map of (TmdbId, MediaType) -> EntryId from library entries
let private buildLibraryMap (entries: LibraryEntry list) : Map<int * MediaType, EntryId> =
    entries
    |> List.choose (fun entry ->
        match entry.Media with
        | LibraryMovie m ->
            let tmdbId = TmdbMovieId.value m.TmdbId
            Some ((tmdbId, MediaType.Movie), entry.Id)
        | LibrarySeries s ->
            let tmdbId = TmdbSeriesId.value s.TmdbId
            Some ((tmdbId, MediaType.Series), entry.Id)
    )
    |> Map.ofList

let update (api: ContributorApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadPersonDetails ->
        let cmd =
            Cmd.OfAsync.either
                api.GetPersonDetails
                model.TmdbPersonId
                PersonDetailsLoaded
                (fun ex -> Error ex.Message |> PersonDetailsLoaded)
        { model with PersonDetails = Loading }, cmd, NoOp

    | PersonDetailsLoaded (Ok details) ->
        { model with PersonDetails = Success details }, Cmd.none, NoOp

    | PersonDetailsLoaded (Error err) ->
        { model with PersonDetails = Failure err }, Cmd.none, NoOp

    | LoadFilmography ->
        let cmd = Cmd.batch [
            Cmd.OfAsync.either
                api.GetFilmography
                model.TmdbPersonId
                FilmographyLoaded
                (fun ex -> Error ex.Message |> FilmographyLoaded)
            Cmd.OfAsync.perform
                api.GetLibrary
                ()
                LibraryLoaded
        ]
        { model with Filmography = Loading }, cmd, NoOp

    | FilmographyLoaded (Ok filmography) ->
        { model with Filmography = Success filmography }, Cmd.none, NoOp

    | FilmographyLoaded (Error err) ->
        { model with Filmography = Failure err }, Cmd.none, NoOp

    | LibraryLoaded entries ->
        let libraryMap = buildLibraryMap entries
        { model with LibraryEntryIds = libraryMap }, Cmd.none, NoOp

    | SetFilmographyFilter filter ->
        { model with FilmographyFilter = filter }, Cmd.none, NoOp

    | SetMediaTypeFilter filter ->
        { model with MediaTypeFilter = filter }, Cmd.none, NoOp

    | SetRoleFilter filter ->
        { model with RoleFilter = filter }, Cmd.none, NoOp

    | AddToLibrary work ->
        let cmd =
            match work.MediaType with
            | MediaType.Movie ->
                let request : AddMovieRequest = {
                    TmdbId = TmdbMovieId work.TmdbId
                    WhyAdded = None
                    InitialTags = []
                    InitialFriends = []
                }
                Cmd.OfAsync.either
                    api.AddMovie
                    request
                    AddToLibraryResult
                    (fun ex -> Error ex.Message |> AddToLibraryResult)
            | MediaType.Series ->
                let request : AddSeriesRequest = {
                    TmdbId = TmdbSeriesId work.TmdbId
                    WhyAdded = None
                    InitialTags = []
                    InitialFriends = []
                }
                Cmd.OfAsync.either
                    api.AddSeries
                    request
                    AddToLibraryResult
                    (fun ex -> Error ex.Message |> AddToLibraryResult)
        model, cmd, NoOp

    | AddToLibraryResult (Ok entry) ->
        // Update the library map with the new entry
        let tmdbId, mediaType =
            match entry.Media with
            | LibraryMovie m -> TmdbMovieId.value m.TmdbId, MediaType.Movie
            | LibrarySeries s -> TmdbSeriesId.value s.TmdbId, MediaType.Series
        let updatedMap = model.LibraryEntryIds |> Map.add (tmdbId, mediaType) entry.Id
        { model with LibraryEntryIds = updatedMap }, Cmd.none, ShowNotification ("Added to library", true)

    | AddToLibraryResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | ViewMovieDetail entryId ->
        model, Cmd.none, NavigateToMovieDetail entryId

    | ViewSeriesDetail entryId ->
        model, Cmd.none, NavigateToSeriesDetail entryId

    | GoBack ->
        model, Cmd.none, NavigateBack

    | CheckIsTracked ->
        let cmd =
            Cmd.OfAsync.perform
                api.GetTrackedByTmdbId
                model.TmdbPersonId
                TrackedStatusLoaded
        model, cmd, NoOp

    | TrackedStatusLoaded trackedOpt ->
        match trackedOpt with
        | Some tracked ->
            { model with IsTracked = true; TrackedContributorId = Some tracked.Id }, Cmd.none, NoOp
        | None ->
            { model with IsTracked = false; TrackedContributorId = None }, Cmd.none, NoOp

    | TrackContributor ->
        match model.PersonDetails with
        | Success details ->
            let request : TrackContributorRequest = {
                TmdbPersonId = model.TmdbPersonId
                Name = details.Name
                ProfilePath = details.ProfilePath
                KnownForDepartment = details.KnownForDepartment
                Notes = None
            }
            let cmd =
                Cmd.OfAsync.either
                    api.Track
                    request
                    TrackContributorResult
                    (fun ex -> Error ex.Message |> TrackContributorResult)
            model, cmd, NoOp
        | _ ->
            model, Cmd.none, ShowNotification ("Cannot track - details not loaded", false)

    | TrackContributorResult (Ok trackedContributor) ->
        { model with IsTracked = true; TrackedContributorId = Some trackedContributor.Id },
        Cmd.none,
        ShowNotification ($"Now tracking {trackedContributor.Name}", true)

    | TrackContributorResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)

    | UntrackContributor ->
        match model.TrackedContributorId with
        | Some trackedId ->
            let cmd =
                Cmd.OfAsync.either
                    api.Untrack
                    trackedId
                    UntrackContributorResult
                    (fun ex -> Error ex.Message |> UntrackContributorResult)
            model, cmd, NoOp
        | None ->
            model, Cmd.none, ShowNotification ("Not currently tracked", false)

    | UntrackContributorResult (Ok ()) ->
        { model with IsTracked = false; TrackedContributorId = None },
        Cmd.none,
        ShowNotification ("Contributor untracked", true)

    | UntrackContributorResult (Error err) ->
        model, Cmd.none, ShowNotification (err, false)
