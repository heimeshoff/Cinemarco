module Components.FriendModal.Types

open Shared.Domain

type Model = {
    EditingFriend: Friend option  // None = creating new, Some = editing existing
    Name: string
    Nickname: string
    IsSubmitting: bool
    Error: string option
}

type Msg =
    | NameChanged of string
    | NicknameChanged of string
    | Submit
    | SubmitResult of Result<Friend, string>
    | Close

type ExternalMsg =
    | NoOp
    | Saved of Friend
    | CloseRequested

module Model =
    let empty = {
        EditingFriend = None
        Name = ""
        Nickname = ""
        IsSubmitting = false
        Error = None
    }

    let fromFriend (friend: Friend) = {
        EditingFriend = Some friend
        Name = friend.Name
        Nickname = friend.Nickname |> Option.defaultValue ""
        IsSubmitting = false
        Error = None
    }
