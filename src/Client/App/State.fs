module App.State

open Elmish
open Common.Types
open Common.Routing
open Shared.Domain
open Types

// Note: This file provides the structure for the App state management.
// The actual API calls would need to be wired up with the real Api module.

/// Helper to update an entry in a list of entries
let private updateEntryInList (updatedEntry: LibraryEntry) (entries: LibraryEntry list) =
    entries |> List.map (fun e -> if e.Id = updatedEntry.Id then updatedEntry else e)

/// Helper to sync an updated entry to LibraryPage and HomePage models
let private syncEntryToPages (entry: LibraryEntry) (model: Model) =
    let updatedLibraryPage =
        model.LibraryPage |> Option.map (fun lp ->
            { lp with Entries = lp.Entries |> RemoteData.map (updateEntryInList entry) })
    let updatedHomePage =
        model.HomePage |> Option.map (fun hp ->
            { hp with Library = hp.Library |> RemoteData.map (updateEntryInList entry) })
    { model with LibraryPage = updatedLibraryPage; HomePage = updatedHomePage }

/// Initialize a page based on page type (used by both NavigateTo and UrlChanged)
/// For slug-based pages, dispatches commands to load entities by slug
let private initializePage (page: Page) (model: Model) : Model * Cmd<Msg> =
    let model' = { model with CurrentPage = page }
    match page with
    | HomePage ->
        match model.HomePage with
        | None ->
            // First time loading - full init
            let pageModel, pageCmd = Pages.Home.State.init ()
            { model' with HomePage = Some pageModel }, Cmd.map HomeMsg pageCmd
        | Some _ ->
            // Already loaded - reload library to get fresh data (e.g., after episode updates)
            // and trigger sync check
            let cmds = Cmd.batch [
                Cmd.map HomeMsg (Cmd.ofMsg Pages.Home.Types.LoadLibrary)
                Cmd.map HomeMsg (Cmd.ofMsg Pages.Home.Types.CheckTraktSync)
            ]
            model', cmds
    | LibraryPage when model.LibraryPage.IsNone ->
        let pageModel, pageCmd = Pages.Library.State.init ()
        { model' with LibraryPage = Some pageModel }, Cmd.map LibraryMsg pageCmd
    | FriendsPage when model.FriendsPage.IsNone ->
        let pageModel, pageCmd = Pages.Friends.State.init ()
        { model' with FriendsPage = Some pageModel }, Cmd.map FriendsMsg pageCmd
    | MovieDetailPage slug ->
        // Load entry by slug
        model', Cmd.ofMsg (LoadEntryBySlug (slug, true))
    | SeriesDetailPage slug ->
        // Load entry by slug
        model', Cmd.ofMsg (LoadEntryBySlug (slug, false))
    | SessionDetailPage slug ->
        // Load session by slug
        model', Cmd.ofMsg (LoadSessionBySlug slug)
    | FriendDetailPage slug ->
        // Load friend by slug
        model', Cmd.ofMsg (LoadFriendBySlug slug)
    | ContributorsPage ->
        // Always reload contributors to get fresh data after tracking/untracking
        let pageModel, pageCmd = Pages.Contributors.State.init ()
        { model' with ContributorsPage = Some pageModel }, Cmd.map ContributorsMsg pageCmd
    | CachePage when model.CachePage.IsNone ->
        let pageModel, pageCmd = Pages.Cache.State.init ()
        { model' with CachePage = Some pageModel }, Cmd.map CacheMsg pageCmd
    | CollectionsPage when model.CollectionsPage.IsNone ->
        let pageModel, pageCmd = Pages.Collections.State.init ()
        { model' with CollectionsPage = Some pageModel }, Cmd.map CollectionsMsg pageCmd
    | CollectionDetailPage slug ->
        // Load collection by slug
        model', Cmd.ofMsg (LoadCollectionBySlug slug)
    | ContributorDetailPage (slug, Some personId) ->
        // Untracked contributor - use TMDB ID directly
        if model.ContributorDetailPage.IsNone || model.ContributorDetailPage |> Option.map (fun m -> m.TmdbPersonId <> personId) |> Option.defaultValue true then
            let pageModel, pageCmd = Pages.ContributorDetail.State.init personId
            { model' with ContributorDetailPage = Some pageModel }, Cmd.map ContributorDetailMsg pageCmd
        else
            model', Cmd.none
    | ContributorDetailPage (slug, None) ->
        // Tracked contributor - look up by slug
        model', Cmd.ofMsg (LoadContributorBySlug slug)
    | StatsPage ->
        // Always refresh stats when navigating to the page
        let pageModel, pageCmd = Pages.Stats.State.init ()
        { model' with StatsPage = Some pageModel }, Cmd.map StatsMsg pageCmd
    | YearInReviewPage (year, viewMode) ->
        // Always refresh year-in-review when navigating to the page
        let pageModel, pageCmd = Pages.YearInReview.State.init year viewMode
        { model' with YearInReviewPage = Some pageModel }, Cmd.map YearInReviewMsg pageCmd
    | TimelinePage ->
        // Always refresh timeline when navigating to the page
        let pageModel, pageCmd = Pages.Timeline.State.init ()
        { model' with TimelinePage = Some pageModel }, Cmd.map TimelineMsg pageCmd
    | ImportPage ->
        // Always refresh import page when navigating to it
        let pageModel, pageCmd = Pages.Import.State.init ()
        { model' with ImportPage = Some pageModel }, Cmd.map ImportMsg pageCmd
    | GenericImportPage ->
        // Always refresh generic import page when navigating to it
        let pageModel, pageCmd = Pages.GenericImport.State.init ()
        { model' with GenericImportPage = Some pageModel }, Cmd.map GenericImportMsg pageCmd
    | GraphPage focus ->
        // Always refresh graph when navigating to the page
        let pageModel, pageCmd = Pages.Graph.State.initWithFocus focus
        { model' with GraphPage = Some pageModel }, Cmd.map GraphMsg pageCmd
    | StyleguidePage ->
        let pageModel, pageCmd = Pages.Styleguide.State.init ()
        { model' with StyleguidePage = Some pageModel }, Cmd.map StyleguideMsg pageCmd
    | _ -> model', Cmd.none

/// Initialize movie detail page with an entry that's already been loaded
let private initializeMovieDetailWithEntry (entry: LibraryEntry) (model: Model) : Model * Cmd<Msg> =
    if model.MovieDetailPage.IsNone || model.MovieDetailPage |> Option.map (fun m -> m.EntryId <> entry.Id) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.MovieDetail.State.init entry.Id
        { model with MovieDetailPage = Some pageModel }, Cmd.map MovieDetailMsg pageCmd
    else
        // Page already exists - refresh tracked contributors in case they changed
        model, Cmd.map MovieDetailMsg (Cmd.ofMsg Pages.MovieDetail.Types.LoadTrackedContributors)

/// Initialize series detail page with an entry that's already been loaded
let private initializeSeriesDetailWithEntry (entry: LibraryEntry) (model: Model) : Model * Cmd<Msg> =
    if model.SeriesDetailPage.IsNone || model.SeriesDetailPage |> Option.map (fun m -> m.EntryId <> entry.Id) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.SeriesDetail.State.init entry.Id
        { model with SeriesDetailPage = Some pageModel }, Cmd.map SeriesDetailMsg pageCmd
    else
        // Page already exists - refresh tracked contributors in case they changed
        model, Cmd.map SeriesDetailMsg (Cmd.ofMsg Pages.SeriesDetail.Types.LoadTrackedContributors)

/// Initialize friend detail page with a friend that's already been loaded
let private initializeFriendDetailWithFriend (friend: Friend) (model: Model) : Model * Cmd<Msg> =
    if model.FriendDetailPage.IsNone || model.FriendDetailPage |> Option.map (fun m -> m.FriendId <> friend.Id) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.FriendDetail.State.init friend.Id
        { model with FriendDetailPage = Some pageModel }, Cmd.map FriendDetailMsg pageCmd
    else
        model, Cmd.none

/// Initialize session detail page with a session that's already been loaded
let private initializeSessionDetailWithSession (sessionWithProgress: WatchSessionWithProgress) (model: Model) : Model * Cmd<Msg> =
    if model.SessionDetailPage.IsNone || model.SessionDetailPage |> Option.map (fun m -> m.SessionId <> sessionWithProgress.Session.Id) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.SessionDetail.State.init sessionWithProgress.Session.Id
        { model with SessionDetailPage = Some pageModel }, Cmd.map SessionDetailMsg pageCmd
    else
        model, Cmd.none

/// Initialize collection detail page with a collection that's already been loaded
let private initializeCollectionDetailWithCollection (collectionWithItems: CollectionWithItems) (model: Model) : Model * Cmd<Msg> =
    if model.CollectionDetailPage.IsNone || model.CollectionDetailPage |> Option.map (fun m -> m.CollectionId <> collectionWithItems.Collection.Id) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.CollectionDetail.State.init collectionWithItems.Collection.Id
        { model with CollectionDetailPage = Some pageModel }, Cmd.map CollectionDetailMsg pageCmd
    else
        model, Cmd.none

/// Initialize contributor detail page with a contributor that's already been loaded
let private initializeContributorDetailWithContributor (contributor: TrackedContributor) (model: Model) : Model * Cmd<Msg> =
    if model.ContributorDetailPage.IsNone || model.ContributorDetailPage |> Option.map (fun m -> m.TmdbPersonId <> contributor.TmdbPersonId) |> Option.defaultValue true then
        let pageModel, pageCmd = Pages.ContributorDetail.State.init contributor.TmdbPersonId
        { model with ContributorDetailPage = Some pageModel }, Cmd.map ContributorDetailMsg pageCmd
    else
        model, Cmd.none

let init () : Model * Cmd<Msg> =
    // Parse the initial URL to determine starting page
    let initialPage = Router.parseCurrentUrl ()
    let model = { Model.empty with CurrentPage = initialPage }
    let cmds = Cmd.batch [
        Cmd.ofMsg LoadFriends
        Cmd.ofMsg (LayoutMsg Components.Layout.Types.CheckHealth)
        // Use UrlChanged for initial page to avoid pushing URL again
        Cmd.ofMsg (UrlChanged initialPage)
    ]
    model, cmds

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    // Navigation - push URL to browser history
    | NavigateTo page ->
        // Push the URL to browser history
        Router.pushUrl (Page.toUrl page)
        // Initialize the page
        initializePage page model

    // URL changed (from browser back/forward) - don't push URL
    | UrlChanged page ->
        initializePage page model

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
        // Get library entries from HomePage or LibraryPage (prefer loaded data)
        let libraryEntries =
            model.HomePage
            |> Option.bind (fun hp -> hp.Library |> RemoteData.toOption)
            |> Option.orElse (model.LibraryPage |> Option.bind (fun lp -> lp.Entries |> RemoteData.toOption))
            |> Option.defaultValue []
        let modalModel = Components.SearchModal.State.init libraryEntries
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
            | Components.SearchModal.Types.TmdbItemSelected item ->
                // Close modal and add directly to library
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (AddTmdbItemDirectly item)]
            | Components.SearchModal.Types.LibraryItemSelected (_, mediaType, title) ->
                let slug = Slug.generate title
                let page =
                    match mediaType with
                    | MediaType.Movie -> MovieDetailPage slug
                    | MediaType.Series -> SeriesDetailPage slug
                { model' with Modal = NoModal }, Cmd.ofMsg (NavigateTo page)
            | Components.SearchModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | AddTmdbItemDirectly item ->
        // Add the item directly to library without showing the modal
        let addCmd =
            match item.MediaType with
            | MediaType.Movie ->
                let request : AddMovieRequest = {
                    TmdbId = TmdbMovieId item.TmdbId
                    WhyAdded = None
                    InitialFriends = []
                }
                Cmd.OfAsync.either
                    Api.api.libraryAddMovie
                    request
                    (fun result -> TmdbItemAddResult (result, MediaType.Movie))
                    (fun ex -> TmdbItemAddResult (Error ex.Message, MediaType.Movie))
            | MediaType.Series ->
                let request : AddSeriesRequest = {
                    TmdbId = TmdbSeriesId item.TmdbId
                    WhyAdded = None
                    InitialFriends = []
                }
                Cmd.OfAsync.either
                    Api.api.libraryAddSeries
                    request
                    (fun result -> TmdbItemAddResult (result, MediaType.Series))
                    (fun ex -> TmdbItemAddResult (Error ex.Message, MediaType.Series))
        model, addCmd

    | TmdbItemAddResult (Ok entry, mediaType) ->
        // Navigate to the detail page
        let slug =
            match entry.Media with
            | LibraryMovie m -> Slug.forMovie m.Title m.ReleaseDate
            | LibrarySeries s -> Slug.forSeries s.Name s.FirstAirDate
        let page =
            match entry.Media with
            | LibraryMovie _ -> MovieDetailPage slug
            | LibrarySeries _ -> SeriesDetailPage slug
        // Clear library/home page caches so they reload with new entry
        { model with LibraryPage = None; HomePage = None },
        Cmd.ofMsg (NavigateTo page)

    | TmdbItemAddResult (Error err, _) ->
        model, Cmd.ofMsg (ShowNotification (err, false))

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
                    | Components.ConfirmModal.Types.Entry entryId ->
                        Cmd.OfAsync.either
                            Api.api.libraryDeleteEntry
                            entryId
                            (fun _ -> EntryDeleted entryId)
                            (fun ex -> ShowNotification (ex.Message, false))
                    | Components.ConfirmModal.Types.Collection (collection, _) ->
                        Cmd.OfAsync.either
                            Api.api.collectionsDelete
                            collection.Id
                            (fun _ -> CollectionDeleted collection.Id)
                            (fun ex -> ShowNotification (ex.Message, false))
                { model' with Modal = NoModal }, Cmd.batch [cmd; deleteCmd]
            | Components.ConfirmModal.Types.Cancelled ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenWatchSessionModal entryId ->
        let modalModel = Components.WatchSessionModal.State.init entryId
        { model with Modal = WatchSessionModal modalModel }, Cmd.none

    | WatchSessionModalMsg sessionModalMsg ->
        match model.Modal with
        | WatchSessionModal modalModel ->
            let sessionApi : Components.WatchSessionModal.State.Api = {
                CreateSession = fun req -> Api.api.sessionsCreate req
                CreateFriend = fun req -> Api.api.friendsCreate req
            }
            let newModal, modalCmd, extMsg = Components.WatchSessionModal.State.update sessionApi sessionModalMsg modalModel
            let model' = { model with Modal = WatchSessionModal newModal }
            let cmd = Cmd.map WatchSessionModalMsg modalCmd
            // Check if a friend was just created and update the global friends list
            let model'' =
                match sessionModalMsg with
                | Components.WatchSessionModal.Types.FriendCreated (Ok friend) ->
                    let updatedFriends =
                        match model'.Friends with
                        | Success friends -> Success (friend :: friends)
                        | other -> other
                    { model' with Friends = updatedFriends }
                | _ -> model'
            match extMsg with
            | Components.WatchSessionModal.Types.NoOp -> model'', cmd
            | Components.WatchSessionModal.Types.Created session ->
                { model'' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (SessionCreated session)]
            | Components.WatchSessionModal.Types.CloseRequested ->
                { model'' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenMovieWatchSessionModal entryId ->
        let modalModel = Components.MovieWatchSessionModal.State.init entryId
        { model with Modal = MovieWatchSessionModal modalModel }, Cmd.none

    | EditMovieWatchSessionModal session ->
        let modalModel = Components.MovieWatchSessionModal.State.initEdit session
        { model with Modal = MovieWatchSessionModal modalModel }, Cmd.none

    | MovieWatchSessionModalMsg movieSessionModalMsg ->
        match model.Modal with
        | MovieWatchSessionModal modalModel ->
            let sessionApi : Components.MovieWatchSessionModal.State.Api = {
                CreateSession = fun req -> Api.api.movieSessionsCreate req
                UpdateSession = fun req -> Api.api.movieSessionsUpdate req
                CreateFriend = fun req -> Api.api.friendsCreate req
            }
            let newModal, modalCmd, extMsg = Components.MovieWatchSessionModal.State.update sessionApi movieSessionModalMsg modalModel
            let model' = { model with Modal = MovieWatchSessionModal newModal }
            let cmd = Cmd.map MovieWatchSessionModalMsg modalCmd
            match extMsg with
            | Components.MovieWatchSessionModal.Types.NoOp -> model', cmd
            | Components.MovieWatchSessionModal.Types.Created session ->
                // Reload watch sessions on the MovieDetail page
                let reloadMovieDetailCmd =
                    match model'.MovieDetailPage with
                    | Some _ -> Cmd.ofMsg (MovieDetailMsg Pages.MovieDetail.Types.LoadWatchSessions)
                    | None -> Cmd.none
                // Invalidate FriendDetail page cache so it reloads fresh data on next visit
                { model' with Modal = NoModal; FriendDetailPage = None }, Cmd.batch [cmd; reloadMovieDetailCmd]
            | Components.MovieWatchSessionModal.Types.Updated session ->
                // Reload watch sessions on the MovieDetail page
                let reloadMovieDetailCmd =
                    match model'.MovieDetailPage with
                    | Some _ -> Cmd.ofMsg (MovieDetailMsg Pages.MovieDetail.Types.LoadWatchSessions)
                    | None -> Cmd.none
                // Invalidate FriendDetail page cache so it reloads fresh data on next visit
                { model' with Modal = NoModal; FriendDetailPage = None }, Cmd.batch [cmd; reloadMovieDetailCmd]
            | Components.MovieWatchSessionModal.Types.FriendCreatedInline friend ->
                let updatedFriends =
                    match model'.Friends with
                    | Success friends -> Success (friend :: friends)
                    | other -> other
                { model' with Friends = updatedFriends }, cmd
            | Components.MovieWatchSessionModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenCollectionModal collection ->
        let modalModel = Components.CollectionModal.State.init collection
        { model with Modal = CollectionModal modalModel }, Cmd.none

    | CollectionModalMsg collectionMsg ->
        match model.Modal with
        | CollectionModal modalModel ->
            let saveApi : Components.CollectionModal.State.SaveApi = {
                Create = fun req -> Api.api.collectionsCreate req
                Update = fun req -> Api.api.collectionsUpdate req
            }
            let newModal, modalCmd, extMsg = Components.CollectionModal.State.update saveApi collectionMsg modalModel
            let model' = { model with Modal = CollectionModal newModal }
            let cmd = Cmd.map CollectionModalMsg modalCmd
            match extMsg with
            | Components.CollectionModal.Types.NoOp -> model', cmd
            | Components.CollectionModal.Types.Saved collection ->
                { model' with Modal = NoModal }, Cmd.batch [cmd; Cmd.ofMsg (CollectionSaved collection)]
            | Components.CollectionModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenAddToCollectionModal (itemRef, title) ->
        let modalModel, modalCmd = Components.AddToCollectionModal.State.init itemRef title
        { model with Modal = AddToCollectionModal modalModel }, Cmd.map AddToCollectionModalMsg modalCmd

    | AddToCollectionModalMsg addToCollectionMsg ->
        match model.Modal with
        | AddToCollectionModal modalModel ->
            let api : Components.AddToCollectionModal.State.Api = {
                GetCollections = fun () -> Api.api.collectionsGetAll ()
                GetCollectionsForItem = fun itemRef -> Api.api.collectionsGetForItem itemRef
                AddToCollection = fun (collectionId, itemRef, notes) -> Api.api.collectionsAddItem (collectionId, itemRef, notes)
                RemoveFromCollection = fun (collectionId, itemRef) -> Api.api.collectionsRemoveItem (collectionId, itemRef)
                CreateCollection = fun req -> Api.api.collectionsCreate req
            }
            let newModal, modalCmd, extMsg = Components.AddToCollectionModal.State.update api addToCollectionMsg modalModel
            let model' = { model with Modal = AddToCollectionModal newModal }
            let cmd = Cmd.map AddToCollectionModalMsg modalCmd
            match extMsg with
            | Components.AddToCollectionModal.Types.NoOp -> model', cmd
            | Components.AddToCollectionModal.Types.CollectionsUpdated ->
                // Reload collections in the active detail page
                let reloadCmd =
                    match model'.MovieDetailPage, model'.SeriesDetailPage with
                    | Some _, _ -> Cmd.ofMsg (MovieDetailMsg Pages.MovieDetail.Types.LoadCollections)
                    | _, Some _ -> Cmd.ofMsg (SeriesDetailMsg Pages.SeriesDetail.Types.LoadCollections)
                    | _ -> Cmd.none
                { model' with Modal = NoModal; CollectionsPage = None },
                Cmd.batch [cmd; reloadCmd]
            | Components.AddToCollectionModal.Types.CloseRequested ->
                { model' with Modal = NoModal }, cmd
        | _ -> model, Cmd.none

    | OpenProfileImageModal friend ->
        let modalModel = Components.ProfileImageEditor.State.init friend.AvatarUrl
        { model with Modal = ProfileImageModal (modalModel, friend.Id) }, Cmd.none

    | ProfileImageModalMsg profileMsg ->
        match model.Modal with
        | ProfileImageModal (modalModel, friendId) ->
            let newModal, _, extMsg = Components.ProfileImageEditor.State.update profileMsg modalModel
            let model' = { model with Modal = ProfileImageModal (newModal, friendId) }
            match extMsg with
            | Components.ProfileImageEditor.Types.NoOp -> model', Cmd.none
            | Components.ProfileImageEditor.Types.Confirmed base64Image ->
                { model' with Modal = NoModal }, Cmd.ofMsg (ProfileImageConfirmed (friendId, base64Image))
            | Components.ProfileImageEditor.Types.Cancelled ->
                { model' with Modal = NoModal }, Cmd.none
        | _ -> model, Cmd.none

    | ProfileImageConfirmed (friendId, base64Image) ->
        // Update the friend with the new avatar
        let updateRequest : UpdateFriendRequest = {
            Id = friendId
            Name = None
            Nickname = None
            AvatarBase64 = Some base64Image
        }
        let updateCmd =
            Cmd.OfAsync.either
                Api.api.friendsUpdate
                updateRequest
                (fun result ->
                    match result with
                    | Ok friend -> FriendSaved friend
                    | Error err -> ShowNotification (err, false))
                (fun ex -> ShowNotification (ex.Message, false))
        { model with Modal = NoModal }, updateCmd

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
            let homeApi: Pages.Home.State.HomeApi = {
                GetLibrary = fun () -> Api.api.libraryGetAll ()
                GetTraktSyncStatus = fun () -> Api.api.traktGetSyncStatus ()
                TraktIncrementalSync = fun () -> Api.api.traktIncrementalSync ()
                TmdbHealthCheck = fun () -> Api.api.tmdbHealthCheck ()
            }
            let newPage, pageCmd, extMsg = Pages.Home.State.update homeApi homeMsg pageModel
            let model' = { model with HomePage = Some newPage }
            let cmd = Cmd.map HomeMsg pageCmd
            match extMsg with
            | Pages.Home.Types.NoOp -> model', cmd
            | Pages.Home.Types.NavigateToLibrary -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.Home.Types.NavigateToMovieDetail (_, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.Home.Types.NavigateToSeriesDetail (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.Home.Types.NavigateToYearInReview ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (YearInReviewPage (None, YearInReviewViewMode.Overview)))]
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
            | Pages.Library.Types.NavigateToMovieDetail (_, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.Library.Types.NavigateToSeriesDetail (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
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
            | Pages.Friends.Types.NavigateToFriendDetail (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (FriendDetailPage slug))]
            | Pages.Friends.Types.RequestOpenAddModal -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenFriendModal None)]
            | Pages.Friends.Types.RequestOpenDeleteModal friend -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Friend friend))]
            | Pages.Friends.Types.RequestOpenProfileImageModal friend -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenProfileImageModal friend)]
        | None -> model, Cmd.none

    // Page messages - FriendDetail
    | FriendDetailMsg friendDetailMsg ->
        match model.FriendDetailPage with
        | Some pageModel ->
            let friendDetailApi : Pages.FriendDetail.State.FriendDetailApi = {
                GetEntries = fun friendId -> Api.api.friendsGetWatchedWith (FriendId.value friendId)
                UpdateFriend = fun req -> Api.api.friendsUpdate req
            }
            let newPage, pageCmd, extMsg = Pages.FriendDetail.State.update friendDetailApi friendDetailMsg pageModel
            let model' = { model with FriendDetailPage = Some newPage }
            let cmd = Cmd.map FriendDetailMsg pageCmd
            match extMsg with
            | Pages.FriendDetail.Types.NoOp -> model', cmd
            | Pages.FriendDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo FriendsPage)]
            | Pages.FriendDetail.Types.NavigateToMovieDetail (_, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.FriendDetail.Types.NavigateToSeriesDetail (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.FriendDetail.Types.NavigateToGraphWithFocus friendId ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (GraphPage (Some (FocusedFriend friendId))))]
            | Pages.FriendDetail.Types.RequestOpenProfileImageModal friend ->
                model', Cmd.batch [cmd; Cmd.ofMsg (OpenProfileImageModal friend)]
            | Pages.FriendDetail.Types.FriendUpdated friend ->
                // Update the global friends list
                let updatedFriends =
                    model'.Friends
                    |> RemoteData.map (List.map (fun f -> if f.Id = friend.Id then friend else f))
                { model' with Friends = updatedFriends }, cmd
        | None -> model, Cmd.none

    // Page messages - Contributors
    | ContributorsMsg contributorsMsg ->
        match model.ContributorsPage with
        | Some pageModel ->
            let contributorsApi : Pages.Contributors.State.ContributorsApi = {
                GetAll = fun () -> Api.api.contributorsGetAll ()
                Untrack = fun trackedId -> Api.api.contributorsUntrack trackedId
            }
            let newPage, pageCmd, extMsg = Pages.Contributors.State.update contributorsApi contributorsMsg pageModel
            let model' = { model with ContributorsPage = Some newPage }
            let cmd = Cmd.map ContributorsMsg pageCmd
            match extMsg with
            | Pages.Contributors.Types.NoOp -> model', cmd
            | Pages.Contributors.Types.NavigateToContributorDetail (_, name) ->
                // From tracked contributors list - use slug only (no ID needed)
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (ContributorDetailPage (slug, None)))]
            | Pages.Contributors.Types.ShowNotification (msg, isSuccess) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - Collections
    | CollectionsMsg collectionsMsg ->
        match model.CollectionsPage with
        | Some pageModel ->
            let collectionsApi : Pages.Collections.State.CollectionsApi = {
                GetAll = fun () -> Api.api.collectionsGetAll ()
                Create = fun req -> Api.api.collectionsCreate req
            }
            let newPage, pageCmd, extMsg = Pages.Collections.State.update collectionsApi collectionsMsg pageModel
            let model' = { model with CollectionsPage = Some newPage }
            let cmd = Cmd.map CollectionsMsg pageCmd
            match extMsg with
            | Pages.Collections.Types.NoOp -> model', cmd
            | Pages.Collections.Types.NavigateToCollectionDetail slug ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (CollectionDetailPage slug))]
            | Pages.Collections.Types.RequestOpenDeleteModal collection ->
                // Fetch item count first, then show confirm modal
                let fetchCmd =
                    Cmd.OfAsync.perform
                        Api.api.collectionsGetById
                        collection.Id
                        (fun result ->
                            match result with
                            | Ok collectionWithItems ->
                                let itemCount = List.length collectionWithItems.Items
                                OpenConfirmDeleteModal (Components.ConfirmModal.Types.Collection (collection, itemCount))
                            | Error _ ->
                                // Assume 0 items if fetch fails
                                OpenConfirmDeleteModal (Components.ConfirmModal.Types.Collection (collection, 0))
                        )
                model', Cmd.batch [cmd; fetchCmd]
            | Pages.Collections.Types.ShowNotification (msg, isSuccess) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - CollectionDetail
    | CollectionDetailMsg collectionDetailMsg ->
        match model.CollectionDetailPage with
        | Some pageModel ->
            let collectionApi : Pages.CollectionDetail.State.CollectionApi = {
                GetCollection = fun collectionId -> Api.api.collectionsGetById collectionId
                GetProgress = fun collectionId -> Api.api.collectionsGetProgress collectionId
                RemoveItem = fun (collectionId, itemRef) -> Api.api.collectionsRemoveItem (collectionId, itemRef)
                ReorderItems = fun (collectionId, itemRefs) -> Api.api.collectionsReorderItems (collectionId, itemRefs)
                UpdateCollection = fun request -> Api.api.collectionsUpdate request
            }
            let newPage, pageCmd, extMsg = Pages.CollectionDetail.State.update collectionApi collectionDetailMsg pageModel
            let model' = { model with CollectionDetailPage = Some newPage }
            let cmd = Cmd.map CollectionDetailMsg pageCmd
            match extMsg with
            | Pages.CollectionDetail.Types.NoOp -> model', cmd
            | Pages.CollectionDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo CollectionsPage)]
            | Pages.CollectionDetail.Types.NavigateToMovieDetail (_, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.CollectionDetail.Types.NavigateToSeriesDetail (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.CollectionDetail.Types.NavigateToSeriesByName (seriesName, firstAirDate) ->
                let slug = Slug.forSeries seriesName firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.CollectionDetail.Types.NavigateToGraphWithFocus collectionId ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (GraphPage (Some (FocusedCollection collectionId))))]
            | Pages.CollectionDetail.Types.ShowNotification (msg, isSuccess) ->
                if isSuccess then model', cmd
                else model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, false))]
        | None -> model, Cmd.none

    // Page messages - MovieDetail
    | MovieDetailMsg movieMsg ->
        match model.MovieDetailPage with
        | Some pageModel ->
            let movieApi : Pages.MovieDetail.State.MovieApi = {
                GetEntry = fun entryId -> async {
                    let! result = Api.api.libraryGetById entryId
                    match result with
                    | Ok entry -> return Some entry
                    | Error _ -> return None
                }
                GetCollections = fun entryId -> Api.api.collectionsGetForEntry entryId
                GetCredits = fun tmdbId -> Api.api.tmdbGetMovieCredits tmdbId
                GetTrackedContributors = fun () -> Api.api.contributorsGetAll ()
                GetWatchSessions = fun entryId -> Api.api.movieSessionsGetForEntry entryId
                CreateWatchSession = fun request -> Api.api.movieSessionsCreate request
                DeleteWatchSession = fun sessionId -> Api.api.movieSessionsDelete sessionId
                UpdateWatchSessionDate = fun request -> Api.api.movieSessionsUpdateDate request
                MarkWatched = fun entryId -> Api.api.libraryMarkMovieWatched (entryId, None)
                MarkUnwatched = fun entryId -> Api.api.libraryMarkMovieUnwatched entryId
                Resume = fun entryId -> Api.api.libraryResumeEntry entryId
                SetRating = fun (entryId, ratingInt) ->
                    let rating = ratingInt |> Option.bind PersonalRating.fromInt
                    Api.api.librarySetRating (entryId, rating)
                UpdateNotes = fun (entryId, notes) -> Api.api.libraryUpdateNotes (entryId, notes)
                ToggleFriend = fun (entryId, friendId) -> Api.api.libraryToggleFriend (entryId, friendId)
                CreateFriend = fun request -> Api.api.friendsCreate request
            }
            let newPage, pageCmd, extMsg = Pages.MovieDetail.State.update movieApi movieMsg pageModel
            let model' = { model with MovieDetailPage = Some newPage }
            let cmd = Cmd.map MovieDetailMsg pageCmd
            match extMsg with
            | Pages.MovieDetail.Types.NoOp -> model', cmd
            | Pages.MovieDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.MovieDetail.Types.NavigateToContributor (personId, name, isTracked) ->
                let slug = Slug.generate name
                let page =
                    if isTracked then ContributorDetailPage (slug, None)  // Tracked: slug only
                    else ContributorDetailPage (slug, Some personId)  // Untracked: slug + ID
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo page)]
            | Pages.MovieDetail.Types.NavigateToFriendDetail (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (FriendDetailPage slug))]
            | Pages.MovieDetail.Types.NavigateToCollectionDetail (_, name) ->
                let slug = Slug.forCollection name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (CollectionDetailPage slug))]
            | Pages.MovieDetail.Types.NavigateToGraphWithFocus entryId ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (GraphPage (Some (FocusedMovie entryId))))]
            | Pages.MovieDetail.Types.RequestOpenAbandonModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenAbandonModal entryId)]
            | Pages.MovieDetail.Types.RequestOpenDeleteModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Entry entryId))]
            | Pages.MovieDetail.Types.RequestOpenAddToCollectionModal (entryId, title) -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenAddToCollectionModal (entryId, title))]
            | Pages.MovieDetail.Types.RequestOpenMovieWatchSessionModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenMovieWatchSessionModal entryId)]
            | Pages.MovieDetail.Types.RequestEditMovieWatchSession session -> model', Cmd.batch [cmd; Cmd.ofMsg (EditMovieWatchSessionModal session)]
            | Pages.MovieDetail.Types.ShowNotification (msg, isSuccess) ->
                if isSuccess then model', cmd
                else model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, false))]
            | Pages.MovieDetail.Types.EntryUpdated entry ->
                let model'' = syncEntryToPages entry model'
                model'', cmd
            | Pages.MovieDetail.Types.FriendCreatedInline friend ->
                // Update global friends list
                let updatedFriends =
                    match model'.Friends with
                    | Success friends -> Success (friend :: friends)
                    | other -> other
                { model' with Friends = updatedFriends }, cmd
            | Pages.MovieDetail.Types.MovieWatchSessionRemoved ->
                // Invalidate FriendDetail page cache so it reloads fresh data on next visit
                { model' with FriendDetailPage = None }, cmd
        | None -> model, Cmd.none

    // Page messages - SeriesDetail
    | SeriesDetailMsg seriesMsg ->
        match model.SeriesDetailPage with
        | Some pageModel ->
            let seriesApi : Pages.SeriesDetail.State.SeriesApi = {
                GetEntry = fun entryId -> async {
                    let! result = Api.api.libraryGetById entryId
                    match result with
                    | Ok entry -> return Some entry
                    | Error _ -> return None
                }
                GetCollections = fun entryId -> Api.api.collectionsGetForEntry entryId
                GetCredits = fun tmdbId -> Api.api.tmdbGetSeriesCredits tmdbId
                GetTrackedContributors = fun () -> Api.api.contributorsGetAll ()
                GetSessions = fun entryId -> Api.api.sessionsGetForEntry entryId
                GetSessionProgress = fun sessionId -> Api.api.sessionsGetProgress sessionId
                GetOverallProgress = fun entryId -> Api.api.sessionsGetOverallProgress entryId
                GetSeasonDetails = fun (tmdbId, seasonNum) -> Api.api.tmdbGetSeasonDetails (tmdbId, seasonNum)
                MarkCompleted = fun entryId -> Api.api.libraryMarkSeriesCompleted entryId
                Abandon = fun entryId -> Api.api.libraryAbandonEntry (entryId, { Reason = None; AbandonedAtSeason = None; AbandonedAtEpisode = None })
                Resume = fun entryId -> Api.api.libraryResumeEntry entryId
                SetRating = fun (entryId, ratingInt) ->
                    let rating = ratingInt |> Option.bind PersonalRating.fromInt
                    Api.api.librarySetRating (entryId, rating)
                UpdateNotes = fun (entryId, notes) -> Api.api.libraryUpdateNotes (entryId, notes)
                ToggleFriend = fun (entryId, friendId) -> Api.api.libraryToggleFriend (entryId, friendId)
                CreateFriend = fun req -> Api.api.friendsCreate req
                ToggleEpisode = fun (sessionId, s, e, w) ->
                    Api.api.sessionsUpdateEpisodeProgress (sessionId, s, e, w)
                MarkSeasonWatched = fun (sessionId, s) ->
                    Api.api.sessionsMarkSeasonWatched (sessionId, s)
                DeleteSession = fun sessionId -> Api.api.sessionsDelete sessionId
                UpdateEpisodeWatchedDate = fun (sessionId, s, e, date) ->
                    Api.api.sessionsUpdateEpisodeWatchedDate (sessionId, s, e, date)
                ToggleSessionFriend = fun (sessionId, friendId) ->
                    Api.api.sessionsToggleFriend (sessionId, friendId)
            }
            let newPage, pageCmd, extMsg = Pages.SeriesDetail.State.update seriesApi seriesMsg pageModel
            let model' = { model with SeriesDetailPage = Some newPage }
            let cmd = Cmd.map SeriesDetailMsg pageCmd
            match extMsg with
            | Pages.SeriesDetail.Types.NoOp -> model', cmd
            | Pages.SeriesDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.SeriesDetail.Types.NavigateToContributor (personId, name, isTracked) ->
                let slug = Slug.generate name
                let page =
                    if isTracked then ContributorDetailPage (slug, None)  // Tracked: slug only
                    else ContributorDetailPage (slug, Some personId)  // Untracked: slug + ID
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo page)]
            | Pages.SeriesDetail.Types.NavigateToFriendDetail (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (FriendDetailPage slug))]
            | Pages.SeriesDetail.Types.NavigateToCollectionDetail (_, name) ->
                let slug = Slug.forCollection name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (CollectionDetailPage slug))]
            | Pages.SeriesDetail.Types.NavigateToGraphWithFocus entryId ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (GraphPage (Some (FocusedSeries entryId))))]
            | Pages.SeriesDetail.Types.RequestOpenDeleteModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenConfirmDeleteModal (Components.ConfirmModal.Types.Entry entryId))]
            | Pages.SeriesDetail.Types.RequestOpenAddToCollectionModal (entryId, title) -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenAddToCollectionModal (entryId, title))]
            | Pages.SeriesDetail.Types.RequestOpenNewSessionModal entryId -> model', Cmd.batch [cmd; Cmd.ofMsg (OpenWatchSessionModal entryId)]
            | Pages.SeriesDetail.Types.ShowNotification (msg, isSuccess) ->
                if isSuccess then model', cmd
                else model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, false))]
            | Pages.SeriesDetail.Types.EntryUpdated entry ->
                let model'' = syncEntryToPages entry model'
                model'', cmd
            | Pages.SeriesDetail.Types.FriendCreatedInline friend ->
                // Update global friends list
                let updatedFriends =
                    match model'.Friends with
                    | Success friends -> Success (friend :: friends)
                    | other -> other
                { model' with Friends = updatedFriends }, cmd
            | Pages.SeriesDetail.Types.WatchSessionRemoved ->
                // Invalidate FriendDetail page cache so it reloads fresh data on next visit
                { model' with FriendDetailPage = None }, cmd
        | None -> model, Cmd.none

    // Page messages - SessionDetail
    | SessionDetailMsg sessionMsg ->
        match model.SessionDetailPage with
        | Some pageModel ->
            let sessionApi : Pages.SessionDetail.State.SessionApi = {
                GetSession = fun sessionId -> Api.api.sessionsGetById sessionId
                UpdateSession = fun req -> Api.api.sessionsUpdate req
                DeleteSession = fun sessionId -> Api.api.sessionsDelete sessionId
                ToggleFriend = fun (sessionId, friendId) -> Api.api.sessionsToggleFriend (sessionId, friendId)
                CreateFriend = fun req -> Api.api.friendsCreate req
                ToggleEpisode = fun (sessionId, s, e, w) -> Api.api.sessionsUpdateEpisodeProgress (sessionId, s, e, w)
                MarkSeasonWatched = fun (sessionId, s) -> Api.api.sessionsMarkSeasonWatched (sessionId, s)
                GetSeasonDetails = fun (tmdbId, seasonNum) -> Api.api.tmdbGetSeasonDetails (tmdbId, seasonNum)
            }
            let newPage, pageCmd, extMsg = Pages.SessionDetail.State.update sessionApi sessionMsg pageModel
            let model' = { model with SessionDetailPage = Some newPage }
            let cmd = Cmd.map SessionDetailMsg pageCmd
            match extMsg with
            | Pages.SessionDetail.Types.NoOp -> model', cmd
            | Pages.SessionDetail.Types.NavigateBack -> model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.SessionDetail.Types.NavigateToSeries (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.SessionDetail.Types.ShowNotification (msg, isSuccess) ->
                if isSuccess then model', cmd
                else model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, false))]
            | Pages.SessionDetail.Types.SessionDeleted sessionId -> model', Cmd.batch [cmd; Cmd.ofMsg (SessionDeleted sessionId)]
            | Pages.SessionDetail.Types.FriendCreatedInline friend ->
                // Update global friends list
                let updatedFriends =
                    match model'.Friends with
                    | Success friends -> Success (friend :: friends)
                    | other -> other
                { model' with Friends = updatedFriends }, cmd
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
        Cmd.ofMsg (FriendsMsg Pages.Friends.Types.LoadFriends)

    | FriendDeleted friendId ->
        let updatedFriends =
            model.Friends |> RemoteData.map (List.filter (fun f -> f.Id <> friendId))
        { model with Friends = updatedFriends },
        Cmd.ofMsg (FriendsMsg Pages.Friends.Types.LoadFriends)

    | EntryAbandoned _ ->
        model, Cmd.none

    | EntryDeleted _ ->
        // Clear the library page cache so it reloads fresh data
        { model with LibraryPage = None; HomePage = None },
        Cmd.ofMsg (NavigateTo LibraryPage)

    | EntryAdded _ ->
        model, Cmd.ofMsg (HomeMsg Pages.Home.Types.LoadLibrary)

    | SessionCreated session ->
        // Reload sessions on the SeriesDetail page and select the new session
        let reloadAndSelectCmd =
            match model.SeriesDetailPage with
            | Some _ ->
                Cmd.batch [
                    Cmd.ofMsg (SeriesDetailMsg Pages.SeriesDetail.Types.LoadSessions)
                    Cmd.ofMsg (SeriesDetailMsg (Pages.SeriesDetail.Types.SelectSession session.Id))
                ]
            | None -> Cmd.none
        // Invalidate FriendDetail page cache so it reloads fresh data on next visit
        { model with FriendDetailPage = None }, reloadAndSelectCmd

    | SessionDeleted _ ->
        // Invalidate FriendDetail page cache so it reloads fresh data on next visit
        { model with SessionDetailPage = None; FriendDetailPage = None },
        Cmd.ofMsg (NavigateTo LibraryPage)

    | CollectionSaved _ ->
        { model with CollectionsPage = None },
        Cmd.ofMsg (NavigateTo CollectionsPage)

    | CollectionDeleted _ ->
        { model with CollectionsPage = None },
        Cmd.ofMsg (NavigateTo CollectionsPage)

    | AddedToCollection collection ->
        // Refresh the collection detail page if we're viewing it
        let model', cmd =
            match model.CollectionDetailPage with
            | Some pageModel when pageModel.CollectionId = collection.Id ->
                // Reload the collection to show the new item
                let reloadCmd = Cmd.map CollectionDetailMsg (Cmd.ofMsg Pages.CollectionDetail.Types.LoadCollection)
                model, reloadCmd
            | _ ->
                // Also invalidate the collections list page so it refreshes when visited
                { model with CollectionsPage = None }, Cmd.none
        model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification ($"Added to {collection.Name}", true))]

    // Page messages - ContributorDetail
    | ContributorDetailMsg contributorMsg ->
        match model.ContributorDetailPage with
        | Some pageModel ->
            let contributorApi : Pages.ContributorDetail.State.ContributorApi = {
                GetPersonDetails = fun personId -> Api.api.tmdbGetPersonDetails personId
                GetFilmography = fun personId -> Api.api.tmdbGetPersonFilmography personId
                GetLibrary = fun () -> Api.api.libraryGetAll ()
                AddMovie = fun req -> Api.api.libraryAddMovie req
                AddSeries = fun req -> Api.api.libraryAddSeries req
                GetTrackedByTmdbId = fun personId -> Api.api.contributorsGetByTmdbId personId
                Track = fun req -> Api.api.contributorsTrack req
                Untrack = fun trackedId -> Api.api.contributorsUntrack trackedId
            }
            let newPage, pageCmd, extMsg = Pages.ContributorDetail.State.update contributorApi contributorMsg pageModel
            let model' = { model with ContributorDetailPage = Some newPage }
            let cmd = Cmd.map ContributorDetailMsg pageCmd
            match extMsg with
            | Pages.ContributorDetail.Types.NoOp -> model', cmd
            | Pages.ContributorDetail.Types.NavigateBack ->
                // Navigate back - could go to library or previous page
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo LibraryPage)]
            | Pages.ContributorDetail.Types.NavigateToMovieDetail (_, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.ContributorDetail.Types.NavigateToSeriesDetail (_, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.ContributorDetail.Types.NavigateToGraphWithFocus contributorId ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (GraphPage (Some (FocusedContributor contributorId))))]
            | Pages.ContributorDetail.Types.ShowNotification (msg, isSuccess) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - Cache
    | CacheMsg cacheMsg ->
        match model.CachePage with
        | Some pageModel ->
            let cacheApi : Pages.Cache.State.CacheApi = {
                GetEntries = fun () -> Api.api.cacheGetEntries ()
                GetStats = fun () -> Api.api.cacheGetStats ()
                ClearAll = fun () -> Api.api.cacheClearAll ()
                ClearExpired = fun () -> Api.api.cacheClearExpired ()
                RecalculateSeriesWatchStatus = fun () -> Api.api.maintenanceRecalculateSeriesWatchStatus ()
            }
            let newPage, pageCmd, extMsg = Pages.Cache.State.update cacheApi cacheMsg pageModel
            let model' = { model with CachePage = Some newPage }
            let cmd = Cmd.map CacheMsg pageCmd
            match extMsg with
            | Pages.Cache.Types.NoOp -> model', cmd
            | Pages.Cache.Types.ShowNotification (msg, isSuccess) ->
                if isSuccess then model', cmd
                else model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, false))]
        | None -> model, Cmd.none

    // Page messages - Stats
    | StatsMsg statsMsg ->
        match model.StatsPage with
        | Some pageModel ->
            let statsApi : Pages.Stats.State.StatsApi = Api.api.statsGetTimeIntelligence
            let newPage, pageCmd, extMsg = Pages.Stats.State.update statsApi statsMsg pageModel
            let model' = { model with StatsPage = Some newPage }
            let cmd = Cmd.map StatsMsg pageCmd
            match extMsg with
            | Pages.Stats.Types.NoOp -> model', cmd
            | Pages.Stats.Types.NavigateToMovieDetail (entryId, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.Stats.Types.NavigateToSeriesDetail (entryId, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.Stats.Types.NavigateToCollection (collectionId, name) ->
                let slug = Slug.forCollection name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (CollectionDetailPage slug))]
        | None -> model, Cmd.none

    // Page messages - Year in Review
    | YearInReviewMsg yearMsg ->
        match model.YearInReviewPage with
        | Some pageModel ->
            let yearApi : Pages.YearInReview.State.YearInReviewApi = {
                GetStats = Api.api.yearInReviewGetStats
                GetAvailableYears = Api.api.yearInReviewGetAvailableYears
            }
            let newPage, pageCmd, extMsg = Pages.YearInReview.State.update yearApi yearMsg pageModel
            let model' = { model with YearInReviewPage = Some newPage }
            let cmd = Cmd.map YearInReviewMsg pageCmd
            match extMsg with
            | Pages.YearInReview.Types.NoOp -> model', cmd
            | Pages.YearInReview.Types.NavigateToMovieDetail (entryId, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.YearInReview.Types.NavigateToSeriesDetail (entryId, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.YearInReview.Types.NavigateToYearView (year, viewMode) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (YearInReviewPage (Some year, viewMode)))]
        | None -> model, Cmd.none

    // Page messages - Timeline
    | TimelineMsg timelineMsg ->
        match model.TimelinePage with
        | Some pageModel ->
            let timelineApi : Pages.Timeline.State.TimelineApi = {
                GetEntries = fun (filter, page, pageSize) -> Api.api.timelineGetEntries (filter, page, pageSize)
                GetDateRange = fun filter -> Api.api.timelineGetDateRange filter
                GetYearStats = fun filter -> Api.api.timelineGetYearStats filter
            }
            let newPage, pageCmd, extMsg = Pages.Timeline.State.update timelineApi timelineMsg pageModel
            let model' = { model with TimelinePage = Some newPage }
            let cmd = Cmd.map TimelineMsg pageCmd
            match extMsg with
            | Pages.Timeline.Types.NoOp -> model', cmd
            | Pages.Timeline.Types.NavigateToMovieDetail (entryId, title, releaseDate) ->
                let slug = Slug.forMovie title releaseDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.Timeline.Types.NavigateToSeriesDetail (entryId, name, firstAirDate) ->
                let slug = Slug.forSeries name firstAirDate
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
        | None -> model, Cmd.none

    // Page messages - Import
    | ImportMsg importMsg ->
        match model.ImportPage with
        | Some pageModel ->
            let importApi : Pages.Import.State.TraktApi = {
                GetAuthUrl = fun () -> Api.api.traktGetAuthUrl ()
                ExchangeCode = fun (code, state) -> Api.api.traktExchangeCode (code, state)
                IsAuthenticated = fun () -> Api.api.traktIsAuthenticated ()
                Logout = fun () -> Api.api.traktLogout ()
                GetImportPreview = fun options -> Api.api.traktGetImportPreview options
                StartImport = fun options -> Api.api.traktStartImport options
                GetImportStatus = fun () -> Api.api.traktGetImportStatus ()
                CancelImport = fun () -> Api.api.traktCancelImport ()
                ResyncSince = fun date -> Api.api.traktResyncSince date
            }
            let newPage, pageCmd, extMsg = Pages.Import.State.update importApi importMsg pageModel
            let model' = { model with ImportPage = Some newPage }
            let cmd = Cmd.map ImportMsg pageCmd
            match extMsg with
            | Pages.Import.Types.NoOp -> model', cmd
            | Pages.Import.Types.ShowNotification (msg, isSuccess) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - GenericImport
    | GenericImportMsg genericImportMsg ->
        match model.GenericImportPage with
        | Some pageModel ->
            let newPage, pageCmd, extMsg = Pages.GenericImport.State.update Api.api genericImportMsg pageModel
            let model' = { model with GenericImportPage = Some newPage }
            let cmd = Cmd.map GenericImportMsg pageCmd
            match extMsg with
            | Pages.GenericImport.Types.NoOp -> model', cmd
            | Pages.GenericImport.Types.ShowNotification (msg, isSuccess) ->
                model', Cmd.batch [cmd; Cmd.ofMsg (ShowNotification (msg, isSuccess))]
        | None -> model, Cmd.none

    // Page messages - Graph
    | GraphMsg graphMsg ->
        match model.GraphPage with
        | Some pageModel ->
            let graphApi : Pages.Graph.State.GraphApi = Api.api.graphGetData
            let newPage, pageCmd, extMsg = Pages.Graph.State.update graphApi graphMsg pageModel
            let model' = { model with GraphPage = Some newPage }
            let cmd = Cmd.map GraphMsg pageCmd
            match extMsg with
            | Pages.Graph.Types.NoOp -> model', cmd
            | Pages.Graph.Types.NavigateToMovieDetail (_, title) ->
                let slug = Slug.generate title
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (MovieDetailPage slug))]
            | Pages.Graph.Types.NavigateToSeriesDetail (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (SeriesDetailPage slug))]
            | Pages.Graph.Types.NavigateToFriendDetail (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (FriendDetailPage slug))]
            | Pages.Graph.Types.NavigateToContributor (_, name) ->
                let slug = Slug.generate name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (ContributorDetailPage (slug, None)))]
            | Pages.Graph.Types.NavigateToCollection (_, name) ->
                let slug = Slug.forCollection name
                model', Cmd.batch [cmd; Cmd.ofMsg (NavigateTo (CollectionDetailPage slug))]
        | None -> model, Cmd.none

    // Page messages - Styleguide
    | StyleguideMsg styleguideMsg ->
        match model.StyleguidePage with
        | Some pageModel ->
            let newPage, pageCmd, _ = Pages.Styleguide.State.update styleguideMsg pageModel
            let model' = { model with StyleguidePage = Some newPage }
            let cmd = Cmd.map StyleguideMsg pageCmd
            model', cmd
        | None -> model, Cmd.none

    // Slug-based entity loading (for URL navigation)
    | LoadEntryBySlug (slug, isMovie) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryGetBySlug
                (slug, Some isMovie)
                (fun result -> EntryBySlugLoaded (result, isMovie))
                (fun ex -> EntryBySlugLoaded (Error ex.Message, isMovie))
        model, cmd

    | EntryBySlugLoaded (Ok entry, isMovie) ->
        if isMovie then
            initializeMovieDetailWithEntry entry model
        else
            initializeSeriesDetailWithEntry entry model

    | EntryBySlugLoaded (Error err, _) ->
        { model with CurrentPage = NotFoundPage },
        Cmd.ofMsg (ShowNotification ($"Entry not found: {err}", false))

    | LoadFriendBySlug slug ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.friendsGetBySlug
                slug
                FriendBySlugLoaded
                (fun ex -> FriendBySlugLoaded (Error ex.Message))
        model, cmd

    | FriendBySlugLoaded (Ok friend) ->
        initializeFriendDetailWithFriend friend model

    | FriendBySlugLoaded (Error err) ->
        { model with CurrentPage = NotFoundPage },
        Cmd.ofMsg (ShowNotification ($"Friend not found: {err}", false))

    | LoadSessionBySlug slug ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.sessionsGetBySlug
                slug
                SessionBySlugLoaded
                (fun ex -> SessionBySlugLoaded (Error ex.Message))
        model, cmd

    | SessionBySlugLoaded (Ok sessionWithProgress) ->
        initializeSessionDetailWithSession sessionWithProgress model

    | SessionBySlugLoaded (Error err) ->
        { model with CurrentPage = NotFoundPage },
        Cmd.ofMsg (ShowNotification ($"Session not found: {err}", false))

    | LoadCollectionBySlug slug ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.collectionsGetBySlug
                slug
                CollectionBySlugLoaded
                (fun ex -> CollectionBySlugLoaded (Error ex.Message))
        model, cmd

    | CollectionBySlugLoaded (Ok collectionWithItems) ->
        initializeCollectionDetailWithCollection collectionWithItems model

    | CollectionBySlugLoaded (Error err) ->
        { model with CurrentPage = NotFoundPage },
        Cmd.ofMsg (ShowNotification ($"Collection not found: {err}", false))

    | LoadContributorBySlug slug ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.contributorsGetBySlug
                slug
                ContributorBySlugLoaded
                (fun ex -> ContributorBySlugLoaded (Error ex.Message))
        model, cmd

    | ContributorBySlugLoaded (Ok contributor) ->
        initializeContributorDetailWithContributor contributor model

    | ContributorBySlugLoaded (Error err) ->
        { model with CurrentPage = NotFoundPage },
        Cmd.ofMsg (ShowNotification ($"Contributor not found: {err}", false))
