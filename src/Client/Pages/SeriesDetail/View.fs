module Pages.SeriesDetail.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

/// Progress bar component
let private progressBar (current: int) (total: int) =
    let percentage = if total > 0 then float current / float total * 100.0 else 0.0
    Html.div [
        prop.className "w-full"
        prop.children [
            Html.div [
                prop.className "flex justify-between text-sm mb-1"
                prop.children [
                    Html.span [ prop.text $"{current} / {total}" ]
                    Html.span [ prop.text $"{percentage:F0}%%" ]
                ]
            ]
            Html.div [
                prop.className "w-full bg-base-300 rounded-full h-2"
                prop.children [
                    Html.div [
                        prop.className "bg-primary h-2 rounded-full transition-all duration-300"
                        prop.style [ Feliz.style.width (Feliz.length.percent (int percentage)) ]
                    ]
                ]
            ]
        ]
    ]

/// Episode checkbox component
let private episodeCheckbox (seasonNum: int) (epNum: int) (isWatched: bool) (dispatch: Msg -> unit) =
    let stateClass = if isWatched then "bg-success/20 border-success" else "bg-base-200 border-base-300 hover:border-primary"
    Html.label [
        prop.className $"cursor-pointer p-2 rounded border transition-colors {stateClass}"
        prop.children [
            Html.input [
                prop.type' "checkbox"
                prop.className "hidden"
                prop.isChecked isWatched
                prop.onChange (fun (checked': bool) ->
                    dispatch (ToggleEpisodeWatched (seasonNum, epNum, checked')))
            ]
            Html.span [
                prop.className "text-xs font-medium"
                prop.text $"E{epNum}"
            ]
        ]
    ]

/// Rating stars component
let private ratingStars (current: int option) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex gap-1"
        prop.children [
            for i in 1..5 do
                let isFilled = current |> Option.map (fun r -> i <= r) |> Option.defaultValue false
                Html.button [
                    prop.className (
                        "w-6 h-6 transition-colors " +
                        if isFilled then "text-yellow-400" else "text-base-content/20 hover:text-yellow-400/50"
                    )
                    prop.onClick (fun _ -> dispatch (SetRating i))
                    prop.children [ starSolid ]
                ]
        ]
    ]

/// Group episode progress by season
let private groupBySeasons (progress: EpisodeProgress list) : Map<int, int list> =
    progress
    |> List.filter (fun p -> p.IsWatched)
    |> List.groupBy (fun p -> p.SeasonNumber)
    |> List.map (fun (season, eps) -> season, eps |> List.map (fun e -> e.EpisodeNumber))
    |> Map.ofList

let view (model: Model) (tags: Tag list) (friends: Friend list) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> dispatch GoBack)
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back to Library" ]
                ]
            ]

            match model.Entry with
            | Loading ->
                Html.div [
                    prop.className "flex justify-center py-16"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]
            | Success entry ->
                match entry.Media with
                | LibrarySeries series ->
                    let watchedCount =
                        model.EpisodeProgress
                        |> List.filter (fun p -> p.IsWatched)
                        |> List.length

                    Html.div [
                        prop.className "grid grid-cols-1 md:grid-cols-3 gap-8"
                        prop.children [
                            // Left column - Poster
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    Html.div [
                                        prop.className "poster-image-container poster-shadow"
                                        prop.children [
                                            match series.PosterPath with
                                            | Some _ ->
                                                Html.img [
                                                    prop.src (getLocalPosterUrl series.PosterPath)
                                                    prop.alt series.Name
                                                    prop.className "poster-image"
                                                ]
                                            | None ->
                                                Html.div [
                                                    prop.className "w-full h-full flex items-center justify-center bg-base-200"
                                                    prop.children [
                                                        Html.span [ prop.className "text-6xl text-base-content/20"; prop.children [ tv ] ]
                                                    ]
                                                ]
                                        ]
                                    ]
                                    // Progress
                                    progressBar watchedCount series.NumberOfEpisodes

                                    // Watch controls
                                    match entry.WatchStatus with
                                    | NotStarted | InProgress _ ->
                                        if watchedCount >= series.NumberOfEpisodes then
                                            Html.button [
                                                prop.className "btn btn-primary btn-sm w-full"
                                                prop.onClick (fun _ -> dispatch MarkSeriesCompleted)
                                                prop.text "Mark as Completed"
                                            ]
                                        Html.button [
                                            prop.className "btn btn-outline btn-error btn-sm w-full"
                                            prop.onClick (fun _ -> dispatch OpenAbandonModal)
                                            prop.text "Abandon"
                                        ]
                                    | Completed ->
                                        Html.div [
                                            prop.className "text-center text-success text-sm"
                                            prop.text "Series Completed"
                                        ]
                                    | Abandoned _ ->
                                        Html.button [
                                            prop.className "btn btn-primary btn-sm w-full"
                                            prop.onClick (fun _ -> dispatch ResumeEntry)
                                            prop.text "Resume Watching"
                                        ]
                                ]
                            ]

                            // Right column - Details
                            Html.div [
                                prop.className "md:col-span-2 space-y-6"
                                prop.children [
                                    // Title and meta
                                    Html.div [
                                        Html.h1 [
                                            prop.className "text-3xl font-bold"
                                            prop.text series.Name
                                        ]
                                        Html.div [
                                            prop.className "flex items-center gap-4 mt-2 text-base-content/60"
                                            prop.children [
                                                match series.FirstAirDate with
                                                | Some d -> Html.span [ prop.text (d.Year.ToString()) ]
                                                | None -> Html.none
                                                Html.span [ prop.text $"{series.NumberOfSeasons} Seasons" ]
                                                Html.span [ prop.text $"{series.NumberOfEpisodes} Episodes" ]
                                            ]
                                        ]
                                    ]

                                    // Overview
                                    match series.Overview with
                                    | Some overview when overview <> "" ->
                                        Html.div [
                                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Overview" ]
                                            Html.p [ prop.className "text-base-content/70"; prop.text overview ]
                                        ]
                                    | _ -> Html.none

                                    // Episode progress summary by season
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-4"; prop.text "Episodes" ]
                                        Html.div [
                                            prop.className "space-y-4"
                                            prop.children [
                                                // Group progress by season and show summary
                                                let seasonProgress =
                                                    model.EpisodeProgress
                                                    |> List.groupBy (fun p -> p.SeasonNumber)
                                                    |> List.sortBy fst

                                                if List.isEmpty seasonProgress then
                                                    Html.div [
                                                        prop.className "card bg-base-200 p-4"
                                                        prop.children [
                                                            Html.p [
                                                                prop.className "text-base-content/60 text-sm"
                                                                prop.text "Episode progress will appear here as you watch."
                                                            ]
                                                        ]
                                                    ]
                                                else
                                                    for (seasonNum, episodes) in seasonProgress do
                                                        let watchedInSeason =
                                                            episodes |> List.filter (fun p -> p.IsWatched) |> List.length
                                                        let totalInSeason = episodes |> List.length

                                                        Html.div [
                                                            prop.className "card bg-base-200"
                                                            prop.children [
                                                                Html.div [
                                                                    prop.className "card-body p-4"
                                                                    prop.children [
                                                                        Html.div [
                                                                            prop.className "flex justify-between items-center mb-3"
                                                                            prop.children [
                                                                                Html.h4 [
                                                                                    prop.className "font-semibold"
                                                                                    prop.text $"Season {seasonNum}"
                                                                                ]
                                                                                Html.div [
                                                                                    prop.className "flex items-center gap-2"
                                                                                    prop.children [
                                                                                        Html.span [
                                                                                            prop.className "text-sm text-base-content/60"
                                                                                            prop.text $"{watchedInSeason}/{totalInSeason}"
                                                                                        ]
                                                                                        Html.button [
                                                                                            prop.className "btn btn-xs btn-ghost"
                                                                                            prop.onClick (fun _ -> dispatch (MarkSeasonWatched seasonNum))
                                                                                            prop.text "Mark All"
                                                                                        ]
                                                                                    ]
                                                                                ]
                                                                            ]
                                                                        ]
                                                                        Html.div [
                                                                            prop.className "grid grid-cols-5 sm:grid-cols-8 md:grid-cols-10 gap-1"
                                                                            prop.children [
                                                                                for ep in episodes |> List.sortBy (fun e -> e.EpisodeNumber) do
                                                                                    episodeCheckbox seasonNum ep.EpisodeNumber ep.IsWatched dispatch
                                                                            ]
                                                                        ]
                                                                    ]
                                                                ]
                                                            ]
                                                        ]
                                            ]
                                        ]
                                    ]

                                    // Rating
                                    Html.div [
                                        Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Your Rating" ]
                                        ratingStars (entry.PersonalRating |> Option.map PersonalRating.toInt) dispatch
                                    ]

                                    // Favorite toggle
                                    Html.div [
                                        prop.className "flex items-center gap-2"
                                        prop.children [
                                            Html.button [
                                                prop.className (
                                                    "btn btn-sm " +
                                                    if entry.IsFavorite then "btn-secondary" else "btn-ghost"
                                                )
                                                prop.onClick (fun _ -> dispatch ToggleFavorite)
                                                prop.children [
                                                    Html.span [ prop.className "w-4 h-4"; prop.children [ if entry.IsFavorite then heartSolid else heart ] ]
                                                    Html.span [ prop.text (if entry.IsFavorite then "Favorited" else "Add to Favorites") ]
                                                ]
                                            ]
                                        ]
                                    ]

                                    // Delete button
                                    Html.div [
                                        prop.className "pt-4 border-t border-base-300"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-error btn-outline btn-sm"
                                                prop.onClick (fun _ -> dispatch OpenDeleteModal)
                                                prop.children [
                                                    Html.span [ prop.className "w-4 h-4"; prop.children [ trash ] ]
                                                    Html.span [ prop.text "Delete Entry" ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                | LibraryMovie _ ->
                    Html.div [
                        prop.className "text-center py-12"
                        prop.text "This is a movie, not a series"
                    ]

            | Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text err
                ]
            | NotAsked -> Html.none
        ]
    ]
