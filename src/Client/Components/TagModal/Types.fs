module Components.TagModal.Types

open Shared.Domain

type Model = {
    EditingTag: Tag option  // None = creating new, Some = editing existing
    Name: string
    Color: string
    Description: string
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NameChanged of string
    | ColorChanged of string
    | DescriptionChanged of string
    | Submit
    | SubmitResult of Result<Tag, string>
    | Close

type ExternalMsg =
    | NoOp
    | Saved of Tag
    | CloseRequested

module Model =
    let empty = {
        EditingTag = None
        Name = ""
        Color = ""
        Description = ""
        IsSubmitting = false
        Error = None
    }

    let fromTag (tag: Tag) = {
        EditingTag = Some tag
        Name = tag.Name
        Color = tag.Color |> Option.defaultValue ""
        Description = tag.Description |> Option.defaultValue ""
        IsSubmitting = false
        Error = None
    }
