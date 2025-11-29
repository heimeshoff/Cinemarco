module Components.CollectionModal.Types

open Shared.Domain

type Model = {
    EditingCollection: Collection option  // None = creating new, Some = editing existing
    Name: string
    Description: string
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NameChanged of string
    | DescriptionChanged of string
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
        IsSubmitting = false
        Error = None
    }

    let fromCollection (collection: Collection) = {
        EditingCollection = Some collection
        Name = collection.Name
        Description = collection.Description |> Option.defaultValue ""
        IsSubmitting = false
        Error = None
    }
