module State

open Elmish
open Shared.Api
open Shared.Domain
open Types

/// Application model
type Model = {
    CurrentPage: Page
    HealthCheck: RemoteData<HealthCheckResponse>
    // Search state
    Search: SearchState
    // Modal state
    Modal: ModalState
    // Reference data (friends and tags for the quick add modal)
    Friends: RemoteData<Friend list>
    Tags: RemoteData<Tag list>
    // Library entries
    Library: RemoteData<LibraryEntry list>
    // Library filters
    LibraryFilters: LibraryFilters
    // Currently viewed detail entry
    DetailEntry: RemoteData<LibraryEntry>
    // Episode progress for series detail view
    EpisodeProgress: RemoteData<EpisodeProgress list>
    // Friend detail page - entries watched with friend
    FriendDetailEntries: RemoteData<LibraryEntry list>
    // Tag detail page - entries with tag
    TagDetailEntries: RemoteData<LibraryEntry list>
    // Notification message (for success/error toasts)
    Notification: (string * bool) option  // (message, isSuccess)
}

/// Application messages
type Msg =
    | NavigateTo of Page
    | CheckHealth
    | HealthCheckResult of Result<HealthCheckResponse, string>
    // Search messages
    | SearchQueryChanged of string
    | SearchDebounced
    | SearchResults of Result<TmdbSearchResult list, string>
    | CloseSearchDropdown
    // Quick add messages
    | OpenQuickAddModal of TmdbSearchResult
    | CloseModal
    | QuickAddNoteChanged of string
    | ToggleQuickAddTag of TagId
    | ToggleQuickAddFriend of FriendId
    | SubmitQuickAdd
    | QuickAddResult of Result<LibraryEntry, string>
    // Reference data
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>
    | LoadTags
    | TagsLoaded of Result<Tag list, string>
    // Friends management
    | OpenAddFriendModal
    | OpenEditFriendModal of Friend
    | FriendModalNameChanged of string
    | FriendModalNicknameChanged of string
    | FriendModalNotesChanged of string
    | SubmitFriendModal
    | FriendSaved of Result<Friend, string>
    | OpenDeleteFriendModal of Friend
    | ConfirmDeleteFriend of FriendId
    | FriendDeleted of Result<unit, string>
    | ViewFriendDetail of FriendId
    | FriendWatchedWithLoaded of Result<LibraryEntry list, string>
    // Tags management
    | OpenAddTagModal
    | OpenEditTagModal of Tag
    | TagModalNameChanged of string
    | TagModalColorChanged of string
    | TagModalDescriptionChanged of string
    | SubmitTagModal
    | TagSaved of Result<Tag, string>
    | OpenDeleteTagModal of Tag
    | ConfirmDeleteTag of TagId
    | TagDeleted of Result<unit, string>
    | ViewTagDetail of TagId
    | TagEntriesLoaded of Result<LibraryEntry list, string>
    // Library
    | LoadLibrary
    | LibraryLoaded of Result<LibraryEntry list, string>
    // Library filters
    | SetLibrarySearchQuery of string
    | SetWatchStatusFilter of WatchStatusFilter
    | ToggleTagFilter of TagId
    | SetMinRatingFilter of int option
    | SetSortBy of LibrarySortBy
    | ToggleSortDirection
    | ClearFilters
    // Detail view
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | DetailEntryLoaded of Result<LibraryEntry, string>
    | DeleteEntry of EntryId
    | EntryDeleted of Result<unit, string>
    // Watch status
    | MarkMovieWatched of EntryId
    | MarkMovieUnwatched of EntryId
    | WatchStatusUpdated of Result<LibraryEntry, string>
    | ToggleEpisodeWatched of EntryId * int * int * bool  // entryId, season, episode, newWatchedState
    | EpisodeProgressUpdated of Result<unit, string>
    | MarkSeasonWatched of EntryId * int  // entryId, seasonNumber
    | SeasonWatchedResult of Result<unit, string>
    | MarkSeriesCompleted of EntryId
    | LoadEpisodeProgress of EntryId
    | EpisodeProgressLoaded of Result<EpisodeProgress list, string>
    // Abandon modal
    | OpenAbandonModal of EntryId
    | AbandonModalReasonChanged of string
    | AbandonModalSeasonChanged of int option
    | AbandonModalEpisodeChanged of int option
    | SubmitAbandonModal
    | AbandonResult of Result<LibraryEntry, string>
    | ResumeEntry of EntryId
    | ResumeResult of Result<LibraryEntry, string>
    // Delete entry confirmation
    | OpenDeleteEntryModal of EntryId
    | ConfirmDeleteEntry of EntryId
    // Notifications
    | ShowNotification of string * bool
    | ClearNotification

/// Initialize the model
let init () : Model * Cmd<Msg> =
    let model = {
        CurrentPage = HomePage
        HealthCheck = NotAsked
        Search = SearchState.empty
        Modal = NoModal
        Friends = NotAsked
        Tags = NotAsked
        Library = NotAsked
        LibraryFilters = LibraryFilters.empty
        DetailEntry = NotAsked
        EpisodeProgress = NotAsked
        FriendDetailEntries = NotAsked
        TagDetailEntries = NotAsked
        Notification = None
    }
    // Load health check, friends, tags on startup
    let cmds = Cmd.batch [
        Cmd.ofMsg CheckHealth
        Cmd.ofMsg LoadFriends
        Cmd.ofMsg LoadTags
        Cmd.ofMsg LoadLibrary
    ]
    model, cmds

/// Debounce delay for search (milliseconds)
let private searchDebounceDelay = 300

/// Update function following the MVU pattern
let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | NavigateTo page ->
        { model with CurrentPage = page }, Cmd.none

    | CheckHealth ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.healthCheck
                ()
                (Ok >> HealthCheckResult)
                (fun ex -> Error ex.Message |> HealthCheckResult)
        { model with HealthCheck = Loading }, cmd

    | HealthCheckResult (Ok response) ->
        { model with HealthCheck = Success response }, Cmd.none

    | HealthCheckResult (Error err) ->
        { model with HealthCheck = Failure err }, Cmd.none

    // =====================================
    // Search
    // =====================================

    | SearchQueryChanged query ->
        let newSearch = { model.Search with Query = query; IsDropdownOpen = query.Length >= 2 }
        let cmd =
            if query.Length >= 2 then
                // Debounce: wait a bit before actually searching
                Cmd.OfAsync.perform
                    (fun () -> async { do! Async.Sleep searchDebounceDelay })
                    ()
                    (fun () -> SearchDebounced)
            else
                Cmd.none
        { model with Search = newSearch }, cmd

    | SearchDebounced ->
        // Only search if query is still valid
        if model.Search.Query.Length >= 2 then
            let cmd =
                Cmd.OfAsync.either
                    Api.api.tmdbSearchAll
                    model.Search.Query
                    (Ok >> SearchResults)
                    (fun ex -> Error ex.Message |> SearchResults)
            { model with Search = { model.Search with Results = Loading } }, cmd
        else
            model, Cmd.none

    | SearchResults (Ok results) ->
        let newSearch = { model.Search with Results = Success results }
        { model with Search = newSearch }, Cmd.none

    | SearchResults (Error err) ->
        let newSearch = { model.Search with Results = Failure err }
        { model with Search = newSearch }, Cmd.none

    | CloseSearchDropdown ->
        { model with Search = { model.Search with IsDropdownOpen = false } }, Cmd.none

    // =====================================
    // Quick Add Modal
    // =====================================

    | OpenQuickAddModal item ->
        let modalState = {
            SelectedItem = item
            WhyAddedNote = ""
            SelectedTags = []
            SelectedFriends = []
            IsSubmitting = false
            Error = None
        }
        { model with
            Modal = QuickAddModal modalState
            Search = { model.Search with IsDropdownOpen = false }
        }, Cmd.none

    | CloseModal ->
        { model with Modal = NoModal }, Cmd.none

    | QuickAddNoteChanged note ->
        match model.Modal with
        | QuickAddModal state ->
            { model with Modal = QuickAddModal { state with WhyAddedNote = note } }, Cmd.none
        | _ -> model, Cmd.none

    | ToggleQuickAddTag tagId ->
        match model.Modal with
        | QuickAddModal state ->
            let newTags =
                if List.contains tagId state.SelectedTags then
                    List.filter (fun t -> t <> tagId) state.SelectedTags
                else
                    tagId :: state.SelectedTags
            { model with Modal = QuickAddModal { state with SelectedTags = newTags } }, Cmd.none
        | _ -> model, Cmd.none

    | ToggleQuickAddFriend friendId ->
        match model.Modal with
        | QuickAddModal state ->
            let newFriends =
                if List.contains friendId state.SelectedFriends then
                    List.filter (fun f -> f <> friendId) state.SelectedFriends
                else
                    friendId :: state.SelectedFriends
            { model with Modal = QuickAddModal { state with SelectedFriends = newFriends } }, Cmd.none
        | _ -> model, Cmd.none

    | SubmitQuickAdd ->
        match model.Modal with
        | QuickAddModal state ->
            let whyAdded =
                if String.length state.WhyAddedNote > 0 then
                    Some {
                        RecommendedBy = None
                        RecommendedByName = None
                        Source = None
                        Context = Some state.WhyAddedNote
                        DateRecommended = None
                    }
                else None

            let cmd =
                match state.SelectedItem.MediaType with
                | Movie ->
                    let request : AddMovieRequest = {
                        TmdbId = TmdbMovieId state.SelectedItem.TmdbId
                        WhyAdded = whyAdded
                        InitialTags = state.SelectedTags
                        InitialFriends = state.SelectedFriends
                    }
                    Cmd.OfAsync.either
                        Api.api.libraryAddMovie
                        request
                        QuickAddResult
                        (fun ex -> Error ex.Message |> QuickAddResult)
                | Series ->
                    let request : AddSeriesRequest = {
                        TmdbId = TmdbSeriesId state.SelectedItem.TmdbId
                        WhyAdded = whyAdded
                        InitialTags = state.SelectedTags
                        InitialFriends = state.SelectedFriends
                    }
                    Cmd.OfAsync.either
                        Api.api.libraryAddSeries
                        request
                        QuickAddResult
                        (fun ex -> Error ex.Message |> QuickAddResult)

            { model with Modal = QuickAddModal { state with IsSubmitting = true; Error = None } }, cmd
        | _ -> model, Cmd.none

    | QuickAddResult (Ok entry) ->
        // Success! Close modal and show notification
        let title =
            match entry.Media with
            | LibraryMovie m -> m.Title
            | LibrarySeries s -> s.Name
        { model with
            Modal = NoModal
            Search = SearchState.empty
        }, Cmd.batch [
            Cmd.ofMsg LoadLibrary
            Cmd.ofMsg (ShowNotification ($"Added \"{title}\" to your library!", true))
        ]

    | QuickAddResult (Error err) ->
        match model.Modal with
        | QuickAddModal state ->
            { model with Modal = QuickAddModal { state with IsSubmitting = false; Error = Some err } }, Cmd.none
        | _ -> model, Cmd.none

    // =====================================
    // Reference Data (Friends & Tags)
    // =====================================

    | LoadFriends ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.friendsGetAll
                ()
                (Ok >> FriendsLoaded)
                (fun ex -> Error ex.Message |> FriendsLoaded)
        { model with Friends = Loading }, cmd

    | FriendsLoaded (Ok friends) ->
        { model with Friends = Success friends }, Cmd.none

    | FriendsLoaded (Error _) ->
        { model with Friends = Success [] }, Cmd.none  // Fallback to empty list

    | LoadTags ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.tagsGetAll
                ()
                (Ok >> TagsLoaded)
                (fun ex -> Error ex.Message |> TagsLoaded)
        { model with Tags = Loading }, cmd

    | TagsLoaded (Ok tags) ->
        { model with Tags = Success tags }, Cmd.none

    | TagsLoaded (Error _) ->
        { model with Tags = Success [] }, Cmd.none  // Fallback to empty list

    // =====================================
    // Friends Management
    // =====================================

    | OpenAddFriendModal ->
        { model with Modal = FriendModal FriendModalState.empty }, Cmd.none

    | OpenEditFriendModal friend ->
        { model with Modal = FriendModal (FriendModalState.fromFriend friend) }, Cmd.none

    | FriendModalNameChanged name ->
        match model.Modal with
        | FriendModal state ->
            { model with Modal = FriendModal { state with Name = name } }, Cmd.none
        | _ -> model, Cmd.none

    | FriendModalNicknameChanged nickname ->
        match model.Modal with
        | FriendModal state ->
            { model with Modal = FriendModal { state with Nickname = nickname } }, Cmd.none
        | _ -> model, Cmd.none

    | FriendModalNotesChanged notes ->
        match model.Modal with
        | FriendModal state ->
            { model with Modal = FriendModal { state with Notes = notes } }, Cmd.none
        | _ -> model, Cmd.none

    | SubmitFriendModal ->
        match model.Modal with
        | FriendModal state when state.Name.Trim().Length > 0 ->
            let cmd =
                match state.EditingFriend with
                | None ->
                    // Create new friend
                    let request : CreateFriendRequest = {
                        Name = state.Name.Trim()
                        Nickname = if state.Nickname.Trim().Length > 0 then Some (state.Nickname.Trim()) else None
                        Notes = if state.Notes.Trim().Length > 0 then Some (state.Notes.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        Api.api.friendsCreate
                        request
                        FriendSaved
                        (fun ex -> Error ex.Message |> FriendSaved)
                | Some existing ->
                    // Update existing friend
                    let request : UpdateFriendRequest = {
                        Id = existing.Id
                        Name = Some (state.Name.Trim())
                        Nickname = if state.Nickname.Trim().Length > 0 then Some (state.Nickname.Trim()) else None
                        Notes = if state.Notes.Trim().Length > 0 then Some (state.Notes.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        Api.api.friendsUpdate
                        request
                        FriendSaved
                        (fun ex -> Error ex.Message |> FriendSaved)
            { model with Modal = FriendModal { state with IsSubmitting = true; Error = None } }, cmd
        | FriendModal state ->
            { model with Modal = FriendModal { state with Error = Some "Name is required" } }, Cmd.none
        | _ -> model, Cmd.none

    | FriendSaved (Ok friend) ->
        { model with Modal = NoModal },
        Cmd.batch [
            Cmd.ofMsg LoadFriends
            Cmd.ofMsg (ShowNotification ($"Friend \"{friend.Name}\" saved!", true))
        ]

    | FriendSaved (Error err) ->
        match model.Modal with
        | FriendModal state ->
            { model with Modal = FriendModal { state with IsSubmitting = false; Error = Some err } }, Cmd.none
        | _ -> model, Cmd.none

    | OpenDeleteFriendModal friend ->
        { model with Modal = ConfirmDeleteFriendModal friend }, Cmd.none

    | ConfirmDeleteFriend (FriendId friendId) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.friendsDelete
                friendId
                FriendDeleted
                (fun ex -> Error ex.Message |> FriendDeleted)
        model, cmd

    | FriendDeleted (Ok ()) ->
        { model with
            Modal = NoModal
            CurrentPage = FriendsPage
        }, Cmd.batch [
            Cmd.ofMsg LoadFriends
            Cmd.ofMsg (ShowNotification ("Friend deleted", true))
        ]

    | FriendDeleted (Error err) ->
        { model with Modal = NoModal },
        Cmd.ofMsg (ShowNotification ($"Failed to delete friend: {err}", false))

    | ViewFriendDetail (FriendId friendId) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.friendsGetWatchedWith
                friendId
                (Ok >> FriendWatchedWithLoaded)
                (fun ex -> Error ex.Message |> FriendWatchedWithLoaded)
        { model with
            CurrentPage = FriendDetailPage (FriendId friendId)
            FriendDetailEntries = Loading
        }, cmd

    | FriendWatchedWithLoaded (Ok entries) ->
        { model with FriendDetailEntries = Success entries }, Cmd.none

    | FriendWatchedWithLoaded (Error err) ->
        { model with FriendDetailEntries = Failure err }, Cmd.none

    // =====================================
    // Tags Management
    // =====================================

    | OpenAddTagModal ->
        { model with Modal = TagModal TagModalState.empty }, Cmd.none

    | OpenEditTagModal tag ->
        { model with Modal = TagModal (TagModalState.fromTag tag) }, Cmd.none

    | TagModalNameChanged name ->
        match model.Modal with
        | TagModal state ->
            { model with Modal = TagModal { state with Name = name } }, Cmd.none
        | _ -> model, Cmd.none

    | TagModalColorChanged color ->
        match model.Modal with
        | TagModal state ->
            { model with Modal = TagModal { state with Color = color } }, Cmd.none
        | _ -> model, Cmd.none

    | TagModalDescriptionChanged description ->
        match model.Modal with
        | TagModal state ->
            { model with Modal = TagModal { state with Description = description } }, Cmd.none
        | _ -> model, Cmd.none

    | SubmitTagModal ->
        match model.Modal with
        | TagModal state when state.Name.Trim().Length > 0 ->
            let cmd =
                match state.EditingTag with
                | None ->
                    // Create new tag
                    let request : CreateTagRequest = {
                        Name = state.Name.Trim()
                        Color = if state.Color.Trim().Length > 0 then Some (state.Color.Trim()) else None
                        Description = if state.Description.Trim().Length > 0 then Some (state.Description.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        Api.api.tagsCreate
                        request
                        TagSaved
                        (fun ex -> Error ex.Message |> TagSaved)
                | Some existing ->
                    // Update existing tag
                    let request : UpdateTagRequest = {
                        Id = existing.Id
                        Name = Some (state.Name.Trim())
                        Color = if state.Color.Trim().Length > 0 then Some (state.Color.Trim()) else None
                        Description = if state.Description.Trim().Length > 0 then Some (state.Description.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        Api.api.tagsUpdate
                        request
                        TagSaved
                        (fun ex -> Error ex.Message |> TagSaved)
            { model with Modal = TagModal { state with IsSubmitting = true; Error = None } }, cmd
        | TagModal state ->
            { model with Modal = TagModal { state with Error = Some "Name is required" } }, Cmd.none
        | _ -> model, Cmd.none

    | TagSaved (Ok tag) ->
        { model with Modal = NoModal },
        Cmd.batch [
            Cmd.ofMsg LoadTags
            Cmd.ofMsg (ShowNotification ($"Tag \"{tag.Name}\" saved!", true))
        ]

    | TagSaved (Error err) ->
        match model.Modal with
        | TagModal state ->
            { model with Modal = TagModal { state with IsSubmitting = false; Error = Some err } }, Cmd.none
        | _ -> model, Cmd.none

    | OpenDeleteTagModal tag ->
        { model with Modal = ConfirmDeleteTagModal tag }, Cmd.none

    | ConfirmDeleteTag (TagId tagId) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.tagsDelete
                tagId
                TagDeleted
                (fun ex -> Error ex.Message |> TagDeleted)
        model, cmd

    | TagDeleted (Ok ()) ->
        { model with
            Modal = NoModal
            CurrentPage = TagsPage
        }, Cmd.batch [
            Cmd.ofMsg LoadTags
            Cmd.ofMsg (ShowNotification ("Tag deleted", true))
        ]

    | TagDeleted (Error err) ->
        { model with Modal = NoModal },
        Cmd.ofMsg (ShowNotification ($"Failed to delete tag: {err}", false))

    | ViewTagDetail (TagId tagId) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.tagsGetTaggedEntries
                tagId
                (Ok >> TagEntriesLoaded)
                (fun ex -> Error ex.Message |> TagEntriesLoaded)
        { model with
            CurrentPage = TagDetailPage (TagId tagId)
            TagDetailEntries = Loading
        }, cmd

    | TagEntriesLoaded (Ok entries) ->
        { model with TagDetailEntries = Success entries }, Cmd.none

    | TagEntriesLoaded (Error err) ->
        { model with TagDetailEntries = Failure err }, Cmd.none

    // =====================================
    // Library
    // =====================================

    | LoadLibrary ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryGetAll
                ()
                (Ok >> LibraryLoaded)
                (fun ex -> Error ex.Message |> LibraryLoaded)
        { model with Library = Loading }, cmd

    | LibraryLoaded (Ok entries) ->
        { model with Library = Success entries }, Cmd.none

    | LibraryLoaded (Error err) ->
        { model with Library = Failure err }, Cmd.none

    // =====================================
    // Library Filters
    // =====================================

    | SetLibrarySearchQuery query ->
        { model with LibraryFilters = { model.LibraryFilters with SearchQuery = query } }, Cmd.none

    | SetWatchStatusFilter status ->
        { model with LibraryFilters = { model.LibraryFilters with WatchStatus = status } }, Cmd.none

    | ToggleTagFilter tagId ->
        let newTags =
            if List.contains tagId model.LibraryFilters.SelectedTags then
                List.filter (fun t -> t <> tagId) model.LibraryFilters.SelectedTags
            else
                tagId :: model.LibraryFilters.SelectedTags
        { model with LibraryFilters = { model.LibraryFilters with SelectedTags = newTags } }, Cmd.none

    | SetMinRatingFilter rating ->
        { model with LibraryFilters = { model.LibraryFilters with MinRating = rating } }, Cmd.none

    | SetSortBy sortBy ->
        { model with LibraryFilters = { model.LibraryFilters with SortBy = sortBy } }, Cmd.none

    | ToggleSortDirection ->
        let newDir =
            match model.LibraryFilters.SortDirection with
            | Ascending -> Descending
            | Descending -> Ascending
        { model with LibraryFilters = { model.LibraryFilters with SortDirection = newDir } }, Cmd.none

    | ClearFilters ->
        { model with LibraryFilters = LibraryFilters.empty }, Cmd.none

    // =====================================
    // Detail View
    // =====================================

    | ViewMovieDetail entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryGetById
                entryId
                DetailEntryLoaded
                (fun ex -> Error ex.Message |> DetailEntryLoaded)
        { model with
            CurrentPage = MovieDetailPage entryId
            DetailEntry = Loading
        }, cmd

    | ViewSeriesDetail entryId ->
        let detailCmd =
            Cmd.OfAsync.either
                Api.api.libraryGetById
                entryId
                DetailEntryLoaded
                (fun ex -> Error ex.Message |> DetailEntryLoaded)
        let progressCmd =
            Cmd.OfAsync.either
                Api.api.libraryGetEpisodeProgress
                entryId
                (Ok >> EpisodeProgressLoaded)
                (fun ex -> Error ex.Message |> EpisodeProgressLoaded)
        { model with
            CurrentPage = SeriesDetailPage entryId
            DetailEntry = Loading
            EpisodeProgress = Loading
        }, Cmd.batch [ detailCmd; progressCmd ]

    | DetailEntryLoaded (Ok entry) ->
        { model with DetailEntry = Success entry }, Cmd.none

    | DetailEntryLoaded (Error err) ->
        { model with DetailEntry = Failure err }, Cmd.none

    | DeleteEntry entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryDeleteEntry
                entryId
                (fun result -> EntryDeleted result)
                (fun ex -> Error ex.Message |> EntryDeleted)
        model, cmd

    | EntryDeleted (Ok ()) ->
        { model with
            CurrentPage = LibraryPage
            DetailEntry = NotAsked
        }, Cmd.batch [
            Cmd.ofMsg LoadLibrary
            Cmd.ofMsg (ShowNotification ("Entry deleted successfully", true))
        ]

    | EntryDeleted (Error err) ->
        model, Cmd.ofMsg (ShowNotification ($"Failed to delete: {err}", false))

    // =====================================
    // Watch Status
    // =====================================

    | MarkMovieWatched entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryMarkMovieWatched
                (entryId, None)
                WatchStatusUpdated
                (fun ex -> Error ex.Message |> WatchStatusUpdated)
        model, cmd

    | MarkMovieUnwatched entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryMarkMovieUnwatched
                entryId
                WatchStatusUpdated
                (fun ex -> Error ex.Message |> WatchStatusUpdated)
        model, cmd

    | WatchStatusUpdated (Ok entry) ->
        // Update the detail entry and reload library
        { model with DetailEntry = Success entry },
        Cmd.batch [
            Cmd.ofMsg LoadLibrary
            Cmd.ofMsg (ShowNotification ("Watch status updated", true))
        ]

    | WatchStatusUpdated (Error err) ->
        model, Cmd.ofMsg (ShowNotification ($"Failed to update watch status: {err}", false))

    | ToggleEpisodeWatched (entryId, season, episode, watched) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryUpdateEpisodeProgress
                (entryId, season, episode, watched)
                EpisodeProgressUpdated
                (fun ex -> Error ex.Message |> EpisodeProgressUpdated)
        model, cmd

    | EpisodeProgressUpdated (Ok ()) ->
        // Reload the entry and episode progress to get updated state
        match model.DetailEntry with
        | Success entry ->
            model, Cmd.batch [
                Cmd.ofMsg (ViewSeriesDetail entry.Id)
                Cmd.ofMsg (LoadEpisodeProgress entry.Id)
                Cmd.ofMsg LoadLibrary
            ]
        | _ -> model, Cmd.none

    | EpisodeProgressUpdated (Error err) ->
        model, Cmd.ofMsg (ShowNotification ($"Failed to update episode: {err}", false))

    | MarkSeasonWatched (entryId, seasonNumber) ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryMarkSeasonWatched
                (entryId, seasonNumber)
                SeasonWatchedResult
                (fun ex -> Error ex.Message |> SeasonWatchedResult)
        model, cmd

    | SeasonWatchedResult (Ok ()) ->
        match model.DetailEntry with
        | Success entry ->
            { model with EpisodeProgress = Loading },
            Cmd.batch [
                Cmd.ofMsg (ViewSeriesDetail entry.Id)
                Cmd.ofMsg (LoadEpisodeProgress entry.Id)
                Cmd.ofMsg LoadLibrary
                Cmd.ofMsg (ShowNotification ("Season marked as watched", true))
            ]
        | _ -> model, Cmd.ofMsg (ShowNotification ("Season marked as watched", true))

    | SeasonWatchedResult (Error err) ->
        model, Cmd.ofMsg (ShowNotification ($"Failed to mark season: {err}", false))

    | MarkSeriesCompleted entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryMarkSeriesCompleted
                entryId
                WatchStatusUpdated
                (fun ex -> Error ex.Message |> WatchStatusUpdated)
        model, cmd

    | LoadEpisodeProgress entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryGetEpisodeProgress
                entryId
                (Ok >> EpisodeProgressLoaded)
                (fun ex -> Error ex.Message |> EpisodeProgressLoaded)
        { model with EpisodeProgress = Loading }, cmd

    | EpisodeProgressLoaded (Ok progress) ->
        { model with EpisodeProgress = Success progress }, Cmd.none

    | EpisodeProgressLoaded (Error err) ->
        { model with EpisodeProgress = Failure err }, Cmd.none

    // =====================================
    // Abandon Modal
    // =====================================

    | OpenAbandonModal entryId ->
        { model with Modal = AbandonModal (AbandonModalState.create entryId) }, Cmd.none

    | AbandonModalReasonChanged reason ->
        match model.Modal with
        | AbandonModal state ->
            { model with Modal = AbandonModal { state with Reason = reason } }, Cmd.none
        | _ -> model, Cmd.none

    | AbandonModalSeasonChanged season ->
        match model.Modal with
        | AbandonModal state ->
            { model with Modal = AbandonModal { state with AbandonedAtSeason = season } }, Cmd.none
        | _ -> model, Cmd.none

    | AbandonModalEpisodeChanged episode ->
        match model.Modal with
        | AbandonModal state ->
            { model with Modal = AbandonModal { state with AbandonedAtEpisode = episode } }, Cmd.none
        | _ -> model, Cmd.none

    | SubmitAbandonModal ->
        match model.Modal with
        | AbandonModal state ->
            let request : AbandonRequest = {
                Reason = if String.length state.Reason > 0 then Some state.Reason else None
                AbandonedAtSeason = state.AbandonedAtSeason
                AbandonedAtEpisode = state.AbandonedAtEpisode
            }
            let cmd =
                Cmd.OfAsync.either
                    Api.api.libraryAbandonEntry
                    (state.EntryId, request)
                    AbandonResult
                    (fun ex -> Error ex.Message |> AbandonResult)
            { model with Modal = AbandonModal { state with IsSubmitting = true } }, cmd
        | _ -> model, Cmd.none

    | AbandonResult (Ok entry) ->
        { model with
            Modal = NoModal
            DetailEntry = Success entry
        }, Cmd.batch [
            Cmd.ofMsg LoadLibrary
            Cmd.ofMsg (ShowNotification ("Entry marked as abandoned", true))
        ]

    | AbandonResult (Error err) ->
        match model.Modal with
        | AbandonModal state ->
            { model with Modal = AbandonModal { state with IsSubmitting = false; Error = Some err } }, Cmd.none
        | _ -> model, Cmd.ofMsg (ShowNotification ($"Failed to abandon: {err}", false))

    | ResumeEntry entryId ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryResumeEntry
                entryId
                ResumeResult
                (fun ex -> Error ex.Message |> ResumeResult)
        model, cmd

    | ResumeResult (Ok entry) ->
        { model with DetailEntry = Success entry },
        Cmd.batch [
            Cmd.ofMsg LoadLibrary
            Cmd.ofMsg (ShowNotification ("Entry resumed", true))
        ]

    | ResumeResult (Error err) ->
        model, Cmd.ofMsg (ShowNotification ($"Failed to resume: {err}", false))

    // =====================================
    // Delete Entry Modal
    // =====================================

    | OpenDeleteEntryModal entryId ->
        { model with Modal = ConfirmDeleteEntryModal entryId }, Cmd.none

    | ConfirmDeleteEntry entryId ->
        { model with Modal = NoModal }, Cmd.ofMsg (DeleteEntry entryId)

    // =====================================
    // Notifications
    // =====================================

    | ShowNotification (message, isSuccess) ->
        { model with Notification = Some (message, isSuccess) },
        // Auto-clear after 4 seconds
        Cmd.OfAsync.perform
            (fun () -> async { do! Async.Sleep 4000 })
            ()
            (fun () -> ClearNotification)

    | ClearNotification ->
        { model with Notification = None }, Cmd.none
