# Cinemarco - Project Overview

## Purpose
Cinemarco is a personal cinema memory tracker - a local-first application for tracking movies and series you've watched, who you watched them with, and your personal ratings and notes.

## Tech Stack
- **Frontend**: Elmish.React + Feliz (MVU architecture) with Fable for F# to JS compilation
- **Styling**: TailwindCSS 4.3 + DaisyUI (dark theme default)
- **Frontend Build**: Vite + vite-plugin-fable
- **Backend**: Giraffe + Fable.Remoting (.NET 9+)
- **Database**: SQLite + Dapper
- **External APIs**: TMDB (movies/series data)
- **Deployment**: Docker + Tailscale
- **Tests**: Expecto

## Project Structure
```
/
├── src/
│   ├── Shared/          # Domain types & API contracts (F#)
│   │   ├── Domain.fs    # All domain types
│   │   └── Api.fs       # API contracts
│   ├── Client/          # Fable + Elmish frontend
│   │   ├── App/         # Main app state and routing
│   │   ├── Pages/       # Page-specific modules
│   │   ├── Components/  # Reusable UI components
│   │   ├── Common/      # Shared UI utilities
│   │   └── styles.css   # TailwindCSS styles
│   ├── Server/          # Giraffe backend
│   │   ├── Api.fs       # API implementation
│   │   ├── Persistence.fs # Database operations
│   │   ├── Migrations.fs  # Database migrations
│   │   ├── TmdbClient.fs  # TMDB API client
│   │   └── Program.fs   # Server entry point
│   └── Tests/           # Expecto tests
├── docs/                # Documentation guides
├── specs/               # Domain specifications
└── milestones.md        # Implementation roadmap
```

## Design Principles
- **Local-first**: All data stored locally in SQLite
- **Single-user**: No authentication complexity
- **Mobile-first design, desktop-primary usage**: Responsive layouts
- **Dark mode default**: High-quality poster visuals pop on dark backgrounds
- **Type safety**: Define types in Shared first
- **Pure domain logic**: No I/O in Domain.fs (server)
- **MVU pattern**: All state changes through update function
- **RemoteData**: Represent async operations explicitly
