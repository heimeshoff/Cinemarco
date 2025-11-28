module Components.TagModal.State

open Elmish
open Shared.Api
open Shared.Domain
open Types

type SaveApi = {
    Create: CreateTagRequest -> Async<Result<Tag, string>>
    Update: UpdateTagRequest -> Async<Result<Tag, string>>
}

let init (tag: Tag option) : Model =
    match tag with
    | Some t -> Model.fromTag t
    | None -> Model.empty

let update (api: SaveApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | NameChanged name ->
        { model with Name = name }, Cmd.none, NoOp

    | ColorChanged color ->
        { model with Color = color }, Cmd.none, NoOp

    | DescriptionChanged description ->
        { model with Description = description }, Cmd.none, NoOp

    | Submit ->
        if model.Name.Trim().Length = 0 then
            { model with Error = Some "Name is required" }, Cmd.none, NoOp
        else
            let cmd =
                match model.EditingTag with
                | None ->
                    let request : CreateTagRequest = {
                        Name = model.Name.Trim()
                        Color = if model.Color.Trim().Length > 0 then Some (model.Color.Trim()) else None
                        Description = if model.Description.Trim().Length > 0 then Some (model.Description.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        api.Create
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
                | Some existing ->
                    let request : UpdateTagRequest = {
                        Id = existing.Id
                        Name = Some (model.Name.Trim())
                        Color = if model.Color.Trim().Length > 0 then Some (model.Color.Trim()) else None
                        Description = if model.Description.Trim().Length > 0 then Some (model.Description.Trim()) else None
                    }
                    Cmd.OfAsync.either
                        api.Update
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok tag) ->
        { model with IsSubmitting = false }, Cmd.none, Saved tag

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
