module Pages.Friends.View

open Feliz
open Common.Types
open Shared.Domain
open Types

/// Filter friends based on search query
let private filterFriends (model: Model) (friends: Friend list) =
    if System.String.IsNullOrWhiteSpace model.SearchQuery then
        friends
    else
        let query = model.SearchQuery.ToLowerInvariant()
        friends
        |> List.filter (fun f ->
            f.Name.ToLowerInvariant().Contains(query) ||
            (f.Nickname |> Option.map (fun n -> n.ToLowerInvariant().Contains(query)) |> Option.defaultValue false)
        )

/// Search input row
let private searchRow (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex items-center gap-4"
        prop.children [
            Html.div [
                prop.className "flex-1 max-w-xs"
                prop.children [
                    Html.input [
                        prop.type'.text
                        prop.placeholder "Search friends..."
                        prop.className "input input-bordered input-sm w-full"
                        prop.value model.SearchQuery
                        prop.onChange (fun v -> dispatch (SetSearchQuery v))
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Header with add button
            Html.div [
                prop.className "flex justify-between items-center"
                prop.children [
                    Html.h2 [
                        prop.className "text-2xl font-bold"
                        prop.text "Friends"
                    ]
                    Html.button [
                        prop.className "btn btn-primary"
                        prop.onClick (fun _ -> dispatch OpenAddFriendModal)
                        prop.children [
                            Html.span [ prop.text "+ Add Friend" ]
                        ]
                    ]
                ]
            ]

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Track who you watch movies and series with. Click on a friend to see what you've watched together."
            ]

            // Search
            searchRow model dispatch

            match model.Friends with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success friendList ->
                let filtered = filterFriends model friendList
                let sortedFriends = filtered |> List.sortBy (fun f -> f.Name.ToLowerInvariant())

                if List.isEmpty friendList then
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.children [
                            Html.p [ prop.className "text-lg mb-2"; prop.text "No friends yet" ]
                            Html.p [ prop.text "Add friends to track who you watch with!" ]
                        ]
                    ]
                elif List.isEmpty filtered then
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.text "No friends match your search."
                    ]
                else
                    Html.ul [
                        prop.className "space-y-2"
                        prop.children [
                            for friend in sortedFriends do
                            Html.li [
                                prop.className "flex items-center justify-between px-4 py-3 bg-base-200 rounded-lg hover:bg-base-300 transition-colors cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewFriendDetail friend.Id))
                                prop.children [
                                    // Left side: Name
                                    Html.div [
                                        prop.children [
                                            Html.span [
                                                prop.className "font-medium"
                                                prop.text friend.Name
                                            ]
                                            match friend.Nickname with
                                            | Some nick ->
                                                Html.span [
                                                    prop.className "text-sm text-base-content/60 ml-2"
                                                    prop.text $"({nick})"
                                                ]
                                            | None -> Html.none
                                        ]
                                    ]
                                    // Right side: Action buttons
                                    Html.div [
                                        prop.className "flex gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm"
                                                prop.onClick (fun e ->
                                                    e.stopPropagation()
                                                    dispatch (OpenEditFriendModal friend)
                                                )
                                                prop.text "Edit"
                                            ]
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm text-error"
                                                prop.onClick (fun e ->
                                                    e.stopPropagation()
                                                    dispatch (OpenDeleteFriendModal friend)
                                                )
                                                prop.text "Delete"
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text $"Error loading friends: {err}"
                ]
            | NotAsked -> Html.none
        ]
    ]
