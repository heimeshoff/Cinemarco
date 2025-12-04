module Pages.Contributors.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Icons

module GlassPanel = Common.Components.GlassPanel.View
module GlassButton = Common.Components.GlassButton.View
module SectionHeader = Common.Components.SectionHeader.View
module FilterChip = Common.Components.FilterChip.View
module RemoteDataView = Common.Components.RemoteDataView.View
module EmptyState = Common.Components.EmptyState.View

/// TMDB image base URL
let private tmdbImageBase = "https://image.tmdb.org/t/p"

/// Get profile image URL
let private getProfileUrl (path: string option) =
    match path with
    | Some p -> $"{tmdbImageBase}/w185{p}"
    | None -> ""

/// Filter contributors based on current filters
let private filterContributors (model: Model) (contributors: TrackedContributor list) =
    contributors
    |> List.filter (fun c ->
        // Department filter
        let passesDepartment =
            match model.DepartmentFilter with
            | AllDepartments -> true
            | ActingOnly -> c.KnownForDepartment = Some "Acting"
            | DirectingOnly -> c.KnownForDepartment = Some "Directing"
            | OtherDepartments ->
                c.KnownForDepartment <> Some "Acting" &&
                c.KnownForDepartment <> Some "Directing"

        // Search filter
        let passesSearch =
            if System.String.IsNullOrWhiteSpace model.SearchQuery then true
            else c.Name.ToLowerInvariant().Contains(model.SearchQuery.ToLowerInvariant())

        passesDepartment && passesSearch
    )

/// Single contributor card
let private contributorCard (contributor: TrackedContributor) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "group cursor-pointer"
        prop.onClick (fun _ -> dispatch (ViewContributorDetail contributor.TmdbPersonId))
        prop.children [
            GlassPanel.standard [
                Html.div [
                    prop.className "flex items-start gap-4"
                    prop.children [
                        // Profile image
                        Html.div [
                            prop.className "flex-shrink-0"
                            prop.children [
                                match contributor.ProfilePath with
                                | Some _ ->
                                    Html.img [
                                        prop.src (getProfileUrl contributor.ProfilePath)
                                        prop.alt contributor.Name
                                        prop.className "w-16 h-24 rounded-lg object-cover shadow-md"
                                        prop.custom ("loading", "lazy")
                                    ]
                                | None ->
                                    Html.div [
                                        prop.className "w-16 h-24 rounded-lg bg-base-300 flex items-center justify-center"
                                        prop.children [
                                            Html.span [
                                                prop.className "text-2xl text-base-content/20"
                                                prop.children [ userPlus ]
                                            ]
                                        ]
                                    ]
                            ]
                        ]

                        // Info
                        Html.div [
                            prop.className "flex-1 min-w-0"
                            prop.children [
                                Html.h3 [
                                    prop.className "font-semibold text-base truncate group-hover:text-primary transition-colors"
                                    prop.text contributor.Name
                                ]

                                match contributor.KnownForDepartment with
                                | Some dept ->
                                    Html.span [
                                        prop.className "inline-block mt-1 px-2 py-0.5 rounded-full bg-primary/10 text-primary text-xs font-medium"
                                        prop.text dept
                                    ]
                                | None -> Html.none

                                match contributor.Notes with
                                | Some notes when not (System.String.IsNullOrWhiteSpace notes) ->
                                    Html.p [
                                        prop.className "mt-2 text-sm text-base-content/60 line-clamp-2"
                                        prop.text notes
                                    ]
                                | _ -> Html.none
                            ]
                        ]

                        // Actions
                        Html.div [
                            prop.className "flex-shrink-0"
                            prop.children [
                                GlassButton.danger trash "Untrack" (fun () ->
                                    dispatch (UntrackContributor contributor.Id))
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Filter row
let private filterRow (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "flex flex-wrap gap-4 items-center"
        prop.children [
            // Department filter
            Html.div [
                prop.className "flex items-center gap-2"
                prop.children [
                    Html.span [ prop.className "text-sm text-base-content/60"; prop.text "Department:" ]
                    Html.div [
                        prop.className "flex gap-1"
                        prop.children [
                            FilterChip.chip "All" (model.DepartmentFilter = AllDepartments) (fun () ->
                                dispatch (SetDepartmentFilter AllDepartments))
                            FilterChip.chip "Acting" (model.DepartmentFilter = ActingOnly) (fun () ->
                                dispatch (SetDepartmentFilter ActingOnly))
                            FilterChip.chip "Directing" (model.DepartmentFilter = DirectingOnly) (fun () ->
                                dispatch (SetDepartmentFilter DirectingOnly))
                            FilterChip.chip "Other" (model.DepartmentFilter = OtherDepartments) (fun () ->
                                dispatch (SetDepartmentFilter OtherDepartments))
                        ]
                    ]
                ]
            ]

            // Search input
            Html.div [
                prop.className "flex-1 max-w-xs"
                prop.children [
                    Html.input [
                        prop.type'.text
                        prop.placeholder "Search contributors..."
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
            // Page header
            SectionHeader.titleLarge "Tracked Contributors"

            Html.p [
                prop.className "text-base-content/70"
                prop.text "Track your favorite actors, directors, and crew members. Click on a contributor to see their full filmography."
            ]

            // Filters
            filterRow model dispatch

            // Content
            RemoteDataView.withSpinner model.Contributors (fun contributors ->
                let filtered = filterContributors model contributors

                if List.isEmpty contributors then
                    EmptyState.emptyWithDesc userPlus "No tracked contributors yet" "Browse movies and series to find contributors to track"
                elif List.isEmpty filtered then
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.text "No contributors match the current filters."
                    ]
                else
                    Html.div [
                        prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                        prop.children [
                            for contributor in filtered do
                                contributorCard contributor dispatch
                        ]
                    ]
            )
        ]
    ]
