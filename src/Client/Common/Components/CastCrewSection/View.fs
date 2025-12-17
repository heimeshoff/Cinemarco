module Common.Components.CastCrewSection.View

open Feliz
open Shared.Domain
open Common.Components.CastCrewSection.Types
open Components.Icons

/// Render a single cast member button
let renderCastMember (castMember: TmdbCastMember) (isTracked: bool) (onViewContributor: TmdbPersonId * string -> unit) =
    Html.button [
        prop.key (TmdbPersonId.value castMember.TmdbPersonId |> string)
        prop.className (
            if isTracked then
                "flex items-center gap-2 px-3 py-2 rounded-lg bg-primary/10 hover:bg-primary/20 border border-primary/30 transition-colors cursor-pointer"
            else
                "flex items-center gap-2 px-3 py-2 rounded-lg bg-base-200 hover:bg-base-300 transition-colors cursor-pointer"
        )
        prop.onClick (fun _ -> onViewContributor (castMember.TmdbPersonId, castMember.Name))
        prop.children [
            Html.div [
                prop.className "relative"
                prop.children [
                    match castMember.ProfilePath with
                    | Some path ->
                        Html.img [
                            prop.src $"https://image.tmdb.org/t/p/w45{path}"
                            prop.className "w-8 h-8 rounded-full object-cover"
                            prop.alt castMember.Name
                        ]
                    | None ->
                        Html.div [
                            prop.className "w-8 h-8 rounded-full bg-base-300 flex items-center justify-center"
                            prop.children [
                                Html.span [ prop.className "w-4 h-4 text-base-content/40"; prop.children [ userPlus ] ]
                            ]
                        ]
                    if isTracked then
                        Html.div [
                            prop.className "absolute -top-1 -right-1 w-4 h-4 rounded-full bg-primary flex items-center justify-center"
                            prop.children [
                                Html.span [ prop.className "w-2.5 h-2.5 text-primary-content"; prop.children [ heart ] ]
                            ]
                        ]
                ]
            ]
            Html.div [
                prop.className "text-left"
                prop.children [
                    Html.span [ prop.className "text-sm font-medium block"; prop.text castMember.Name ]
                    match castMember.Character with
                    | Some char ->
                        Html.span [ prop.className "text-xs text-base-content/60"; prop.text char ]
                    | None -> Html.none
                ]
            ]
        ]
    ]

/// Render a single crew member button
let renderCrewMember (crewMember: TmdbCrewMember) (onViewContributor: TmdbPersonId * string -> unit) =
    Html.button [
        prop.key (TmdbPersonId.value crewMember.TmdbPersonId |> string)
        prop.className "flex items-center gap-2 px-3 py-2 rounded-lg bg-base-200 hover:bg-base-300 transition-colors cursor-pointer"
        prop.onClick (fun _ -> onViewContributor (crewMember.TmdbPersonId, crewMember.Name))
        prop.children [
            match crewMember.ProfilePath with
            | Some path ->
                Html.img [
                    prop.src $"https://image.tmdb.org/t/p/w45{path}"
                    prop.className "w-8 h-8 rounded-full object-cover"
                    prop.alt crewMember.Name
                ]
            | None ->
                Html.div [
                    prop.className "w-8 h-8 rounded-full bg-base-300 flex items-center justify-center"
                    prop.children [
                        Html.span [ prop.className "w-4 h-4 text-base-content/40"; prop.children [ userPlus ] ]
                    ]
                ]
            Html.div [
                prop.className "text-left"
                prop.children [
                    Html.span [ prop.className "text-sm font-medium block"; prop.text crewMember.Name ]
                    Html.span [ prop.className "text-xs text-base-content/60"; prop.text crewMember.Job ]
                ]
            ]
        ]
    ]

/// Sort departments by importance
let private departmentOrder dept =
    match dept with
    | "Directing" -> 0
    | "Writing" -> 1
    | "Production" -> 2
    | "Camera" -> 3
    | "Sound" -> 4
    | "Editing" -> 5
    | "Art" -> 6
    | "Costume & Make-Up" -> 7
    | "Visual Effects" -> 8
    | _ -> 99

/// Main cast & crew section view
let view (config: Config) =
    let credits = config.Credits

    // Sort cast by TMDB billing order (Order field, lower = top-billed)
    let sortedByBilling = credits.Cast |> List.sortBy (fun c -> c.Order)

    // Top billed cast: first 10 by billing order
    let topBilledCount = 10
    let topBilledCast = sortedByBilling |> List.truncate topBilledCount
    let remainingCast = sortedByBilling |> List.skip (min topBilledCount (List.length sortedByBilling))

    // Group crew by department for expanded view
    let crewByDepartment =
        credits.Crew
        |> List.distinctBy (fun c -> TmdbPersonId.value c.TmdbPersonId, c.Job)
        |> List.groupBy (fun c -> c.Department)
        |> List.sortBy (fun (dept, _) -> departmentOrder dept)

    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Top Billed Cast section
            if not (List.isEmpty topBilledCast) then
                Html.div [
                    prop.children [
                        Html.h3 [ prop.className "font-semibold mb-3"; prop.text "Top Billed Cast" ]
                        Html.div [
                            prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                            prop.children [
                                for castMember in topBilledCast do
                                    let isTracked = config.TrackedPersonIds |> Set.contains castMember.TmdbPersonId
                                    renderCastMember castMember isTracked config.OnViewContributor
                            ]
                        ]
                    ]
                ]

            // Full Cast and Crew button
            let hasMoreContent = not (List.isEmpty remainingCast) || not (List.isEmpty credits.Crew)
            if hasMoreContent then
                Html.div [
                    prop.className "mt-4"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost btn-sm gap-2 w-full justify-center border border-base-300 hover:border-primary/50"
                            prop.onClick (fun _ -> config.OnToggleExpanded())
                            prop.children [
                                Html.span [ prop.text (if config.IsExpanded then "Hide Full Cast & Crew" else "Full Cast & Crew") ]
                                Html.span [
                                    prop.className "w-4 h-4 transition-transform"
                                    prop.style [ if config.IsExpanded then style.transform (transform.rotate 180) ]
                                    prop.children [ chevronDown ]
                                ]
                            ]
                        ]
                    ]
                ]

            // Expanded full cast and crew section
            if config.IsExpanded then
                Html.div [
                    prop.className "mt-6 space-y-6 animate-in fade-in slide-in-from-top-2 duration-200"
                    prop.children [
                        // Remaining cast (if any)
                        if not (List.isEmpty remainingCast) then
                            Html.div [
                                prop.children [
                                    Html.h3 [ prop.className "font-semibold mb-3 text-base-content/70"; prop.text "Supporting Cast" ]
                                    Html.div [
                                        prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                        prop.children [
                                            for castMember in remainingCast do
                                                let isTracked = config.TrackedPersonIds |> Set.contains castMember.TmdbPersonId
                                                renderCastMember castMember isTracked config.OnViewContributor
                                        ]
                                    ]
                                ]
                            ]

                        // Crew grouped by department
                        for (department, crewMembers) in crewByDepartment do
                            Html.div [
                                prop.key department
                                prop.children [
                                    Html.h3 [ prop.className "font-semibold mb-3 text-base-content/70"; prop.text department ]
                                    Html.div [
                                        prop.className "grid grid-cols-2 lg:grid-cols-3 gap-2"
                                        prop.children [
                                            for crewMember in crewMembers do
                                                renderCrewMember crewMember config.OnViewContributor
                                        ]
                                    ]
                                ]
                            ]
                    ]
                ]
        ]
    ]

/// Wrapper with RemoteData handling for the entire tab
let viewWithLoading (creditsData: Common.Types.RemoteData<TmdbCredits>) (trackedIds: Set<TmdbPersonId>) (isExpanded: bool) (onViewContributor: TmdbPersonId * string -> unit) (onToggleExpanded: unit -> unit) =
    match creditsData with
    | Common.Types.Success credits ->
        view (Config.create credits trackedIds isExpanded onViewContributor onToggleExpanded)
    | Common.Types.Loading ->
        Html.div [
            prop.className "flex justify-center py-8"
            prop.children [
                Html.span [ prop.className "loading loading-spinner loading-lg" ]
            ]
        ]
    | Common.Types.Failure _ ->
        Html.div [
            prop.className "text-center py-8 text-base-content/60"
            prop.text "Could not load cast and crew"
        ]
    | Common.Types.NotAsked -> Html.none
