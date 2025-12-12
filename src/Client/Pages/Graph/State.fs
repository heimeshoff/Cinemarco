module Pages.Graph.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type GraphApi = GraphFilter -> Async<RelationshipGraph>

/// Debounce delay for search input (in milliseconds)
let private searchDebounceMs = 400

/// Create a debounced command that delays execution
let private debounce (delayMs: int) (msg: Msg) : Cmd<Msg> =
    Cmd.OfAsync.perform
        (fun () -> async {
            do! Async.Sleep delayMs
            return ()
        })
        ()
        (fun () -> msg)

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg LoadGraph

let update (api: GraphApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | LoadGraph ->
        let cmd =
            Cmd.OfAsync.either
                api
                model.Filter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        // Only set to Loading if we don't have any data yet
        let newGraph =
            match model.Graph with
            | NotAsked | Failure _ -> Loading
            | _ -> model.Graph
        { model with Graph = newGraph; IsRefreshing = true }, cmd, NoOp

    | GraphLoaded (Ok graph) ->
        { model with Graph = Success graph; IsRefreshing = false }, Cmd.none, NoOp

    | GraphLoaded (Error err) ->
        { model with Graph = Failure err; IsRefreshing = false }, Cmd.none, NoOp

    | UpdateFilter filter ->
        // Update filter and reload graph - keep existing graph visible while refreshing
        let cmd =
            Cmd.OfAsync.either
                api
                filter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        { model with Filter = filter; IsRefreshing = true }, cmd, NoOp

    | SetSearchQuery query ->
        // Just update the filter locally, debounce the actual search
        let newFilter = { model.Filter with SearchQuery = if System.String.IsNullOrWhiteSpace(query) then None else Some query }
        // Debounce the search execution
        let cmd = debounce searchDebounceMs ExecuteSearch
        { model with Filter = newFilter }, cmd, NoOp

    | ExecuteSearch ->
        // Execute the actual search with the current filter
        let cmd =
            Cmd.OfAsync.either
                api
                model.Filter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        { model with IsRefreshing = true }, cmd, NoOp

    | SelectNode node ->
        { model with SelectedNode = node; IsPanelOpen = true }, Cmd.none, NoOp

    | FocusOnNode node ->
        // Convert SelectedNode to FocusedGraphNode and load neighborhood
        // Note: When focusing, we ignore the search query - focused mode shows all connections
        let (focusedNode, nodeId) =
            match node with
            | SelectedMovie (entryId, _, _) ->
                Some (FocusedMovie entryId), Some $"movie-{EntryId.value entryId}"
            | SelectedSeries (entryId, _, _) ->
                Some (FocusedSeries entryId), Some $"series-{EntryId.value entryId}"
            | SelectedFriend (friendId, _, _) ->
                Some (FocusedFriend friendId), Some $"friend-{FriendId.value friendId}"
            | SelectedContributor (contributorId, _, _, _) ->
                Some (FocusedContributor contributorId), Some $"contributor-{ContributorId.value contributorId}"
            | SelectedCollection (collectionId, _, _) ->
                Some (FocusedCollection collectionId), Some $"collection-{CollectionId.value collectionId}"
            | NoSelection -> None, None

        match focusedNode with
        | Some focused ->
            // Clear search query when focusing - focus mode shows 2-layer neighborhood regardless of search
            let focusFilter = { model.Filter with FocusedNode = Some focused; SearchQuery = None }
            let cmd =
                Cmd.OfAsync.either
                    api
                    focusFilter
                    (Ok >> GraphLoaded)
                    (fun ex -> Error ex.Message |> GraphLoaded)
            { model with
                Filter = focusFilter
                SelectedNode = node
                IsRefreshing = true
                FocusedNodeId = nodeId }, cmd, NoOp
        | None ->
            model, Cmd.none, NoOp

    | ClearFocus ->
        // Return to normal filtered view
        let clearedFilter = { model.Filter with FocusedNode = None }
        let cmd =
            Cmd.OfAsync.either
                api
                clearedFilter
                (Ok >> GraphLoaded)
                (fun ex -> Error ex.Message |> GraphLoaded)
        { model with
            Filter = clearedFilter
            IsRefreshing = true
            FocusedNodeId = None }, cmd, NoOp

    | DeselectNode ->
        { model with SelectedNode = NoSelection }, Cmd.none, NoOp

    | TogglePanel ->
        { model with IsPanelOpen = not model.IsPanelOpen }, Cmd.none, NoOp

    | SetZoom zoom ->
        { model with Zoom = zoom }, Cmd.none, NoOp

    | ViewMovieDetail (entryId, title) ->
        model, Cmd.none, NavigateToMovieDetail (entryId, title)

    | ViewSeriesDetail (entryId, name) ->
        model, Cmd.none, NavigateToSeriesDetail (entryId, name)

    | ViewFriendDetail (friendId, name) ->
        model, Cmd.none, NavigateToFriendDetail (friendId, name)

    | ViewContributor (contributorId, name) ->
        model, Cmd.none, NavigateToContributor (contributorId, name)

    | ViewCollection (collectionId, name) ->
        model, Cmd.none, NavigateToCollection (collectionId, name)
