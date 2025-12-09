# Cinemarco

Your personal cinema memory tracker.

[![Ko-fi](https://img.shields.io/badge/Ko--fi-Support%20Me-FF5E5B?style=for-the-badge&logo=ko-fi&logoColor=white)](https://ko-fi.com/heimeshoff)

**[☕ Buy me a coffee on Ko-fi](https://ko-fi.com/heimeshoff)** — Your support helps keep this project alive!

---

## What is Cinemarco?

Cinemarco is a local-first application for tracking your movie and TV series watching history. Unlike cloud services, all your data stays on your machine in a SQLite database.

### Features

- **Track Movies & Series** - Search TMDB, add to your library with one click
- **Personal Ratings** - 5-tier rating system (Brilliant → Nope)
- **Watch Progress** - Track episode-by-episode progress for series
- **Friends & Tags** - Remember who you watched with and organize with custom tags
- **Collections** - Create franchises and custom lists (MCU order, Ghibli marathon)
- **Statistics** - See your watching history, time spent, year-in-review
- **Relationship Graph** - Visualize connections between movies, friends, and contributors
- **Dark Mode** - Designed for movie posters to pop

### Local-First Philosophy

- All data stored locally in SQLite
- No account required, no cloud sync
- Deploy to your home server via Tailscale
- Your data is yours

## Tech Stack

| Layer | Technology |
|-------|------------|
| Frontend | F# with Elmish.React + Feliz |
| Styling | TailwindCSS 4.3 + DaisyUI (dark theme) |
| Backend | F# with Giraffe + Fable.Remoting |
| Database | SQLite + Dapper |
| External APIs | TMDB (movie data), Trakt.tv (import) |
| Deployment | Docker + Tailscale |

## Quick Start

### Prerequisites

- .NET 9+ SDK
- Node.js 20+
- Docker (for deployment)

### Development

```bash
# Clone and install
git clone https://github.com/heimeshoff/Cinemarco.git
cd Cinemarco
npm install

# Start backend (Terminal 1)
cd src/Server && dotnet watch run

# Start frontend (Terminal 2)
npm run dev

# Run tests
dotnet test src/Tests/Tests.fsproj
```

Open `http://localhost:5173` for the frontend (proxies API calls to backend on port 5000).

### Build & Deploy

```bash
# Build Docker image
docker build -t cinemarco .

# Run locally
docker run -p 5000:5000 -v $(pwd)/data:/app/data cinemarco

# Deploy with Tailscale (set TS_AUTHKEY in .env)
docker-compose up -d
```

# Deploy to Portainer:

```markdown
docker-compose build
```

```markdown
docker save fsharp-counter-app:latest | gzip > fsharp-counter-app.tar.gz
```

Your app is now accessible on your Tailnet at `https://cinemarco`.

## Project Structure

```
src/
├── Shared/           # Domain types + API contracts
│   ├── Domain.fs     # Business types (movies, series, ratings, etc.)
│   └── Api.fs        # Fable.Remoting API interfaces
├── Client/           # Elmish frontend
│   ├── Types.fs      # Client-only types (RemoteData, Page routes)
│   ├── State.fs      # Model, Msg, update (MVU)
│   ├── View.fs       # UI components (Feliz)
│   └── App.fs        # Entry point
├── Server/           # Giraffe backend
│   ├── Persistence.fs# SQLite database operations
│   ├── Api.fs        # Fable.Remoting implementation
│   └── Program.fs    # Entry point
└── Tests/            # Expecto tests

specs/                # Domain specifications
├── DOMAIN-MODEL.md   # All domain types
├── API-CONTRACT.md   # API interfaces
├── DATABASE-SCHEMA.md# SQLite schema
└── UI-UX-SPECIFICATION.md

docs/                 # Implementation guides
milestones.md         # Development roadmap
```

## Development Roadmap

See [milestones.md](milestones.md) for the full implementation plan.

| Milestone | Status | Description |
|-----------|--------|-------------|
| M0 | ✅ | Foundation Reset |
| M1 | 🔲 | Core Domain Model |
| M2 | 🔲 | Database Schema |
| M3 | 🔲 | TMDB Integration |
| M4 | 🔲 | Quick Capture |
| M5 | 🔲 | Library View |
| ... | ... | ... |

## External APIs

### TMDB (The Movie Database)

Cinemarco uses TMDB for movie and series metadata. You'll need a free API key:

1. Create account at [themoviedb.org](https://www.themoviedb.org/)
2. Request API key in settings
3. Set `TMDB_API_KEY` environment variable

### Trakt.tv (Optional)

For importing existing watch history:

1. Create app at [trakt.tv/oauth/applications](https://trakt.tv/oauth/applications)
2. Set `TRAKT_CLIENT_ID` and `TRAKT_CLIENT_SECRET`

## License

Anyone is free to copy, modify, publish, use, compile, sell, or
distribute this software, either in source code form or as a compiled
binary, for any purpose, commercial or non-commercial, and by any
means.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.

For more information, please refer to <https://unlicense.org>
