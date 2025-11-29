module Components.ConfirmModal.Types

open Shared.Domain

/// What we're confirming deletion of
type DeleteTarget =
    | Friend of Friend
    | Tag of Tag
    | Entry of EntryId
    | Collection of Collection * itemCount: int

type Model = {
    Target: DeleteTarget
    IsSubmitting: bool
}

type Msg =
    | Confirm
    | Cancel

type ExternalMsg =
    | NoOp
    | Confirmed of DeleteTarget
    | Cancelled

module Model =
    let create target = {
        Target = target
        IsSubmitting = false
    }
