module Components.SearchModal.View

open Feliz
open Common.Types
open Shared.Domain
open Common.Components.PosterCard.Types
open Types
open State
open Components.Icons
open Components.Cards.View

module PosterCard = Common.Components.PosterCard.View

/// Find a library entry matching a TMDB search result (if it exists)
let private findInLibrary (libraryEntries: LibraryEntry list) (item: TmdbSearchResult) =
    libraryEntries |> List.tryFind (fun entry ->
        match item.MediaType, entry.Media with
        | MediaType.Movie, LibraryMovie m -> m.TmdbId = TmdbMovieId item.TmdbId
        | MediaType.Series, LibrarySeries s -> s.TmdbId = TmdbSeriesId item.TmdbId
        | _ -> false
    )

/// TMDB search result poster card with title, sized for the search modal grid
let private searchPosterCard (item: TmdbSearchResult) (onSelect: TmdbSearchResult -> unit) (isInLibrary: bool) =
    let year =
        item.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let posterUrl = getTmdbPosterUrl "w342" item.PosterPath

    let config =
        { Config.searchResult
            (if posterUrl = "" then None else Some posterUrl)
            item.Title
            item.MediaType
            (fun () -> onSelect item)
            isInLibrary
          with Size = Mobile }

    Html.div [
        prop.children [
            PosterCard.view config

            Html.div [
                prop.className "mt-2 space-y-0.5"
                prop.children [
                    Html.p [
                        prop.className (if isInLibrary then "font-medium text-xs truncate text-base-content/50" else "font-medium text-xs truncate text-base-content/90")
                        prop.title item.Title
                        prop.text item.Title
                    ]
                    if year <> "" then
                        Html.p [
                            prop.className "text-[10px] text-base-content/50"
                            prop.text year
                        ]
                ]
            ]
        ]
    ]

/// Library entry poster card with title, sized for the search modal grid
let private libraryPosterCard (entry: LibraryEntry) (onClick: unit -> unit) =
    let (title, posterPath) =
        match entry.Media with
        | LibraryMovie m -> (m.Title, m.PosterPath)
        | LibrarySeries s -> (s.Name, s.PosterPath)

    let topLeftBadge =
        entry.PersonalRating
        |> Option.map (PosterCard.ratingToBadge >> RatingBadge)

    let bottomOverlay =
        match entry.Media with
        | LibrarySeries _ ->
            match entry.WatchStatus with
            | Completed -> Some FinishedBanner
            | Abandoned _ -> Some AbandonedBanner
            | _ -> None
        | LibraryMovie _ -> None

    let config = {
        PosterUrl = posterPath |> Option.map (fun p -> PosterCard.getLocalPosterUrl (Some p))
        Title = title
        OnClick = onClick
        TopLeftBadge = topLeftBadge
        BottomOverlay = bottomOverlay
        IsGrayscale = false
        IsDimmed = false
        MediaType = Some (match entry.Media with LibraryMovie _ -> Movie | LibrarySeries _ -> Series)
        ShowInLibraryOverlay = false
        MediaTypeBadge = None
        ShowAddButton = false
        Size = Mobile
    }

    Html.div [
        prop.children [
            PosterCard.view config
            Html.p [
                prop.className "text-[10px] mt-2 truncate text-base-content/70 group-hover:text-primary transition-colors"
                prop.text title
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-start justify-center pt-[10vh] p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "modal-backdrop"
                prop.onClick (fun _ -> dispatch Close)
            ]
            // Modal content
            Html.div [
                prop.className "modal-content relative w-full max-w-5xl"
                prop.children [
                    // Search input
                    Html.div [
                        prop.className "p-4 border-b border-base-300/50"
                        prop.children [
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.input [
                                        prop.className "w-full pl-12 pr-4 py-3 bg-transparent text-lg placeholder:text-base-content/30 focus:outline-none"
                                        prop.placeholder "Search movies and series..."
                                        prop.value model.Query
                                        prop.autoFocus true
                                        prop.onChange (fun (e: string) -> dispatch (QueryChanged e))
                                        prop.onKeyDown (fun e ->
                                            if e.key = "Escape" then dispatch Close
                                        )
                                    ]
                                    Html.span [
                                        prop.className "absolute left-4 top-1/2 -translate-y-1/2 w-5 h-5 text-base-content/40"
                                        prop.children [ search ]
                                    ]
                                    if RemoteData.isLoading model.Results then
                                        Html.span [
                                            prop.className "absolute right-4 top-1/2 -translate-y-1/2"
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner loading-sm text-primary" ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                    // Results area
                    Html.div [
                        prop.className "max-h-[60vh] overflow-y-auto"
                        prop.children [
                            let hasQuery = model.Query.Length >= 2
                            let filteredLibrary =
                                if hasQuery then filterLibraryEntries model.Query model.LibraryEntries
                                else model.LibraryEntries |> List.truncate 5  // Show 5 most recent when no query

                            if not hasQuery && List.isEmpty model.LibraryEntries then
                                // No query and no library entries
                                Html.div [
                                    prop.className "p-8 text-center text-base-content/40"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-12 h-12 mx-auto mb-3 opacity-30 block"
                                            prop.children [ search ]
                                        ]
                                        Html.p [ prop.text "Start typing to search" ]
                                    ]
                                ]
                            else if not hasQuery then
                                // No query but has library entries - show recent
                                Html.div [
                                    prop.className "p-4"
                                    prop.children [
                                        Html.h3 [
                                            prop.className "text-xs font-semibold text-base-content/50 uppercase tracking-wider mb-3"
                                            prop.text "Recent from Library"
                                        ]
                                        Html.div [
                                            prop.className "grid grid-cols-5 gap-3"
                                            prop.children [
                                                for entry in filteredLibrary do
                                                    let mediaType =
                                                        match entry.Media with
                                                        | LibraryMovie _ -> MediaType.Movie
                                                        | LibrarySeries _ -> MediaType.Series
                                                    let title =
                                                        match entry.Media with
                                                        | LibraryMovie m -> m.Title
                                                        | LibrarySeries s -> s.Name
                                                    libraryPosterCard entry (fun () ->
                                                        dispatch (SelectLibraryItem (entry.Id, mediaType, title)))
                                            ]
                                        ]
                                    ]
                                ]
                            else
                                Html.div [
                                    prop.className "p-4 space-y-6"
                                    prop.children [
                                        // TMDB Results Section
                                        Html.div [
                                            prop.children [
                                                Html.h3 [
                                                    prop.className "text-xs font-semibold text-base-content/50 uppercase tracking-wider mb-3"
                                                    prop.text "TMDB Results"
                                                ]
                                                match model.Results with
                                                | Loading ->
                                                    Html.div [
                                                        prop.className "flex items-center gap-2 text-base-content/50"
                                                        prop.children [
                                                            Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                            Html.span [ prop.text "Searching..." ]
                                                        ]
                                                    ]
                                                | Success results when List.isEmpty results ->
                                                    Html.p [
                                                        prop.className "text-sm text-base-content/40"
                                                        prop.text "No results from TMDB"
                                                    ]
                                                | Success results ->
                                                    Html.div [
                                                        prop.className "grid grid-cols-5 gap-3"
                                                        prop.children [
                                                            for item in results |> List.truncate 5 do
                                                                let libraryEntry = findInLibrary model.LibraryEntries item
                                                                let clickHandler =
                                                                    match libraryEntry with
                                                                    | Some entry ->
                                                                        let title = match entry.Media with LibraryMovie m -> m.Title | LibrarySeries s -> s.Name
                                                                        fun _ -> dispatch (SelectLibraryItem (entry.Id, item.MediaType, title))
                                                                    | None ->
                                                                        fun i -> dispatch (SelectTmdbItem i)
                                                                searchPosterCard item clickHandler (Option.isSome libraryEntry)
                                                        ]
                                                    ]
                                                | Failure err ->
                                                    Html.p [
                                                        prop.className "text-sm text-error"
                                                        prop.text $"Error: {err}"
                                                    ]
                                                | NotAsked ->
                                                    Html.p [
                                                        prop.className "text-sm text-base-content/40"
                                                        prop.text "Searching..."
                                                    ]
                                            ]
                                        ]

                                        // My Library Section
                                        Html.div [
                                            prop.children [
                                                Html.h3 [
                                                    prop.className "text-xs font-semibold text-base-content/50 uppercase tracking-wider mb-3"
                                                    prop.text "My Library"
                                                ]
                                                if List.isEmpty filteredLibrary then
                                                    Html.p [
                                                        prop.className "text-sm text-base-content/40"
                                                        prop.text "No matches in your library"
                                                    ]
                                                else
                                                    Html.div [
                                                        prop.className "grid grid-cols-5 gap-3"
                                                        prop.children [
                                                            for entry in filteredLibrary do
                                                                let mediaType =
                                                                    match entry.Media with
                                                                    | LibraryMovie _ -> MediaType.Movie
                                                                    | LibrarySeries _ -> MediaType.Series
                                                                let title =
                                                                    match entry.Media with
                                                                    | LibraryMovie m -> m.Title
                                                                    | LibrarySeries s -> s.Name
                                                                libraryPosterCard entry (fun () ->
                                                                    dispatch (SelectLibraryItem (entry.Id, mediaType, title)))
                                                        ]
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]
                        ]
                    ]
                    // Footer with keyboard hints
                    Html.div [
                        prop.className "p-3 border-t border-base-300/50 flex items-center justify-end gap-4 text-xs text-base-content/40"
                        prop.children [
                            Html.span [
                                prop.className "flex items-center gap-1"
                                prop.children [
                                    Html.kbd [ prop.className "px-1.5 py-0.5 bg-base-300/50 rounded text-[10px]"; prop.text "ESC" ]
                                    Html.span [ prop.text "to close" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
