module Components.AbandonModal.View

open Feliz
open Types
open Components.Modal.View

let view (model: Model) (dispatch: Msg -> unit) =
    wrapper {
        OnClose = fun () -> if not model.IsSubmitting then dispatch Close
        CanClose = not model.IsSubmitting
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "Abandon Entry" None (not model.IsSubmitting) (fun () -> dispatch Close)

            // Form body
            body [
                // Error message
                errorAlert model.Error

                Html.p [
                    prop.className "text-base-content/70 text-sm"
                    prop.text "Mark this entry as abandoned. You can optionally specify where you stopped and why."
                ]

                // Reason field
                formField "Reason" false [
                    Html.textarea [
                        prop.className "textarea textarea-bordered h-20 w-full"
                        prop.placeholder "Why did you stop watching?"
                        prop.value model.Reason
                        prop.onChange (fun (e: string) -> dispatch (ReasonChanged e))
                        prop.disabled model.IsSubmitting
                    ]
                ]

                // Abandoned at season/episode (for series)
                Html.div [
                    prop.className "grid grid-cols-2 gap-4"
                    prop.children [
                        formField "Stopped at Season" false [
                            Html.input [
                                prop.className "input input-bordered w-full"
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
                        formField "Episode" false [
                            Html.input [
                                prop.className "input input-bordered w-full"
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

                // Action buttons
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
    }
