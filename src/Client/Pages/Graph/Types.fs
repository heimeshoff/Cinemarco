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
    | SelectedFriend of FriendId * name: string
    | SelectedContributor of ContributorId * name: string * profilePath: string option

type Model = {
    Graph: RemoteData<RelationshipGraph>
    Filter: GraphFilter
    SelectedNode: SelectedNode
    IsPanelOpen: bool
    Zoom: float
}

type Msg =
    | LoadGraph
    | GraphLoaded of Result<RelationshipGraph, string>
    | UpdateFilter of GraphFilter
    | SelectNode of SelectedNode
    | DeselectNode
    | TogglePanel
    | SetZoom of float
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string
    | ViewFriendDetail of friendId: FriendId * name: string
    | ViewContributor of contributorId: ContributorId * name: string

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string
    | NavigateToFriendDetail of friendId: FriendId * name: string
    | NavigateToContributor of contributorId: ContributorId * name: string

module Model =
    let defaultFilter = {
        IncludeMovies = true
        IncludeSeries = true
        IncludeFriends = true
        IncludeContributors = false
        IncludeGenres = false
        MaxNodes = Some 100
        WatchStatusFilter = None
    }

    let empty = {
        Graph = NotAsked
        Filter = defaultFilter
        SelectedNode = NoSelection
        IsPanelOpen = true
        Zoom = 1.0
    }
