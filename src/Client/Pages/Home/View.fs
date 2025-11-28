module Pages.Home.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-10"
        prop.children [
            // Hero section
            Html.div [
                prop.className "text-center py-16 space-y-6"
                prop.children [
                    // Logo icon
                    Html.div [
                        prop.className "mx-auto w-20 h-20 rounded-2xl bg-gradient-to-br from-primary/20 to-secondary/20 flex items-center justify-center mb-6 shadow-glow-primary"
                        prop.children [
                            Html.span [
                                prop.className "w-10 h-10 text-primary"
                                prop.children [ clapperboard ]
                            ]
                        ]
                    ]

                    Html.h2 [
                        prop.className "text-4xl font-bold"
                        prop.children [
                            Html.span [ prop.text "Your " ]
                            Html.span [ prop.className "text-gradient"; prop.text "Cinema" ]
                            Html.span [ prop.text " Memory Tracker" ]
                        ]
                    ]

                    Html.p [
                        prop.className "text-base-content/60 max-w-xl mx-auto text-lg leading-relaxed"
                        prop.text "Search for movies and series to add them to your personal library. Track what you've watched, who you watched with, and capture your thoughts."
                    ]

                    // Quick tips
                    Html.div [
                        prop.className "flex flex-wrap justify-center gap-4 mt-8"
                        prop.children [
                            for (icon, tip) in [
                                (search, "Search above to find titles")
                                (plus, "Click any result to add")
                                (friends, "Track who you watch with")
                            ] do
                                Html.div [
                                    prop.className "flex items-center gap-2 px-4 py-2 glass rounded-full text-sm text-base-content/60"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-4 h-4"
                                            prop.children [ icon ]
                                        ]
                                        Html.span [ prop.text tip ]
                                    ]
                                ]
                        ]
                    ]
                ]
            ]

            // Recently added section
            match model.Library with
            | Success entries when not (List.isEmpty entries) ->
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        Html.div [
                            prop.className "flex justify-between items-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "text-xl font-bold"
                                    prop.text "Recently Added"
                                ]
                                Html.button [
                                    prop.className "flex items-center gap-2 text-sm text-primary hover:underline"
                                    prop.onClick (fun _ -> dispatch ViewLibrary)
                                    prop.children [
                                        Html.span [ prop.text "View All" ]
                                        Html.span [
                                            prop.className "w-4 h-4"
                                            prop.children [ arrowRight ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                        Html.div [
                            prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                            prop.children [
                                for entry in entries |> List.sortByDescending (fun e -> e.DateAdded) |> List.truncate 12 do
                                    libraryEntryCard entry (fun id isMovie ->
                                        if isMovie then dispatch (ViewMovieDetail id)
                                        else dispatch (ViewSeriesDetail id))
                            ]
                        ]
                    ]
                ]
            | Success _ ->
                Html.div [
                    prop.className "text-center py-16"
                    prop.children [
                        Html.div [
                            prop.className "w-20 h-20 mx-auto mb-6 rounded-full bg-base-200 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "w-10 h-10 text-base-content/30"
                                    prop.children [ library ]
                                ]
                            ]
                        ]
                        Html.h3 [
                            prop.className "text-xl font-semibold mb-2 text-base-content/70"
                            prop.text "Your library is empty"
                        ]
                        Html.p [
                            prop.className "text-base-content/50"
                            prop.text "Search for a movie or series above to get started"
                        ]
                    ]
                ]
            | Loading ->
                Html.div [
                    prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                    prop.children [
                        for _ in 1..6 do
                            Html.div [
                                prop.className "space-y-3"
                                prop.children [
                                    Html.div [ prop.className "skeleton aspect-[2/3] rounded-lg" ]
                                    Html.div [ prop.className "skeleton h-4 w-3/4 rounded" ]
                                    Html.div [ prop.className "skeleton h-3 w-1/2 rounded" ]
                                ]
                            ]
                    ]
                ]
            | Failure err ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.children [
                        Html.span [
                            prop.className "w-12 h-12 mx-auto mb-4 text-error/50 block"
                            prop.children [ error ]
                        ]
                        Html.p [
                            prop.className "text-error"
                            prop.text $"Error loading library: {err}"
                        ]
                    ]
                ]
            | NotAsked -> Html.none
        ]
    ]
