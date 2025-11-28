# Hierarchical MVU Refactoring Plan

## Overview

Refactor the monolithic MVU structure into a hierarchical component-based architecture with:
- **Common/** folder for shared utilities
- **Components/** folder for reusable UI components
- **Pages/** folder for page-level MVU components
- **App/** folder for the main application MVU component
- Each child component has its own Types.fs, State.fs, View.fs
- Mapped messages pattern: parent wraps child messages (e.g., `LibraryMsg of Library.Msg`)

## Target Folder Structure

```
src/Client/
├── Common/
│   ├── Types.fs          # RemoteData, shared utilities
│   └── Routing.fs        # Page type, URL helpers
├── Components/
│   ├── Icons.fs          # SVG icon definitions
│   ├── Layout/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs       # Sidebar, MobileNav, MobileMenuDrawer
│   ├── SearchModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── QuickAddModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── FriendModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── TagModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── AbandonModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── ConfirmModal/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs       # Generic confirm delete modal
│   ├── Notification/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   └── Cards/
│       └── View.fs       # PosterCard, LibraryEntryCard (stateless)
├── Pages/
│   ├── Home/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── Library/
│   │   ├── Types.fs      # LibraryFilters, LibrarySortBy, etc.
│   │   ├── State.fs
│   │   └── View.fs
│   ├── MovieDetail/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── SeriesDetail/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── Friends/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── FriendDetail/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── Tags/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   ├── TagDetail/
│   │   ├── Types.fs
│   │   ├── State.fs
│   │   └── View.fs
│   └── NotFound/
│       └── View.fs       # Stateless placeholder page
├── App/
│   ├── Types.fs          # AppModel, AppMsg with mapped child messages
│   ├── State.fs          # init, update with message routing
│   └── View.fs           # Main layout, router
├── Api.fs                # Keep as-is
└── Main.fs               # Entry point (renamed from App.fs)
```

## Implementation Phases

### Phase 1: Create Common Infrastructure (Foundation)

**1.1 Create `Common/Types.fs`**
```fsharp
module Common.Types

/// Remote data state for async operations
type RemoteData<'T> =
    | NotAsked
    | Loading
    | Success of 'T
    | Failure of string

module RemoteData =
    let isLoading = function Loading -> true | _ -> false
    let isSuccess = function Success _ -> true | _ -> false
    let toOption = function Success x -> Some x | _ -> None
    let defaultValue def = function Success x -> x | _ -> def
```

**1.2 Create `Common/Routing.fs`**
```fsharp
module Common.Routing

open Shared.Domain

type Page =
    | HomePage
    | LibraryPage
    | MovieDetailPage of EntryId
    | SeriesDetailPage of EntryId
    | FriendsPage
    | FriendDetailPage of FriendId
    | TagsPage
    | TagDetailPage of TagId
    | CollectionsPage
    | StatsPage
    | TimelinePage
    | GraphPage
    | ImportPage
    | NotFoundPage

module Page =
    let toUrl = function ...
    let toString = function ...
```

### Phase 2: Create Stateless Components

**2.1 Move Icons.fs to `Components/Icons.fs`**
- Keep as-is, just move location

**2.2 Create `Components/Cards/View.fs`**
- Move `posterCard` and `libraryEntryCard` functions
- These are stateless view helpers, no Types.fs/State.fs needed

### Phase 3: Create Modal Components

Each modal follows this pattern:

**3.1 Example: `Components/SearchModal/`**

`Types.fs`:
```fsharp
module Components.SearchModal.Types

open Common.Types
open Shared.Domain

type Model = {
    Query: string
    Results: RemoteData<TmdbSearchResult list>
    IsDropdownOpen: bool
}

type Msg =
    | QueryChanged of string
    | SearchDebounced
    | SearchResults of Result<TmdbSearchResult list, string>
    | CloseDropdown
    | SelectItem of TmdbSearchResult

type ExternalMsg =
    | NoOp
    | ItemSelected of TmdbSearchResult
    | CloseRequested
```

`State.fs`:
```fsharp
module Components.SearchModal.State

open Elmish
open Types

let init () = { Query = ""; Results = NotAsked; IsDropdownOpen = false }

let update msg model =
    match msg with
    | QueryChanged query -> ...
    // Returns (Model * Cmd<Msg> * ExternalMsg)
```

`View.fs`:
```fsharp
module Components.SearchModal.View

open Feliz
open Types

let view (model: Model) (dispatch: Msg -> unit) = ...
```

**3.2 Create similar structure for:**
- QuickAddModal (has Tags/Friends selection)
- FriendModal (add/edit friend)
- TagModal (add/edit tag)
- AbandonModal (abandon entry with reason)
- ConfirmModal (generic delete confirmation)
- Notification (toast component)

### Phase 4: Create Layout Component

**4.1 `Components/Layout/`**
- Manages sidebar, mobile nav, mobile menu drawer
- Has its own state for `IsMobileMenuOpen`

`Types.fs`:
```fsharp
type Model = { IsMobileMenuOpen: bool }
type Msg = ToggleMobileMenu | CloseMobileMenu
```

### Phase 5: Create Page Components

Each page follows this pattern:

**5.1 Example: `Pages/Library/`**

`Types.fs`:
```fsharp
module Pages.Library.Types

open Common.Types
open Shared.Domain

type WatchStatusFilter = AllStatuses | FilterNotStarted | ...
type LibrarySortBy = SortByDateAdded | SortByTitle | ...
type SortDirection = Ascending | Descending

type LibraryFilters = { SearchQuery: string; WatchStatus: WatchStatusFilter; ... }

type Model = {
    Entries: RemoteData<LibraryEntry list>
    Filters: LibraryFilters
}

type Msg =
    | LoadEntries
    | EntriesLoaded of Result<LibraryEntry list, string>
    | SetSearchQuery of string
    | SetWatchStatusFilter of WatchStatusFilter
    | ToggleTagFilter of TagId
    | SetMinRatingFilter of int option
    | SetSortBy of LibrarySortBy
    | ToggleSortDirection
    | ClearFilters
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
```

`State.fs`:
```fsharp
module Pages.Library.State

let init () = { Entries = NotAsked; Filters = LibraryFilters.empty }

let update msg model =
    match msg with
    | LoadEntries -> ...
    // Returns (Model * Cmd<Msg>)
```

**5.2 Create similar structure for:**
- Home (dashboard with recent/stats)
- MovieDetail (single movie view with watch controls)
- SeriesDetail (series view with episode progress)
- Friends (friends list with add/edit/delete)
- FriendDetail (friend detail with watched-together entries)
- Tags (tags list with add/edit/delete)
- TagDetail (tag detail with tagged entries)
- NotFound (simple stateless view)

### Phase 6: Create App Component

**6.1 `App/Types.fs`**
```fsharp
module App.Types

open Common.Types
open Common.Routing
open Shared.Domain

type Model = {
    CurrentPage: Page
    HealthCheck: RemoteData<HealthCheckResponse>

    // Global data (shared across pages)
    Friends: RemoteData<Friend list>
    Tags: RemoteData<Tag list>

    // Layout state
    Layout: Components.Layout.Types.Model

    // Active modal (only one at a time)
    ActiveModal: ActiveModal

    // Notification state
    Notification: Components.Notification.Types.Model option

    // Page models (lazy loaded)
    HomeModel: Pages.Home.Types.Model option
    LibraryModel: Pages.Library.Types.Model option
    MovieDetailModel: Pages.MovieDetail.Types.Model option
    SeriesDetailModel: Pages.SeriesDetail.Types.Model option
    FriendsModel: Pages.Friends.Types.Model option
    FriendDetailModel: Pages.FriendDetail.Types.Model option
    TagsModel: Pages.Tags.Types.Model option
    TagDetailModel: Pages.TagDetail.Types.Model option
}

and ActiveModal =
    | NoModal
    | SearchModal of Components.SearchModal.Types.Model
    | QuickAddModal of Components.QuickAddModal.Types.Model
    | FriendModal of Components.FriendModal.Types.Model
    | TagModal of Components.TagModal.Types.Model
    | AbandonModal of Components.AbandonModal.Types.Model
    | ConfirmDeleteModal of Components.ConfirmModal.Types.Model

type Msg =
    | NavigateTo of Page
    | CheckHealth
    | HealthCheckResult of Result<HealthCheckResponse, string>

    // Global data
    | LoadFriends
    | FriendsLoaded of Result<Friend list, string>
    | LoadTags
    | TagsLoaded of Result<Tag list, string>

    // Layout
    | LayoutMsg of Components.Layout.Types.Msg

    // Modals
    | OpenSearchModal
    | SearchModalMsg of Components.SearchModal.Types.Msg
    | OpenQuickAddModal of TmdbSearchResult
    | QuickAddModalMsg of Components.QuickAddModal.Types.Msg
    | OpenFriendModal of Friend option  // None = new, Some = edit
    | FriendModalMsg of Components.FriendModal.Types.Msg
    | OpenTagModal of Tag option
    | TagModalMsg of Components.TagModal.Types.Msg
    | OpenAbandonModal of EntryId
    | AbandonModalMsg of Components.AbandonModal.Types.Msg
    | OpenConfirmDeleteModal of ConfirmDeleteTarget
    | ConfirmModalMsg of Components.ConfirmModal.Types.Msg
    | CloseModal

    // Notifications
    | ShowNotification of string * bool
    | NotificationMsg of Components.Notification.Types.Msg

    // Page messages (mapped)
    | HomeMsg of Pages.Home.Types.Msg
    | LibraryMsg of Pages.Library.Types.Msg
    | MovieDetailMsg of Pages.MovieDetail.Types.Msg
    | SeriesDetailMsg of Pages.SeriesDetail.Types.Msg
    | FriendsMsg of Pages.Friends.Types.Msg
    | FriendDetailMsg of Pages.FriendDetail.Types.Msg
    | TagsMsg of Pages.Tags.Types.Msg
    | TagDetailMsg of Pages.TagDetail.Types.Msg

and ConfirmDeleteTarget =
    | DeleteFriend of Friend
    | DeleteTag of Tag
    | DeleteEntry of EntryId
```

**6.2 `App/State.fs`**
- `init`: Initialize app model, trigger health check + load global data
- `update`: Route messages to child components, handle external messages

**6.3 `App/View.fs`**
- Main layout structure
- Route to page views based on CurrentPage
- Render active modal
- Render notification

### Phase 7: Update Project File

Update `Client.fsproj` with new file order:
```xml
<ItemGroup>
  <!-- Common -->
  <Compile Include="Common/Types.fs" />
  <Compile Include="Common/Routing.fs" />

  <!-- Components -->
  <Compile Include="Components/Icons.fs" />
  <Compile Include="Components/Cards/View.fs" />
  <Compile Include="Components/Layout/Types.fs" />
  <Compile Include="Components/Layout/State.fs" />
  <Compile Include="Components/Layout/View.fs" />
  <Compile Include="Components/SearchModal/Types.fs" />
  <Compile Include="Components/SearchModal/State.fs" />
  <Compile Include="Components/SearchModal/View.fs" />
  <!-- ... other components ... -->

  <!-- Pages -->
  <Compile Include="Pages/Home/Types.fs" />
  <Compile Include="Pages/Home/State.fs" />
  <Compile Include="Pages/Home/View.fs" />
  <Compile Include="Pages/Library/Types.fs" />
  <Compile Include="Pages/Library/State.fs" />
  <Compile Include="Pages/Library/View.fs" />
  <!-- ... other pages ... -->

  <!-- App -->
  <Compile Include="App/Types.fs" />
  <Compile Include="App/State.fs" />
  <Compile Include="App/View.fs" />

  <!-- Entry point -->
  <Compile Include="Api.fs" />
  <Compile Include="Main.fs" />
</ItemGroup>
```

### Phase 8: Create Main.fs Entry Point

```fsharp
module Main

open Elmish
open Elmish.React
open Elmish.HMR

Fable.Core.JsInterop.importSideEffects "./styles.css"

Program.mkProgram App.State.init App.State.update App.View.view
|> Program.withReactSynchronous "root"
|> Program.run
```

## Implementation Order

1. **Foundation** (Phase 1)
   - Common/Types.fs
   - Common/Routing.fs

2. **Stateless Components** (Phase 2)
   - Components/Icons.fs (move)
   - Components/Cards/View.fs

3. **Modal Components** (Phase 3) - in order:
   - Notification (simplest)
   - ConfirmModal (simple)
   - SearchModal (moderate)
   - FriendModal
   - TagModal
   - AbandonModal
   - QuickAddModal (most complex, depends on others)

4. **Layout Component** (Phase 4)
   - Components/Layout/*

5. **Simple Pages First** (Phase 5) - in order:
   - NotFound
   - Home
   - Friends
   - FriendDetail
   - Tags
   - TagDetail
   - Library
   - MovieDetail
   - SeriesDetail

6. **App Component** (Phase 6)
   - App/Types.fs
   - App/State.fs
   - App/View.fs

7. **Project File & Entry Point** (Phase 7-8)
   - Update Client.fsproj
   - Create Main.fs
   - Delete old files

8. **Verification**
   - `dotnet build`
   - `npm run dev`
   - Manual testing

## Key Patterns to Follow

### Child Component Communication

Each child returns `(Model * Cmd<Msg> * ExternalMsg)` where:
- `Model`: Updated child model
- `Cmd<Msg>`: Commands for the child
- `ExternalMsg`: Messages for the parent

Parent handles external messages:
```fsharp
| SearchModalMsg childMsg ->
    match model.ActiveModal with
    | SearchModal childModel ->
        let newChildModel, childCmd, extMsg = SearchModal.State.update childMsg childModel
        let model' = { model with ActiveModal = SearchModal newChildModel }
        let cmd = Cmd.map SearchModalMsg childCmd
        match extMsg with
        | SearchModal.Types.NoOp -> model', cmd
        | SearchModal.Types.ItemSelected item ->
            model', Cmd.batch [cmd; Cmd.ofMsg (OpenQuickAddModal item)]
        | SearchModal.Types.CloseRequested ->
            { model' with ActiveModal = NoModal }, cmd
    | _ -> model, Cmd.none
```

### Shared Data Access

Pages receive global data as parameters to their view:
```fsharp
// In App/View.fs
Pages.Library.View.view
    libraryModel
    (RemoteData.defaultValue [] model.Tags)
    (LibraryMsg >> dispatch)
```

### Lazy Page Initialization

Initialize pages lazily when navigating:
```fsharp
| NavigateTo LibraryPage ->
    let libraryModel, libraryCmd =
        match model.LibraryModel with
        | Some m -> m, Cmd.none
        | None -> Pages.Library.State.init ()
    { model with
        CurrentPage = LibraryPage
        LibraryModel = Some libraryModel
    }, Cmd.map LibraryMsg libraryCmd
```

## Files to Delete After Refactoring

- `src/Client/Types.fs` (replaced by Common/* and component Types.fs)
- `src/Client/State.fs` (replaced by App/State.fs and component State.fs)
- `src/Client/View.fs` (replaced by App/View.fs and component View.fs)
- `src/Client/App.fs` (replaced by Main.fs)

## Estimated File Count

- Common: 2 files
- Components: ~25 files (7 components × 3 files + Icons + Cards)
- Pages: ~25 files (9 pages × ~3 files)
- App: 3 files
- Other: 2 files (Api.fs, Main.fs)

**Total: ~57 files** (up from 6 files)

## Risk Mitigation

1. **Keep old files until verified** - Don't delete until everything compiles
2. **Test incrementally** - Verify build after each phase
3. **Git commits per phase** - Easy rollback if needed
4. **Start with simplest components** - Build confidence before complex ones
