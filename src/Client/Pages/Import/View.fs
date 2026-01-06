module Pages.Import.View

open System
open Feliz
open Common.Types
open Shared.Domain
open Types

// Icon aliases for convenience
let private externalLinkIcon = Components.Icons.import
let private checkIcon = Components.Icons.check
let private infoIcon = Components.Icons.info
let private arrowLeftIcon = Components.Icons.arrowLeft
let private arrowRightIcon = Components.Icons.arrowRight
let private downloadIcon = Components.Icons.import
let private warningIcon = Components.Icons.warning

// Import common components
module GlassPanel = Common.Components.GlassPanel.View
module SectionHeader = Common.Components.SectionHeader.View
module RemoteDataView = Common.Components.RemoteDataView.View
module GlassButton = Common.Components.GlassButton.View

/// Step indicator component
let private stepIndicator (currentStep: ImportStep) =
    let steps = [
        (Connect, "Connect", "1")
        (SelectOptions, "Options", "2")
        (Preview, "Preview", "3")
        (Importing, "Import", "4")
        (Complete, "Done", "5")
    ]

    let stepIndex step =
        match step with
        | Connect -> 0
        | SelectOptions -> 1
        | Preview -> 2
        | Importing -> 3
        | Complete -> 4

    let currentIndex = stepIndex currentStep

    Html.ul [
        prop.className "steps steps-horizontal w-full mb-8"
        prop.children [
            for (step, label, num) in steps do
                let idx = stepIndex step
                let stepClass =
                    if idx < currentIndex then "step step-primary"
                    elif idx = currentIndex then "step step-primary"
                    else "step"
                Html.li [
                    prop.className stepClass
                    prop.custom ("data-content", num)
                    prop.text label
                ]
        ]
    ]

/// Connect step view
let private connectStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-8"
                    prop.children [
                        // Trakt logo placeholder
                        Html.div [
                            prop.className "text-6xl mb-4"
                            prop.children [ externalLinkIcon ]
                        ]

                        Html.h2 [
                            prop.className "text-2xl font-bold mb-2"
                            prop.text "Connect to Trakt.tv"
                        ]

                        Html.p [
                            prop.className "text-base-content/70 mb-6 max-w-md mx-auto"
                            prop.text "Import your watch history, ratings, and watchlist from Trakt.tv. Your data will be matched with TMDB and added to your Cinemarco library."
                        ]

                        match model.ConnectionStatus with
                        | NotChecked | Checking ->
                            Html.div [
                                prop.className "flex items-center justify-center gap-2"
                                prop.children [
                                    Html.span [ prop.className "loading loading-spinner" ]
                                    Html.span [ prop.text "Checking connection..." ]
                                ]
                            ]

                        | Connected ->
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    Html.div [
                                        prop.className "badge badge-success badge-lg gap-2"
                                        prop.children [
                                            checkIcon
                                            Html.span [ prop.text "Connected to Trakt" ]
                                        ]
                                    ]
                                    Html.div [
                                        prop.className "flex gap-2 justify-center"
                                        prop.children [
                                            Html.button [
                                                prop.className "btn btn-primary"
                                                prop.onClick (fun _ -> dispatch (GoToStep SelectOptions))
                                                prop.text "Continue to Import"
                                            ]
                                            Html.button [
                                                prop.className "btn btn-ghost btn-sm"
                                                prop.onClick (fun _ -> dispatch Logout)
                                                prop.text "Disconnect"
                                            ]
                                        ]
                                    ]
                                ]
                            ]

                        | NotConnected ->
                            Html.div [
                                prop.className "space-y-4"
                                prop.children [
                                    match model.AuthUrl with
                                    | NotAsked ->
                                        Html.button [
                                            prop.className "btn btn-primary btn-lg"
                                            prop.onClick (fun _ -> dispatch GetAuthUrl)
                                            prop.children [
                                                externalLinkIcon
                                                Html.span [ prop.text "Connect with Trakt" ]
                                            ]
                                        ]

                                    | Loading ->
                                        Html.button [
                                            prop.className "btn btn-primary btn-lg"
                                            prop.disabled true
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner" ]
                                                Html.span [ prop.text "Loading..." ]
                                            ]
                                        ]

                                    | Success authUrl ->
                                        Html.div [
                                            prop.className "space-y-4 max-w-md mx-auto"
                                            prop.children [
                                                Html.div [
                                                    prop.className "alert alert-info"
                                                    prop.children [
                                                        Html.div [
                                                            prop.children [
                                                                Html.p [ prop.className "font-semibold"; prop.text "Step 1: Authorize on Trakt" ]
                                                                Html.p [ prop.className "text-sm"; prop.text "Click the button below to open Trakt authorization page. After authorizing, copy the code shown." ]
                                                            ]
                                                        ]
                                                    ]
                                                ]

                                                Html.a [
                                                    prop.className "btn btn-primary w-full"
                                                    prop.href authUrl.Url
                                                    prop.target "_blank"
                                                    prop.children [
                                                        externalLinkIcon
                                                        Html.span [ prop.text "Open Trakt Authorization" ]
                                                    ]
                                                ]

                                                Html.div [
                                                    prop.className "divider"
                                                    prop.text "Then"
                                                ]

                                                Html.div [
                                                    prop.className "form-control"
                                                    prop.children [
                                                        Html.label [
                                                            prop.className "label"
                                                            prop.children [
                                                                Html.span [
                                                                    prop.className "label-text"
                                                                    prop.text "Step 2: Enter the authorization code"
                                                                ]
                                                            ]
                                                        ]
                                                        Html.input [
                                                            prop.className "input input-bordered w-full"
                                                            prop.type' "text"
                                                            prop.placeholder "Paste authorization code here"
                                                            prop.value model.AuthCode
                                                            prop.onChange (fun (e: string) -> dispatch (UpdateAuthCode e))
                                                        ]
                                                    ]
                                                ]

                                                Html.button [
                                                    prop.className "btn btn-success w-full"
                                                    prop.disabled (model.AuthCode = "")
                                                    prop.onClick (fun _ -> dispatch SubmitAuthCode)
                                                    prop.children [
                                                        checkIcon
                                                        Html.span [ prop.text "Complete Connection" ]
                                                    ]
                                                ]
                                            ]
                                        ]

                                    | Failure err ->
                                        Html.div [
                                            prop.className "space-y-4"
                                            prop.children [
                                                Html.div [
                                                    prop.className "alert alert-error"
                                                    prop.text err
                                                ]
                                                Html.button [
                                                    prop.className "btn btn-primary"
                                                    prop.onClick (fun _ -> dispatch GetAuthUrl)
                                                    prop.text "Try Again"
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

/// Options step view
let private optionsStep (model: Model) (dispatch: Msg -> unit) =
    let opts = model.ImportOptions

    Html.div [
        prop.className "space-y-6"
        prop.children [
            GlassPanel.standard [
                SectionHeader.title "What to Import"

                Html.div [
                    prop.className "space-y-4 mt-4"
                    prop.children [
                        // Watched Movies toggle
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label cursor-pointer justify-start gap-4"
                                    prop.children [
                                        Html.input [
                                            prop.type' "checkbox"
                                            prop.className "checkbox checkbox-primary"
                                            prop.isChecked opts.ImportWatchedMovies
                                            prop.onChange (fun (_: bool) -> dispatch ToggleImportMovies)
                                        ]
                                        Html.div [
                                            Html.span [ prop.className "label-text font-semibold"; prop.text "Watched Movies" ]
                                            Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Import all movies you've marked as watched on Trakt" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                        // Watched Series toggle
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label cursor-pointer justify-start gap-4"
                                    prop.children [
                                        Html.input [
                                            prop.type' "checkbox"
                                            prop.className "checkbox checkbox-primary"
                                            prop.isChecked opts.ImportWatchedSeries
                                            prop.onChange (fun (_: bool) -> dispatch ToggleImportSeries)
                                        ]
                                        Html.div [
                                            Html.span [ prop.className "label-text font-semibold"; prop.text "Watched Series" ]
                                            Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Import all TV shows you've watched episodes of" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                        // Ratings toggle
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label cursor-pointer justify-start gap-4"
                                    prop.children [
                                        Html.input [
                                            prop.type' "checkbox"
                                            prop.className "checkbox checkbox-primary"
                                            prop.isChecked opts.ImportRatings
                                            prop.onChange (fun (_: bool) -> dispatch ToggleImportRatings)
                                        ]
                                        Html.div [
                                            Html.span [ prop.className "label-text font-semibold"; prop.text "Ratings" ]
                                            Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Import your ratings (will be converted to Cinemarco's 5-tier scale)" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                        // Watchlist toggle
                        Html.div [
                            prop.className "form-control"
                            prop.children [
                                Html.label [
                                    prop.className "label cursor-pointer justify-start gap-4"
                                    prop.children [
                                        Html.input [
                                            prop.type' "checkbox"
                                            prop.className "checkbox checkbox-primary"
                                            prop.isChecked opts.ImportWatchlist
                                            prop.onChange (fun (_: bool) -> dispatch ToggleImportWatchlist)
                                        ]
                                        Html.div [
                                            Html.span [ prop.className "label-text font-semibold"; prop.text "Watchlist" ]
                                            Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Import items from your Trakt watchlist (as unwatched)" ]
                                        ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Rating conversion info
            GlassPanel.subtle [
                Html.div [
                    prop.className "flex items-start gap-3"
                    prop.children [
                        Html.div [
                            prop.className "text-info"
                            prop.children [ infoIcon ]
                        ]
                        Html.div [
                            Html.p [ prop.className "font-semibold text-sm"; prop.text "Rating Conversion" ]
                            Html.p [
                                prop.className "text-sm text-base-content/70 mt-1"
                                prop.text "Trakt ratings (1-10) will be converted: 9-10 = Outstanding, 7-8 = Entertaining, 5-6 = Decent, 3-4 = Meh, 1-2 = Waste"
                            ]
                        ]
                    ]
                ]
            ]

            // Action buttons
            Html.div [
                prop.className "flex justify-between"
                prop.children [
                    GlassButton.withLabel arrowLeftIcon "Back" "Go back" (fun () -> dispatch (GoToStep Connect))
                    if opts.ImportWatchedMovies || opts.ImportWatchedSeries || opts.ImportWatchlist then
                        GlassButton.primaryWithLabel arrowRightIcon "Preview Import" "Preview what will be imported" (fun () -> dispatch ProceedToPreview)
                ]
            ]
        ]
    ]

/// Preview step view
let private previewStep (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            RemoteDataView.withSpinner model.Preview (fun preview ->
                Html.div [
                    prop.className "space-y-6"
                    prop.children [
                        // Summary cards
                        Html.div [
                            prop.className "grid grid-cols-2 md:grid-cols-4 gap-4"
                            prop.children [
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body p-4 text-center"
                                            prop.children [
                                                Html.p [ prop.className "text-3xl font-bold"; prop.text (string preview.TotalItems) ]
                                                Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Total Items" ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body p-4 text-center"
                                            prop.children [
                                                Html.p [ prop.className "text-3xl font-bold text-success"; prop.text (string preview.NewItems) ]
                                                Html.p [ prop.className "text-sm text-base-content/60"; prop.text "New Items" ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body p-4 text-center"
                                            prop.children [
                                                Html.p [ prop.className "text-3xl font-bold"; prop.text (string preview.Movies.Length) ]
                                                Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Movies" ]
                                            ]
                                        ]
                                    ]
                                ]
                                Html.div [
                                    prop.className "card bg-base-200"
                                    prop.children [
                                        Html.div [
                                            prop.className "card-body p-4 text-center"
                                            prop.children [
                                                Html.p [ prop.className "text-3xl font-bold"; prop.text (string preview.Series.Length) ]
                                                Html.p [ prop.className "text-sm text-base-content/60"; prop.text "Series" ]
                                            ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                        // Already in library notice
                        if preview.AlreadyInLibrary > 0 then
                            Html.div [
                                prop.className "alert"
                                prop.children [
                                    infoIcon
                                    Html.span [
                                        prop.text (sprintf "%d items are already in your library and will be skipped (watch dates and ratings will be updated if applicable)" preview.AlreadyInLibrary)
                                    ]
                                ]
                            ]

                        // Movies list
                        if not preview.Movies.IsEmpty then
                            GlassPanel.standard [
                                SectionHeader.title (sprintf "Movies (%d)" preview.Movies.Length)
                                Html.div [
                                    prop.className "grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2 mt-4 max-h-64 overflow-y-auto"
                                    prop.children [
                                        for movie in preview.Movies |> List.truncate 24 do
                                            Html.div [
                                                prop.className "text-sm truncate p-2 bg-base-200 rounded"
                                                prop.title movie.Title
                                                prop.text movie.Title
                                            ]
                                    ]
                                ]
                                if preview.Movies.Length > 24 then
                                    Html.p [
                                        prop.className "text-sm text-base-content/60 mt-2"
                                        prop.text (sprintf "...and %d more" (preview.Movies.Length - 24))
                                    ]
                            ]

                        // Series list
                        if not preview.Series.IsEmpty then
                            GlassPanel.standard [
                                SectionHeader.title (sprintf "Series (%d)" preview.Series.Length)
                                Html.div [
                                    prop.className "grid grid-cols-2 md:grid-cols-4 lg:grid-cols-6 gap-2 mt-4 max-h-64 overflow-y-auto"
                                    prop.children [
                                        for series in preview.Series |> List.truncate 24 do
                                            Html.div [
                                                prop.className "text-sm truncate p-2 bg-base-200 rounded"
                                                prop.title series.Title
                                                prop.text series.Title
                                            ]
                                    ]
                                ]
                                if preview.Series.Length > 24 then
                                    Html.p [
                                        prop.className "text-sm text-base-content/60 mt-2"
                                        prop.text (sprintf "...and %d more" (preview.Series.Length - 24))
                                    ]
                            ]
                    ]
                ]
            )

            // Action buttons
            Html.div [
                prop.className "flex justify-between"
                prop.children [
                    GlassButton.withLabel arrowLeftIcon "Back" "Go back to options" (fun () -> dispatch BackToOptions)
                    match model.Preview with
                    | Success p when p.NewItems > 0 ->
                        GlassButton.successWithLabel downloadIcon "Start Import" "Begin importing items" (fun () -> dispatch StartImport)
                    | _ -> ()
                ]
            ]
        ]
    ]

/// Importing step view
let private importingStep (model: Model) (dispatch: Msg -> unit) =
    let status = model.ImportStatus
    let progress =
        if status.Total > 0 then
            float status.Completed / float status.Total * 100.0
        else 0.0

    Html.div [
        prop.className "space-y-6"
        prop.children [
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-8"
                    prop.children [
                        Html.div [
                            prop.className "mb-6"
                            prop.children [
                                Html.span [ prop.className "loading loading-spinner loading-lg" ]
                            ]
                        ]

                        Html.h2 [
                            prop.className "text-2xl font-bold mb-2"
                            prop.text "Importing..."
                        ]

                        match status.CurrentItem with
                        | Some item ->
                            Html.p [
                                prop.className "text-base-content/70 mb-4"
                                prop.text (sprintf "Importing: %s" item)
                            ]
                        | None -> Html.none

                        // Progress bar
                        Html.div [
                            prop.className "w-full max-w-md mx-auto mb-4"
                            prop.children [
                                Html.progress [
                                    prop.className "progress progress-primary w-full"
                                    prop.value (int progress)
                                    prop.max 100
                                ]
                                Html.p [
                                    prop.className "text-sm text-base-content/60 mt-2"
                                    prop.text (sprintf "%d of %d items" status.Completed status.Total)
                                ]
                            ]
                        ]

                        // Errors so far
                        if not status.Errors.IsEmpty then
                            Html.div [
                                prop.className "mt-4"
                                prop.children [
                                    Html.p [
                                        prop.className "text-error text-sm"
                                        prop.text (sprintf "%d errors so far" status.Errors.Length)
                                    ]
                                ]
                            ]

                        // Cancel button
                        Html.div [
                            prop.className "mt-4"
                            prop.children [
                                GlassButton.withLabel Components.Icons.close "Cancel" "Cancel import" (fun () -> dispatch CancelImport)
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Complete step view
let private completeStep (model: Model) (dispatch: Msg -> unit) =
    let status = model.ImportStatus

    Html.div [
        prop.className "space-y-6"
        prop.children [
            GlassPanel.standard [
                Html.div [
                    prop.className "text-center py-8"
                    prop.children [
                        // Success/warning icon
                        Html.div [
                            prop.className (if status.Errors.IsEmpty then "text-success text-6xl mb-4" else "text-warning text-6xl mb-4")
                            prop.children [
                                if status.Errors.IsEmpty then checkIcon else warningIcon
                            ]
                        ]

                        Html.h2 [
                            prop.className "text-2xl font-bold mb-2"
                            prop.text (if status.Errors.IsEmpty then "Import Complete!" else "Import Completed with Errors")
                        ]

                        Html.p [
                            prop.className "text-base-content/70 mb-6"
                            prop.text (sprintf "Successfully imported %d items" status.Completed)
                        ]

                        // Errors list
                        if not status.Errors.IsEmpty then
                            Html.div [
                                prop.className "text-left max-w-md mx-auto mb-6"
                                prop.children [
                                    Html.p [ prop.className "font-semibold text-error mb-2"; prop.text "Errors:" ]
                                    Html.ul [
                                        prop.className "list-disc list-inside text-sm text-base-content/70 max-h-48 overflow-y-auto"
                                        prop.children [
                                            for error in status.Errors |> List.truncate 10 do
                                                Html.li [ prop.text error ]
                                        ]
                                    ]
                                    if status.Errors.Length > 10 then
                                        Html.p [
                                            prop.className "text-sm text-base-content/60 mt-2"
                                            prop.text (sprintf "...and %d more errors" (status.Errors.Length - 10))
                                        ]
                                ]
                            ]

                        // Action buttons
                        Html.div [
                            prop.className "flex gap-4 justify-center"
                            prop.children [
                                Html.a [
                                    prop.className "detail-action-btn detail-action-btn-with-label detail-action-btn-emphasis"
                                    prop.href "/library"
                                    prop.children [
                                        Html.span [ prop.className "w-5 h-5"; prop.children [ Components.Icons.film ] ]
                                        Html.span [ prop.className "text-sm font-medium"; prop.text "View Library" ]
                                    ]
                                ]
                                GlassButton.withLabel arrowRightIcon "Import More" "Import more items" (fun () -> dispatch (GoToStep SelectOptions))
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.children [
            // Header
            Html.div [
                prop.className "mb-8"
                prop.children [
                    Html.h1 [
                        prop.className "text-3xl font-bold"
                        prop.text "Import from Trakt.tv"
                    ]
                    Html.p [
                        prop.className "text-base-content/60 mt-1"
                        prop.text "Import your watch history and ratings from Trakt.tv"
                    ]
                ]
            ]

            // Step indicator
            stepIndicator model.CurrentStep

            // Error display
            match model.Error with
            | Some err ->
                Html.div [
                    prop.className "alert alert-error mb-6"
                    prop.children [
                        warningIcon
                        Html.span [ prop.text err ]
                        Html.button [
                            prop.className "btn btn-ghost btn-sm"
                            prop.onClick (fun _ -> dispatch (GoToStep model.CurrentStep))
                            prop.text "Dismiss"
                        ]
                    ]
                ]
            | None -> Html.none

            // Current step content
            match model.CurrentStep with
            | Connect -> connectStep model dispatch
            | SelectOptions -> optionsStep model dispatch
            | Preview -> previewStep model dispatch
            | Importing -> importingStep model dispatch
            | Complete -> completeStep model dispatch

            // Alternative import option
            Html.div [
                prop.className "mt-12 pt-8 border-t border-base-content/10"
                prop.children [
                    Html.div [
                        prop.className "text-center"
                        prop.children [
                            Html.p [
                                prop.className "text-base-content/50 text-sm mb-3"
                                prop.text "Have a JSON export from another source?"
                            ]
                            Html.a [
                                prop.href "/import-json"
                                prop.className "btn btn-ghost btn-sm"
                                prop.children [
                                    downloadIcon
                                    Html.span [ prop.text "Import from JSON" ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]

            // Resync section - for filling gaps in watch history
            if model.ConnectionStatus = Connected then
                Html.div [
                    prop.className "mt-8 pt-8 border-t border-base-content/10"
                    prop.children [
                        GlassPanel.subtle [
                            SectionHeader.titleSmall "Resync from Date"

                            Html.p [
                                prop.className "text-base-content/60 text-sm mb-4"
                                prop.text "Fill gaps in your watch history by resyncing from a specific date. This won't duplicate existing entries."
                            ]

                            Html.div [
                                prop.className "flex flex-wrap items-end gap-4"
                                prop.children [
                                    Html.div [
                                        prop.className "form-control"
                                        prop.children [
                                            Html.label [
                                                prop.className "label"
                                                prop.children [
                                                    Html.span [ prop.className "label-text"; prop.text "Sync from" ]
                                                ]
                                            ]
                                            Html.input [
                                                prop.type' "date"
                                                prop.className "input input-bordered input-sm w-40"
                                                prop.value (
                                                    match model.ResyncDate with
                                                    | Some d -> d.ToString("yyyy-MM-dd")
                                                    | None -> ""
                                                )
                                                prop.onChange (fun (value: string) ->
                                                    match DateTime.TryParse(value) with
                                                    | true, date -> dispatch (SetResyncDate date)
                                                    | false, _ -> ()
                                                )
                                            ]
                                        ]
                                    ]

                                    match model.ResyncStatus with
                                    | Loading ->
                                        Html.button [
                                            prop.className "btn btn-sm btn-primary"
                                            prop.disabled true
                                            prop.children [
                                                Html.span [ prop.className "loading loading-spinner loading-xs" ]
                                                Html.span [ prop.text "Syncing..." ]
                                            ]
                                        ]
                                    | _ ->
                                        Html.button [
                                            prop.className "btn btn-sm btn-primary"
                                            prop.disabled model.ResyncDate.IsNone
                                            prop.onClick (fun _ -> dispatch StartResync)
                                            prop.text "Start Resync"
                                        ]
                                ]
                            ]

                            // Show result
                            match model.ResyncStatus with
                            | Success result ->
                                Html.div [
                                    prop.className "mt-4 text-sm"
                                    prop.children [
                                        Html.div [
                                            prop.className "badge badge-success gap-1"
                                            prop.children [
                                                checkIcon
                                                Html.span [
                                                    prop.text (sprintf "%d movies, %d episodes synced" result.NewMovieWatches result.NewEpisodeWatches)
                                                ]
                                            ]
                                        ]
                                        if not result.Errors.IsEmpty then
                                            Html.p [
                                                prop.className "text-warning mt-2"
                                                prop.text (sprintf "%d errors occurred" result.Errors.Length)
                                            ]
                                    ]
                                ]
                            | Failure err ->
                                Html.div [
                                    prop.className "mt-4 alert alert-error text-sm"
                                    prop.text err
                                ]
                            | _ -> Html.none
                        ]
                    ]
                ]
        ]
    ]
