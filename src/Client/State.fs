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
        let cmd =
            Cmd.OfAsync.either
                Api.api.libraryGetById
                entryId
                DetailEntryLoaded
                (fun ex -> Error ex.Message |> DetailEntryLoaded)
        { model with
            CurrentPage = SeriesDetailPage entryId
            DetailEntry = Loading
        }, cmd

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
