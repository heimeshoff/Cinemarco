module Components.Layout.Types

open Common.Routing
open Common.Types
open Shared.Api

type Model = {
    IsMobileMenuOpen: bool
    IsSidebarExpanded: bool
    HealthCheck: RemoteData<HealthCheckResponse>
}

type Msg =
    | ToggleMobileMenu
    | CloseMobileMenu
    | ToggleSidebar
    | CheckHealth
    | HealthCheckResult of Result<HealthCheckResponse, string>

type ExternalMsg =
    | NoOp
    | NavigateTo of Page
    | OpenSearchModal

module Model =
    let empty = {
        IsMobileMenuOpen = false
        IsSidebarExpanded = false
        HealthCheck = NotAsked
    }
