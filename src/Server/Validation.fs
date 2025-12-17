module Server.Validation

open System
open Shared.Domain

/// Validation result type
type ValidationResult<'T> = Result<'T, string list>

/// Module for string validation
module String =
    /// Check if string is not null or whitespace
    let notEmpty fieldName (value: string) : ValidationResult<string> =
        if String.IsNullOrWhiteSpace value then
            Error [ $"{fieldName} cannot be empty" ]
        else
            Ok value

    /// Check if string length is within bounds
    let lengthBetween minLen maxLen fieldName (value: string) : ValidationResult<string> =
        if String.IsNullOrWhiteSpace value then
            Error [ $"{fieldName} cannot be empty" ]
        elif value.Length < minLen then
            Error [ $"{fieldName} must be at least {minLen} characters" ]
        elif value.Length > maxLen then
            Error [ $"{fieldName} cannot exceed {maxLen} characters" ]
        else
            Ok value

    /// Check if string matches a pattern
    let matches pattern fieldName (value: string) : ValidationResult<string> =
        if String.IsNullOrWhiteSpace value then
            Error [ $"{fieldName} cannot be empty" ]
        elif System.Text.RegularExpressions.Regex.IsMatch(value, pattern) then
            Ok value
        else
            Error [ $"{fieldName} has an invalid format" ]

/// Module for numeric validation
module Number =
    /// Check if value is positive
    let positive fieldName (value: int) : ValidationResult<int> =
        if value > 0 then Ok value
        else Error [ $"{fieldName} must be positive" ]

    /// Check if value is non-negative
    let nonNegative fieldName (value: int) : ValidationResult<int> =
        if value >= 0 then Ok value
        else Error [ $"{fieldName} cannot be negative" ]

    /// Check if value is within range
    let between minVal maxVal fieldName (value: int) : ValidationResult<int> =
        if value < minVal then
            Error [ $"{fieldName} must be at least {minVal}" ]
        elif value > maxVal then
            Error [ $"{fieldName} cannot exceed {maxVal}" ]
        else
            Ok value

/// Module for option validation
module Option =
    /// Check if option has a value
    let required fieldName (value: 'T option) : ValidationResult<'T> =
        match value with
        | Some v -> Ok v
        | None -> Error [ $"{fieldName} is required" ]

    /// Map over a validated optional value
    let map f (value: 'T option) : 'U option =
        value |> Option.map f

/// Module for list validation
module List =
    /// Check if list is not empty
    let notEmpty fieldName (value: 'T list) : ValidationResult<'T list> =
        if List.isEmpty value then
            Error [ $"{fieldName} cannot be empty" ]
        else
            Ok value

    /// Check if list has at least n items
    let minLength n fieldName (value: 'T list) : ValidationResult<'T list> =
        if List.length value < n then
            Error [ $"{fieldName} must have at least {n} items" ]
        else
            Ok value

/// Module for combining validations
module Combine =
    /// Combine two validation results
    let map2 f r1 r2 =
        match r1, r2 with
        | Ok v1, Ok v2 -> Ok (f v1 v2)
        | Error e1, Error e2 -> Error (e1 @ e2)
        | Error e, _ | _, Error e -> Error e

    /// Combine three validation results
    let map3 f r1 r2 r3 =
        match r1, r2, r3 with
        | Ok v1, Ok v2, Ok v3 -> Ok (f v1 v2 v3)
        | Error e1, Error e2, Error e3 -> Error (e1 @ e2 @ e3)
        | Error e1, Error e2, _ | Error e1, _, Error e2 | _, Error e1, Error e2 -> Error (e1 @ e2)
        | Error e, _, _ | _, Error e, _ | _, _, Error e -> Error e

    /// Apply a validation result to another
    let apply fRes xRes =
        match fRes, xRes with
        | Ok f, Ok x -> Ok (f x)
        | Error e1, Error e2 -> Error (e1 @ e2)
        | Error e, _ | _, Error e -> Error e

    /// Collect all errors from a list of results
    let sequence (results: ValidationResult<'T> list) : ValidationResult<'T list> =
        let folder state item =
            match state, item with
            | Ok items, Ok item -> Ok (item :: items)
            | Error errs, Error err -> Error (errs @ err)
            | Error errs, _ -> Error errs
            | _, Error err -> Error err

        results
        |> List.fold folder (Ok [])
        |> Result.map List.rev

/// Domain-specific validators
module Domain =
    /// Validate friend name
    let validateFriendName name =
        String.lengthBetween 1 100 "Friend name" name

    /// Validate collection name
    let validateCollectionName name =
        String.lengthBetween 1 200 "Collection name" name

    /// Validate notes
    let validateNotes notes =
        match notes with
        | None -> Ok None
        | Some n when String.IsNullOrWhiteSpace n -> Ok None
        | Some n when n.Length > 10000 -> Error [ "Notes cannot exceed 10000 characters" ]
        | Some n -> Ok (Some n)

    /// Validate personal rating
    let validateRating (rating: int option) =
        match rating with
        | None -> Ok None
        | Some r when r >= 0 && r <= 5 -> Ok (Some r)
        | Some _ -> Error [ "Rating must be between 0 and 5" ]

    /// Validate season number
    let validateSeasonNumber season =
        Number.between 0 100 "Season number" season

    /// Validate episode number
    let validateEpisodeNumber episode =
        Number.between 1 1000 "Episode number" episode

/// Helper to convert validation result to standard Result
let toResult (validation: ValidationResult<'T>) : Result<'T, string> =
    match validation with
    | Ok v -> Ok v
    | Error errors -> Error (String.concat "; " errors)

/// Helper to validate and map in one step
let validateAndMap (validate: 'T -> ValidationResult<'T>) (mapper: 'T -> 'U) (value: 'T) : Result<'U, string> =
    validate value
    |> Result.map mapper
    |> toResult
