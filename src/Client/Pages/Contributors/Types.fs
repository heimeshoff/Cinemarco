module Pages.Contributors.Types

open Common.Types
open Shared.Domain

/// Filter by department
type DepartmentFilter =
    | AllDepartments
    | ActingOnly
    | DirectingOnly
    | OtherDepartments

type Model = {
    Contributors: RemoteData<TrackedContributor list>
    DepartmentFilter: DepartmentFilter
    SearchQuery: string
}

type Msg =
    | LoadContributors
    | ContributorsLoaded of Result<TrackedContributor list, string>
    | SetDepartmentFilter of DepartmentFilter
    | SetSearchQuery of string
    | ViewContributorDetail of personId: TmdbPersonId * name: string
    | UntrackContributor of TrackedContributorId
    | UntrackResult of Result<unit, string>

type ExternalMsg =
    | NoOp
    | NavigateToContributorDetail of personId: TmdbPersonId * name: string
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let empty = {
        Contributors = NotAsked
        DepartmentFilter = AllDepartments
        SearchQuery = ""
    }
