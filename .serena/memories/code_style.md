# Cinemarco - Code Style & Conventions

## F# Conventions

### Types
- Use records for data structures, discriminated unions for variants
- Define ALL types in `src/Shared/Domain.fs` before implementing
- Use `Result<'T, string>` for fallible operations
- Use `RemoteData<'T>` for async operations in frontend

```fsharp
type RemoteData<'T> = NotAsked | Loading | Success of 'T | Failure of string
```

### Domain Logic
- `src/Server/Domain.fs` must have NO I/O operations
- All side effects go in `Persistence.fs` or `Api.fs`
- Domain functions are pure transformations

### Naming
- PascalCase for types, modules, and public functions
- camelCase for local values and parameters
- Descriptive names preferred over abbreviations

### API Pattern
```fsharp
type IEntityApi = {
    getAll: unit -> Async<Entity list>
    getById: int -> Async<Result<Entity, string>>
    save: Entity -> Async<Result<Entity, string>>
}
```

## Frontend (Elmish/Feliz)

### MVU Architecture
- All state changes through `update` function
- Use `Cmd` for side effects (API calls, etc.)
- View is pure function of Model

### Common Components (MANDATORY)
Always import and use these instead of raw HTML:
- `GlassPanel` - For content sections
- `GlassButton` - For icon action buttons
- `SectionHeader` - For section titles
- `FilterChip` - For filter toggles
- `RemoteDataView` - For async data display
- `EmptyState` / `ErrorState` - For empty/error states

### DaisyUI Classes
Use for standard UI elements:
- Buttons: `btn btn-primary`, `btn btn-ghost`
- Badges: `badge badge-primary`
- Loading: `loading loading-spinner`
- Alerts: `alert alert-error`

## Development Order for Features
1. `src/Shared/Domain.fs` → Define types
2. `src/Shared/Api.fs` → Define API contract
3. `src/Server/Validation.fs` → Input validation
4. `src/Server/Domain.fs` → Business logic (pure)
5. `src/Server/Persistence.fs` → Database operations
6. `src/Server/Api.fs` → Implement API
7. `src/Client/State.fs` → Model, Msg, update
8. `src/Client/View.fs` → UI components
9. `src/Tests/` → Tests
