module Pages.FriendDetail.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

let view (model: Model) (friend: Friend option) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> dispatch GoBack)
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back to Friends" ]
                ]
            ]

            match friend with
            | Some f ->
                // Friend header
                Html.div [
                    prop.className "flex items-center gap-4"
                    prop.children [
                        Html.div [
                            prop.className "avatar placeholder"
                            prop.children [
                                Html.div [
                                    prop.className "bg-primary text-primary-content rounded-full w-16"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-2xl"
                                            prop.text (f.Name.Substring(0, 1).ToUpperInvariant())
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            Html.h2 [
                                prop.className "text-2xl font-bold"
                                prop.text f.Name
                            ]
                            match f.Nickname with
                            | Some nick ->
                                Html.p [
                                    prop.className "text-base-content/60"
                                    prop.text nick
                                ]
                            | None -> Html.none
                        ]
                    ]
                ]

                // Watched together section
                Html.h3 [
                    prop.className "text-xl font-semibold mt-8"
                    prop.text "Watched Together"
                ]

                match model.Entries with
                | Loading ->
                    Html.div [
                        prop.className "flex justify-center py-12"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-lg" ]
                        ]
                    ]
                | Success entries when List.isEmpty entries ->
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.text "No entries watched with this friend yet."
                    ]
                | Success entries ->
                    Html.div [
                        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                        prop.children [
                            for entry in entries do
                                libraryEntryCard entry (fun id isMovie ->
                                    if isMovie then dispatch (ViewMovieDetail id)
                                    else dispatch (ViewSeriesDetail id))
                        ]
                    ]
                | Failure err ->
                    Html.div [
                        prop.className "alert alert-error"
                        prop.text err
                    ]
                | NotAsked -> Html.none

            | None ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Friend not found"
                ]
        ]
    ]
