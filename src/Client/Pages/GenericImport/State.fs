module Pages.GenericImport.State

open Elmish
open Pages.GenericImport.Types
open Common.Types
open Shared.Domain
open Shared.Api

let init () =
    Model.empty, Cmd.none

let update (api: ICinemarcoApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    // =====================================
    // File Selection Step
    // =====================================

    | FileSelected (fileName, content) ->
        { model with
            SelectedFileName = Some fileName
            FileContent = Some content
            ParsedItems = NotAsked
            Error = None },
        Cmd.none,
        NoOp

    | ParseFile ->
        match model.FileContent with
        | None ->
            { model with Error = Some "No file selected" }, Cmd.none, NoOp
        | Some content ->
            { model with ParsedItems = Loading; Error = None },
            Cmd.OfAsync.either
                api.genericImportParseJson
                content
                FileParsed
                (fun ex -> FileParsed (Error ex.Message)),
            NoOp

    | FileParsed result ->
        match result with
        | Ok items ->
            { model with
                ParsedItems = Success items
                Error = None },
            Cmd.none,
            NoOp
        | Error err ->
            { model with
                ParsedItems = Failure err
                Error = Some err },
            Cmd.none,
            NoOp

    | ClearFile ->
        { model with
            SelectedFileName = None
            FileContent = None
            ParsedItems = NotAsked
            Error = None },
        Cmd.none,
        NoOp

    | ProceedToMatching ->
        match model.ParsedItems with
        | Success items ->
            { model with
                CurrentStep = MatchingPreview
                Preview = Loading },
            Cmd.OfAsync.either
                api.genericImportPreview
                items
                PreviewLoaded
                (fun ex -> PreviewLoaded (Error ex.Message)),
            NoOp
        | _ ->
            model, Cmd.none, NoOp

    // =====================================
    // Matching Preview Step
    // =====================================

    | LoadPreview ->
        match model.ParsedItems with
        | Success items ->
            { model with Preview = Loading },
            Cmd.OfAsync.either
                api.genericImportPreview
                items
                PreviewLoaded
                (fun ex -> PreviewLoaded (Error ex.Message)),
            NoOp
        | _ ->
            model, Cmd.none, NoOp

    | PreviewLoaded result ->
        match result with
        | Ok preview ->
            { model with
                Preview = Success preview
                EditingItems = preview.Items
                Error = None },
            Cmd.none,
            NoOp
        | Error err ->
            { model with
                Preview = Failure err
                Error = Some err },
            Cmd.none,
            NoOp

    | BackToFile ->
        { model with
            CurrentStep = SelectFile
            Preview = NotAsked
            EditingItems = []
            ResolvingIndex = None },
        Cmd.none,
        NoOp

    // =====================================
    // Resolve Ambiguous Step
    // =====================================

    | StartResolving ->
        // Find the first ambiguous item
        let firstAmbiguous =
            model.EditingItems
            |> List.tryFindIndex (fun item ->
                match item.MatchStatus with
                | MultipleMatches _ -> true
                | _ -> false)
        { model with
            CurrentStep = ResolveAmbiguous
            ResolvingIndex = firstAmbiguous },
        Cmd.none,
        NoOp

    | ConfirmMatch (index, selectedMatch) ->
        model,
        Cmd.OfAsync.either
            api.genericImportConfirmMatch
            (index, selectedMatch)
            MatchConfirmationReceived
            (fun ex -> MatchConfirmationReceived (Error ex.Message)),
        NoOp

    | MatchConfirmationReceived result ->
        match result with
        | Ok updatedItem ->
            // Update the item in editingItems
            let updatedItems =
                model.EditingItems
                |> List.mapi (fun i item ->
                    match model.ResolvingIndex with
                    | Some idx when i = idx -> updatedItem
                    | _ -> item)
            { model with EditingItems = updatedItems },
            Cmd.ofMsg NextAmbiguous,
            NoOp
        | Error err ->
            { model with Error = Some err },
            Cmd.none,
            ShowNotification ($"Match confirmation failed: {err}", false)

    | SkipItem index ->
        // Mark the item to be skipped (set match status to NoMatchFound to exclude from import)
        let updatedItems =
            model.EditingItems
            |> List.mapi (fun i item ->
                if i = index then { item with MatchStatus = NoMatchFound }
                else item)
        { model with EditingItems = updatedItems },
        Cmd.ofMsg NextAmbiguous,
        NoOp

    | NextAmbiguous ->
        // Find the next ambiguous item after the current one
        let nextIndex =
            match model.ResolvingIndex with
            | Some current ->
                model.EditingItems
                |> List.indexed
                |> List.tryFind (fun (i, item) ->
                    i > current &&
                    match item.MatchStatus with
                    | MultipleMatches _ -> true
                    | _ -> false)
                |> Option.map fst
            | None -> None
        match nextIndex with
        | Some idx ->
            { model with ResolvingIndex = Some idx }, Cmd.none, NoOp
        | None ->
            // No more ambiguous items, go back to preview
            { model with
                CurrentStep = MatchingPreview
                ResolvingIndex = None
                Preview =
                    match model.Preview with
                    | Success preview ->
                        Success { preview with Items = model.EditingItems }
                    | other -> other },
            Cmd.none,
            NoOp

    | ProceedToImport ->
        { model with CurrentStep = Importing },
        Cmd.ofMsg StartImport,
        NoOp

    // =====================================
    // Import Step
    // =====================================

    | StartImport ->
        // Filter to importable items (with ExactMatch or MatchConfirmed)
        let importableItems =
            model.EditingItems
            |> List.filter (fun item ->
                match item.MatchStatus with
                | ExactMatch _ | MatchConfirmed _ -> true
                | _ -> false)
        { model with
            Progress = { Model.emptyProgress with InProgress = true; TotalItems = importableItems.Length }
            IsPollingProgress = true },
        Cmd.batch [
            Cmd.OfAsync.either
                api.genericImportStart
                importableItems
                ImportStarted
                (fun ex -> ImportStarted (Error ex.Message))
            // Start polling after a short delay
            Cmd.OfAsync.perform
                (fun () -> async { do! Async.Sleep 500 })
                ()
                (fun _ -> PollProgress)
        ],
        NoOp

    | ImportStarted result ->
        match result with
        | Ok () ->
            model, Cmd.none, NoOp
        | Error err ->
            { model with
                Error = Some err
                IsPollingProgress = false },
            Cmd.none,
            ShowNotification ($"Import failed to start: {err}", false)

    | PollProgress ->
        if model.IsPollingProgress then
            model,
            Cmd.OfAsync.either
                api.genericImportGetProgress
                ()
                ProgressReceived
                (fun ex -> ProgressReceived { Model.emptyProgress with Errors = [ex.Message] }),
            NoOp
        else
            model, Cmd.none, NoOp

    | ProgressReceived progress ->
        if progress.InProgress then
            // Continue polling
            { model with Progress = progress },
            Cmd.OfAsync.perform
                (fun () -> async { do! Async.Sleep 500 })
                ()
                (fun _ -> PollProgress),
            NoOp
        else
            // Import completed
            let result: GenericImportResult = {
                ImportedMovies = progress.CompletedSuccessfully  // Approximate
                ImportedSeries = 0
                AddedWatchSessions = 0
                ImportedEpisodes = 0
                CreatedFriends = 0
                Skipped = progress.Skipped
                Errors = progress.Errors
            }
            { model with
                Progress = progress
                IsPollingProgress = false
                CurrentStep = Complete
                Result = Some result },
            Cmd.none,
            if progress.Errors.IsEmpty then
                ShowNotification ("Import completed successfully!", true)
            else
                ShowNotification ($"Import completed with {progress.Errors.Length} errors", false)

    | CancelImport ->
        { model with IsPollingProgress = false },
        Cmd.OfAsync.perform
            api.genericImportCancel
            ()
            (fun _ -> PollProgress),
        ShowNotification ("Import cancelled", false)

    // =====================================
    // Navigation
    // =====================================

    | GoToStep step ->
        { model with CurrentStep = step }, Cmd.none, NoOp
