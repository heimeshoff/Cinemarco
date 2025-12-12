module Pages.Graph.Types

open Common.Types
open Shared.Domain

/// Represents a node's position in the graph
type NodePosition = {
    X: float
    Y: float
}

/// Selected node for showing detail panel
type SelectedNode =
    | NoSelection
    | SelectedMovie of EntryId * title: string * posterPath: string option
    | SelectedSeries of EntryId * name: string * posterPath: string option
    | SelectedFriend of FriendId * name: string * avatarUrl: string option
    | SelectedContributor of ContributorId * name: string * profilePath: string option * knownFor: string option
    | SelectedCollection of CollectionId * name: string * coverImagePath: string option

type Model = {
    Graph: RemoteData<RelationshipGraph>
    Filter: GraphFilter
    SelectedNode: SelectedNode
    IsPanelOpen: bool
    Zoom: float
    IsRefreshing: bool  // True when graph is being refreshed (but we keep showing old data)
    FocusedNodeId: string option  // Node ID to center view on after graph loads
}

type Msg =
    | LoadGraph
    | GraphLoaded of Result<RelationshipGraph, string>
    | UpdateFilter of GraphFilter
    | SetSearchQuery of string
    | ExecuteSearch  // Debounced search execution
    | SelectNode of SelectedNode
    | DeselectNode
    | FocusOnNode of SelectedNode  // Double-click to focus and show 2-level neighborhood
    | ClearFocus  // Return to normal filtered view
    | TogglePanel
    | SetZoom of float
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string
    | ViewFriendDetail of friendId: FriendId * name: string
    | ViewContributor of contributorId: ContributorId * name: string
    | ViewCollection of collectionId: CollectionId * name: string

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | NavigateToContributor of contributorId: ContributorId * name: string
    | NavigateToCollection of collectionId: CollectionId * name: string

module Model =
    let defaultFilter = {
        MaxNodes = Some 100
        SearchQuery = None
        FocusedNode = None
    }

    let empty = {
        Graph = NotAsked
        Filter = defaultFilter
        SelectedNode = NoSelection
        IsPanelOpen = true
        Zoom = 1.0
        IsRefreshing = false
        FocusedNodeId = None
    }
