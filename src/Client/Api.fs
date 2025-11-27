module Api

open Fable.Remoting.Client
open Shared.Api

/// Client proxy for the Cinemarco API
let api =
    Remoting.createApi()
    |> Remoting.withRouteBuilder (fun typeName methodName -> $"/api/{typeName}/{methodName}")
    |> Remoting.buildProxy<ICinemarcoApi>
