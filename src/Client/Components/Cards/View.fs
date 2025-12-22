module Components.Cards.View

open Feliz
open Shared.Domain
open Components.Icons

// Import shared PosterCard component
module PosterCard = Common.Components.PosterCard.View
module PosterCardTypes = Common.Components.PosterCard.Types

/// TMDB image base URL (for search results)
let private tmdbImageBase = "https://image.tmdb.org/t/p"


/// Get poster URL from TMDB CDN (for search results)
let getTmdbPosterUrl (size: string) (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/{size}{p}"
    | None -> ""

/// Get local cached poster URL (for library items)
let getLocalPosterUrl (path: string option) =
    PosterCard.getLocalPosterUrl path

/// Get local cached backdrop URL (for library items)
let getLocalBackdropUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/backdrops/{filename}"
    | None -> ""

/// Get local cached episode still URL (for library items)
let getLocalStillUrl (path: string option) =
    match path with
    | Some p ->
        let filename = if p.StartsWith("/") then p.Substring(1) else p
        $"/images/stills/{filename}"
    | None -> ""

/// Poster card component for search results (TMDB data)
/// isInLibrary: when true, shows a gray overlay to indicate the item is already in library
let posterCard (item: TmdbSearchResult) (onSelect: TmdbSearchResult -> unit) (isInLibrary: bool) =
    let year =
        item.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let posterUrl = getTmdbPosterUrl "w342" item.PosterPath

    let config =
        PosterCardTypes.Config.searchResult
            (if posterUrl = "" then None else Some posterUrl)
            item.Title
            item.MediaType
            (fun () -> onSelect item)
            isInLibrary

    Html.div [
        prop.children [
            PosterCard.view config

            // Title and year below poster
            Html.div [
                prop.className "mt-3 space-y-1"
                prop.children [
                    Html.p [
                        prop.className (if isInLibrary then "font-medium text-sm truncate text-base-content/50" else "font-medium text-sm truncate text-base-content/90")
                        prop.title item.Title
                        prop.text item.Title
                    ]
                    if year <> "" then
                        Html.p [
                            prop.className "text-xs text-base-content/50"
                            prop.text year
                        ]
                ]
            ]
        ]
    ]

/// Library entry card component (for library items)
let libraryEntryCard (entry: LibraryEntry) (onViewDetail: EntryId -> bool -> unit) =
    PosterCard.libraryEntry entry onViewDetail
