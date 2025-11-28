module Components.Notification.Types

type Model = {
    Message: string
    IsSuccess: bool
    IsVisible: bool
}

type Msg =
    | Show of message: string * isSuccess: bool
    | Hide

type ExternalMsg =
    | NoOp
    | Dismissed

module Model =
    let empty = {
        Message = ""
        IsSuccess = true
        IsVisible = false
    }
