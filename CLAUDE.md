# Cinemarco - Claude Code Instructions

## Git Commit Attribution

Do not include "Co-Authored-By" or any Claude attribution in commit messages. All commits should be attributed solely to the repository owner.

## About Cinemarco

Cinemarco is a personal cinema memory tracker - a local-first application for tracking movies and series you've watched, who you watched them with, and your personal ratings and notes. Built with F# using Elmish.React + Feliz (frontend), Giraffe + Fable.Remoting (backend), SQLite (persistence), and deployed via Docker + Tailscale.

## Before Implementing Anything

**Always read the relevant documentation first:**

1. Check `/docs/09-QUICK-REFERENCE.md` for code templates
2. Read the specific guide for your task (see Documentation Map below)
3. Check `/specs/` for domain specifications
4. Look at existing code to match patterns

## Documentation Map

| Task | Read This First |
|------|-----------------|
| Complete new feature | `/docs/09-QUICK-REFERENCE.md` + specific guides below |
| Domain types/API contracts | `/docs/04-SHARED-TYPES.md` + `/specs/DOMAIN-MODEL.md` |
| Frontend (UI, state) | `/docs/02-FRONTEND-GUIDE.md` + `/specs/UI-UX-SPECIFICATION.md` |
| Backend (API, logic) | `/docs/03-BACKEND-GUIDE.md` + `/specs/API-CONTRACT.md` |
| Database/files | `/docs/05-PERSISTENCE.md` + `/specs/DATABASE-SCHEMA.md` |
| Tests | `/docs/06-TESTING.md` |
| Docker/deployment | `/docs/07-BUILD-DEPLOY.md` |
| Tailscale networking | `/docs/08-TAILSCALE-INTEGRATION.md` |
| Architecture overview | `/docs/00-ARCHITECTURE.md` |
| Implementation roadmap | `/milestones.md` |

## Using Skills

Skills provide focused guidance. Invoke them based on the task:

| Skill | When to Use |
|-------|-------------|
| `fsharp-feature` | Complete feature implementation (orchestrates all layers) |
| `fsharp-shared` | Defining types and API contracts in `src/Shared/` |
| `fsharp-backend` | Backend implementation (validation, domain, persistence, API) |
| `fsharp-validation` | Input validation patterns |
| `fsharp-persistence` | Database tables, file storage, event sourcing |
| `fsharp-frontend` | Elmish state and Feliz views |
| `fsharp-tests` | Writing Expecto tests |
| `tailscale-deploy` | Docker + Tailscale deployment |

## Implementing User Specifications

When the user provides a specification file (markdown describing a feature):

1. **Read the specification file** thoroughly
2. **Read `/docs/09-QUICK-REFERENCE.md`** for patterns
3. **Plan the implementation** using the development order below
4. **Implement each layer**, testing as you go
5. **Verify with build and tests**

### Development Order

```
1. src/Shared/Domain.fs     → Define types
2. src/Shared/Api.fs        → Define API contract
3. src/Server/Validation.fs → Input validation
4. src/Server/Domain.fs     → Business logic (PURE - no I/O!)
5. src/Server/Persistence.fs → Database/file operations
6. src/Server/Api.fs        → Implement API
7. src/Client/State.fs      → Model, Msg, update
8. src/Client/View.fs       → UI components
9. src/Tests/               → Tests
```

## Key Principles

### Type Safety First
- Define ALL types in `src/Shared/` before implementing
- Use `Result<'T, string>` for fallible operations
- Use discriminated unions for state variations

### Pure Domain Logic
- `src/Server/Domain.fs` must have NO I/O operations
- All side effects go in `Persistence.fs` or `Api.fs`
- Domain functions are pure transformations

### MVU Architecture
- All frontend state changes through `update` function
- Use `Cmd` for side effects (API calls, etc.)
- View is pure function of Model

### RemoteData Pattern
```fsharp
type RemoteData<'T> = NotAsked | Loading | Success of 'T | Failure of string
```
Use this for all async operations in frontend state.

### Validate Early
- Validate at API boundary before any processing
- Return clear error messages

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
// CORRECT - Use GlassPanel
GlassPanel.standard [
    Html.h3 [ prop.text "Section Title" ]
    Html.p [ prop.text "Content here" ]
]

// Variants available:
GlassPanel.standard children   // Most common - subtle glass effect
GlassPanel.strong children     // Emphasized sections
GlassPanel.subtle children     // Minimal glass effect
GlassPanel.standardWith "mt-4" children  // With extra classes

// WRONG - Never do this
Html.div [
    prop.className "glass rounded-lg p-4"
    prop.children [ ... ]
]
```

### GlassButton - For ALL Icon Action Buttons
Use GlassButton for icon-based action buttons (watch, rate, delete, etc.). NEVER write manual button styling for actions.

```fsharp
// CORRECT - Use GlassButton variants
GlassButton.button checkIcon "Mark as watched" (fun () -> dispatch MarkWatched)
GlassButton.success checkIcon "Watched" (fun () -> dispatch MarkWatched)
GlassButton.successActive checkIcon "Watched" (fun () -> dispatch MarkUnwatched)
GlassButton.danger trashIcon "Delete" (fun () -> dispatch Delete)
GlassButton.primary starIcon "Rate" (fun () -> dispatch OpenRating)
GlassButton.primaryActive starIcon "Rated" (fun () -> dispatch OpenRating)
GlassButton.disabled lockIcon "Locked"

// WRONG - Never manually style action buttons
Html.button [
    prop.className "btn btn-ghost ..."
    prop.onClick (fun _ -> dispatch MarkWatched)
    prop.children [ checkIcon ]
]
```

### SectionHeader - For ALL Section Titles
Use SectionHeader for titled sections with optional actions. NEVER use plain h2/h3 for section headers.

```fsharp
// CORRECT - Use SectionHeader
SectionHeader.title "Recently Watched"
SectionHeader.titleLarge "Library"
SectionHeader.titleSmall "Tags"
SectionHeader.withLink "Friends" "See all" (Some arrowRight) (fun () -> dispatch GoToFriends)
SectionHeader.withButton "Collections" "Add" (Some plus) (fun () -> dispatch AddCollection)

// WRONG - Never do this
Html.h3 [
    prop.className "text-xl font-bold"
    prop.text "Section Title"
]
```

### FilterChip - For ALL Filter/Toggle Pills
Use FilterChip for filter toggles and category pills. NEVER manually style filter buttons.

```fsharp
// CORRECT - Use FilterChip
FilterChip.chip "Movies" (filter = Movies) (fun () -> dispatch (SetFilter Movies))
FilterChip.chipWithIcon filmIcon "Movies" (filter = Movies) (fun () -> dispatch (SetFilter Movies))

// WRONG - Never do this
Html.button [
    prop.className (if active then "filter-chip-active" else "filter-chip")
    prop.text "Movies"
]
```

### RemoteDataView - For ALL Async Data Display
Use RemoteDataView when displaying RemoteData. NEVER manually match on RemoteData in views.

```fsharp
// CORRECT - Use RemoteDataView
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

// WRONG - Never manually match RemoteData in view
match model.Entries with
| Loading -> Html.span [ prop.className "loading loading-spinner" ]
| Success entries -> ...
| Failure err -> Html.div [ prop.text err ]
```

### EmptyState - For Empty Lists/No Results
Use EmptyState when a list or search has no results.

```fsharp
// CORRECT
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
// CORRECT
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

## Code Patterns

### Backend API Implementation
```fsharp
let api : IEntityApi = {
    getAll = fun () -> Persistence.getAllEntities()

    getById = fun id -> async {
        match! Persistence.getById id with
        | Some e -> return Ok e
        | None -> return Error "Not found"
    }

    save = fun entity -> async {
        match Validation.validate entity with
        | Error errs -> return Error (String.concat ", " errs)
        | Ok valid ->
            let processed = Domain.process valid
            do! Persistence.save processed
            return Ok processed
    }
}
```

### Frontend State
```fsharp
type Model = { Entities: RemoteData<Entity list> }

type Msg =
    | LoadEntities
    | EntitiesLoaded of Result<Entity list, string>

let update msg model =
    match msg with
    | LoadEntities ->
        { model with Entities = Loading },
        Cmd.OfAsync.either Api.api.getAll () (Ok >> EntitiesLoaded) (Error >> EntitiesLoaded)

    | EntitiesLoaded (Ok entities) ->
        { model with Entities = Success entities }, Cmd.none

    | EntitiesLoaded (Error err) ->
        { model with Entities = Failure err }, Cmd.none
```

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

## Quick Commands

```bash
# Development
cd src/Server && dotnet watch run  # Backend with hot reload
npm run dev                         # Frontend with HMR
dotnet test                         # Run tests

# Build
docker build -t cinemarco .         # Build image
docker-compose up -d                # Deploy with Tailscale
```

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

## Tech Stack Reference

| Layer | Technology |
|-------|------------|
| Frontend | Elmish.React + Feliz |
| Styling | TailwindCSS 4.3, DaisyUI |
| Build | Vite + fable-plugin |
| Backend | Giraffe + Fable.Remoting |
| Database | SQLite + Dapper |
| Tests | Expecto |
| Runtime | .NET 9+ |
| Deployment | Docker + Tailscale |


<frontend_aesthetics>
You tend to converge toward generic, "on distribution" outputs. In frontend design, this creates what users call the "AI slop" aesthetic. Avoid this: make creative, distinctive frontends that surprise and delight. Focus on:

Typography: Choose fonts that are beautiful, unique, and interesting. Avoid generic fonts like Arial and Inter; opt instead for distinctive choices that elevate the frontend's aesthetics.

Color & Theme: Commit to a cohesive aesthetic. Use CSS variables for consistency. Dominant colors with sharp accents outperform timid, evenly-distributed palettes. Draw from IDE themes and cultural aesthetics for inspiration.

Motion: Use animations for effects and micro-interactions. Prioritize CSS-only solutions for HTML. Use Motion library for React when available. Focus on high-impact moments: one well-orchestrated page load with staggered reveals (animation-delay) creates more delight than scattered micro-interactions.

Backgrounds: Create atmosphere and depth rather than defaulting to solid colors. Layer CSS gradients, use geometric patterns, or add contextual effects that match the overall aesthetic.

Avoid generic AI-generated aesthetics:
- Overused font families (Inter, Roboto, Arial, system fonts)
- Clichéd color schemes (particularly purple gradients on white backgrounds)
- Predictable layouts and component patterns
- Cookie-cutter design that lacks context-specific character

Interpret creatively and make unexpected choices that feel genuinely designed for the context. Vary between light and dark themes, different fonts, different aesthetics. You still tend to converge on common choices (Space Grotesk, for example) across generations. Avoid this: it is critical that you think outside the box!
</frontend_aesthetics>