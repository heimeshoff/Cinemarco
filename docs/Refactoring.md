# Cinemarco F# Codebase Refactoring Plan

## Overview
Comprehensive refactoring to reduce duplication, improve idiomatic F# usage, and slim down the codebase. This is an aggressive restructure touching frontend components, backend patterns, and file organization.

---

## Phase 1: Frontend Component Extraction (Highest Impact)

### 1.1 Extract & Unify PosterCard Component
**Files to create:** `src/Client/Common/Components/PosterCard/Types.fs`, `View.fs`

**Current state:**
- `src/Client/Components/Cards/View.fs` has `libraryEntryCard` and `posterCard` (canonical versions with proper hover effects)
- `src/Client/Pages/YearInReview/View.fs` has **inconsistent** inline implementations (3 places) that:
  - Use `group-hover:scale-105` on image instead of container scale
  - Missing `poster-card` and `poster-image-container` classes
  - Missing the proper container expand effect

**Preferred version (from Library/libraryEntryCard):**
- `poster-card group` container class
- `poster-image-container poster-shadow` with CSS transform scale(1.04) on hover
- `poster-shine` overlay that fades in on hover
- Rating badge appears on hover (optional)

**Implementation:**
```fsharp
module PosterCard

type Config = {
    PosterUrl: string
    Title: string
    OnClick: unit -> unit
    RatingBadge: (ReactElement * string * string) option  // icon, colorClass, label
    BottomOverlay: ReactElement option  // For "Next: S1 E2" or "Finished" banners
    IsGrayscale: bool  // For "In Library" items in search
}

let view (config: Config) =
    Html.div [
        prop.className "poster-card group relative cursor-pointer"
        prop.onClick (fun _ -> config.OnClick())
        prop.children [
            Html.div [
                prop.className "poster-image-container poster-shadow"
                prop.children [
                    Html.img [
                        prop.src config.PosterUrl
                        prop.alt config.Title
                        prop.className (if config.IsGrayscale then "poster-image grayscale opacity-60" else "poster-image")
                        prop.custom ("loading", "lazy")
                    ]
                    // Optional rating badge (top-left, appears on hover)
                    // Optional bottom overlay (episode banner, "Finished", etc.)
                    Html.div [ prop.className "poster-shine" ]
                ]
            ]
        ]
    ]
```

**Files to refactor:**
- `src/Client/Pages/YearInReview/View.fs` (lines 294-356, 362-429, 435-543) - Replace inline implementations
- `src/Client/Pages/Home/View.fs` (lines 139-224) - `seriesScrollListWithEpisode` can use PosterCard with `BottomOverlay`
- `src/Client/Components/Cards/View.fs` - Refactor `libraryEntryCard` and `posterCard` to use shared PosterCard

**NOT changed:** `src/Client/Pages/Graph/View.fs` - Uses JS/D3 rendering, stays as-is

---

### 1.2 Extract RatingButton Component
**Files to create:** `src/Client/Common/Components/RatingButton/Types.fs`, `View.fs`

**Deduplicated from:**
- `src/Client/Pages/MovieDetail/View.fs` (lines 16-88)
- `src/Client/Pages/SeriesDetail/View.fs` (lines 405-474)

**Implementation:**
- Extract `ratingOptions` list with (value, label, description, icon, colorClass)
- Create `RatingButton.view` that takes current rating and dispatch
- Use in both MovieDetail and SeriesDetail

### 1.3 Extract CastCrewSection Component
**Files to create:** `src/Client/Common/Components/CastCrewSection/Types.fs`, `View.fs`

**Deduplicated from:**
- `src/Client/Pages/MovieDetail/View.fs` (lines 217-421)
- `src/Client/Pages/SeriesDetail/View.fs` (lines 646-850)

**Implementation:**
- Extract `renderCastMember`, `renderCrewMember` functions
- Create `CastCrewSection.view` taking credits, tracked IDs, isExpanded, dispatch
- Abstract the Msg type via a generic dispatch function

### 1.4 Extract OverviewNotesSection Component
**Files to create:** `src/Client/Common/Components/OverviewNotes/Types.fs`, `View.fs`

**Deduplicated from:**
- `src/Client/Pages/MovieDetail/View.fs` (lines 189-214)
- `src/Client/Pages/SeriesDetail/View.fs` (lines 617-643)

### 1.5 Extract BackButton Component
**File to create:** `src/Client/Common/Components/BackButton/View.fs`

**Deduplicated from:**
- `src/Client/Pages/MovieDetail/View.fs` (lines 658-665)
- `src/Client/Pages/SeriesDetail/View.fs` (lines 1029-1036)
- `src/Client/Pages/FriendDetail/View.fs` (lines 120-127)
- `src/Client/Pages/ContributorDetail/View.fs` (lines 476-483)

### 1.6 Extract ProgressBar Component
**File to create:** `src/Client/Common/Components/ProgressBar/View.fs`

**Deduplicated from:**
- `src/Client/Pages/SeriesDetail/View.fs` (lines 16-38)
- `src/Client/Pages/ContributorDetail/View.fs` (lines 35-66)

### 1.7 Consistent GlassButton Usage
**Files to modify:**
- `src/Client/Pages/MovieDetail/View.fs` (lines 91-186)
- `src/Client/Pages/SeriesDetail/View.fs` (lines 476-568)

**Change:** Replace manual button construction with `GlassButton` component

### 1.8 Consistent FilterChip Usage
**File to modify:** `src/Client/Pages/ContributorDetail/View.fs` (lines 69-163)

**Change:** Replace manual filter buttons with `FilterChip.chip`

---

## Phase 2: Frontend State/Type Unification

### 2.1 Create AsyncCmd Helper Module
**File to create:** `src/Client/Common/AsyncCmd.fs`

```fsharp
module AsyncCmd =
    /// Standard pattern for loading data into RemoteData
    let load apiCall arg toMsg =
        Cmd.OfAsync.either apiCall arg (Ok >> toMsg) (fun ex -> Error ex.Message |> toMsg)

    /// Fire-and-forget async call
    let perform apiCall arg toMsg =
        Cmd.OfAsync.perform apiCall arg toMsg
```

**Files to refactor:** All `State.fs` files (100+ occurrences of boilerplate)

### 2.2 Extend RemoteData Module
**File to modify:** `src/Client/Common/Types.fs`

**Add helpers:**
```fsharp
let mapError f = function | Failure e -> Failure (f e) | x -> x
let fold onNotAsked onLoading onSuccess onFailure = function
    | NotAsked -> onNotAsked
    | Loading -> onLoading
    | Success x -> onSuccess x
    | Failure e -> onFailure e
let orElse fallback = function | Success x -> Success x | _ -> fallback
let combine rd1 rd2 = // For combining multiple RemoteData values
```

### 2.3 Extract Common ExternalMsg Variants
**File to create:** `src/Client/Common/ExternalMsg.fs`

```fsharp
module CommonExternalMsg =
    type Navigation =
        | Back
        | ToContributor of TmdbPersonId * name: string * isTracked: bool
        | ToFriend of FriendId * name: string
        | ToCollection of CollectionId * name: string
        | ToGraph of GraphNode option
```

---

## Phase 3: Backend Helper Extraction

### 3.1 Create DbHelpers Module
**File to create:** `src/Server/DbHelpers.fs`

**Extract from Persistence.fs:**
- `toOption` (null/empty string to Option)
- `parseDateTime`, `formatDateTime`
- `nullableToOption`, `optionToNullable`
- `parseGenres`, `formatGenres`

### 3.2 Create SlugResolver Module
**File to create:** `src/Server/SlugResolver.fs`

**Extract pattern from Api.fs** (appears in 4+ places):
```fsharp
let resolveBySlug getAllFn getSlugFn getIdFn entityName slug = async {
    let! items = getAllFn()
    let (baseSlug, index) = Slug.parseSlugWithSuffix slug |> fun (b,i) -> b, defaultArg i 0
    let matches = items |> List.filter (fun i -> Slug.matches baseSlug (getSlugFn i)) |> List.sortBy getIdFn
    match List.tryItem index matches with
    | Some i -> return Ok i
    | None -> return Error $"{entityName} not found for slug: {slug}"
}
```

### 3.3 Create ApiHelpers Module
**File to create:** `src/Server/ApiHelpers.fs`

**Extract patterns:**
```fsharp
/// Fetch details, cache images, then persist
let fetchAndCache fetchFn posterPath backdropPath persistFn = async {
    match! fetchFn() with
    | Error err -> return Error $"Failed to fetch details: {err}"
    | Ok details ->
        do! ImageCache.downloadPoster (posterPath details)
        do! ImageCache.downloadBackdrop (backdropPath details)
        return! persistFn details
}

/// Update then return refreshed entry
let updateAndReturn entryId updateFn = async {
    do! updateFn()
    match! Persistence.getLibraryEntryById entryId with
    | Some e -> return Ok e
    | None -> return Error "Entry not found after update"
}
```

### 3.4 Create Validation.fs Module
**File to create:** `src/Server/Validation.fs`

**Implement:**
```fsharp
module Validation =
    let validateAddMovieRequest (req: AddMovieRequest) : Result<AddMovieRequest, string list> = ...
    let validateAddSeriesRequest (req: AddSeriesRequest) : Result<AddSeriesRequest, string list> = ...
    let validateFriend (friend: Friend) : Result<Friend, string list> = ...
    // etc.
```

---

## Phase 4: File Splitting (Aggressive Restructure)

### 4.1 Split Domain.fs (1232 lines)
**Current:** `src/Shared/Domain.fs`

**Split into:**
- `src/Shared/Domain/Ids.fs` - All ID types and modules (MovieId, SeriesId, etc.)
- `src/Shared/Domain/Core.fs` - Enums, basic types (MediaType, WatchStatus, PersonalRating)
- `src/Shared/Domain/Media.fs` - Movie, Series, Episode types
- `src/Shared/Domain/Library.fs` - LibraryEntry, WatchSession, LibraryFilters
- `src/Shared/Domain/Social.fs` - Friend, Collection types
- `src/Shared/Domain/Stats.fs` - Statistics types
- `src/Shared/Domain/Tmdb.fs` - TMDB API response types
- `src/Shared/Domain/Graph.fs` - GraphNode, GraphData types

### 4.2 Split graphGetData Function (534 lines)
**File:** `src/Server/Api.fs` (lines 1072-1606)

**Extract to:** `src/Server/GraphBuilder.fs`

```fsharp
module GraphBuilder =
    let buildFocusedGraph focusNode = async { ... }
    let buildSearchGraph query = async { ... }
    let buildDefaultGraph () = async { ... }
    let buildGraph (request: GraphRequest) = async { ... }
```

---

## Phase 5: Idiomatic F# Improvements

### 5.1 ID Module Unification
**File:** `src/Shared/Domain/Ids.fs` (new)

```fsharp
// Generic ID pattern
[<AutoOpen>]
module IdHelpers =
    let inline createId< ^Id, ^T when ^Id : (static member Create: ^T -> ^Id)> (value: ^T) : ^Id =
        (^Id : (static member Create: ^T -> ^Id) value)

// Or use a simpler approach with inline functions
module MovieId =
    let create = MovieId
    let value (MovieId id) = id
```

### 5.2 Result Computation Expression
**File to create:** `src/Server/ResultCE.fs` (or use FsToolkit.ErrorHandling)

```fsharp
type ResultBuilder() =
    member _.Bind(x, f) = Result.bind f x
    member _.Return(x) = Ok x
    member _.ReturnFrom(x) = x

let result = ResultBuilder()

// Usage:
result {
    let! movie = getMovie id
    let! details = getDetails movie.TmdbId
    return { movie with Details = details }
}
```

### 5.3 Pipeline-Style Async
**Files to modify:** `src/Server/Stats.fs`, `src/Server/Api.fs`

```fsharp
// Before (Stats.fs):
let movieMinutesTotal =
    watchedEntries
    |> List.choose (fun e ->
        match e.Media with
        | LibraryMovie m when e.WatchStatus = Completed -> Some (movieMinutes m)
        | _ -> None)
    |> List.sum

// After:
let movieMinutesTotal =
    watchedEntries
    |> List.sumBy (function
        | { Media = LibraryMovie m; WatchStatus = Completed } -> movieMinutes m
        | _ -> 0)
```

### 5.4 Remove Async.RunSynchronously
**File:** `src/Server/Api.fs` (lines 888-892, 929-935)

**Change:** Pre-fetch data or use `Async.Parallel` instead of blocking calls

### 5.5 Active Patterns for JSON Parsing
**File:** `src/Server/TmdbClient.fs`

```fsharp
let (|NullOrEmpty|NonEmpty|) (token: JToken) =
    match token with
    | null | t when t.Type = JTokenType.Null -> NullOrEmpty
    | t ->
        let s = string t
        if String.IsNullOrEmpty s then NullOrEmpty else NonEmpty s
```

---

## Execution Order

1. **Phase 1.1** - PosterCard component (highest visual impact, fixes YearInReview inconsistency)
2. **Phase 1.2-1.6** - Extract remaining shared components (self-contained, low risk)
3. **Phase 2.1** - AsyncCmd helper (immediately reduces boilerplate)
4. **Phase 3.4** - Create Validation.fs (fills documented gap)
5. **Phase 3.1-3.3** - Backend helpers (prep for larger refactor)
6. **Phase 4.2** - Split graphGetData (high impact, moderate risk)
7. **Phase 4.1** - Split Domain.fs (requires careful import management)
8. **Phase 1.7-1.8** - Consistent component usage (cleanup)
9. **Phase 2.2-2.3** - Extended helpers and common types
10. **Phase 5** - Idiomatic improvements (polish)

---

## Files Modified/Created Summary

### New Files (18):
- `src/Client/Common/AsyncCmd.fs`
- `src/Client/Common/ExternalMsg.fs`
- `src/Client/Common/Components/PosterCard/{Types,View}.fs`
- `src/Client/Common/Components/RatingButton/{Types,View}.fs`
- `src/Client/Common/Components/CastCrewSection/{Types,View}.fs`
- `src/Client/Common/Components/OverviewNotes/{Types,View}.fs`
- `src/Client/Common/Components/BackButton/View.fs`
- `src/Client/Common/Components/ProgressBar/View.fs`
- `src/Server/DbHelpers.fs`
- `src/Server/SlugResolver.fs`
- `src/Server/ApiHelpers.fs`
- `src/Server/Validation.fs`
- `src/Server/GraphBuilder.fs`
- `src/Shared/Domain/*.fs` (7 files replacing Domain.fs)

### Modified Files:
- `src/Client/Pages/MovieDetail/View.fs` - Use new components
- `src/Client/Pages/SeriesDetail/View.fs` - Use new components
- `src/Client/Pages/ContributorDetail/View.fs` - Use FilterChip, ProgressBar
- `src/Client/Pages/FriendDetail/View.fs` - Use BackButton
- `src/Client/Pages/YearInReview/View.fs` - Use PosterCard (fix inconsistent poster styling)
- `src/Client/Pages/Home/View.fs` - Use PosterCard for seriesScrollListWithEpisode
- `src/Client/Components/Cards/View.fs` - Refactor to use shared PosterCard
- `src/Client/Pages/*/State.fs` - Use AsyncCmd helper
- `src/Client/Common/Types.fs` - Extend RemoteData
- `src/Server/Api.fs` - Use helpers, remove graphGetData
- `src/Server/Persistence.fs` - Use DbHelpers
- `src/Server/Stats.fs` - Idiomatic improvements
- `src/Server/TmdbClient.fs` - Active patterns
- Various `.fsproj` files - Add new file references

---

## Estimated Impact

- **Lines removed:** ~2000+ (duplication elimination)
- **Lines added:** ~800 (new modules/components)
- **Net reduction:** ~1200 lines
- **Files consolidated:** Domain.fs (1 → 7 smaller files)
- **Boilerplate reduction:** ~100 occurrences of async pattern simplified
