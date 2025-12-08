module Components.ProfileImageEditor.State

open Elmish
open Types

let init (existingImageUrl: string option) : Model =
    match existingImageUrl with
    | Some url -> Model.withImage url
    | None -> Model.empty

let private clampPan (model: Model) : Position =
    if model.ImageWidth = 0 || model.ImageHeight = 0 then
        model.Pan
    else
        // Calculate how much the image extends beyond the visible area
        let canvasSize = 300.0  // Preview canvas size
        let minDim = min model.ImageWidth model.ImageHeight |> float
        let scale = (canvasSize / minDim) * model.Zoom

        let scaledWidth = float model.ImageWidth * scale
        let scaledHeight = float model.ImageHeight * scale

        // Max pan is half the overflow on each side
        let maxPanX = max 0.0 ((scaledWidth - canvasSize) / 2.0)
        let maxPanY = max 0.0 ((scaledHeight - canvasSize) / 2.0)

        { X = max -maxPanX (min maxPanX model.Pan.X)
          Y = max -maxPanY (min maxPanY model.Pan.Y) }

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> * ExternalMsg =
    match msg with
    | FileSelected base64DataUrl ->
        { model with
            OriginalImage = Some base64DataUrl
            Zoom = 1.0
            Pan = { X = 0.0; Y = 0.0 }
            ImageWidth = 0
            ImageHeight = 0 }, Cmd.none, NoOp

    | ImageLoaded (width, height) ->
        { model with
            ImageWidth = width
            ImageHeight = height }, Cmd.none, NoOp

    | ZoomChanged zoom ->
        let newZoom = max 1.0 (min 3.0 zoom)  // Clamp between 1x and 3x
        let newModel = { model with Zoom = newZoom }
        { newModel with Pan = clampPan newModel }, Cmd.none, NoOp

    | StartDrag (x, y) ->
        { model with
            IsDragging = true
            DragStart = Some { X = x; Y = y } }, Cmd.none, NoOp

    | Drag (x, y) ->
        match model.DragStart with
        | Some start when model.IsDragging ->
            let dx = x - start.X
            let dy = y - start.Y
            let newPan = { X = model.Pan.X + dx; Y = model.Pan.Y + dy }
            let newModel = { model with
                                Pan = newPan
                                DragStart = Some { X = x; Y = y } }
            { newModel with Pan = clampPan newModel }, Cmd.none, NoOp
        | _ -> model, Cmd.none, NoOp

    | EndDrag ->
        { model with
            IsDragging = false
            DragStart = None }, Cmd.none, NoOp

    | Confirm ->
        // The actual cropping happens in the View via Canvas
        // This message is handled there and triggers the external msg
        model, Cmd.none, NoOp

    | Cancel ->
        model, Cmd.none, Cancelled
