module Components.AbandonModal.View

open Feliz
open Types

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop
            Html.div [
                prop.className "absolute inset-0 bg-black/60"
                prop.onClick (fun _ -> if not model.IsSubmitting then dispatch Close)
            ]
            // Modal content
            Html.div [
                prop.className "relative bg-base-100 rounded-xl shadow-2xl w-full max-w-md"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "p-6 border-b border-base-300"
                        prop.children [
                            Html.h2 [
                                prop.className "text-xl font-bold"
                                prop.text "Abandon Entry"
                            ]
                            Html.button [
                                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                                prop.onClick (fun _ -> dispatch Close)
                                prop.disabled model.IsSubmitting
                                prop.text "X"
                            ]
                        ]
                    ]

                    // Form
                    Html.div [
                        prop.className "p-6 space-y-4"
                        prop.children [
                            // Error message
                            match model.Error with
                            | Some err ->
                                Html.div [
                                    prop.className "alert alert-error"
                                    prop.text err
                                ]
                            | None -> Html.none

                            Html.p [
                                prop.className "text-base-content/70 text-sm"
                                prop.text "Mark this entry as abandoned. You can optionally specify where you stopped and why."
                            ]

                            // Reason field
                            Html.div [
                                prop.className "form-control"
                                prop.children [
                                    Html.label [
                                        prop.className "label"
                                        prop.children [
                                            Html.span [ prop.className "label-text"; prop.text "Reason (optional)" ]
                                        ]
                                    ]
                                    Html.textarea [
                                        prop.className "textarea textarea-bordered h-20"
                                        prop.placeholder "Why did you stop watching?"
                                        prop.value model.Reason
                                        prop.onChange (fun (e: string) -> dispatch (ReasonChanged e))
                                        prop.disabled model.IsSubmitting
                                    ]
                                ]
                            ]

                            // Abandoned at season/episode (for series)
                            Html.div [
                                prop.className "grid grid-cols-2 gap-4"
                                prop.children [
                                    Html.div [
                                        prop.className "form-control"
                                        prop.children [
                                            Html.label [
                                                prop.className "label"
                                                prop.children [
                                                    Html.span [ prop.className "label-text"; prop.text "Stopped at Season" ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.className "input input-bordered"
                                                prop.type' "number"
                                                prop.min 1
                                                prop.placeholder "Season"
                                                prop.value (model.AbandonedAtSeason |> Option.map string |> Option.defaultValue "")
                                                prop.onChange (fun (e: string) ->
                                                    let v = match System.Int32.TryParse(e) with | true, n -> Some n | _ -> None
                                                    dispatch (SeasonChanged v))
                                                prop.disabled model.IsSubmitting
                                            ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "form-control"
                                        prop.children [
                                            Html.label [
                                                prop.className "label"
                                                prop.children [
                                                    Html.span [ prop.className "label-text"; prop.text "Episode" ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.className "input input-bordered"
                                                prop.type' "number"
                                                prop.min 1
                                                prop.placeholder "Episode"
                                                prop.value (model.AbandonedAtEpisode |> Option.map string |> Option.defaultValue "")
                                                prop.onChange (fun (e: string) ->
                                                    let v = match System.Int32.TryParse(e) with | true, n -> Some n | _ -> None
                                                    dispatch (EpisodeChanged v))
                                                prop.disabled model.IsSubmitting
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                            // Submit button
                            Html.div [
                                prop.className "flex gap-2 justify-end mt-4"
                                prop.children [
                                    Html.button [
                                        prop.className "btn btn-ghost"
                                        prop.onClick (fun _ -> dispatch Close)
                                        prop.disabled model.IsSubmitting
                                        prop.text "Cancel"
                                    ]
                                    Html.button [
                                        prop.className "btn btn-error"
                                        prop.onClick (fun _ -> dispatch Submit)
                                        prop.disabled model.IsSubmitting
                                        prop.children [
                                            if model.IsSubmitting then
                                                Html.span [ prop.className "loading loading-spinner loading-sm" ]
                                            else
                                                Html.span [ prop.text "Abandon" ]
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
