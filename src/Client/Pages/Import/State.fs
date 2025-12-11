module Pages.Import.State

open Elmish
open Common.Types
open Shared.Domain
open Types

type TraktApi = {
    GetAuthUrl: unit -> Async<Result<TraktAuthUrl, string>>
    ExchangeCode: string * string -> Async<Result<unit, string>>
    IsAuthenticated: unit -> Async<bool>
    Logout: unit -> Async<unit>
    GetImportPreview: TraktImportOptions -> Async<Result<TraktImportPreview, string>>
    StartImport: TraktImportOptions -> Async<Result<unit, string>>
    GetImportStatus: unit -> Async<ImportStatus>
    CancelImport: unit -> Async<unit>
}

let init () : Model * Cmd<Msg> =
    Model.empty, Cmd.ofMsg CheckConnection

let update (api: TraktApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    // Connection
    | CheckConnection ->
        let cmd =
            Cmd.OfAsync.perform
                api.IsAuthenticated
                ()
                ConnectionChecked
        { model with ConnectionStatus = Checking }, cmd, NoOp

    | ConnectionChecked isAuth ->
        let status = if isAuth then Connected else NotConnected
        let step = if isAuth then SelectOptions else Connect
        { model with ConnectionStatus = status; CurrentStep = step }, Cmd.none, NoOp

    | GetAuthUrl ->
        let cmd =
            Cmd.OfAsync.either
                api.GetAuthUrl
                ()
                (fun result -> AuthUrlReceived result)
                (fun ex -> AuthUrlReceived (Error ex.Message))
        { model with AuthUrl = Loading }, cmd, NoOp

    | AuthUrlReceived (Ok authUrl) ->
        { model with AuthUrl = Success authUrl }, Cmd.none, NoOp

    | AuthUrlReceived (Error err) ->
        { model with AuthUrl = Failure err; Error = Some err }, Cmd.none, NoOp

    | UpdateAuthCode code ->
        { model with AuthCode = code }, Cmd.none, NoOp

    | SubmitAuthCode ->
        if model.AuthCode = "" then
            model, Cmd.none, ShowNotification ("Please enter the authorization code", false)
        else
            // Use the state from the auth URL, or empty string if not available
            let state =
                match model.AuthUrl with
                | Success authUrl -> authUrl.State
                | _ -> ""
            let cmd =
                Cmd.OfAsync.either
                    api.ExchangeCode
                    (model.AuthCode, state)
                    AuthCodeSubmitted
                    (fun ex -> AuthCodeSubmitted (Error ex.Message))
            model, cmd, NoOp

    | AuthCodeSubmitted (Ok ()) ->
        { model with
            ConnectionStatus = Connected
            CurrentStep = SelectOptions
            AuthCode = ""
            AuthUrl = NotAsked
        }, Cmd.none, ShowNotification ("Successfully connected to Trakt!", true)

    | AuthCodeSubmitted (Error err) ->
        { model with Error = Some err },
        Cmd.none,
        ShowNotification ($"Failed to connect: {err}", false)

    | Logout ->
        let cmd = Cmd.OfAsync.perform api.Logout () (fun () -> LoggedOut)
        model, cmd, NoOp

    | LoggedOut ->
        { Model.empty with ConnectionStatus = NotConnected },
        Cmd.none,
        ShowNotification ("Disconnected from Trakt", true)

    // Options
    | ToggleImportMovies ->
        let newOpts = { model.ImportOptions with ImportWatchedMovies = not model.ImportOptions.ImportWatchedMovies }
        { model with ImportOptions = newOpts }, Cmd.none, NoOp

    | ToggleImportSeries ->
        let newOpts = { model.ImportOptions with ImportWatchedSeries = not model.ImportOptions.ImportWatchedSeries }
        { model with ImportOptions = newOpts }, Cmd.none, NoOp

    | ToggleImportRatings ->
        let newOpts = { model.ImportOptions with ImportRatings = not model.ImportOptions.ImportRatings }
        { model with ImportOptions = newOpts }, Cmd.none, NoOp

    | ToggleImportWatchlist ->
        let newOpts = { model.ImportOptions with ImportWatchlist = not model.ImportOptions.ImportWatchlist }
        { model with ImportOptions = newOpts }, Cmd.none, NoOp

    | ProceedToPreview ->
        { model with CurrentStep = Preview }, Cmd.ofMsg LoadPreview, NoOp

    // Preview
    | LoadPreview ->
        let cmd =
            Cmd.OfAsync.either
                api.GetImportPreview
                model.ImportOptions
                PreviewLoaded
                (fun ex -> PreviewLoaded (Error ex.Message))
        { model with Preview = Loading }, cmd, NoOp

    | PreviewLoaded (Ok preview) ->
        { model with Preview = Success preview }, Cmd.none, NoOp

    | PreviewLoaded (Error err) ->
        { model with Preview = Failure err; Error = Some err },
        Cmd.none,
        ShowNotification ($"Failed to load preview: {err}", false)

    | BackToOptions ->
        { model with CurrentStep = SelectOptions; Preview = NotAsked }, Cmd.none, NoOp

    // Import
    | StartImport ->
        let cmd =
            Cmd.OfAsync.either
                api.StartImport
                model.ImportOptions
                ImportStarted
                (fun ex -> ImportStarted (Error ex.Message))
        { model with CurrentStep = Importing }, cmd, NoOp

    | ImportStarted (Ok ()) ->
        // Start polling for status
        let pollCmd = Cmd.ofMsg PollImportStatus
        { model with IsPollingStatus = true }, pollCmd, NoOp

    | ImportStarted (Error err) ->
        { model with
            CurrentStep = Preview
            Error = Some err
        }, Cmd.none, ShowNotification ($"Failed to start import: {err}", false)

    | PollImportStatus ->
        if not model.IsPollingStatus then
            model, Cmd.none, NoOp
        else
            let cmd =
                Cmd.OfAsync.perform
                    api.GetImportStatus
                    ()
                    ImportStatusReceived
            model, cmd, NoOp

    | ImportStatusReceived status ->
        let nextModel = { model with ImportStatus = status }

        if status.InProgress then
            // Continue polling after a delay
            let delayedPoll = Cmd.OfAsync.perform
                                (fun () -> async { do! Async.Sleep 1000 })
                                ()
                                (fun () -> PollImportStatus)
            nextModel, delayedPoll, NoOp
        else
            // Import complete
            let msg =
                if status.Errors.IsEmpty then
                    sprintf "Import complete! Imported %d items." status.Completed
                else
                    sprintf "Import complete with %d errors. Imported %d of %d items."
                        status.Errors.Length status.Completed status.Total
            { nextModel with
                CurrentStep = Complete
                IsPollingStatus = false
            }, Cmd.none, ShowNotification (msg, status.Errors.IsEmpty)

    | CancelImport ->
        let cmd = Cmd.OfAsync.perform api.CancelImport () (fun () -> PollImportStatus)
        model, cmd, ShowNotification ("Cancelling import...", true)

    // Navigation
    | GoToStep step ->
        { model with CurrentStep = step; Error = None }, Cmd.none, NoOp
