module Pages.Friends.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons

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

/// Render a friend avatar (round, clickable to edit)
let private friendAvatar (friend: Friend) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative group cursor-pointer"
        prop.onClick (fun e ->
            e.stopPropagation()
            dispatch (OpenProfileImageModal friend))
        prop.children [
            // Avatar container with round shape
            Html.div [
                prop.className "avatar"
                prop.children [
                    match friend.AvatarUrl with
                    | Some url ->
                        Html.div [
                            prop.className "w-12 h-12 rounded-full ring ring-primary/20 ring-offset-base-100 ring-offset-1 overflow-hidden"
                            prop.children [
                                Html.img [
                                    prop.src $"/images/avatars{url}"
                                    prop.alt friend.Name
                                    prop.className "w-full h-full object-cover"
                                ]
                            ]
                        ]
                    | None ->
                        // Placeholder with initials
                        Html.div [
                            prop.className "avatar placeholder"
                            prop.children [
                                Html.div [
                                    prop.className "w-12 h-12 rounded-full bg-primary/20 text-primary-content flex items-center justify-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-lg font-semibold"
                                            prop.text (friend.Name.Substring(0, 1).ToUpperInvariant())
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
            // Edit overlay on hover
            Html.div [
                prop.className "absolute inset-0 rounded-full bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
                prop.children [
                    Html.span [
                        prop.className "text-white w-5 h-5"
                        prop.children [ camera ]
                    ]
                ]
            ]
        ]
    ]

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
                                prop.className "flex items-center gap-4 px-4 py-3 bg-base-200 rounded-lg hover:bg-base-300 transition-colors cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewFriendDetail (friend.Id, friend.Name)))
                                prop.children [
                                    // Avatar (clickable to edit image)
                                    friendAvatar friend dispatch

                                    // Name and nickname (links to detail)
                                    Html.div [
                                        prop.className "flex-1"
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

                                    // Right side: Delete button only
                                    Html.div [
                                        prop.className "flex gap-2"
                                        prop.children [
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
