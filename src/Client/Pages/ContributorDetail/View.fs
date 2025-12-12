module Pages.ContributorDetail.View

open Feliz
open Browser.Dom
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

module SectionHeader = Common.Components.SectionHeader.View
module GlassPanel = Common.Components.GlassPanel.View
module FilterChip = Common.Components.FilterChip.View
module GlassButton = Common.Components.GlassButton.View

/// Format a date nicely
let private formatDate (d: System.DateTime) =
    sprintf "%s %d, %d" (d.ToString("MMMM")) d.Day d.Year

/// TMDB image base URL
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Get profile image URL
let private getProfileUrl (path: string option) (size: string) =
    match path with
    | Some p -> $"{tmdbImageBase}/{size}{p}"
    | None -> ""

/// Get poster URL for filmography works
let private getPosterUrl (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/w185{p}"
    | None -> ""

/// Progress bar component
let private progressBar (seen: int) (total: int) =
    let percentage = if total > 0 then (float seen / float total) * 100.0 else 0.0
    let percentText = $"{int (System.Math.Round(percentage))}%%"

    Html.div [
        prop.className "space-y-2"
        prop.children [
            Html.div [
                prop.className "flex justify-between items-center text-sm"
                prop.children [
                    Html.span [
                        prop.className "text-base-content/70"
                        prop.text $"{seen} of {total} seen"
                    ]
                    Html.span [
                        prop.className "font-semibold text-primary"
                        prop.text percentText
                    ]
                ]
            ]
            Html.div [
                prop.className "h-3 bg-base-300 rounded-full overflow-hidden"
                prop.children [
                    Html.div [
                        prop.className "h-full bg-gradient-to-r from-primary to-secondary transition-all duration-500 ease-out rounded-full"
                        prop.style [ style.width (length.percent percentage) ]
                    ]
                ]
            ]
        ]
    ]

/// Filter chips row
let private filterRow (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex flex-wrap gap-3 mb-6"
        prop.children [
            // Filmography filter
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "Show:" ]
                    Html.div [
                        prop.className "flex gap-1"
                        prop.children [
                            Html.button [
                                prop.className (if model.FilmographyFilter = AllWorks then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetFilmographyFilter AllWorks))
                                prop.text "All"
                            ]
                            Html.button [
                                prop.className (if model.FilmographyFilter = SeenWorks then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetFilmographyFilter SeenWorks))
                                prop.text "Seen"
                            ]
                            Html.button [
                                prop.className (if model.FilmographyFilter = UnseenWorks then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetFilmographyFilter UnseenWorks))
                                prop.text "Unseen"
                            ]
                        ]
                    ]
                ]
            ]

            // Media type filter
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "Type:" ]
                    Html.div [
                        prop.className "flex gap-1"
                        prop.children [
                            Html.button [
                                prop.className (if model.MediaTypeFilter = AllMedia then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter AllMedia))
                                prop.text "All"
                            ]
                            Html.button [
                                prop.className (if model.MediaTypeFilter = MoviesOnly then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter MoviesOnly))
                                prop.children [
                                    Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ film ] ]
                                    Html.span [ prop.text "Movies" ]
                                ]
                            ]
                            Html.button [
                                prop.className (if model.MediaTypeFilter = SeriesOnly then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetMediaTypeFilter SeriesOnly))
                                prop.children [
                                    Html.span [ prop.className "w-4 h-4 mr-1"; prop.children [ tv ] ]
                                    Html.span [ prop.text "Series" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Role filter
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "Role:" ]
                    Html.div [
                        prop.className "flex gap-1"
                        prop.children [
                            Html.button [
                                prop.className (if model.RoleFilter = AllRoles then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetRoleFilter AllRoles))
                                prop.text "All"
                            ]
                            Html.button [
                                prop.className (if model.RoleFilter = CastOnly then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetRoleFilter CastOnly))
                                prop.text "Cast"
                            ]
                            Html.button [
                                prop.className (if model.RoleFilter = CrewOnly then "filter-chip filter-chip-active" else "filter-chip")
                                prop.onClick (fun _ -> dispatch (SetRoleFilter CrewOnly))
                                prop.text "Crew"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Get role display string
let private roleToString (role: ContributorRole) =
    match role with
    | Director -> "Director"
    | Actor character ->
        match character with
        | Some c -> $"Actor ({c})"
        | None -> "Actor"
    | Writer -> "Writer"
    | Cinematographer -> "Cinematographer"
    | Composer -> "Composer"
    | Producer -> "Producer"
    | ExecutiveProducer -> "Executive Producer"
    | CreatedBy -> "Creator"
    | Other dept -> dept

/// Check if a role is a cast role
let private isCastRole (role: ContributorRole) =
    match role with
    | Actor _ -> true
    | _ -> false

/// Single filmography work card
let private workCard (work: TmdbWork) (isInLibrary: bool) (entryId: EntryId option) (dispatch: Msg -> unit) =
    let year =
        work.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let handleClick _ =
        match entryId with
        | Some id ->
            match work.MediaType with
            | MediaType.Movie -> dispatch (ViewMovieDetail (id, work.Title, work.ReleaseDate))
            | MediaType.Series -> dispatch (ViewSeriesDetail (id, work.Title, work.ReleaseDate))
        | None ->
            dispatch (AddToLibrary work)

    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick handleClick
        prop.children [
            // Poster container
            Html.div [
                prop.className "poster-image-container poster-shadow"
                prop.children [
                    match work.PosterPath with
                    | Some _ ->
                        Html.img [
                            prop.src (getPosterUrl work.PosterPath)
                            prop.alt work.Title
                            prop.className (if isInLibrary then "poster-image" else "poster-image opacity-70")
                            prop.custom ("loading", "lazy")
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-full h-full flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "text-4xl text-base-content/20"
                                    prop.children [ if work.MediaType = MediaType.Movie then film else tv ]
                                ]
                            ]
                        ]

                    // Shine effect
                    Html.div [ prop.className "poster-shine" ]

                    // In library badge (top left)
                    if isInLibrary then
                        Html.div [
                            prop.className "absolute top-2 left-2 px-2 py-1 rounded-md bg-success/90 backdrop-blur-sm text-success-content text-xs font-medium flex items-center gap-1"
                            prop.children [
                                Html.span [ prop.className "w-3 h-3"; prop.children [ checkCircleSolid ] ]
                                Html.span [ prop.text "In Library" ]
                            ]
                        ]

                    // Role badge (bottom)
                    Html.div [
                        prop.className "absolute bottom-0 left-0 right-0 px-2 py-1.5 bg-gradient-to-t from-black/80 to-transparent"
                        prop.children [
                            Html.span [
                                prop.className "text-xs text-white/90 line-clamp-1"
                                prop.text (roleToString work.Role)
                            ]
                        ]
                    ]

                    // Add overlay for unseen works
                    if not isInLibrary then
                        Html.div [
                            prop.className "poster-overlay flex items-center justify-center"
                            prop.children [
                                Html.div [
                                    prop.className "w-12 h-12 rounded-full bg-gradient-to-br from-primary to-secondary flex items-center justify-center transform scale-90 group-hover:scale-100 transition-transform shadow-lg"
                                    prop.children [
                                        Html.span [ prop.className "w-5 h-5 text-white"; prop.children [ plus ] ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]

            // Title and year
            Html.div [
                prop.className "mt-2 space-y-0.5"
                prop.children [
                    Html.p [
                        prop.className "font-medium text-sm truncate"
                        prop.title work.Title
                        prop.text work.Title
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

/// Filmography grid
let private filmographyGrid (model: Model) (filmography: TmdbFilmography) (dispatch: Msg -> unit) =
    // Combine cast and crew credits
    let allWorks =
        let castWorks = filmography.CastCredits |> List.map (fun w -> (w, true))
        let crewWorks = filmography.CrewCredits |> List.map (fun w -> (w, false))
        castWorks @ crewWorks
        |> List.distinctBy (fun (w, _) -> (w.TmdbId, w.MediaType))

    // Apply filters
    let filteredWorks =
        allWorks
        |> List.filter (fun (work, isCast) ->
            // Media type filter
            let passesMediaFilter =
                match model.MediaTypeFilter with
                | AllMedia -> true
                | MoviesOnly -> work.MediaType = MediaType.Movie
                | SeriesOnly -> work.MediaType = MediaType.Series

            // Role filter
            let passesRoleFilter =
                match model.RoleFilter with
                | AllRoles -> true
                | CastOnly -> isCast
                | CrewOnly -> not isCast

            // Seen/unseen filter
            let isInLibrary = model.LibraryEntryIds |> Map.containsKey (work.TmdbId, work.MediaType)
            let passesSeenFilter =
                match model.FilmographyFilter with
                | AllWorks -> true
                | SeenWorks -> isInLibrary
                | UnseenWorks -> not isInLibrary

            passesMediaFilter && passesRoleFilter && passesSeenFilter
        )
        |> List.sortByDescending (fun (w, _) -> w.ReleaseDate |> Option.defaultValue System.DateTime.MinValue)
        |> List.map fst

    if List.isEmpty filteredWorks then
        Html.div [
            prop.className "text-center py-12 text-base-content/60"
            prop.text "No works match the current filters."
        ]
    else
        Html.div [
            prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
            prop.children [
                for work in filteredWorks do
                    let key = (work.TmdbId, work.MediaType)
                    let isInLibrary = model.LibraryEntryIds |> Map.containsKey key
                    let entryId = model.LibraryEntryIds |> Map.tryFind key
                    workCard work isInLibrary entryId dispatch
            ]
        ]

/// Person header section
let private personHeader (details: TmdbPersonDetails) (filmography: TmdbFilmography) (model: Model) (dispatch: Msg -> unit) =
    let allWorks =
        (filmography.CastCredits @ filmography.CrewCredits)
        |> List.distinctBy (fun w -> (w.TmdbId, w.MediaType))

    let totalWorks = List.length allWorks
    let seenWorks =
        allWorks
        |> List.filter (fun w -> model.LibraryEntryIds |> Map.containsKey (w.TmdbId, w.MediaType))
        |> List.length

    Html.div [
        prop.className "flex flex-col md:flex-row gap-6 mb-8"
        prop.children [
            // Profile image
            Html.div [
                prop.className "flex-shrink-0"
                prop.children [
                    match details.ProfilePath with
                    | Some _ ->
                        Html.img [
                            prop.src (getProfileUrl details.ProfilePath "w185")
                            prop.alt details.Name
                            prop.className "w-32 h-48 md:w-40 md:h-60 rounded-lg object-cover shadow-lg"
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-32 h-48 md:w-40 md:h-60 rounded-lg bg-base-300 flex items-center justify-center"
                            prop.children [
                                Html.span [
                                    prop.className "text-5xl text-base-content/20"
                                    prop.children [ userPlus ]
                                ]
                            ]
                        ]
                ]
            ]

            // Info
            Html.div [
                prop.className "flex-1 space-y-4"
                prop.children [
                    // Name and action buttons row
                    Html.div [
                        prop.className "flex items-start justify-between gap-4"
                        prop.children [
                            Html.h1 [
                                prop.className "text-3xl font-bold"
                                prop.text details.Name
                            ]

                            Html.div [
                                prop.className "flex items-center gap-2"
                                prop.children [
                                    // View in Graph button
                                    GlassButton.button graph "View in Graph" (fun () -> dispatch ViewInGraph)

                                    // Track/Untrack button
                                    if model.IsTracked then
                                        GlassButton.primaryActive heart "Tracking" (fun () -> dispatch UntrackContributor)
                                    else
                                        GlassButton.button heart "Track" (fun () -> dispatch TrackContributor)
                                ]
                            ]
                        ]
                    ]

                    // Known for department
                    match details.KnownForDepartment with
                    | Some dept ->
                        Html.span [
                            prop.className "inline-block px-3 py-1 rounded-full bg-primary/20 text-primary text-sm font-medium"
                            prop.text dept
                        ]
                    | None -> Html.none

                    // Progress bar
                    GlassPanel.standard [
                        progressBar seenWorks totalWorks
                    ]

                    // Biography (truncated)
                    match details.Biography with
                    | Some bio when bio.Length > 0 ->
                        Html.div [
                            Html.h3 [ prop.className "font-semibold mb-2"; prop.text "Biography" ]
                            Html.p [
                                prop.className "text-base-content/70 text-sm line-clamp-4"
                                prop.text bio
                            ]
                        ]
                    | _ -> Html.none

                    // Birth/death info
                    Html.div [
                        prop.className "flex flex-wrap gap-4 text-sm text-base-content/60"
                        prop.children [
                            match details.Birthday with
                            | Some bday ->
                                Html.span [
                                    prop.text (sprintf "Born: %s" (formatDate bday))
                                ]
                            | None -> Html.none

                            match details.Deathday with
                            | Some dday ->
                                Html.span [
                                    prop.text (sprintf "Died: %s" (formatDate dday))
                                ]
                            | None -> Html.none

                            match details.PlaceOfBirth with
                            | Some place ->
                                Html.span [
                                    prop.text place
                                ]
                            | None -> Html.none
                        ]
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button - uses browser history for proper navigation
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> window.history.back())
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back" ]
                ]
            ]

            match model.PersonDetails, model.Filmography with
            | Loading, _ | _, Loading ->
                Html.div [
                    prop.className "flex justify-center py-16"
                    prop.children [
                        Html.span [ prop.className "loading loading-spinner loading-lg" ]
                    ]
                ]

            | Failure err, _ | _, Failure err ->
                Html.div [
                    prop.className "alert alert-error"
                    prop.text err
                ]

            | Success details, Success filmography ->
                // Person header with progress
                personHeader details filmography model dispatch

                // Filters
                Html.div [
                    prop.className "space-y-4"
                    prop.children [
                        SectionHeader.title "Filmography"
                        filterRow model dispatch
                        filmographyGrid model filmography dispatch
                    ]
                ]

            | NotAsked, _ | _, NotAsked -> Html.none
        ]
    ]
