module Common.AsyncCmd

open Elmish

/// Helper module for reducing Cmd.OfAsync boilerplate
/// Provides common patterns for async command handling
module AsyncCmd =

    /// Execute async operation, wrapping result in Ok/Error
    /// Use this when your message takes Result<'T, string>
    /// Example: loadResult api.getThings () ThingsLoaded
    let loadResult (apiCall: 'Arg -> Async<'Result>) (arg: 'Arg) (toMsg: Result<'Result, string> -> 'Msg) : Cmd<'Msg> =
        Cmd.OfAsync.either
            apiCall
            arg
            (Ok >> toMsg)
            (fun ex -> Error ex.Message |> toMsg)

    /// Execute async operation with unit arg, wrapping result in Ok/Error
    /// Use this when your message takes Result<'T, string> and API needs no args
    /// Example: loadResult0 api.getAll ItemsLoaded
    let loadResult0 (apiCall: unit -> Async<'Result>) (toMsg: Result<'Result, string> -> 'Msg) : Cmd<'Msg> =
        loadResult apiCall () toMsg

    /// Execute async operation that already returns Result
    /// The API returns Result<'T, string> so we just map success/failure
    /// Example: callApi api.saveItem item ItemSaved
    let callApi (apiCall: 'Arg -> Async<Result<'Result, string>>) (arg: 'Arg) (toMsg: Result<'Result, string> -> 'Msg) : Cmd<'Msg> =
        Cmd.OfAsync.either
            apiCall
            arg
            toMsg
            (fun ex -> Error ex.Message |> toMsg)

    /// Execute async operation that already returns Result, with unit arg
    /// Example: callApi0 api.getAllItems ItemsLoaded
    let callApi0 (apiCall: unit -> Async<Result<'Result, string>>) (toMsg: Result<'Result, string> -> 'Msg) : Cmd<'Msg> =
        callApi apiCall () toMsg

    /// Execute async operation, handling success with one message and failure with notification
    /// Use when you want to handle errors uniformly via notification
    /// Example: load api.getThings () ThingsLoaded (fun err -> ShowNotification (err, false))
    let load (apiCall: 'Arg -> Async<'Result>) (arg: 'Arg) (onSuccess: 'Result -> 'Msg) (onError: string -> 'Msg) : Cmd<'Msg> =
        Cmd.OfAsync.either
            apiCall
            arg
            onSuccess
            (fun ex -> onError ex.Message)

    /// Execute async operation with unit arg, handling success and failure separately
    /// Example: load0 api.getAll ItemsLoaded (fun err -> ShowNotification (err, false))
    let load0 (apiCall: unit -> Async<'Result>) (onSuccess: 'Result -> 'Msg) (onError: string -> 'Msg) : Cmd<'Msg> =
        load apiCall () onSuccess onError

    /// Execute async operation, converting directly to message
    /// Use when the operation can't fail meaningfully (or failure should be silent)
    /// Example: perform api.getOptionalThing () GotOptional
    let perform (apiCall: 'Arg -> Async<'Result>) (arg: 'Arg) (toMsg: 'Result -> 'Msg) : Cmd<'Msg> =
        Cmd.OfAsync.perform
            apiCall
            arg
            toMsg

    /// Execute async operation with unit arg, converting directly to message
    /// Example: perform0 api.getOptionalThing GotOptional
    let perform0 (apiCall: unit -> Async<'Result>) (toMsg: 'Result -> 'Msg) : Cmd<'Msg> =
        perform apiCall () toMsg

    /// Execute multiple async operations in parallel, combining results
    /// Returns when all complete
    let parallel2
        (cmd1: Cmd<'Msg>)
        (cmd2: Cmd<'Msg>)
        : Cmd<'Msg> =
        Cmd.batch [ cmd1; cmd2 ]

    /// Execute multiple async operations in parallel
    let parallelAll (cmds: Cmd<'Msg> list) : Cmd<'Msg> =
        Cmd.batch cmds
