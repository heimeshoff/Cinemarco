module App.State

open Elmish
open Common.Types
open Common.Routing
open Shared.Domain
open Types

// Note: This file provides the structure for the App state management.
// The actual API calls would need to be wired up with the real Api module.

let init () : Model * Cmd<Msg> =
    let model = Model.empty
    let cmds = Cmd.batch [
        Cmd.ofMsg LoadFriends
        Cmd.ofMsg LoadTags
        Cmd.ofMsg (LayoutMsg Components.Layout.Types.CheckHealth)
        Cmd.ofMsg (NavigateTo HomePage)
    ]
    model, cmds

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    // Navigation
    | NavigateTo page ->
        let model' = { model with CurrentPage = page }
        // Initialize page if needed
        match page with
        | HomePage when model.HomePage.IsNone ->
            let pageModel, pageCmd = Pages.Home.State.init ()
            { model' with HomePage = Some pageModel }, Cmd.map HomeMsg pageCmd
        | LibraryPage when model.LibraryPage.IsNone ->
            let pageModel, pageCmd = Pages.Library.State.init ()
            { model' with LibraryPage = Some pageModel }, Cmd.map LibraryMsg pageCmd
        | FriendsPage when model.FriendsPage.IsNone ->
            let pageModel, pageCmd = Pages.Friends.State.init ()
            { model' with FriendsPage = Some pageModel }, Cmd.map FriendsMsg pageCmd
        | TagsPage when model.TagsPage.IsNone ->
            let pageModel, pageCmd = Pages.Tags.State.init ()
            { model' with TagsPage = Some pageModel }, Cmd.map TagsMsg pageCmd
        | MovieDetailPage entryId when model.MovieDetailPage.IsNone || model.MovieDetailPage |> Option.map (fun m -> m.EntryId <> entryId) |> Option.defaultValue true ->
            let pageModel, pageCmd = Pages.MovieDetail.State.init entryId
            { model' with MovieDetailPage = Some pageModel }, Cmd.map MovieDetailMsg pageCmd
        | SeriesDetailPage entryId when model.SeriesDetailPage.IsNone || model.SeriesDetailPage |> Option.map (fun m -> m.EntryId <> entryId) |> Option.defaultValue true ->
            let pageModel, pageCmd = Pages.SeriesDetail.State.init entryId
            { model' with SeriesDetailPage = Some pageModel }, Cmd.map SeriesDetailMsg pageCmd
        | FriendDetailPage friendId when model.FriendDetailPage.IsNone || model.FriendDetailPage |> Option.map (fun m -> m.FriendId <> friendId) |> Option.defaultValue true ->
            let pageModel, pageCmd = Pages.FriendDetail.State.init friendId
            { model' with FriendDetailPage = Some pageModel }, Cmd.map FriendDetailMsg pageCmd
        | TagDetailPage tagId when model.TagDetailPage.IsNone || model.TagDetailPage |> Option.map (fun m -> m.TagId <> tagId) |> Option.defaultValue true ->
            let pageModel, pageCmd = Pages.TagDetail.State.init tagId
            { model' with TagDetailPage = Some pageModel }, Cmd.map TagDetailMsg pageCmd
        | CachePage when model.CachePage.IsNone ->
            let pageModel, pageCmd = Pages.Cache.State.init ()
            { model' with CachePage = Some pageModel }, Cmd.map CacheMsg pageCmd
        | _ -> model', Cmd.none

    // Global data loading
    | LoadFriends ->
        { model with Friends = Loading },
        Cmd.OfAsync.either
            Api.api.friendsGetAll
            ()
            (Ok >> FriendsLoaded)
            (fun ex -> Error ex.Message |> FriendsLoaded)

    | FriendsLoaded (Ok friends) ->
        { model with Friends = Success friends }, Cmd.none

    | FriendsLoaded (Error _) ->
        { model with Friends = Success [] }, Cmd.none

    | LoadTags ->
        { model with Tags = Loading },
        Cmd.OfAsync.either
            Api.api.tagsGetAll
            ()
            (Ok >> TagsLoaded)
            (fun ex -> Error ex.Message |> TagsLoaded)

    | TagsLoaded (Ok tags) ->
        { model with Tags = Success tags }, Cmd.none

    | TagsLoaded (Error _) ->
        { model with Tags = Success [] }, Cmd.none

    // Layout messages
    | LayoutMsg layoutMsg ->
        let healthApi = fun () -> Api.api.healthCheck ()
        let newLayout, layoutCmd, extMsg = Components.Layout.State.update healthApi layoutMsg model.Layout
        let model' = { model with Layout = newLayout }
        let cmd = Cmd.map LayoutMsg layoutCmd
        match extMsg with
        | Components.Layout.Types.NoOp -> model', cmd
        | Components.Layout.Types.NavigateTo page -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo page)]
        | Components.Layout.Types.OpenSearchModal -> model', Cmd.batch [cmd; Cmd.ofMsg OpenSearchModal]

    // Modal management
    | OpenSearchModal ->
        let modalModel = Components.SearchModal.State.init ()
        { model with Modal = SearchModal modalModel }, Cmd.none

    | CloseModal ->
        { model with Modal = NoModal }, Cmd.none

    | SearchModalMsg searchMsg ->
        match model.Modal with
        | SearchModal modalModel ->
            let searchApi = fun () query -> Api.api.tmdbSearchAll query
            let newModal, modalCmd, extMsg = Components.SearchModal.State.update searchApi searchMsg modalModel
            let model' = { model with Modal = SearchModal newModal }
            let cmd = Cmd.map SearchModalMsg modalCmd
            match extMsg with
            | Components.SearchModal.Types.NoOp -> model', cmd
            | Components.SearchModal.Types.ItemSelected item ->
                model', Cmd.batch [cmd; Cmd.ofMsg (OpenQuickAddModal item)]
            | Components.SearchModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenQuickAddModal item ->
        let modalModel = Components.QuickAddModal.State.init item
        { model with Modal = QuickAddModal modalModel }, Cmd.none

    | QuickAddModalMsg quickAddMsg ->
        match model.Modal with
        | QuickAddModal modalModel ->
            let addApi : Components.QuickAddModal.State.AddApi = {
                AddMovie = fun req -> Api.api.libraryAddMovie req
                AddSeries = fun req -> Api.api.libraryAddSeries req
            }
            let newModal, modalCmd, extMsg = Components.QuickAddModal.State.update addApi quickAddMsg modalModel
            let model' = { model with Modal = QuickAddModal newModal }
            let cmd = Cmd.map QuickAddModalMsg modalCmd
            match extMsg with
            | Components.QuickAddModal.Types.NoOp -> model', cmd
            | Components.QuickAddModal.Types.Added entry ->
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (EntryAdded entry)]
            | Components.QuickAddModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenFriendModal friend ->
        let modalModel = Components.FriendModal.State.init friend
        { model with Modal = FriendModal modalModel }, Cmd.none

    | FriendModalMsg friendMsg ->
        match model.Modal with
        | FriendModal modalModel ->
            let saveApi : Components.FriendModal.State.SaveApi = {
                Create = fun req -> Api.api.friendsCreate req
                Update = fun req -> Api.api.friendsUpdate req
            }
            let newModal, modalCmd, extMsg = Components.FriendModal.State.update saveApi friendMsg modalModel
            let model' = { model with Modal = FriendModal newModal }
            let cmd = Cmd.map FriendModalMsg modalCmd
            match extMsg with
            | Components.FriendModal.Types.NoOp -> model', cmd
            | Components.FriendModal.Types.Saved friend ->
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (FriendSaved friend)]
            | Components.FriendModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenTagModal tag ->
        let modalModel = Components.TagModal.State.init tag
        { model with Modal = TagModal modalModel }, Cmd.none

    | TagModalMsg tagMsg ->
        match model.Modal with
        | TagModal modalModel ->
            let saveApi : Components.TagModal.State.SaveApi = {
                Create = fun req -> Api.api.tagsCreate req
                Update = fun req -> Api.api.tagsUpdate req
            }
            let newModal, modalCmd, extMsg = Components.TagModal.State.update saveApi tagMsg modalModel
            let model' = { model with Modal = TagModal newModal }
            let cmd = Cmd.map TagModalMsg modalCmd
            match extMsg with
            | Components.TagModal.Types.NoOp -> model', cmd
            | Components.TagModal.Types.Saved tag ->
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (TagSaved tag)]
            | Components.TagModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenAbandonModal entryId ->
        let modalModel = Components.AbandonModal.State.init entryId
        { model with Modal = AbandonModal modalModel }, Cmd.none

    | AbandonModalMsg abandonMsg ->
        match model.Modal with
        | AbandonModal modalModel ->
            let abandonApi : Components.AbandonModal.State.AbandonApi =
                fun (entryId, req) -> Api.api.libraryAbandonEntry (entryId, req)
            let newModal, modalCmd, extMsg = Components.AbandonModal.State.update abandonApi abandonMsg modalModel
            let model' = { model with Modal = AbandonModal newModal }
            let cmd = Cmd.map AbandonModalMsg modalCmd
            match extMsg with
            | Components.AbandonModal.Types.NoOp -> model', cmd
            | Components.AbandonModal.Types.Abandoned entry ->
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (EntryAbandoned entry)]
            | Components.AbandonModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenConfirmDeleteModal target ->
        let modalModel = Components.ConfirmModal.State.init target
        { model with Modal = ConfirmDeleteModal modalModel }, Cmd.none

    | ConfirmModalMsg confirmMsg ->
        match model.Modal with
        | ConfirmDeleteModal modalModel ->
            let newModal, modalCmd, extMsg = Components.ConfirmModal.State.update confirmMsg modalModel
            let model' = { model with Modal = ConfirmDeleteModal newModal }
            let cmd = Cmd.map ConfirmModalMsg modalCmd
            match extMsg with
            | Components.ConfirmModal.Types.NoOp -> model', cmd
            | Components.ConfirmModal.Types.Confirmed target ->
                let deleteCmd =
                    match target with
                    | Components.ConfirmModal.Types.Friend f ->
                        Cmd.OfAsync.either
                            Api.api.friendsDelete
                            (FriendId.value f.Id)
                            (fun _ -> FriendDeleted f.Id)
                            (fun ex -> ShowNotification (ex.Message, false))
                    | Components.ConfirmModal.Types.Tag t ->
                        Cmd.OfAsync.either
                            Api.api.tagsDelete
                            (TagId.value t.Id)
                            (fun _ -> TagDeleted t.Id)
                            (fun ex -> ShowNotification (ex.Message, false))
                    | Components.ConfirmModal.Types.Entry entryId ->
                        Cmd.OfAsync.either
                            Api.api.libraryDeleteEntry
                            entryId
                            (fun _ -> EntryDeleted entryId)
                            (fun ex -> ShowNotification (ex.Message, false))
                { model' with Modal = NoModal }, Cmd.batch [cmd; deleteCmd]
            | Components.ConfirmModal.Types.Cancelled ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    // Notification
    | ShowNotification (message, isSuccess) ->
        let newNotification, notificationCmd, _ =
            Components.Notification.State.update
                (Components.Notification.Types.Show (message, isSuccess))
                model.Notification
        { model with Notification = newNotification }, Cmd.map NotificationMsg notificationCmd

    | NotificationMsg notificationMsg ->
        let newNotification, notificationCmd, _ =
            Components.Notification.State.update notificationMsg model.Notification
        { model with Notification = newNotification }, Cmd.map NotificationMsg notificationCmd

    // Page messages - Home
    | HomeMsg homeMsg ->
        match model.HomePage with
        | Some pageModel ->
            let libraryApi = fun () -> Api.api.libraryGetAll ()
            let newPage, pageCmd, extMsg = Pages.Home.State.update libraryApi homeMsg pageModel
            let model' = { model with HomePage = Some newPage }
            let cmd = Cmd.map HomeMsg pageCmd
            match extMsg with
            | Pages.Home.Types.NoOp -> model', cmd
            | Pages.Home.Types.NavigateToLibrary -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.Home.Types.NavigateToMovieDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage entryId))]
            | Pages.Home.Types.NavigateToSeriesDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage entryId))]
        | None -> model, Cmd.none

    // Page messages - Library
    | LibraryMsg libraryMsg ->
        match model.LibraryPage with
        | Some pageModel ->
            let libraryApi = fun () -> Api.api.libraryGetAll ()
            let newPage, pageCmd, extMsg = Pages.Library.State.update libraryApi libraryMsg pageModel
            let model' = { model with LibraryPage = Some newPage }
            let cmd = Cmd.map LibraryMsg pageCmd
            match extMsg with
            | Pages.Library.Types.NoOp -> model', cmd
            | Pages.Library.Types.NavigateToMovieDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage entryId))]
            | Pages.Library.Types.NavigateToSeriesDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage entryId))]
        | None -> model, Cmd.none

    // Page messages - Friends
    | FriendsMsg friendsMsg ->
        match model.FriendsPage with
        | Some pageModel ->
            let friendsApi = fun () -> Api.api.friendsGetAll ()
            let newPage, pageCmd, extMsg = Pages.Friends.State.update friendsApi friendsMsg pageModel
            let model' = { model with FriendsPage = Some newPage }
            let cmd = Cmd.map FriendsMsg pageCmd
            match extMsg with
            | Pages.Friends.Types.NoOp -> model', cmd
            | Pages.Friends.Types.NavigateToFriendDetail friendId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (FriendDetailPage friendId))]
            | Pages.Friends.Types.RequestOpenAddModal -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenFriendModal None)]
            | Pages.Friends.Types.RequestOpenEditModal friend -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenFriendModal (Some friend))]
            | Pages.Friends.Types.RequestOpenDeleteModal friend -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Friend friend))]
        | None -> model, Cmd.none

    // Page messages - FriendDetail
    | FriendDetailMsg friendDetailMsg ->
        match model.FriendDetailPage with
        | Some pageModel ->
            let entriesApi = fun friendId -> Api.api.friendsGetWatchedWith (FriendId.value friendId)
            let newPage, pageCmd, extMsg = Pages.FriendDetail.State.update entriesApi friendDetailMsg pageModel
            let model' = { model with FriendDetailPage = Some newPage }
            let cmd = Cmd.map FriendDetailMsg pageCmd
            match extMsg with
            | Pages.FriendDetail.Types.NoOp -> model', cmd
            | Pages.FriendDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo FriendsPage)]
            | Pages.FriendDetail.Types.NavigateToMovieDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage entryId))]
            | Pages.FriendDetail.Types.NavigateToSeriesDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage entryId))]
        | None -> model, Cmd.none

    // Page messages - Tags
    | TagsMsg tagsMsg ->
        match model.TagsPage with
        | Some pageModel ->
            let tagsApi = fun () -> Api.api.tagsGetAll ()
            let newPage, pageCmd, extMsg = Pages.Tags.State.update tagsApi tagsMsg pageModel
            let model' = { model with TagsPage = Some newPage }
            let cmd = Cmd.map TagsMsg pageCmd
            match extMsg with
            | Pages.Tags.Types.NoOp -> model', cmd
            | Pages.Tags.Types.NavigateToTagDetail tagId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (TagDetailPage tagId))]
            | Pages.Tags.Types.RequestOpenAddModal -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenTagModal None)]
            | Pages.Tags.Types.RequestOpenEditModal tag -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenTagModal (Some tag))]
            | Pages.Tags.Types.RequestOpenDeleteModal tag -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Tag tag))]
        | None -> model, Cmd.none

    // Page messages - TagDetail
    | TagDetailMsg tagDetailMsg ->
        match model.TagDetailPage with
        | Some pageModel ->
            let entriesApi = fun tagId -> Api.api.tagsGetTaggedEntries (TagId.value tagId)
            let newPage, pageCmd, extMsg = Pages.TagDetail.State.update entriesApi tagDetailMsg pageModel
            let model' = { model with TagDetailPage = Some newPage }
            let cmd = Cmd.map TagDetailMsg pageCmd
            match extMsg with
            | Pages.TagDetail.Types.NoOp -> model', cmd
            | Pages.TagDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo TagsPage)]
            | Pages.TagDetail.Types.NavigateToMovieDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage entryId))]
            | Pages.TagDetail.Types.NavigateToSeriesDetail entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage entryId))]
        | None -> model, Cmd.none

    // Page messages - MovieDetail
    | MovieDetailMsg movieMsg ->
        match model.MovieDetailPage with
        | Some pageModel ->
            // Note: MovieApi would need to be properly constructed with real API calls
            // Some operations (toggleFavorite, setRating, etc.) may need to be added to ICinemarcoApi
            let movieApi : Pages.MovieDetail.State.MovieApi = {
                GetEntry = fun entryId -> async {
                    let! result = Api.api.libraryGetById entryId
                    match result with
                    | Ok entry -> return Some entry
                    | Error _ -> return None
                }
                MarkWatched = fun entryId -> Api.api.libraryMarkMovieWatched (entryId, None)
                MarkUnwatched = fun entryId -> Api.api.libraryMarkMovieUnwatched entryId
                Resume = fun entryId -> Api.api.libraryResumeEntry entryId
                // These operations may need to be added to the API
                ToggleFavorite = fun _ -> async { return Error "Not implemented" }
                SetRating = fun _ -> async { return Error "Not implemented" }
                UpdateNotes = fun _ -> async { return Error "Not implemented" }
                ToggleTag = fun _ -> async { return Error "Not implemented" }
                ToggleFriend = fun _ -> async { return Error "Not implemented" }
            }
            let newPage, pageCmd, extMsg = Pages.MovieDetail.State.update movieApi movieMsg pageModel
            let model' = { model with MovieDetailPage = Some newPage }
            let cmd = Cmd.map MovieDetailMsg pageCmd
            match extMsg with
            | Pages.MovieDetail.Types.NoOp -> model', cmd
            | Pages.MovieDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.MovieDetail.Types.RequestOpenAbandonModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenAbandonModal entryId)]
            | Pages.MovieDetail.Types.RequestOpenDeleteModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Entry entryId))]
            | Pages.MovieDetail.Types.ShowNotification (msg, isSuccess) -> model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - SeriesDetail
    | SeriesDetailMsg seriesMsg ->
        match model.SeriesDetailPage with
        | Some pageModel ->
            // Note: SeriesApi would need to be properly constructed with real API calls
            let seriesApi : Pages.SeriesDetail.State.SeriesApi = {
                GetEntry = fun entryId -> async {
                    let! result = Api.api.libraryGetById entryId
                    let! progress = Api.api.libraryGetEpisodeProgress entryId
                    match result with
                    | Ok entry -> return Some (entry, progress)
                    | Error _ -> return None
                }
                MarkCompleted = fun entryId -> Api.api.libraryMarkSeriesCompleted entryId
                Resume = fun entryId -> Api.api.libraryResumeEntry entryId
                // These operations may need to be added to the API
                ToggleFavorite = fun _ -> async { return Error "Not implemented" }
                SetRating = fun _ -> async { return Error "Not implemented" }
                UpdateNotes = fun _ -> async { return Error "Not implemented" }
                ToggleTag = fun _ -> async { return Error "Not implemented" }
                ToggleFriend = fun _ -> async { return Error "Not implemented" }
                ToggleEpisode = fun (entryId, s, e, w) -> async {
                    let! result = Api.api.libraryUpdateEpisodeProgress (entryId, s, e, w)
                    match result with
                    | Ok () ->
                        let! progress = Api.api.libraryGetEpisodeProgress entryId
                        return Ok progress
                    | Error err -> return Error err
                }
                MarkSeasonWatched = fun (entryId, s) -> async {
                    let! result = Api.api.libraryMarkSeasonWatched (entryId, s)
                    match result with
                    | Ok () ->
                        let! progress = Api.api.libraryGetEpisodeProgress entryId
                        return Ok progress
                    | Error err -> return Error err
                }
            }
            let newPage, pageCmd, extMsg = Pages.SeriesDetail.State.update seriesApi seriesMsg pageModel
            let model' = { model with SeriesDetailPage = Some newPage }
            let cmd = Cmd.map SeriesDetailMsg pageCmd
            match extMsg with
            | Pages.SeriesDetail.Types.NoOp -> model', cmd
            | Pages.SeriesDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.SeriesDetail.Types.RequestOpenAbandonModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenAbandonModal entryId)]
            | Pages.SeriesDetail.Types.RequestOpenDeleteModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Entry entryId))]
            | Pages.SeriesDetail.Types.ShowNotification (msg, isSuccess) -> model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // API result handlers
    | FriendSaved friend ->
        let updatedFriends =
            match model.Friends with
            | Success friends ->
                let exists = friends |> List.exists (fun f -> f.Id = friend.Id)
                if exists then
                    friends |> List.map (fun f -> if f.Id = friend.Id then friend else f) |> Success
                else
                    (friend :: friends) |> Success
            | other -> other
        { model with Friends = updatedFriends },
        Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Friend saved", true))
            Cmd.ofMsg (FriendsMsg Pages.Friends.Types.LoadFriends)
        ]

    | FriendDeleted friendId ->
        let updatedFriends =
            model.Friends |> RemoteData.map (List.filter (fun f -> f.Id <> friendId))
        { model with Friends = updatedFriends },
        Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Friend deleted", true))
            Cmd.ofMsg (FriendsMsg Pages.Friends.Types.LoadFriends)
        ]

    | TagSaved tag ->
        let updatedTags =
            match model.Tags with
            | Success tags ->
                let exists = tags |> List.exists (fun t -> t.Id = tag.Id)
                if exists then
                    tags |> List.map (fun t -> if t.Id = tag.Id then tag else t) |> Success
                else
                    (tag :: tags) |> Success
            | other -> other
        { model with Tags = updatedTags },
        Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Tag saved", true))
            Cmd.ofMsg (TagsMsg Pages.Tags.Types.LoadTags)
        ]

    | TagDeleted tagId ->
        let updatedTags =
            model.Tags |> RemoteData.map (List.filter (fun t -> t.Id <> tagId))
        { model with Tags = updatedTags },
        Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Tag deleted", true))
            Cmd.ofMsg (TagsMsg Pages.Tags.Types.LoadTags)
        ]

    | EntryAbandoned _ ->
        model, Cmd.ofMsg (ShowNotification ("Entry abandoned", true))

    | EntryDeleted _ ->
        // Clear the library page cache so it reloads fresh data
        { model with LibraryPage = None; HomePage = None },
        Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Entry deleted", true))
            Cmd.ofMsg (NavigateTo LibraryPage)
        ]

    | EntryAdded _ ->
        model, Cmd.batch [
            Cmd.ofMsg (ShowNotification ("Added to library", true))
            Cmd.ofMsg (HomeMsg Pages.Home.Types.LoadLibrary)
        ]

    // Page messages - Cache
    | CacheMsg cacheMsg ->
        match model.CachePage with
        | Some pageModel ->
            let cacheApi : Pages.Cache.State.CacheApi = {
                GetEntries = fun () -> Api.api.cacheGetEntries ()
                GetStats = fun () -> Api.api.cacheGetStats ()
                ClearAll = fun () -> Api.api.cacheClearAll ()
                ClearExpired = fun () -> Api.api.cacheClearExpired ()
            }
            let newPage, pageCmd, extMsg = Pages.Cache.State.update cacheApi cacheMsg pageModel
            let model' = { model with CachePage = Some newPage }
            let cmd = Cmd.map CacheMsg pageCmd
            match extMsg with
            | Pages.Cache.Types.NoOp -> model', cmd
            | Pages.Cache.Types.ShowNotification (msg, isSuccess) -> model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none
