module Common.Types

/// Represents the state of a remote data fetch
type RemoteData<'T> =
    | NotAsked
    | Loading
    | Success of 'T
    | Failure of string

/// Helper functions for RemoteData
module RemoteData =
    let isLoading = function
        | Loading -> true
        | _ -> false

    let isSuccess = function
        | Success _ -> true
        | _ -> false

    let toOption = function
        | Success x -> Some x
        | _ -> None

    let defaultValue def = function
        | Success x -> x
        | _ -> def

    let map f = function
        | NotAsked -> NotAsked
        | Loading -> Loading
        | Success x -> Success (f x)
        | Failure err -> Failure err

    let bind f = function
        | NotAsked -> NotAsked
        | Loading -> Loading
        | Success x -> f x
        | Failure err -> Failure err
