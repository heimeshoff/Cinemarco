module Pages.Graph.View

open Feliz
open Fable.Core
open Fable.Core.JsInterop
open Browser
open Common.Types
open Shared.Domain
open Types
open Components.Icons

// Import the forceGraph JavaScript module using Emit for correct JS interop
module private ForceGraph =
    [<Emit("import('../../forceGraph.js').then(m => m.initializeGraph($0, $1, $2, $3))")>]
    let initializeGraph (containerId: string) (graphData: obj) (onSelect: obj -> unit) (onFocus: obj -> unit) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.destroyGraph($0))")>]
    let destroyGraph (containerId: string) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.setZoom($0))")>]
    let setZoom (level: float) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.resetZoom())")>]
    let resetZoom () : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.focusOnNode($0))")>]
    let focusOnNode (nodeId: string) : JS.Promise<unit> = jsNative

// =====================================
// Helper Functions
// =====================================

/// Helper to convert string option to JS-friendly null/string
let private optionToJs (opt: string option) : obj =
    match opt with
    | Some s -> box s
    | None -> null

/// Convert F# graph data to JS-friendly format
let private toJsGraph (graph: RelationshipGraph) : obj =
    createObj [
        "Nodes" ==> (graph.Nodes |> List.map (fun node ->
            match node with
            | MovieNode (entryId, title, posterPath) ->
                createObj [
                    "Case" ==> "MovieNode"
                    "Fields" ==> [|
                        createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                        title
                        optionToJs posterPath
                    |]
                ]
            | SeriesNode (entryId, name, posterPath) ->
                createObj [
                    "Case" ==> "SeriesNode"
                    "Fields" ==> [|
                        createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                        name
                        optionToJs posterPath
                    |]
                ]
            | FriendNode (friendId, name) ->
                createObj [
                    "Case" ==> "FriendNode"
                    "Fields" ==> [|
                        createObj [ "Fields" ==> [| FriendId.value friendId |] ]
                        name
                    |]
                ]
            | ContributorNode (contributorId, name, profilePath) ->
                createObj [
                    "Case" ==> "ContributorNode"
                    "Fields" ==> [|
                        createObj [ "Fields" ==> [| ContributorId.value contributorId |] ]
                        name
                        optionToJs profilePath
                    |]
                ]
            | CollectionNode (collectionId, name) ->
                createObj [
                    "Case" ==> "CollectionNode"
                    "Fields" ==> [|
                        createObj [ "Fields" ==> [| CollectionId.value collectionId |] ]
                        name
                    |]
                ]
        ) |> List.toArray)

        "Edges" ==> (graph.Edges |> List.map (fun edge ->
            createObj [
                "Source" ==> (
                    match edge.Source with
                    | MovieNode (entryId, title, posterPath) ->
                        createObj [
                            "Case" ==> "MovieNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                                title
                                optionToJs posterPath
                            |]
                        ]
                    | SeriesNode (entryId, name, posterPath) ->
                        createObj [
                            "Case" ==> "SeriesNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                                name
                                optionToJs posterPath
                            |]
                        ]
                    | FriendNode (friendId, name) ->
                        createObj [
                            "Case" ==> "FriendNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| FriendId.value friendId |] ]
                                name
                            |]
                        ]
                    | ContributorNode (contributorId, name, profilePath) ->
                        createObj [
                            "Case" ==> "ContributorNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| ContributorId.value contributorId |] ]
                                name
                                optionToJs profilePath
                            |]
                        ]
                    | CollectionNode (collectionId, name) ->
                        createObj [
                            "Case" ==> "CollectionNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| CollectionId.value collectionId |] ]
                                name
                            |]
                        ]
                )
                "Target" ==> (
                    match edge.Target with
                    | MovieNode (entryId, title, posterPath) ->
                        createObj [
                            "Case" ==> "MovieNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                                title
                                optionToJs posterPath
                            |]
                        ]
                    | SeriesNode (entryId, name, posterPath) ->
                        createObj [
                            "Case" ==> "SeriesNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| EntryId.value entryId |] ]
                                name
                                optionToJs posterPath
                            |]
                        ]
                    | FriendNode (friendId, name) ->
                        createObj [
                            "Case" ==> "FriendNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| FriendId.value friendId |] ]
                                name
                            |]
                        ]
                    | ContributorNode (contributorId, name, profilePath) ->
                        createObj [
                            "Case" ==> "ContributorNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| ContributorId.value contributorId |] ]
                                name
                                optionToJs profilePath
                            |]
                        ]
                    | CollectionNode (collectionId, name) ->
                        createObj [
                            "Case" ==> "CollectionNode"
                            "Fields" ==> [|
                                createObj [ "Fields" ==> [| CollectionId.value collectionId |] ]
                                name
                            |]
                        ]
                )
                "Relationship" ==> (
                    match edge.Relationship with
                    | WatchedWith -> createObj [ "Case" ==> "WatchedWith" ]
                    | WorkedOn role -> createObj [ "Case" ==> "WorkedOn"; "Fields" ==> [| role |] ]
                    | InCollection collId -> createObj [ "Case" ==> "InCollection"; "Fields" ==> [| CollectionId.value collId |] ]
                    | BelongsToCollection -> createObj [ "Case" ==> "BelongsToCollection" ]
                )
            ]
        ) |> List.toArray)
    ]

/// Parse node selection from JS callback
let private parseNodeSelection (jsNode: obj) : SelectedNode =
    if isNull jsNode then NoSelection
    else
        let nodeType = jsNode?``type`` |> unbox<string>
        let name = jsNode?name |> unbox<string>
        match nodeType with
        | "movie" ->
            let entryId = jsNode?entryId |> unbox<int> |> EntryId.create
            let posterPath = jsNode?posterPath |> Option.ofObj |> Option.map unbox<string>
            SelectedMovie (entryId, name, posterPath)
        | "series" ->
            let entryId = jsNode?entryId |> unbox<int> |> EntryId.create
            let posterPath = jsNode?posterPath |> Option.ofObj |> Option.map unbox<string>
            SelectedSeries (entryId, name, posterPath)
        | "friend" ->
            let friendId = jsNode?friendId |> unbox<int> |> FriendId.create
            SelectedFriend (friendId, name)
        | "contributor" ->
            let contributorId = jsNode?contributorId |> unbox<int> |> ContributorId.create
            let profilePath = jsNode?profilePath |> Option.ofObj |> Option.map unbox<string>
            SelectedContributor (contributorId, name, profilePath)
        | "collection" ->
            let collectionId = jsNode?collectionId |> unbox<int> |> CollectionId.create
            SelectedCollection (collectionId, name)
        | _ -> NoSelection

// =====================================
// Components
// =====================================

/// Graph visualization React component
[<ReactComponent>]
let private GraphVisualization (graph: RelationshipGraph) (focusedNodeId: string option) (dispatch: Msg -> unit) =
    let containerId = "force-graph-container"

    React.useEffect(fun () ->
        let jsGraph = toJsGraph graph

        let onSelect = fun jsNode ->
            let selection = parseNodeSelection jsNode
            dispatch (SelectNode selection)

        let onFocus = fun jsNode ->
            let selection = parseNodeSelection jsNode
            dispatch (FocusOnNode selection)

        ForceGraph.initializeGraph containerId jsGraph onSelect onFocus |> ignore

        // Cleanup on unmount
        React.createDisposable(fun () ->
            ForceGraph.destroyGraph containerId |> ignore
        )
    , [| box graph |])

    // Center on focused node when it changes
    React.useEffect(fun () ->
        match focusedNodeId with
        | Some nodeId ->
            // Small delay to let the graph settle before centering
            Browser.Dom.window.setTimeout((fun () ->
                ForceGraph.focusOnNode nodeId |> ignore
            ), 500) |> ignore
        | None -> ()
    , [| box focusedNodeId |])

    Html.div [
        prop.id containerId
        prop.className "w-full h-full min-h-[500px] bg-base-300/30 rounded-2xl overflow-hidden"
    ]

/// Search bar component
let private searchBar (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative"
        prop.children [
            Html.input [
                prop.type' "text"
                prop.className "w-full pl-10 pr-4 py-2.5 bg-base-100/50 border border-white/10 rounded-lg text-sm placeholder:text-base-content/40 focus:outline-none focus:border-primary/50"
                prop.placeholder "Search movies, series, friends..."
                prop.value (model.Filter.SearchQuery |> Option.defaultValue "")
                prop.onChange (fun (value: string) -> dispatch (SetSearchQuery value))
            ]
            Html.span [
                prop.className "absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-base-content/40"
                prop.children [ search ]
            ]
        ]
    ]

/// Zoom controls component
let private zoomControls () =
    Html.div [
        prop.className "flex items-center gap-1 glass rounded-lg px-2 py-1"
        prop.children [
            Html.button [
                prop.className "btn btn-ghost btn-xs"
                prop.onClick (fun _ -> ForceGraph.setZoom 0.5 |> ignore)
                prop.text "−"
            ]
            Html.button [
                prop.className "btn btn-ghost btn-xs"
                prop.onClick (fun _ -> ForceGraph.resetZoom() |> ignore)
                prop.text "Fit"
            ]
            Html.button [
                prop.className "btn btn-ghost btn-xs"
                prop.onClick (fun _ -> ForceGraph.setZoom 2.0 |> ignore)
                prop.text "+"
            ]
        ]
    ]

/// Selected node detail panel
let private selectedNodePanel (model: Model) (dispatch: Msg -> unit) =
    match model.SelectedNode with
    | NoSelection -> Html.none
    | selected ->
        let (title, subtitle, posterPath, action) =
            match selected with
            | SelectedMovie (entryId, title, posterPath) ->
                (title, "Movie", posterPath, (fun () -> dispatch (ViewMovieDetail (entryId, title))))
            | SelectedSeries (entryId, name, posterPath) ->
                (name, "Series", posterPath, (fun () -> dispatch (ViewSeriesDetail (entryId, name))))
            | SelectedFriend (friendId, name) ->
                (name, "Friend", None, (fun () -> dispatch (ViewFriendDetail (friendId, name))))
            | SelectedContributor (contributorId, name, profilePath) ->
                (name, "Contributor", profilePath, (fun () -> dispatch (ViewContributor (contributorId, name))))
            | SelectedCollection (collectionId, name) ->
                (name, "Collection", None, (fun () -> dispatch (ViewCollection (collectionId, name))))
            | NoSelection -> ("", "", None, (fun () -> ()))

        Html.div [
            prop.className "glass rounded-xl p-4 space-y-3"
            prop.children [
                // Close button
                Html.div [
                    prop.className "flex justify-between items-center"
                    prop.children [
                        Html.span [
                            prop.className "text-xs uppercase tracking-wide text-base-content/60"
                            prop.text subtitle
                        ]
                        Html.button [
                            prop.className "btn btn-ghost btn-xs btn-circle"
                            prop.onClick (fun _ -> dispatch DeselectNode)
                            prop.children [ close ]
                        ]
                    ]
                ]

                // Poster/Avatar
                match posterPath with
                | Some path ->
                    Html.div [
                        prop.className "flex justify-center"
                        prop.children [
                            Html.img [
                                prop.className "w-20 h-28 object-cover rounded-lg shadow-lg"
                                prop.src $"/images/posters{path}"
                                prop.alt title
                            ]
                        ]
                    ]
                | None ->
                    Html.div [
                        prop.className "flex justify-center"
                        prop.children [
                            Html.div [
                                prop.className "w-16 h-16 rounded-full bg-base-300 flex items-center justify-center text-2xl font-bold"
                                prop.children [
                                    Html.text (title.Substring(0, min 2 title.Length).ToUpperInvariant())
                                ]
                            ]
                        ]
                    ]

                // Title
                Html.h3 [
                    prop.className "text-center font-semibold text-lg"
                    prop.text title
                ]

                // View details button
                Html.button [
                    prop.className "btn btn-primary btn-sm w-full"
                    prop.onClick (fun _ -> action())
                    prop.text "View Details"
                ]
            ]
        ]

// =====================================
// Main View
// =====================================

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header
            Html.div [
                prop.className "flex items-center gap-3"
                prop.children [
                    Html.div [
                        prop.className "w-12 h-12 rounded-xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center"
                        prop.children [
                            Html.span [
                                prop.className "w-6 h-6 text-primary"
                                prop.children [ stats ]  // Using stats icon as placeholder
                            ]
                        ]
                    ]
                    Html.div [
                        prop.children [
                            Html.h1 [
                                prop.className "text-2xl font-bold"
                                prop.text "Relationship Graph"
                            ]
                            Html.p [
                                prop.className "text-sm text-base-content/60"
                                prop.text "Explore connections between your movies, series, and friends"
                            ]
                        ]
                    ]
                ]
            ]

            // Main content
            match model.Graph with
            | NotAsked | Loading ->
                Html.div [
                    prop.className "flex items-center justify-center h-96"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg text-primary" ]
                    ]
                ]

            | Failure err ->
                Html.div [
                    prop.className "glass rounded-2xl p-8 text-center"
                    prop.children [
                        Html.span [
                            prop.className "w-12 h-12 text-error mx-auto block mb-4"
                            prop.children [ error ]
                        ]
                        Html.p [
                            prop.className "text-error"
                            prop.text $"Failed to load graph: {err}"
                        ]
                        Html.button [
                            prop.className "btn btn-primary mt-4"
                            prop.onClick (fun _ -> dispatch LoadGraph)
                            prop.text "Retry"
                        ]
                    ]
                ]

            | Success graph ->
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        // Search bar at top
                        Html.div [
                            prop.className "max-w-md"
                            prop.children [ searchBar model dispatch ]
                        ]

                        // Graph canvas with overlays
                        Html.div [
                            prop.className "relative h-[calc(100vh-280px)] min-h-[500px]"
                            prop.children [
                                GraphVisualization graph model.FocusedNodeId dispatch

                                // Zoom controls (bottom right)
                                Html.div [
                                    prop.className "absolute bottom-4 right-4"
                                    prop.children [ zoomControls () ]
                                ]

                                // Show subtle loading indicator when refreshing
                                if model.IsRefreshing then
                                    Html.div [
                                        prop.className "absolute top-4 right-4 flex items-center gap-2 glass rounded-lg px-3 py-2"
                                        prop.children [
                                            Html.span [ prop.className "loading loading-spinner loading-sm text-primary" ]
                                            Html.span [
                                                prop.className "text-sm text-base-content/70"
                                                prop.text "Updating..."
                                            ]
                                        ]
                                    ]

                                // Show focus mode indicator with clear button
                                if model.Filter.FocusedNode.IsSome then
                                    Html.div [
                                        prop.className "absolute top-4 left-4 flex items-center gap-2 glass rounded-lg px-3 py-2"
                                        prop.children [
                                            Html.span [
                                                prop.className "text-sm text-primary font-medium"
                                                prop.text "Focus Mode"
                                            ]
                                            Html.button [
                                                prop.className "btn btn-ghost btn-xs"
                                                prop.onClick (fun _ -> dispatch ClearFocus)
                                                prop.children [
                                                    Html.span [
                                                        prop.className "text-xs"
                                                        prop.text "✕ Clear"
                                                    ]
                                                ]
                                            ]
                                        ]
                                    ]

                                // Selected node panel (bottom left)
                                Html.div [
                                    prop.className "absolute bottom-4 left-4 w-56"
                                    prop.children [ selectedNodePanel model dispatch ]
                                ]
                            ]
                        ]
                    ]
                ]
        ]
    ]
