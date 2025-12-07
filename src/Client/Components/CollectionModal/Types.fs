module Components.CollectionModal.Types

open Shared.Domain

type Model = {
    EditingCollection: Collection option  // None = creating new, Some = editing existing
    Name: string
    Description: string
    LogoBase64: string option  // Base64 encoded image data
    LogoPreview: string option  // Data URL for preview (existing or new)
    LogoRemoved: bool  // True if user explicitly removed the logo
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NameChanged of string
    | DescriptionChanged of string
    | LogoSelected of string  // Base64 data URL
    | LogoRemoved
    | Submit
    | SubmitResult of Result<Collection, string>
    | Close

type ExternalMsg =
    | NoOp
    | Saved of Collection
    | CloseRequested

module Model =
    let empty = {
        EditingCollection = None
        Name = ""
        Description = ""
        LogoBase64 = None
        LogoPreview = None
        LogoRemoved = false
        IsSubmitting = false
        Error = None
    }

    let fromCollection (collection: Collection) = {
        EditingCollection = Some collection
        Name = collection.Name
        Description = collection.Description |> Option.defaultValue ""
        LogoBase64 = None  // Don't send existing logo back
        LogoPreview = collection.CoverImagePath |> Option.map (fun p -> $"/images/collections{p}")
        LogoRemoved = false
        IsSubmitting = false
        Error = None
    }
