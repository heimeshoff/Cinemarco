module Shared.Api

/// Health check response
type HealthCheckResponse = {
    Status: string
    Version: string
    Timestamp: System.DateTime
}

/// API contract for Cinemarco operations
/// Sub-APIs will be added incrementally in later milestones
type ICinemarcoApi = {
    /// Health check endpoint
    healthCheck: unit -> Async<HealthCheckResponse>
}
