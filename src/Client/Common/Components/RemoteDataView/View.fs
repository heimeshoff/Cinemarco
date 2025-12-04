module Common.Components.RemoteDataView.View

open Feliz
open Common.Types
open Common.Components.RemoteDataView.Types

module PosterSkeleton = Common.Components.PosterSkeleton.View
module ErrorStateView = Common.Components.ErrorState.View
module ErrorStateTypes = Common.Components.ErrorState.Types

let view<'T> (model: Model<'T>) (onSuccess: 'T -> ReactElement) =
    match model.Data with
    | NotAsked -> Html.none
    | Loading ->
        match model.LoadingView with
        | Spinner ->
            Html.div [
                prop.className "flex justify-center py-12"
                prop.children [
                    Html.span [ prop.className "loading loading-spinner loading-lg" ]
                ]
            ]
        | Skeleton count -> PosterSkeleton.view count
        | Custom element -> element
    | Success data -> onSuccess data
    | Failure err ->
        ErrorStateView.view {
            ErrorStateTypes.Message = err
            Context = model.ErrorContext
        }

/// Simple view with spinner loading
let withSpinner (data: RemoteData<'T>) (onSuccess: 'T -> ReactElement) =
    view (Model.create data) onSuccess

/// View with skeleton loading for poster grids
let withSkeleton (count: int) (data: RemoteData<'T>) (onSuccess: 'T -> ReactElement) =
    view (Model.create data |> Model.withSkeleton count) onSuccess

/// View with error context
let withContext (ctx: string) (data: RemoteData<'T>) (onSuccess: 'T -> ReactElement) =
    view (Model.create data |> Model.withErrorContext ctx) onSuccess

/// View with skeleton and error context
let withSkeletonAndContext (count: int) (ctx: string) (data: RemoteData<'T>) (onSuccess: 'T -> ReactElement) =
    view (Model.create data |> Model.withSkeleton count |> Model.withErrorContext ctx) onSuccess
