# Intermission: UI Consistency & Contributor Management

This document outlines the implementation plan for improving UI consistency and adding contributor management features.

---

## Milestone 1: Documentation Update for UI Consistency

**Goal:** Update CLAUDE.md and related documentation to ensure Claude Code consistently uses common components and DaisyUI patterns.

### 1.1 Add to CLAUDE.md: UI Component Requirements

Add the following section after "## Key Principles":

```markdown
## UI Component Requirements (MANDATORY)

When implementing any frontend UI, you MUST use the common components. Do NOT write raw HTML/CSS when a component exists.

### Import Pattern
Always import common components using module aliases at the top of View files:
```fsharp
module GlassPanel = Common.Components.GlassPanel.View
module GlassButton = Common.Components.GlassButton.View
module SectionHeader = Common.Components.SectionHeader.View
module FilterChip = Common.Components.FilterChip.View
module RemoteDataView = Common.Components.RemoteDataView.View
module EmptyState = Common.Components.EmptyState.View
module ErrorState = Common.Components.ErrorState.View
module PosterGrid = Common.Components.PosterGrid.View
```

### GlassPanel - For ALL Content Sections
Use GlassPanel to wrap any distinct content area. NEVER use plain `Html.div` with manual glass styling.

```fsharp
// ✅ CORRECT - Use GlassPanel
GlassPanel.standard [
    Html.h3 [ prop.text "Section Title" ]
    Html.p [ prop.text "Content here" ]
]

// ✅ Variants available:
GlassPanel.standard children   // Most common - subtle glass effect
GlassPanel.strong children     // Emphasized sections
GlassPanel.subtle children     // Minimal glass effect
GlassPanel.standardWith "mt-4" children  // With extra classes

// ❌ WRONG - Never do this
Html.div [
    prop.className "glass rounded-lg p-4"
    prop.children [ ... ]
]
```

### GlassButton - For ALL Icon Action Buttons
Use GlassButton for icon-based action buttons (watch, rate, delete, etc.). NEVER write manual button styling for actions.

```fsharp
// ✅ CORRECT - Use GlassButton variants
GlassButton.button checkIcon "Mark as watched" (fun () -> dispatch MarkWatched)
GlassButton.success checkIcon "Watched" (fun () -> dispatch MarkWatched)
GlassButton.successActive checkIcon "Watched" (fun () -> dispatch MarkUnwatched)
GlassButton.danger trashIcon "Delete" (fun () -> dispatch Delete)
GlassButton.primary starIcon "Rate" (fun () -> dispatch OpenRating)
GlassButton.primaryActive starIcon "Rated" (fun () -> dispatch OpenRating)
GlassButton.disabled lockIcon "Locked"

// ❌ WRONG - Never manually style action buttons
Html.button [
    prop.className "btn btn-ghost ..."
    prop.onClick (fun _ -> dispatch MarkWatched)
    prop.children [ checkIcon ]
]
```

### SectionHeader - For ALL Section Titles
Use SectionHeader for titled sections with optional actions. NEVER use plain h2/h3 for section headers.

```fsharp
// ✅ CORRECT - Use SectionHeader
SectionHeader.title "Recently Watched"
SectionHeader.titleLarge "Library"
SectionHeader.titleSmall "Tags"
SectionHeader.withLink "Friends" "See all" (Some arrowRight) (fun () -> dispatch GoToFriends)
SectionHeader.withButton "Collections" "Add" (Some plus) (fun () -> dispatch AddCollection)

// ❌ WRONG - Never do this
Html.h3 [
    prop.className "text-xl font-bold"
    prop.text "Section Title"
]
```

### FilterChip - For ALL Filter/Toggle Pills
Use FilterChip for filter toggles and category pills. NEVER manually style filter buttons.

```fsharp
// ✅ CORRECT - Use FilterChip
FilterChip.chip "Movies" (filter = Movies) (fun () -> dispatch (SetFilter Movies))
FilterChip.chipWithIcon filmIcon "Movies" (filter = Movies) (fun () -> dispatch (SetFilter Movies))

// ❌ WRONG - Never do this
Html.button [
    prop.className (if active then "filter-chip-active" else "filter-chip")
    prop.text "Movies"
]
```

### RemoteDataView - For ALL Async Data Display
Use RemoteDataView when displaying RemoteData. NEVER manually match on RemoteData in views.

```fsharp
// ✅ CORRECT - Use RemoteDataView
RemoteDataView.withSpinner model.Entries (fun entries ->
    Html.div [
        for entry in entries do
            renderEntry entry
    ]
)

// With poster skeleton loading:
RemoteDataView.withSkeleton 6 model.Movies (fun movies -> renderMovieGrid movies)

// With error context:
RemoteDataView.withContext "loading your library" model.Library (fun lib -> renderLibrary lib)

// ❌ WRONG - Never manually match RemoteData in view
match model.Entries with
| Loading -> Html.span [ prop.className "loading loading-spinner" ]
| Success entries -> ...
| Failure err -> Html.div [ prop.text err ]
```

### EmptyState - For Empty Lists/No Results
Use EmptyState when a list or search has no results.

```fsharp
// ✅ CORRECT
EmptyState.view {
    Title = "No movies yet"
    Description = "Add your first movie to get started"
    Icon = Some filmIcon
    Action = Some ("Add Movie", fun () -> dispatch OpenAddMovie)
}
```

### ErrorState - For Error Display
Use ErrorState for error messages with context.

```fsharp
// ✅ CORRECT
ErrorState.view {
    Message = "Failed to load"
    Context = Some "while fetching your library"
}
```

### DaisyUI Components - Use These Classes

For elements NOT covered by common components, use DaisyUI classes:

#### Buttons (when not using GlassButton)
```fsharp
// Text buttons
Html.button [ prop.className "btn btn-primary"; prop.text "Save" ]
Html.button [ prop.className "btn btn-ghost"; prop.text "Cancel" ]
Html.button [ prop.className "btn btn-error"; prop.text "Delete" ]
Html.button [ prop.className "btn btn-sm btn-ghost"; prop.text "Small" ]
```

#### Badges
```fsharp
Html.span [ prop.className "badge badge-primary"; prop.text "New" ]
Html.span [ prop.className "badge badge-outline"; prop.text "Tag" ]
Html.span [ prop.className "badge badge-success"; prop.text "Watched" ]
```

#### Tabs (use DaisyUI tabs)
```fsharp
Html.div [
    prop.className "tabs tabs-boxed"
    prop.children [
        Html.a [ prop.className "tab tab-active"; prop.text "Overview" ]
        Html.a [ prop.className "tab"; prop.text "Cast" ]
    ]
]
```

#### Loading States
```fsharp
Html.span [ prop.className "loading loading-spinner loading-sm" ]
Html.span [ prop.className "loading loading-spinner loading-lg" ]
```

#### Alerts
```fsharp
Html.div [ prop.className "alert alert-error"; prop.text "Error message" ]
Html.div [ prop.className "alert alert-success"; prop.text "Success!" ]
```

#### Dropdowns
```fsharp
Html.div [
    prop.className "dropdown dropdown-end"
    prop.children [
        Html.label [ prop.className "btn btn-ghost"; prop.tabIndex 0; prop.text "Menu" ]
        Html.ul [
            prop.className "dropdown-content menu p-2 shadow bg-base-100 rounded-box w-52"
            prop.tabIndex 0
            prop.children [ ... ]
        ]
    ]
]
```

### Poster Cards - Use poster-card Classes
For movie/series poster displays, use the established poster card pattern:

```fsharp
Html.div [
    prop.className "poster-card group"
    prop.children [
        Html.div [
            prop.className "poster-image-container poster-shadow"
            prop.children [
                Html.img [ prop.className "poster-image"; prop.src posterUrl ]
                Html.div [ prop.className "poster-shine" ]  // Always include shine effect
                Html.div [ prop.className "poster-overlay"; ... ]  // Hover overlay
            ]
        ]
    ]
]
```

### CSS Classes Reference

| Class | Usage |
|-------|-------|
| `glass` | Standard glassmorphism panel |
| `glass-strong` | Emphasized glass panel |
| `glass-subtle` | Minimal glass panel |
| `detail-action-btn` | Icon action button base |
| `poster-card` | Poster card container |
| `poster-shine` | Poster hover shine effect |
| `poster-overlay` | Poster hover overlay |
| `filter-chip` | Filter pill button |
| `filter-chip-active` | Active filter pill |
```

### 1.2 Add to CLAUDE.md: Anti-Patterns Section Update

Update the "Anti-Patterns to Avoid" section:

```markdown
## Anti-Patterns to Avoid

### Domain & Architecture
- **I/O in Domain.fs** - Keep domain logic pure
- **Ignoring Result types** - Always handle errors explicitly
- **Classes for domain types** - Use records and unions
- **Skipping validation** - Validate at API boundary
- **Not reading documentation** - Check guides before implementing

### UI Components (CRITICAL)
- **Raw glass styling** - Use GlassPanel component instead
- **Manual button styling** - Use GlassButton for icon actions
- **Plain h2/h3 headers** - Use SectionHeader component
- **Manual filter buttons** - Use FilterChip component
- **Matching RemoteData in views** - Use RemoteDataView component
- **Custom loading spinners** - Use DaisyUI `loading loading-spinner`
- **Custom badge styling** - Use DaisyUI `badge` classes
- **Missing poster-shine** - Always include shine effect on poster cards
```

### 1.3 Add to CLAUDE.md: Verification Checklist Update

Update the verification checklist:

```markdown
## Verification Checklist

Before marking a feature complete:

### Backend
- [ ] Types defined in `src/Shared/Domain.fs`
- [ ] API contract in `src/Shared/Api.fs`
- [ ] Validation in `src/Server/Validation.fs`
- [ ] Domain logic is pure (no I/O) in `src/Server/Domain.fs`
- [ ] Persistence in `src/Server/Persistence.fs`
- [ ] API implementation in `src/Server/Api.fs`

### Frontend
- [ ] Frontend state in `src/Client/State.fs`
- [ ] Frontend view in `src/Client/View.fs`
- [ ] **Uses GlassPanel for content sections**
- [ ] **Uses GlassButton for icon actions**
- [ ] **Uses SectionHeader for section titles**
- [ ] **Uses RemoteDataView for async data**
- [ ] **Uses DaisyUI classes for buttons, badges, alerts**
- [ ] **Poster cards include poster-shine effect**

### Quality
- [ ] Tests written (at minimum: domain + validation)
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes
```

### 1.4 Update docs/02-FRONTEND-GUIDE.md

Add a "Common Components" section with:
- [ ] Full import example showing all components
- [ ] Decision tree: "Which component should I use?"
- [ ] Each component with 2-3 usage examples
- [ ] Link back to CLAUDE.md for quick reference

### 1.5 Update docs/09-QUICK-REFERENCE.md

Add snippets section:
- [ ] GlassPanel card snippet
- [ ] GlassButton action bar snippet
- [ ] SectionHeader with action snippet
- [ ] FilterChip row snippet
- [ ] RemoteDataView with grid snippet
- [ ] Complete page template using all components

### 1.6 Update specs/UI-UX-SPECIFICATION.md

Add:
- [ ] Component hierarchy diagram
- [ ] When to use each glass variant
- [ ] Color usage with DaisyUI theme
- [ ] Animation/transition guidelines

### Acceptance Criteria for Milestone 1
- [ ] CLAUDE.md has complete "UI Component Requirements" section
- [ ] All code examples in docs use common components
- [ ] Anti-patterns explicitly list UI mistakes to avoid
- [ ] Verification checklist includes UI component checks
- [ ] A developer reading only CLAUDE.md knows which components to use

---

## Milestone 2: Contributor Management System

**Goal:** Allow users to track selected contributors (actors, directors, etc.) from TMDB in their personal database.

### 2.1 Database Schema

#### New Table: `tracked_contributors`
```sql
CREATE TABLE tracked_contributors (
    id TEXT PRIMARY KEY,
    tmdb_person_id INTEGER NOT NULL UNIQUE,
    name TEXT NOT NULL,
    profile_path TEXT,
    known_for_department TEXT,
    created_at TEXT NOT NULL,
    notes TEXT
);

CREATE INDEX idx_tracked_contributors_tmdb_id ON tracked_contributors(tmdb_person_id);
```

### 2.2 Shared Types (`src/Shared/Domain.fs`)

- [ ] Add `TrackedContributorId` type wrapper
- [ ] Add `TrackedContributor` record:
  ```fsharp
  type TrackedContributor = {
      Id: TrackedContributorId
      TmdbPersonId: TmdbPersonId
      Name: string
      ProfilePath: string option
      KnownForDepartment: string option
      CreatedAt: System.DateTime
      Notes: string option
  }
  ```
- [ ] Add `TrackContributorRequest` record
- [ ] Add `UpdateTrackedContributorRequest` record

### 2.3 API Contract (`src/Shared/Api.fs`)

- [ ] Add to `ICinemarcoApi`:
  ```fsharp
  // Tracked Contributors
  contributorsGetAll: unit -> Async<TrackedContributor list>
  contributorsGetById: TrackedContributorId -> Async<Result<TrackedContributor, string>>
  contributorsTrack: TrackContributorRequest -> Async<Result<TrackedContributor, string>>
  contributorsUntrack: TrackedContributorId -> Async<Result<unit, string>>
  contributorsUpdateNotes: TrackedContributorId * string option -> Async<Result<TrackedContributor, string>>
  contributorsIsTracked: TmdbPersonId -> Async<bool>
  ```

### 2.4 Server Implementation

#### Persistence (`src/Server/Persistence.fs`)
- [ ] Add `TrackedContributors` module with CRUD operations

#### API Implementation (`src/Server/Api.fs`)
- [ ] Implement all contributor API endpoints

### 2.5 Client Implementation

#### New Page: Contributors List (`src/Client/Pages/Contributors/`)
- [ ] `Types.fs` - Model, messages
- [ ] `State.fs` - Load, filter logic
- [ ] `View.fs` - Grid using **GlassPanel**, **SectionHeader**, **RemoteDataView**

**View Requirements:**
- Use `SectionHeader.titleLarge "Tracked Contributors"` for page title
- Use `RemoteDataView.withSkeleton` for loading state
- Use `GlassPanel.standard` for each contributor card
- Use `FilterChip` for department filters (Actor, Director, etc.)
- Use `GlassButton.danger` for untrack action

#### Update Routing
- [ ] Add `ContributorsPage` to `Page` union
- [ ] Add route `/contributors`

#### Update Navigation
- [ ] Add "Contributors" menu item with `userPlus` icon
- [ ] Position between "Friends" and "Tags"

#### Update ContributorDetail Page
- [ ] Add Track/Untrack toggle using `GlassButton`
- [ ] Show "Tracked" badge when tracked
- [ ] Add notes field for tracked contributors

#### Update MovieDetail & SeriesDetail Cast Section
- [ ] Show small indicator on tracked contributors
- [ ] Add quick "Track" button on hover

### Acceptance Criteria for Milestone 2
- [ ] "Contributors" appears in navigation
- [ ] Contributors page lists all tracked contributors
- [ ] Can track from ContributorDetail page
- [ ] Can untrack from Contributors list or detail page
- [ ] Tracking status visible on cast/crew lists
- [ ] All views use common components (GlassPanel, GlassButton, etc.)

---

## Milestone 3: Tabbed Detail Views

**Goal:** Reorganize Movie and Series detail pages with tabbed navigation.

### 3.1 Tab Structure

#### Movie Detail Tabs
1. **Overview** - Poster, title, rating, status, collections, tags, notes
2. **Cast & Crew** - Full cast and crew lists
3. **Friends** - Who watched with, friend selector

#### Series Detail Tabs
1. **Overview** - Poster, title, rating, status, collections, tags, notes
2. **Cast & Crew** - Full cast and crew lists
3. **Episodes** - Season/episode grid with progress
4. **Friends** - Who watched with, friend selector

### 3.2 Create Tabs Component

#### `src/Client/Common/Components/Tabs/`
- [ ] `Types.fs`:
  ```fsharp
  type Tab = {
      Id: string
      Label: string
      Icon: ReactElement option
  }
  ```
- [ ] `View.fs`:
  ```fsharp
  let view (tabs: Tab list) (activeId: string) (onSelect: string -> unit) (content: ReactElement) =
      Html.div [
          prop.children [
              // Tab bar using DaisyUI tabs
              Html.div [
                  prop.className "tabs tabs-boxed glass mb-4"
                  prop.children [
                      for tab in tabs do
                          Html.a [
                              prop.className ("tab " + if tab.Id = activeId then "tab-active" else "")
                              prop.onClick (fun _ -> onSelect tab.Id)
                              prop.children [
                                  match tab.Icon with
                                  | Some icon -> Html.span [ prop.className "w-4 h-4 mr-2"; prop.children [icon] ]
                                  | None -> ()
                                  Html.span [ prop.text tab.Label ]
                              ]
                          ]
                  ]
              ]
              // Content panel
              GlassPanel.standard [ content ]
          ]
      ]
  ```

### 3.3 Update MovieDetail Page

#### Types.fs
- [ ] Add tab type and state:
  ```fsharp
  type MovieTab = Overview | CastCrew | Friends

  type Model = {
      // ... existing fields
      ActiveTab: MovieTab
  }
  ```

#### State.fs
- [ ] Add `SetActiveTab of MovieTab` message
- [ ] Initialize with `Overview` tab

#### View.fs
- [ ] Restructure layout:
  ```
  [Back Button]
  [Poster | Title/Year/Status | Action Buttons (GlassButton row)]
  [Tab Bar: Overview | Cast & Crew | Friends]
  [Tab Content in GlassPanel]
  ```
- [ ] Extract tab content into separate functions:
  - `overviewTab` - Rating, collections, tags, notes
  - `castCrewTab` - Existing credits section, enhanced
  - `friendsTab` - Friend pills and selector

### 3.4 Update SeriesDetail Page

#### Types.fs
- [ ] Add tab type:
  ```fsharp
  type SeriesTab = Overview | CastCrew | Episodes | Friends
  ```

#### View.fs
- [ ] Same restructure as MovieDetail
- [ ] Tab content functions:
  - `overviewTab` - Rating, collections, tags, notes
  - `castCrewTab` - Cast and crew lists
  - `episodesTab` - Existing season/episode grid
  - `friendsTab` - Friend pills and selector

### 3.5 Styling Requirements

- [ ] Tab bar uses `tabs tabs-boxed glass` for glassmorphism
- [ ] Active tab uses `tab-active`
- [ ] Tab content wrapped in `GlassPanel.standard`
- [ ] Icons from `Components.Icons` for each tab
- [ ] Smooth content transitions (CSS transition on opacity)

### Acceptance Criteria for Milestone 3
- [ ] MovieDetail shows 3 tabs with correct content
- [ ] SeriesDetail shows 4 tabs with correct content
- [ ] Action buttons remain above tabs (not inside)
- [ ] Tab selection persists during session
- [ ] All existing functionality preserved
- [ ] Uses Tabs component with DaisyUI styling
- [ ] Content wrapped in GlassPanel

---

## Implementation Order

1. **Milestone 1** (Documentation) - Establish patterns first
2. **Milestone 2** (Contributors) - New feature following patterns
3. **Milestone 3** (Tabs) - UI refactoring using patterns

## File Change Summary

| Milestone | Files to Modify | New Files |
|-----------|----------------|-----------|
| 1 | CLAUDE.md, 02-FRONTEND-GUIDE.md, 09-QUICK-REFERENCE.md, UI-UX-SPECIFICATION.md | 0 |
| 2 | Domain.fs, Api.fs, Persistence.fs, Api.fs (server), Routing.fs, Layout/View.fs, ContributorDetail/*, App/* | Contributors/Types.fs, State.fs, View.fs |
| 3 | MovieDetail/*, SeriesDetail/*, Client.fsproj | Tabs/Types.fs, Tabs/View.fs |
