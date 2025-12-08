module Components.ProfileImageEditor.View

open Feliz
open Browser.Types
open Browser.Dom
open Fable.Core
open Fable.Core.JsInterop
open Types
open Components.Modal.View

/// Canvas size for preview (will be square)
let private canvasSize = 300

/// Handle file selection from input
let private handleFileSelect (dispatch: Msg -> unit) (e: Event) =
    let input = e.target :?> HTMLInputElement
    if input.files.length > 0 then
        let file = input.files.[0]
        let reader = FileReader.Create()
        reader.onload <- fun _ ->
            let result = reader.result :?> string
            dispatch (FileSelected result)
        reader.readAsDataURL(file)

/// Draw the image on canvas with current zoom/pan settings
let private drawCanvas (canvas: HTMLCanvasElement) (img: HTMLImageElement) (model: Model) =
    let ctx = canvas.getContext_2d()
    let size = float canvasSize

    // Clear canvas
    ctx.clearRect(0.0, 0.0, size, size)

    if model.ImageWidth > 0 && model.ImageHeight > 0 then
        // Calculate scale to fit smallest dimension
        let minDim = min model.ImageWidth model.ImageHeight |> float
        let baseScale = size / minDim
        let scale = baseScale * model.Zoom

        // Calculate source and destination
        let scaledWidth = float model.ImageWidth * scale
        let scaledHeight = float model.ImageHeight * scale

        // Center the image, then apply pan offset
        let dx = (size - scaledWidth) / 2.0 + model.Pan.X
        let dy = (size - scaledHeight) / 2.0 + model.Pan.Y

        ctx.drawImage(U3.Case1 img, dx, dy, scaledWidth, scaledHeight)

/// Export the cropped circular area as base64 PNG
let private exportCroppedImage (canvas: HTMLCanvasElement) (img: HTMLImageElement) (model: Model) : string =
    // Create output canvas at desired resolution
    let outputCanvas = document.createElement("canvas") :?> HTMLCanvasElement
    outputCanvas.width <- model.OutputSize
    outputCanvas.height <- model.OutputSize
    let ctx = outputCanvas.getContext_2d()

    let outSize = float model.OutputSize
    let previewSize = float canvasSize

    // Scale factor from preview to output
    let scaleFactor = outSize / previewSize

    // Clear and set up circular clip
    ctx.clearRect(0.0, 0.0, outSize, outSize)
    ctx.beginPath()
    ctx.arc(outSize / 2.0, outSize / 2.0, outSize / 2.0, 0.0, 2.0 * System.Math.PI)
    ctx.closePath()
    ctx.clip()

    // Draw the image with same transform as preview, but scaled
    if model.ImageWidth > 0 && model.ImageHeight > 0 then
        let minDim = min model.ImageWidth model.ImageHeight |> float
        let baseScale = previewSize / minDim
        let scale = baseScale * model.Zoom * scaleFactor

        let scaledWidth = float model.ImageWidth * scale
        let scaledHeight = float model.ImageHeight * scale

        let dx = (outSize - scaledWidth) / 2.0 + model.Pan.X * scaleFactor
        let dy = (outSize - scaledHeight) / 2.0 + model.Pan.Y * scaleFactor

        ctx.drawImage(U3.Case1 img, dx, dy, scaledWidth, scaledHeight)

    outputCanvas.toDataURL("image/png")

[<ReactComponent>]
let View (model: Model) (dispatch: Msg -> unit) (onConfirm: string -> unit) =
    // Refs for canvas and image elements
    let canvasRef = React.useRef<HTMLCanvasElement option>(None)
    let imageRef = React.useRef<HTMLImageElement option>(None)

    // Create and load image when OriginalImage changes
    React.useEffect((fun () ->
        match model.OriginalImage with
        | Some src ->
            let img = document.createElement("img") :?> HTMLImageElement
            img.onload <- fun _ ->
                imageRef.current <- Some img
                dispatch (ImageLoaded (int img.naturalWidth, int img.naturalHeight))
            img.src <- src
        | None ->
            imageRef.current <- None
    ), [| box model.OriginalImage |])

    // Redraw canvas when model changes
    React.useEffect((fun () ->
        match canvasRef.current, imageRef.current with
        | Some canvas, Some img when model.ImageWidth > 0 ->
            drawCanvas canvas img model
        | _ -> ()
    ), [| box model.Zoom; box model.Pan.X; box model.Pan.Y; box model.ImageWidth |])

    // Mouse event handlers
    let handleMouseDown (e: MouseEvent) =
        e.preventDefault()
        dispatch (StartDrag (e.clientX, e.clientY))

    let handleMouseMove (e: MouseEvent) =
        if model.IsDragging then
            dispatch (Drag (e.clientX, e.clientY))

    let handleMouseUp (_: MouseEvent) =
        dispatch EndDrag

    let handleWheel (e: WheelEvent) =
        e.preventDefault()
        let delta = if e.deltaY < 0.0 then 0.1 else -0.1
        dispatch (ZoomChanged (model.Zoom + delta))

    // Handle confirm - export and send result
    let handleConfirm () =
        match canvasRef.current, imageRef.current with
        | Some canvas, Some img when model.ImageWidth > 0 ->
            let cropped = exportCroppedImage canvas img model
            onConfirm cropped
        | _ -> ()

    wrapper {
        OnClose = fun () -> dispatch Cancel
        CanClose = true
        MaxWidth = Some "max-w-sm"
        Children = [
            header "Edit Photo" (Some "Position and zoom your photo") true (fun () -> dispatch Cancel)

            body [
                Html.div [
                    prop.className "flex flex-col items-center gap-4"
                    prop.children [
                        // File input (only show if no image selected)
                        if model.OriginalImage.IsNone then
                            Html.label [
                                prop.className "w-[300px] h-[300px] border-2 border-dashed border-base-300 rounded-full flex flex-col items-center justify-center cursor-pointer hover:border-primary transition-colors"
                                prop.children [
                                    Html.div [
                                        prop.className "text-4xl text-base-content/30 mb-2"
                                        prop.text "+"
                                    ]
                                    Html.span [
                                        prop.className "text-base-content/50"
                                        prop.text "Click to select photo"
                                    ]
                                    Html.input [
                                        prop.type'.file
                                        prop.accept "image/png,image/jpeg,image/gif,image/webp"
                                        prop.className "hidden"
                                        prop.onChange (handleFileSelect dispatch)
                                    ]
                                ]
                            ]
                        else
                            // Canvas preview area
                            Html.div [
                                prop.className "relative"
                                prop.style [
                                    style.width canvasSize
                                    style.height canvasSize
                                ]
                                prop.children [
                                    // The canvas for drawing
                                    Html.canvas [
                                        prop.ref (fun el ->
                                            if not (isNull el) then
                                                canvasRef.current <- Some (el :?> HTMLCanvasElement)
                                        )
                                        prop.width canvasSize
                                        prop.height canvasSize
                                        prop.className "rounded-full"
                                        prop.style [
                                            style.cursor (if model.IsDragging then "grabbing" else "grab")
                                        ]
                                        prop.onMouseDown handleMouseDown
                                        prop.onMouseMove handleMouseMove
                                        prop.onMouseUp handleMouseUp
                                        prop.onMouseLeave (fun _ -> dispatch EndDrag)
                                        prop.onWheel handleWheel
                                    ]

                                    // Circular overlay border
                                    Html.div [
                                        prop.className "absolute inset-0 rounded-full border-4 border-primary/50 pointer-events-none"
                                    ]

                                    // Corner indicators showing it's cropped
                                    Html.div [
                                        prop.className "absolute inset-0 pointer-events-none"
                                        prop.style [
                                            style.custom ("boxShadow", "0 0 0 9999px rgba(0,0,0,0.5)")
                                            style.borderRadius (length.percent 50)
                                        ]
                                    ]
                                ]
                            ]

                            // Zoom slider
                            Html.div [
                                prop.className "w-full flex items-center gap-3 px-4"
                                prop.children [
                                    Html.span [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text "-"
                                    ]
                                    Html.input [
                                        prop.type'.range
                                        prop.className "range range-primary range-sm flex-1"
                                        prop.min 1.0
                                        prop.max 3.0
                                        prop.step 0.05
                                        prop.value model.Zoom
                                        prop.onChange (fun (v: float) -> dispatch (ZoomChanged v))
                                    ]
                                    Html.span [
                                        prop.className "text-sm text-base-content/60"
                                        prop.text "+"
                                    ]
                                ]
                            ]

                            // Hint text
                            Html.p [
                                prop.className "text-xs text-base-content/50 text-center"
                                prop.text "Drag to reposition, scroll or use slider to zoom"
                            ]

                            // Change photo button
                            Html.label [
                                prop.className "btn btn-sm btn-ghost"
                                prop.children [
                                    Html.span [ prop.text "Choose different photo" ]
                                    Html.input [
                                        prop.type'.file
                                        prop.accept "image/png,image/jpeg,image/gif,image/webp"
                                        prop.className "hidden"
                                        prop.onChange (handleFileSelect dispatch)
                                    ]
                                ]
                            ]
                    ]
                ]

                // Action buttons
                Html.div [
                    prop.className "flex gap-2 pt-4"
                    prop.children [
                        Html.button [
                            prop.className "btn btn-ghost flex-1"
                            prop.onClick (fun _ -> dispatch Cancel)
                            prop.text "Cancel"
                        ]
                        Html.button [
                            prop.className "btn btn-primary flex-1"
                            prop.disabled model.OriginalImage.IsNone
                            prop.onClick (fun _ -> handleConfirm ())
                            prop.text "Apply"
                        ]
                    ]
                ]
            ]
        ]
    }
