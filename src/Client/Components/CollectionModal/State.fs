module Components.CollectionModal.State

open Elmish
open Shared.Api
open Shared.Domain
open Types

type SaveApi = {
    Create: CreateCollectionRequest -> Async<Result<Collection, string>>
    Update: UpdateCollectionRequest -> Async<Result<Collection, string>>
}

let init (collection: Collection option) : Model =
    match collection with
    | Some c -> Model.fromCollection c
    | None -> Model.empty

let update (api: SaveApi) (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | NameChanged name ->
        { model with Name = name }, Cmd.none, NoOp

    | DescriptionChanged description ->
        { model with Description = description }, Cmd.none, NoOp

    | LogoSelected base64DataUrl ->
        { model with
            LogoBase64 = Some base64DataUrl
            LogoPreview = Some base64DataUrl
            LogoRemoved = false }, Cmd.none, NoOp

    | LogoRemoved ->
        { model with
            LogoBase64 = None
            LogoPreview = None
            LogoRemoved = true }, Cmd.none, NoOp

    | Submit ->
        if model.Name.Trim().Length = 0 then
            { model with Error = Some "Name is required" }, Cmd.none, NoOp
        else
            let description = if model.Description.Trim().Length > 0 then Some (model.Description.Trim()) else None
            let cmd =
                match model.EditingCollection with
                | None ->
                    let request : CreateCollectionRequest = {
                        Name = model.Name.Trim()
                        Description = description
                        LogoBase64 = model.LogoBase64
                    }
                    Cmd.OfAsync.either
                        api.Create
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
                | Some existing ->
                    // For update: None = keep existing, Some "" = remove, Some data = new logo
                    let logoForUpdate =
                        if model.LogoRemoved then Some ""
                        elif model.LogoBase64.IsSome then model.LogoBase64
                        else None
                    let request : UpdateCollectionRequest = {
                        Id = existing.Id
                        Name = Some (model.Name.Trim())
                        Description = description
                        LogoBase64 = logoForUpdate
                    }
                    Cmd.OfAsync.either
                        api.Update
                        request
                        SubmitResult
                        (fun ex -> Error ex.Message |> SubmitResult)
            { model with IsSubmitting = true; Error = None }, cmd, NoOp

    | SubmitResult (Ok collection) ->
        { model with IsSubmitting = false }, Cmd.none, Saved collection

    | SubmitResult (Error err) ->
        { model with IsSubmitting = false; Error = Some err }, Cmd.none, NoOp

    | Close ->
        model, Cmd.none, CloseRequested
