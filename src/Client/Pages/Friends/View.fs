module Pages.Friends.View

open Feliz
open Common.Types
open Shared.Domain
open Types

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

            match model.Friends with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-12"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success friendList when List.isEmpty friendList ->
                Html.div [
                    prop.className "text-center py-12 text-base-content/60"
                    prop.children [
                        Html.p [ prop.className "text-lg mb-2"; prop.text "No friends yet" ]
                        Html.p [ prop.text "Add friends to track who you watch with!" ]
                    ]
                ]
            | Success friendList ->
                Html.div [
                    prop.className "grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4"
                    prop.children [
                        for friend in friendList do
                            Html.div [
                                prop.className "card bg-base-200 hover:shadow-lg transition-shadow cursor-pointer"
                                prop.onClick (fun _ -> dispatch (ViewFriendDetail friend.Id))
                                prop.children [
                                    Html.div [
                                        prop.className "card-body"
                                        prop.children [
                                            Html.div [
                                                prop.className "flex items-center gap-3"
                                                prop.children [
                                                    // Avatar placeholder
                                                    Html.div [
                                                        prop.className "avatar placeholder"
                                                        prop.children [
                                                            Html.div [
                                                                prop.className "bg-primary text-primary-content rounded-full w-12"
                                                                prop.children [
                                                                    Html.span [
                                                                        prop.className "text-xl"
                                                                        prop.text (friend.Name.Substring(0, 1).ToUpperInvariant())
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex-1"
                                                        prop.children [
                                                            Html.h3 [
                                                                prop.className "font-bold"
                                                                prop.text friend.Name
                                                            ]
                                                            match friend.Nickname with
                                                            | Some nick ->
                                                                Html.p [
                                                                    prop.className "text-sm text-base-content/60"
                                                                    prop.text nick
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                ]
                                            ]
                                            // Action buttons
                                            Html.div [
                                                prop.className "card-actions justify-end mt-2"
                                                prop.children [
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs"
                                                        prop.onClick (fun e ->
                                                            e.stopPropagation()
                                                            dispatch (OpenEditFriendModal friend)
                                                        )
                                                        prop.text "Edit"
                                                    ]
                                                    Html.button [
                                                        prop.className "btn btn-ghost btn-xs text-error"
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
