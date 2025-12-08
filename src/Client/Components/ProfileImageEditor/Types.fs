module Components.ProfileImageEditor.Types

/// Position offset in pixels (relative to center)
type Position = { X: float; Y: float }

/// Editor state for cropping/zooming profile images
type Model = {
    /// The original image as base64 data URL
    OriginalImage: string option
    /// Natural dimensions of the loaded image
    ImageWidth: int
    ImageHeight: int
    /// Current zoom level (1.0 = fit to canvas, >1 = zoomed in)
    Zoom: float
    /// Pan offset in pixels (0,0 = centered)
    Pan: Position
    /// Whether user is currently dragging
    IsDragging: bool
    /// Last mouse position during drag
    DragStart: Position option
    /// Output canvas size (diameter of circular crop)
    OutputSize: int
    /// Is the editor loading/processing
    IsProcessing: bool
}

type Msg =
    /// User selected a file from disk
    | FileSelected of base64DataUrl: string
    /// Image finished loading, we now know its dimensions
    | ImageLoaded of width: int * height: int
    /// Zoom level changed (slider or wheel)
    | ZoomChanged of float
    /// Mouse/touch events for panning
    | StartDrag of x: float * y: float
    | Drag of x: float * y: float
    | EndDrag
    /// User confirmed the crop
    | Confirm
    /// User cancelled
    | Cancel

type ExternalMsg =
    | NoOp
    /// Editor confirmed with cropped base64 image
    | Confirmed of base64DataUrl: string
    /// User cancelled editing
    | Cancelled

module Model =
    let empty = {
        OriginalImage = None
        ImageWidth = 0
        ImageHeight = 0
        Zoom = 1.0
        Pan = { X = 0.0; Y = 0.0 }
        IsDragging = false
        DragStart = None
        OutputSize = 256  // Output 256x256 image
        IsProcessing = false
    }

    /// Initialize with an existing image URL (for editing current avatar)
    let withImage (imageUrl: string) = {
        empty with
            OriginalImage = Some imageUrl
    }
