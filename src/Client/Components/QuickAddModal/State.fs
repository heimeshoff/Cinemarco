module Components.QuickAddModal.State

open Elmish
open Shared.Api
open Shared.Domain
open Types

type AddApi = {
    AddMovie: AddMovieRequest -> Async<Result<LibraryEntry, string>>
    AddSeries: AddSeriesRequest -> Async<Result<LibraryEntry, string>>
}

let init (item: TmdbSearchResult) : Model =
    Model.create item

let update (api: AddApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | NoteChanged note ->
        { model with WhyAddedNote = note }, Cmd.none, NoOp

    | ToggleTag tagId ->
        let newTags =
            if List.contains tagId model.SelectedTags then
                List.filter (fun t -> t <> tagId) model.SelectedTags
            else
                tagId :: model.SelectedTags
        { model with SelectedTags = newTags }, Cmd.none, NoOp

    | ToggleFriend friendId ->
        let newFriends =
            if List.contains friendId model.SelectedFriends then
                List.filter (fun f -> f <> friendId) model.SelectedFriends
            else
                friendId :: model.SelectedFriends
        { model with SelectedFriends = newFriends }, Cmd.none, NoOp

    | Submit ->
        let whyAdded =
            if String.length model.WhyAddedNote > 0 then
                Some {
                    RecommendedBy = None
                    RecommendedByName = None
                    Source = None
                    Context = Some model.WhyAddedNote
                    DateRecommended = None
                }
            else None

        let cmd =
            match model.SelectedItem.MediaType with
            | Movie ->
                let request : AddMovieRequest = {
                    TmdbId = TmdbMovieId model.SelectedItem.TmdbId
                    WhyAdded = whyAdded
                    InitialTags = model.SelectedTags
                    InitialFriends = model.SelectedFriends
                }
                Cmd.OfAsync.either
                    api.AddMovie
                    request
                    SubmitResult
                    (fun ex -> Error ex.Message |> SubmitResult)
            | Series ->
                let request : AddSeriesRequest = {
                    TmdbId = TmdbSeriesId model.SelectedItem.TmdbId
                    WhyAdded = whyAdded
                    InitialTags = model.SelectedTags
                    InitialFriends = model.SelectedFriends
                }
                Cmd.OfAsync.either
                    api.AddSeries
                    request
                    SubmitResult
                    (fun ex -> Error ex.Message |> SubmitResult)

        { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok entry) ->
        { model with IsSubmitting = false }, Cmd.none, Added entry

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
