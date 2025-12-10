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
    [<Emit("import('../../forceGraph.js').then(m => m.initializeGraph($0, $1, $2))")>]
    let initializeGraph (containerId: string) (graphData: obj) (onSelect: obj -> unit) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.destroyGraph($0))")>]
    let destroyGraph (containerId: string) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.setZoom($0))")>]
    let setZoom (level: float) : JS.Promise<unit> = jsNative

    [<Emit("import('../../forceGraph.js').then(m => m.resetZoom())")>]
    let resetZoom () : JS.Promise<unit> = jsNative

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
                )
                "Relationship" ==> (
                    match edge.Relationship with
                    | WatchedWith -> createObj [ "Case" ==> "WatchedWith" ]
                    | WorkedOn role -> createObj [ "Case" ==> "WorkedOn"; "Fields" ==> [| role |] ]
                    | InCollection collId -> createObj [ "Case" ==> "InCollection"; "Fields" ==> [| CollectionId.value collId |] ]
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
        | _ -> NoSelection

// =====================================
// Components
// =====================================

/// Graph visualization React component
[<ReactComponent>]
let private GraphVisualization (graph: RelationshipGraph) (dispatch: Msg -> unit) =
    let containerId = "force-graph-container"

    React.useEffect(fun () ->
        console.log("F# graph", graph)
        console.log("F# graph.Nodes", graph.Nodes)
        let jsGraph = toJsGraph graph
        console.log("jsGraph", jsGraph)
        let onSelect = fun jsNode ->
            let selection = parseNodeSelection jsNode
            dispatch (SelectNode selection)

        ForceGraph.initializeGraph containerId jsGraph onSelect |> ignore

        // Cleanup on unmount
        React.createDisposable(fun () ->
            ForceGraph.destroyGraph containerId |> ignore
        )
    , [| box graph |])

    Html.div [
        prop.id containerId
        prop.className "w-full h-full min-h-[500px] bg-base-300/30 rounded-2xl overflow-hidden"
    ]

/// Filter controls panel
let private filterPanel (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "glass rounded-xl p-4 space-y-4"
        prop.children [
            Html.h3 [
                prop.className "text-sm font-semibold text-base-content/80 uppercase tracking-wide"
                prop.text "Filters"
            ]

            // Node type toggles
            Html.div [
                prop.className "space-y-2"
                prop.children [
                    // Movies toggle
                    Html.label [
                        prop.className "flex items-center gap-2 cursor-pointer"
                        prop.children [
                            Html.input [
                                prop.type' "checkbox"
                                prop.className "checkbox checkbox-primary checkbox-sm"
                                prop.isChecked model.Filter.IncludeMovies
                                prop.onChange (fun (checked': bool) ->
                                    dispatch (UpdateFilter { model.Filter with IncludeMovies = checked' })
                                )
                            ]
                            Html.span [
                                prop.className "flex items-center gap-1.5 text-sm"
                                prop.children [
                                    Html.span [ prop.className "w-3 h-3 rounded-full bg-amber-500" ]
                                    Html.span [ prop.text "Movies" ]
                                ]
                            ]
                        ]
                    ]

                    // Series toggle
                    Html.label [
                        prop.className "flex items-center gap-2 cursor-pointer"
                        prop.children [
                            Html.input [
                                prop.type' "checkbox"
                                prop.className "checkbox checkbox-secondary checkbox-sm"
                                prop.isChecked model.Filter.IncludeSeries
                                prop.onChange (fun (checked': bool) ->
                                    dispatch (UpdateFilter { model.Filter with IncludeSeries = checked' })
                                )
                            ]
                            Html.span [
                                prop.className "flex items-center gap-1.5 text-sm"
                                prop.children [
                                    Html.span [ prop.className "w-3 h-3 rounded-full bg-purple-500" ]
                                    Html.span [ prop.text "Series" ]
                                ]
                            ]
                        ]
                    ]

                    // Friends toggle
                    Html.label [
                        prop.className "flex items-center gap-2 cursor-pointer"
                        prop.children [
                            Html.input [
                                prop.type' "checkbox"
                                prop.className "checkbox checkbox-success checkbox-sm"
                                prop.isChecked model.Filter.IncludeFriends
                                prop.onChange (fun (checked': bool) ->
                                    dispatch (UpdateFilter { model.Filter with IncludeFriends = checked' })
                                )
                            ]
                            Html.span [
                                prop.className "flex items-center gap-1.5 text-sm"
                                prop.children [
                                    Html.span [ prop.className "w-3 h-3 rounded-full bg-green-500" ]
                                    Html.span [ prop.text "Friends" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Zoom controls
            Html.div [
                prop.className "pt-4 border-t border-base-content/10 space-y-2"
                prop.children [
                    Html.h4 [
                        prop.className "text-sm font-semibold text-base-content/80"
                        prop.text "Zoom"
                    ]
                    Html.div [
                        prop.className "flex gap-2"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-ghost btn-sm flex-1"
                                prop.onClick (fun _ -> ForceGraph.setZoom 0.5 |> ignore)
                                prop.text "-"
                            ]
                            Html.button [
                                prop.className "btn btn-ghost btn-sm flex-1"
                                prop.onClick (fun _ -> ForceGraph.resetZoom() |> ignore)
                                prop.text "Fit"
                            ]
                            Html.button [
                                prop.className "btn btn-ghost btn-sm flex-1"
                                prop.onClick (fun _ -> ForceGraph.setZoom 2.0 |> ignore)
                                prop.text "+"
                            ]
                        ]
                    ]
                ]
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
                (title, "Movie", posterPath, fun () -> dispatch (ViewMovieDetail (entryId, title)))
            | SelectedSeries (entryId, name, posterPath) ->
                (name, "Series", posterPath, fun () -> dispatch (ViewSeriesDetail (entryId, name)))
            | SelectedFriend (friendId, name) ->
                (name, "Friend", None, fun () -> dispatch (ViewFriendDetail (friendId, name)))
            | SelectedContributor (contributorId, name, profilePath) ->
                (name, "Contributor", profilePath, fun () -> dispatch (ViewContributor (contributorId, name)))
            | NoSelection -> ("", "", None, fun () -> ())

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

/// Legend component
let private legend () =
    Html.div [
        prop.className "glass rounded-xl p-3"
        prop.children [
            Html.h4 [
                prop.className "text-xs uppercase tracking-wide text-base-content/60 mb-2"
                prop.text "Legend"
            ]
            Html.div [
                prop.className "flex flex-wrap gap-3 text-xs"
                prop.children [
                    Html.div [
                        prop.className "flex items-center gap-1.5"
                        prop.children [
                            Html.span [ prop.className "w-3 h-3 rounded bg-amber-500" ]
                            Html.span [ prop.text "Movie" ]
                        ]
                    ]
                    Html.div [
                        prop.className "flex items-center gap-1.5"
                        prop.children [
                            Html.span [ prop.className "w-3 h-3 rounded bg-purple-500" ]
                            Html.span [ prop.text "Series" ]
                        ]
                    ]
                    Html.div [
                        prop.className "flex items-center gap-1.5"
                        prop.children [
                            Html.span [ prop.className "w-3 h-3 rounded-full bg-green-500" ]
                            Html.span [ prop.text "Friend" ]
                        ]
                    ]
                ]
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
                if List.isEmpty graph.Nodes then
                    Html.div [
                        prop.className "glass rounded-2xl p-8 text-center"
                        prop.children [
                            Html.p [
                                prop.className "text-base-content/60 text-lg"
                                prop.text "No data to display"
                            ]
                            Html.p [
                                prop.className "text-base-content/40 text-sm mt-2"
                                prop.text "Add some movies or series to your library to see the relationship graph"
                            ]
                        ]
                    ]
                else
                    Html.div [
                        prop.className "flex gap-4 h-[calc(100vh-220px)] min-h-[500px]"
                        prop.children [
                            // Left sidebar
                            Html.div [
                                prop.className "w-56 flex-shrink-0 space-y-4"
                                prop.children [
                                    filterPanel model dispatch
                                    legend ()
                                    selectedNodePanel model dispatch
                                ]
                            ]

                            // Graph canvas
                            Html.div [
                                prop.className "flex-1"
                                prop.children [
                                    GraphVisualization graph dispatch
                                ]
                            ]
                        ]
                    ]
        ]
    ]
