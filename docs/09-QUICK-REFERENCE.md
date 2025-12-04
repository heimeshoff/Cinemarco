# Quick Reference Guide for Claude Code

This is a condensed reference for common patterns and commands when developing F# full-stack applications.

## Project Structure

```
/
├── src/
│   ├── Shared/          # Domain types & API contracts
│   ├── Client/          # Fable + Elmish frontend
│   ├── Server/          # Giraffe backend
│   └── Tests/           # Expecto tests
├── vite.config.js
├── tailwind.config.js
├── Dockerfile
└── docker-compose.yml
```

## Tech Stack Quick Facts

| Component | Technology | Purpose |
|-----------|-----------|---------|
| Frontend Framework | Elmish.React + Feliz | MVU architecture |
| Frontend Build | Vite + fable-plugin | HMR development |
| Frontend Styling | TailwindCSS 4.3 + DaisyUI | Utility-first CSS |
| Frontend Routing | Feliz.Router | Client-side routing |
| Backend Framework | Giraffe | Functional ASP.NET Core |
| RPC | Fable.Remoting | Type-safe client/server |
| Persistence | SQLite + JSON files | Structured + simple data |
| Testing | Expecto | F# test framework |
| Networking | Tailscale (tsnet) | Private network access |

## Common Commands

### Development

```bash
# Start backend (Terminal 1)
cd src/Server && dotnet watch run

# Start frontend (Terminal 2)
npm run dev

# Run tests
dotnet test

# Build for production
npm run build && dotnet publish src/Server -c Release
```

### Docker

```bash
# Build image
docker build -t my-app:latest .

# Run locally
docker run -p 5000:5000 -v $(pwd)/data:/app/data my-app:latest

# Deploy stack
docker-compose up -d

# View logs
docker logs my-app
docker logs -f my-app-tailscale
```

## Code Templates

### Shared Domain Type

```fsharp
// src/Shared/Domain.fs
module Shared.Domain

type Item = {
    Id: int
    Name: string
    CreatedAt: DateTime
}

type ItemUpdate = {
    Name: string option
}
```

### Shared API Contract

```fsharp
// src/Shared/Api.fs
module Shared.Api

open Domain

type IItemApi = {
    getItems: unit -> Async<Item list>
    getItem: int -> Async<Result<Item, string>>
    saveItem: Item -> Async<Result<Item, string>>
    deleteItem: int -> Async<Result<unit, string>>
}
```

### Client State (Elmish)

```fsharp
// src/Client/State.fs
module State

type Model = {
    Items: RemoteData<Item list>
    SelectedItem: RemoteData<Item>
}

type Msg =
    | LoadItems
    | ItemsLoaded of Result<Item list, string>
    | LoadItem of int
    | ItemLoaded of Result<Item, string>

let init () : Model * Cmd<Msg> =
    let model = { Items = NotAsked; SelectedItem = NotAsked }
    let cmd = Cmd.ofMsg LoadItems
    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | LoadItems ->
        let cmd = 
            Cmd.OfAsync.either
                Api.api.getItems
                ()
                (Ok >> ItemsLoaded)
                (fun ex -> Error ex.Message |> ItemsLoaded)
        { model with Items = Loading }, cmd
    
    | ItemsLoaded (Ok items) ->
        { model with Items = Success items }, Cmd.none
    
    | ItemsLoaded (Error err) ->
        { model with Items = Failure err }, Cmd.none
    
    | LoadItem id ->
        let cmd =
            Cmd.OfAsync.either
                Api.api.getItem
                id
                (Ok >> ItemLoaded)
                (fun ex -> Error ex.Message |> ItemLoaded)
        { model with SelectedItem = Loading }, cmd
    
    | ItemLoaded result ->
        { model with SelectedItem = 
            match result with
            | Ok item -> Success item
            | Error err -> Failure err
        }, Cmd.none
```

### Client View (Feliz)

```fsharp
// src/Client/View.fs
module View

open Feliz

let private itemCard (item: Item) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "card bg-base-100 shadow-xl"
        prop.children [
            Html.div [
                prop.className "card-body"
                prop.children [
                    Html.h2 [ prop.className "card-title"; prop.text item.Name ]
                    Html.div [
                        prop.className "card-actions justify-end"
                        prop.children [
                            Html.button [
                                prop.className "btn btn-primary"
                                prop.onClick (fun _ -> dispatch (LoadItem item.Id))
                                prop.text "View"
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "container mx-auto p-4"
        prop.children [
            Html.h1 [ prop.className "text-4xl font-bold mb-8"; prop.text "Items" ]
            
            match model.Items with
            | NotAsked -> Html.div "Click to load"
            | Loading -> Html.div [ prop.className "loading loading-spinner" ]
            | Success items ->
                Html.div [
                    prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"
                    prop.children [
                        for item in items -> itemCard item dispatch
                    ]
                ]
            | Failure err ->
                Html.div [ prop.className "alert alert-error"; prop.text err ]
        ]
    ]
```

### Server API Implementation

```fsharp
// src/Server/Api.fs
module Api

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Shared.Api

let itemApi : IItemApi = {
    getItems = fun () -> async {
        return! Persistence.getAllItems()
    }
    
    getItem = fun itemId -> async {
        match! Persistence.getItemById itemId with
        | Some item -> return Ok item
        | None -> return Error $"Item {itemId} not found"
    }
    
    saveItem = fun item -> async {
        try
            match Validation.validateItem item with
            | Error errors -> return Error (String.concat ", " errors)
            | Ok valid ->
                let processed = Domain.processItem valid
                do! Persistence.saveItem processed
                return Ok processed
        with ex ->
            return Error ex.Message
    }
    
    deleteItem = fun itemId -> async {
        try
            do! Persistence.deleteItem itemId
            return Ok ()
        with ex ->
            return Error ex.Message
    }
}

let webApp =
    Remoting.createApi()
    |> Remoting.withRouteBuilder (fun t m -> $"/api/{t}/{m}")
    |> Remoting.fromValue itemApi
    |> Remoting.buildHttpHandler
```

### SQLite Persistence

```fsharp
// src/Server/Persistence.fs
module Persistence

open Microsoft.Data.Sqlite
open Dapper

let private connectionString = "Data Source=./data/app.db"
let private getConnection () = new SqliteConnection(connectionString)

let initializeDatabase () =
    use conn = getConnection()
    conn.Open()
    
    use cmd = new SqliteCommand("""
        CREATE TABLE IF NOT EXISTS items (
            id INTEGER PRIMARY KEY AUTOINCREMENT,
            name TEXT NOT NULL,
            created_at TEXT NOT NULL
        );
    """, conn)
    
    cmd.ExecuteNonQuery() |> ignore

let getAllItems () : Async<Item list> =
    async {
        use conn = getConnection()
        let! items = 
            conn.QueryAsync<Item>("SELECT * FROM items ORDER BY created_at DESC")
            |> Async.AwaitTask
        return items |> Seq.toList
    }

let getItemById (id: int) : Async<Item option> =
    async {
        use conn = getConnection()
        let! item =
            conn.QueryFirstOrDefaultAsync<Item>(
                "SELECT * FROM items WHERE id = @Id",
                {| Id = id |}
            )
            |> Async.AwaitTask
        return if isNull (box item) then None else Some item
    }

let saveItem (item: Item) : Async<unit> =
    async {
        use conn = getConnection()
        
        if item.Id = 0 then
            // Insert
            do! conn.ExecuteAsync(
                "INSERT INTO items (name, created_at) VALUES (@Name, @CreatedAt)",
                item
            ) |> Async.AwaitTask |> Async.Ignore
        else
            // Update
            do! conn.ExecuteAsync(
                "UPDATE items SET name = @Name WHERE id = @Id",
                item
            ) |> Async.AwaitTask |> Async.Ignore
    }

let deleteItem (id: int) : Async<unit> =
    async {
        use conn = getConnection()
        do! conn.ExecuteAsync(
            "DELETE FROM items WHERE id = @Id",
            {| Id = id |}
        ) |> Async.AwaitTask |> Async.Ignore
    }
```

### Expecto Test

```fsharp
// src/Tests/Server.Tests/DomainTests.fs
module Tests.DomainTests

open Expecto
open Domain

[<Tests>]
let domainTests =
    testList "Domain Tests" [
        testCase "Process item works correctly" <| fun () ->
            let item = { Id = 1; Name = "  Test  "; CreatedAt = DateTime.UtcNow }
            let processed = Domain.processItem item
            Expect.equal processed.Name "Test" "Should trim name"
        
        testAsync "Load items returns data" {
            let! items = Api.itemApi.getItems()
            Expect.isNotEmpty items "Should return items"
        }
    ]
```

## UI Component Snippets

Copy-paste these snippets for common UI patterns. **Always use these components instead of raw HTML/CSS**.

### Component Imports
```fsharp
// Add at top of View files
module GlassPanel = Common.Components.GlassPanel.View
module GlassButton = Common.Components.GlassButton.View
module SectionHeader = Common.Components.SectionHeader.View
module FilterChip = Common.Components.FilterChip.View
module RemoteDataView = Common.Components.RemoteDataView.View
module EmptyState = Common.Components.EmptyState.View
module ErrorState = Common.Components.ErrorState.View
module PosterGrid = Common.Components.PosterGrid.View
```

### GlassPanel Card
```fsharp
GlassPanel.standard [
    SectionHeader.title "Section Title"
    Html.div [
        prop.className "space-y-4"
        prop.children [
            Html.p [ prop.text "Content goes here" ]
        ]
    ]
]
```

### GlassButton Action Bar
```fsharp
Html.div [
    prop.className "flex gap-2"
    prop.children [
        GlassButton.success Icons.check "Watched" (fun () -> dispatch MarkWatched)
        GlassButton.primary Icons.star "Rate" (fun () -> dispatch OpenRating)
        GlassButton.danger Icons.trash "Delete" (fun () -> dispatch Delete)
    ]
]
```

### SectionHeader with Action
```fsharp
GlassPanel.standard [
    SectionHeader.withLink "Recent Movies" "See all" (Some Icons.arrowRight) (fun () ->
        dispatch (NavigateTo LibraryPage)
    )
    // Content below header...
]
```

### FilterChip Row
```fsharp
Html.div [
    prop.className "flex flex-wrap gap-2 mb-4"
    prop.children [
        FilterChip.chip "All" (model.Filter = All) (fun () -> dispatch (SetFilter All))
        FilterChip.chipWithIcon Icons.film "Movies" (model.Filter = Movies) (fun () -> dispatch (SetFilter Movies))
        FilterChip.chipWithIcon Icons.tv "Series" (model.Filter = Series) (fun () -> dispatch (SetFilter Series))
    ]
]
```

### RemoteDataView with Poster Grid
```fsharp
RemoteDataView.withSkeleton 6 model.Library (fun entries ->
    if List.isEmpty entries then
        EmptyState.view {
            Title = "No movies yet"
            Description = "Add your first movie to get started"
            Icon = Some Icons.film
            Action = Some ("Add Movie", fun () -> dispatch OpenSearch)
        }
    else
        Html.div [
            prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
            prop.children [
                for entry in entries do
                    PosterCard.view entry (fun () -> dispatch (SelectEntry entry.Id))
            ]
        ]
)
```

### Complete Page Template
```fsharp
module MyPage.View

open Feliz
open App.Types

// Import common components
module GlassPanel = Common.Components.GlassPanel.View
module GlassButton = Common.Components.GlassButton.View
module SectionHeader = Common.Components.SectionHeader.View
module FilterChip = Common.Components.FilterChip.View
module RemoteDataView = Common.Components.RemoteDataView.View
module EmptyState = Common.Components.EmptyState.View

let view (model: Model) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Page header
            SectionHeader.titleLarge "My Page"

            // Filters
            Html.div [
                prop.className "flex flex-wrap gap-2"
                prop.children [
                    FilterChip.chip "All" (model.Filter = All) (fun () -> dispatch (SetFilter All))
                    FilterChip.chip "Active" (model.Filter = Active) (fun () -> dispatch (SetFilter Active))
                ]
            ]

            // Main content
            GlassPanel.standard [
                RemoteDataView.withSpinner model.Items (fun items ->
                    if List.isEmpty items then
                        EmptyState.view {
                            Title = "No items"
                            Description = "Nothing to show"
                            Icon = None
                            Action = None
                        }
                    else
                        Html.div [
                            prop.className "space-y-4"
                            prop.children [
                                for item in items do
                                    renderItem item dispatch
                            ]
                        ]
                )
            ]
        ]
    ]
```

### Poster Card with Shine Effect
```fsharp
Html.div [
    prop.className "poster-card group cursor-pointer"
    prop.onClick (fun _ -> dispatch (SelectEntry entry.Id))
    prop.children [
        Html.div [
            prop.className "poster-image-container poster-shadow"
            prop.children [
                Html.img [
                    prop.className "poster-image"
                    prop.src (Tmdb.posterUrl entry.PosterPath)
                    prop.alt entry.Title
                ]
                Html.div [ prop.className "poster-shine" ]  // Always include!
                Html.div [
                    prop.className "poster-overlay"
                    prop.children [
                        // Overlay content on hover
                        Html.div [
                            prop.className "flex gap-1"
                            prop.children [
                                GlassButton.button Icons.info "Details" (fun () -> ())
                            ]
                        ]
                    ]
                ]
            ]
        ]
        Html.div [
            prop.className "mt-2"
            prop.children [
                Html.h3 [ prop.className "font-medium text-sm truncate"; prop.text entry.Title ]
                Html.p [ prop.className "text-xs text-base-content/60"; prop.text (string entry.Year) ]
            ]
        ]
    ]
]
```

---

## Tailwind Class Patterns

```fsharp
// Layout
prop.className "container mx-auto p-4"
prop.className "flex flex-col gap-4"
prop.className "grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4"

// Typography
prop.className "text-4xl font-bold"
prop.className "text-lg text-gray-600"

// Buttons (DaisyUI)
prop.className "btn btn-primary"
prop.className "btn btn-secondary"
prop.className "btn btn-ghost"

// Cards (DaisyUI)
prop.className "card bg-base-100 shadow-xl"
prop.className "card-body"
prop.className "card-title"
prop.className "card-actions justify-end"

// Alerts (DaisyUI)
prop.className "alert alert-success"
prop.className "alert alert-error"
prop.className "alert alert-info"

// Loading (DaisyUI)
prop.className "loading loading-spinner"
prop.className "loading loading-dots"
```

## File Locations Reference

| What | Where | Example |
|------|-------|---------|
| Domain types | `src/Shared/Domain.fs` | `type Item = { ... }` |
| API contracts | `src/Shared/Api.fs` | `type IItemApi = { ... }` |
| Client state | `src/Client/State.fs` | `type Model`, `type Msg` |
| Client views | `src/Client/View.fs` | `let view model dispatch` |
| Server API | `src/Server/Api.fs` | `let itemApi : IItemApi` |
| Persistence | `src/Server/Persistence.fs` | SQLite + Dapper |
| Domain logic | `src/Server/Domain.fs` | Pure functions |
| Validation | `src/Server/Validation.fs` | `validateItem` |
| Tests | `src/Tests/*/` | Expecto tests |

## Troubleshooting Quick Fixes

### Frontend not loading
```bash
# Check Vite is running
npm run dev

# Check proxy in vite.config.js points to correct port
```

### API calls failing
```bash
# Check backend is running
cd src/Server && dotnet watch run

# Check Fable.Remoting route builder matches
# Client: /api/{TypeName}/{MethodName}
# Server: Same route builder
```

### Type errors after changing Shared
```bash
# Rebuild Shared
cd src/Shared && dotnet build

# Rebuild Client and Server
# They should auto-detect and rebuild
```

### Docker build failing
```bash
# Clean build
docker build --no-cache -t my-app:latest .

# Check Dockerfile COPY paths are correct
```

### Tailscale not connecting
```bash
# Check logs
docker logs my-app-tailscale

# Verify auth key
# Check /dev/net/tun exists
# Verify CAP_NET_ADMIN in docker-compose
```

## Next Steps Checklist

When starting a new feature:
- [ ] Define types in `src/Shared/Domain.fs`
- [ ] Define API contract in `src/Shared/Api.fs`
- [ ] Implement server API in `src/Server/Api.fs`
- [ ] Add persistence in `src/Server/Persistence.fs`
- [ ] Add domain logic in `src/Server/Domain.fs`
- [ ] Update client state in `src/Client/State.fs`
- [ ] Update client view in `src/Client/View.fs`
- [ ] Write tests in `src/Tests/`
- [ ] Test locally with `dotnet watch run` + `npm run dev`
- [ ] Build Docker image
- [ ] Deploy to Portainer

## Key Principles to Remember

1. **Type safety**: Define types in Shared first
2. **Pure functions**: Domain logic has no side effects
3. **MVU pattern**: All state changes through update function
4. **RemoteData**: Represent async operations explicitly
5. **Result types**: Use Result<'T, string> for fallible operations
6. **Validation**: Validate early at API boundary
7. **Async everywhere**: All I/O operations are async
8. **Immutability**: Use records, not mutable classes
9. **Pattern matching**: Leverage F#'s exhaustive matching
10. **Keep it simple**: SQLite and files are enough for most home server apps

## Documentation Index

1. `00-ARCHITECTURE.md` - High-level overview and principles
2. `01-PROJECT-SETUP.md` - Initialize new project from scratch
3. `02-FRONTEND-GUIDE.md` - Elmish + Feliz patterns
4. `03-BACKEND-GUIDE.md` - Giraffe + Fable.Remoting patterns
5. `04-SHARED-TYPES.md` - Type design and sharing
6. `05-PERSISTENCE.md` - SQLite and file storage
7. `06-TESTING.md` - Expecto testing strategies
8. `07-BUILD-DEPLOY.md` - Docker and deployment
9. `08-TAILSCALE-INTEGRATION.md` - Private networking
10. `09-QUICK-REFERENCE.md` - This document
