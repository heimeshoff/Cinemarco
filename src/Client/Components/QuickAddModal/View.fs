module Components.QuickAddModal.View

open Feliz
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

let view (model: Model) (friends: Friend list) (tags: Tag list) (dispatch: Msg -> unit) =
    let year =
        model.SelectedItem.ReleaseDate
        |> Option.map (fun d -> d.Year.ToString())
        |> Option.defaultValue ""

    let mediaTypeLabel =
        match model.SelectedItem.MediaType with
        | Movie -> "Movie"
        | Series -> "Series"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "modal-backdrop"
                prop.onClick (fun _ -> if not model.IsSubmitting then dispatch Close)
            ]
            // Modal content
            Html.div [
                prop.className "modal-content relative w-full max-w-lg max-h-[90vh] overflow-y-auto"
                prop.children [
                    // Header with poster background
                    Html.div [
                        prop.className "relative h-52 overflow-hidden"
                        prop.children [
                            // Background poster
                            match model.SelectedItem.PosterPath with
                            | Some _ ->
                                Html.img [
                                    prop.src (getTmdbPosterUrl "w500" model.SelectedItem.PosterPath)
                                    prop.alt model.SelectedItem.Title
                                    prop.className "w-full h-full object-cover opacity-40 blur-sm scale-110"
                                ]
                            | None ->
                                Html.div [
                                    prop.className "w-full h-full bg-gradient-to-br from-primary/20 to-secondary/20"
                                ]

                            // Gradient overlay
                            Html.div [
                                prop.className "absolute inset-0 bg-gradient-to-t from-base-100 via-base-100/80 to-transparent"
                            ]

                            // Close button
                            Html.button [
                                prop.className "absolute top-4 right-4 w-8 h-8 rounded-full glass flex items-center justify-center hover:bg-white/10 transition-colors"
                                prop.onClick (fun _ -> dispatch Close)
                                prop.disabled model.IsSubmitting
                                prop.children [
                                    Html.span [
                                        prop.className "w-4 h-4 text-base-content/70"
                                        prop.children [ close ]
                                    ]
                                ]
                            ]

                            // Title and info
                            Html.div [
                                prop.className "absolute bottom-4 left-4 right-4 flex gap-4"
                                prop.children [
                                    // Small poster thumbnail
                                    match model.SelectedItem.PosterPath with
                                    | Some _ ->
                                        Html.img [
                                            prop.src (getTmdbPosterUrl "w185" model.SelectedItem.PosterPath)
                                            prop.alt model.SelectedItem.Title
                                            prop.className "w-16 h-24 rounded-lg object-cover shadow-lg"
                                        ]
                                    | None -> Html.none

                                    Html.div [
                                        prop.className "flex-1"
                                        prop.children [
                                            Html.h2 [
                                                prop.className "text-xl font-bold"
                                                prop.text model.SelectedItem.Title
                                            ]
                                            Html.div [
                                                prop.className "flex items-center gap-2 mt-1 text-sm text-base-content/60"
                                                prop.children [
                                                    Html.span [
                                                        prop.className "flex items-center gap-1"
                                                        prop.children [
                                                            Html.span [
                                                                prop.className "w-4 h-4"
                                                                prop.children [
                                                                    match model.SelectedItem.MediaType with
                                                                    | Movie -> film
                                                                    | Series -> tv
                                                                ]
                                                            ]
                                                            Html.span [ prop.text mediaTypeLabel ]
                                                        ]
                                                    ]
                                                    if year <> "" then
                                                        Html.span [
                                                            prop.className "w-1 h-1 rounded-full bg-base-content/30"
                                                        ]
                                                        Html.span [ prop.text year ]
                                                ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-6"
                        prop.children [
                            // Error message
                            match model.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "flex items-center gap-3 p-4 rounded-lg bg-error/10 border border-error/30"
                                    prop.children [
                                        Html.span [
                                            prop.className "w-5 h-5 text-error"
                                            prop.children [ error ]
                                        ]
                                        Html.span [
                                            prop.className "text-sm text-error"
                                            prop.text err
                                        ]
                                    ]
                                ]
                            | None -> Html.none

                            // Why I added note
                            Html.div [
                                prop.className "space-y-2"
                                prop.children [
                                    Html.label [
                                        prop.className "text-sm font-medium text-base-content/70"
                                        prop.text "Why are you adding this?"
                                    ]
                                    Html.textarea [
                                        prop.className "w-full px-4 py-3 bg-base-200/50 border border-white/5 rounded-lg text-sm placeholder:text-base-content/30 focus:outline-none focus:ring-2 focus:ring-primary/30 focus:border-primary/50 transition-all resize-none"
                                        prop.rows 3
                                        prop.placeholder "e.g., Recommended by Sarah, saw the trailer..."
                                        prop.value model.WhyAddedNote
                                        prop.onChange (fun (e: string) -> dispatch (NoteChanged e))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]

                            // Tags selection
                            if not (List.isEmpty tags) then
                                Html.div [
                                    prop.className "space-y-3"
                                    prop.children [
                                        Html.label [
                                            prop.className "flex items-center gap-2 text-sm font-medium text-base-content/70"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-4 h-4"
                                                    prop.children [ Components.Icons.tags ]
                                                ]
                                                Html.span [ prop.text "Tags" ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "flex flex-wrap gap-2"
                                            prop.children [
                                                for tag in tags do
                                                    let isSelected = List.contains tag.Id model.SelectedTags
                                                    Html.button [
                                                        prop.type' "button"
                                                        prop.className (
                                                            "px-3 py-1.5 rounded-full text-sm font-medium transition-all " +
                                                            if isSelected then "bg-primary text-primary-content shadow-glow-primary"
                                                            else "bg-base-200/50 text-base-content/60 hover:bg-base-200 hover:text-base-content"
                                                        )
                                                        prop.onClick (fun _ -> dispatch (ToggleTag tag.Id))
                                                        prop.disabled model.IsSubmitting
                                                        prop.text tag.Name
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Friends selection
                            if not (List.isEmpty friends) then
                                Html.div [
                                    prop.className "space-y-3"
                                    prop.children [
                                        Html.label [
                                            prop.className "flex items-center gap-2 text-sm font-medium text-base-content/70"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-4 h-4"
                                                    prop.children [ Components.Icons.friends ]
                                                ]
                                                Html.span [ prop.text "Watch with" ]
                                            ]
                                        ]
                                        Html.div [
                                            prop.className "flex flex-wrap gap-2"
                                            prop.children [
                                                for friend in friends do
                                                    let isSelected = List.contains friend.Id model.SelectedFriends
                                                    Html.button [
                                                        prop.type' "button"
                                                        prop.className (
                                                            "px-3 py-1.5 rounded-full text-sm font-medium transition-all " +
                                                            if isSelected then "bg-secondary text-secondary-content shadow-glow-secondary"
                                                            else "bg-base-200/50 text-base-content/60 hover:bg-base-200 hover:text-base-content"
                                                        )
                                                        prop.onClick (fun _ -> dispatch (ToggleFriend friend.Id))
                                                        prop.disabled model.IsSubmitting
                                                        prop.text friend.Name
                                                    ]
                                            ]
                                        ]
                                    ]
                                ]

                            // Divider
                            Html.div [ prop.className "border-t border-white/5" ]

                            // Submit button
                            Html.button [
                                prop.className "btn-gradient w-full py-3 rounded-lg font-semibold transition-all disabled:opacity-50"
                                prop.onClick (fun _ -> dispatch Submit)
                                prop.disabled model.IsSubmitting
                                prop.children [
                                    if model.IsSubmitting then
                                        Html.span [
                                            prop.className "flex items-center justify-center gap-2"
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                                Html.span [ prop.text "Adding..." ]
                                            ]
                                        ]
                                    else
                                        Html.span [
                                            prop.className "flex items-center justify-center gap-2"
                                            prop.children [
                                                Html.span [
                                                    prop.className "w-5 h-5"
                                                    prop.children [ plus ]
                                                ]
                                                Html.span [ prop.text "Add to Library" ]
                                            ]
                                        ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]
