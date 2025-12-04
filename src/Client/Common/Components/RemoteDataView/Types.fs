module Common.Components.RemoteDataView.Types

open Feliz
open Common.Types

type LoadingView =
    | Spinner
    | Skeleton of count: int
    | Custom of ReactElement

type Model<'T> = {
    Data: RemoteData<'T>
    LoadingView: LoadingView
    ErrorContext: string option
}

module Model =
    let create data = {
        Data = data
        LoadingView = Spinner
        ErrorContext = None
    }

    let withSkeleton count model = { model with LoadingView = Skeleton count }
    let withSpinner model = { model with LoadingView = Spinner }
    let withCustomLoading element model = { model with LoadingView = Custom element }
    let withErrorContext ctx model = { model with ErrorContext = Some ctx }
