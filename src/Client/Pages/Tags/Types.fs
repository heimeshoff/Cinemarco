module Pages.Tags.Types

open Common.Types
open Shared.Domain

type Model = {
    Tags: RemoteData<Tag list>
}

type Msg =
    | LoadTags
    | TagsLoaded of Result<Tag list, string>
    | ViewTagDetail of TagId
    | OpenAddTagModal
    | OpenEditTagModal of Tag
    | OpenDeleteTagModal of Tag

type ExternalMsg =
    | NoOp
    | NavigateToTagDetail of TagId
    | RequestOpenAddModal
    | RequestOpenEditModal of Tag
    | RequestOpenDeleteModal of Tag

module Model =
    let empty = { Tags = NotAsked }
