module Components.SearchModal.View

open Feliz
open Common.Types
open Types
open Components.Icons
open Components.Cards.View

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
                prop.className "modal-content relative w-full max-w-2xl"
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
                            match model.Results with
                            | Loading ->
                                Html.div [
                                    prop.className "p-12 flex flex-col items-center justify-center gap-3"
                                    prop.children [
                                        Html.span [ prop.className "loading loading-spinner loading-lg text-primary" ]
                                        Html.span [ prop.className "text-sm text-base-content/50"; prop.text "Searching..." ]
                                    ]
                                ]
                            | Success results when List.isEmpty results ->
                                Html.div [
                                    prop.className "p-12 text-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-4xl opacity-30 mb-3 block"
                                            prop.children [ film ]
                                        ]
                                        Html.p [
                                            prop.className "text-base-content/60"
                                            prop.text "No results found"
                                        ]
                                        Html.p [
                                            prop.className "text-sm text-base-content/40 mt-1"
                                            prop.text "Try a different search term"
                                        ]
                                    ]
                                ]
                            | Success results ->
                                Html.div [
                                    prop.className "p-4"
                                    prop.children [
                                        Html.p [
                                            prop.className "text-xs text-base-content/50 mb-3 px-1"
                                            prop.text $"Found {List.length results} results"
                                        ]
                                        Html.div [
                                            prop.className "grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-4"
                                            prop.children [
                                                for item in results |> List.truncate 15 do
                                                    posterCard item (fun i -> dispatch (SelectItem i))
                                            ]
                                        ]
                                    ]
                                ]
                            | Failure err ->
                                Html.div [
                                    prop.className "p-8 text-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-3xl mb-2 block"
                                            prop.children [ error ]
                                        ]
                                        Html.p [
                                            prop.className "text-error"
                                            prop.text $"Error: {err}"
                                        ]
                                    ]
                                ]
                            | NotAsked ->
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
