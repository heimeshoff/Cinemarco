# Cinemarco Implementation Milestones

This document outlines the implementation strategy for Cinemarco, a personal cinema memory application. The plan is organized into incremental milestones, each delivering working functionality while building toward the complete vision.

## Architecture Decisions

### Technology Choices (per docs)
- **Frontend**: Elmish.React + Feliz (MVU architecture)
- **Styling**: TailwindCSS 4.3 + DaisyUI (dark theme default)
- **Backend**: Giraffe + Fable.Remoting
- **Database**: SQLite + Dapper
- **External APIs**: TMDB (movies/series data), Trakt.tv (import)
- **Deployment**: Docker + Tailscale

### Design Principles
- **Local-first**: All data stored locally in SQLite
- **Single-user**: No authentication complexity
- **Mobile-first design, desktop-primary usage**: Responsive layouts
- **Dark mode default**: High-quality poster visuals pop on dark backgrounds

---

## Milestone 0: Foundation Reset (COMPLETED)

**Goal**: Replace the demo counter app with Cinemarco's foundation.

### Tasks

- [x] **M0.1** Clear demo code from Domain.fs, Api.fs, State.fs, View.fs
- [x] **M0.2** Set up dark theme in index.html (`data-theme="dark"`)
- [x] **M0.3** Configure TailwindCSS for dark mode preference
- [x] **M0.4** Create basic app shell with navigation skeleton
- [x] **M0.5** Add health check endpoint to server
- [x] **M0.6** Verify build and tests pass

### Definition of Done
- [x] App loads with dark theme
- [x] Empty navigation shell renders
- [x] Backend health endpoint works

---

## Milestone 1: Core Domain Model (COMPLETED)

**Goal**: Define all domain types in `src/Shared/Domain.fs`.

### Tasks

- [x] **M1.1** Define core media types:
  - `MediaType` (Movie | Series)
  - `Movie` record with TMDB fields
  - `Series` record with season/episode structure
  - `Episode` record
  - `Season` record

- [x] **M1.2** Define watch tracking types:
  - `WatchStatus` (NotStarted | InProgress | Completed | Abandoned of episodeInfo)
  - `WatchProgress` record
  - `WatchSession` record (for named rewatches)
  - `EpisodeProgress` record

- [x] **M1.3** Define people types:
  - `Friend` record (real people you know)
  - `Contributor` record (actors, directors, etc.)
  - `ContributorRole` (Director | Actor | Writer | Cinematographer | Composer | etc.)

- [x] **M1.4** Define organization types:
  - `Tag` record
  - `Collection` record (franchises, custom lists)
  - `CollectionItem` record (ordered items in collection)

- [x] **M1.5** Define rating type:
  - `PersonalRating` DU (Outstanding | Entertaining | Decent | Meh | Waste)

- [x] **M1.6** Define library entry type:
  - `LibraryEntry` record (wraps movie/series with personal metadata)
  - `WhyAdded` record (recommendation source, context)

- [x] **M1.7** Define statistics types:
  - `WatchTimeStats` record
  - `YearInReview` record
  - `FilmographyProgress` record

### Files Modified
- `src/Shared/Domain.fs` - All domain types

### Definition of Done
- [x] All types compile
- [x] Types follow F# idioms (records, DUs)
- [x] Types are serializable by Fable.Remoting

---

## Milestone 2: Database Schema (COMPLETED)

**Goal**: Create SQLite schema and persistence layer.

### Tasks

- [x] **M2.1** Create migration system in Migrations.fs
- [x] **M2.2** Create movies table
- [x] **M2.3** Create series table
- [x] **M2.4** Create seasons table
- [x] **M2.5** Create episodes table
- [x] **M2.6** Create library_entries table (personal wrapper)
- [x] **M2.7** Create watch_sessions table
- [x] **M2.8** Create episode_progress table
- [x] **M2.9** Create friends table
- [x] **M2.10** Create contributors table
- [x] **M2.11** Create tags table
- [x] **M2.12** Create collections table
- [x] **M2.13** Create collection_items table
- [x] **M2.14** Create junction tables:
  - entry_tags
  - entry_friends
  - media_contributors
  - session_tags
  - session_friends

- [x] **M2.15** Create basic CRUD functions for each entity
- [x] **M2.16** Create indexes for common queries

### Files Modified
- `src/Server/Persistence.fs` - Database initialization and queries
- `src/Server/Migrations.fs` - Database migrations with 5 migration versions

### Definition of Done
- [x] Database initializes with all tables
- [x] Basic CRUD operations work
- [x] Indexes exist for performance

---

## Milestone 3: TMDB Integration (COMPLETED)

**Goal**: Search and import movie/series data from TMDB.

### Tasks

- [x] **M3.1** Create `src/Server/TmdbClient.fs`
- [x] **M3.2** Implement movie search endpoint
- [x] **M3.3** Implement movie details fetch (with poster, credits)
- [x] **M3.4** Implement series search endpoint
- [x] **M3.5** Implement series details fetch (seasons, episodes)
- [x] **M3.6** Implement contributor/cast extraction
- [x] **M3.7** Cache TMDB responses locally (reduce API calls)
- [x] **M3.8** Handle rate limiting gracefully

### API Contract Additions
```fsharp
type ITmdbApi = {
    searchMovies: query: string -> Async<TmdbSearchResult list>
    searchSeries: query: string -> Async<TmdbSearchResult list>
    getMovieDetails: tmdbId: int -> Async<Result<MovieDetails, string>>
    getSeriesDetails: tmdbId: int -> Async<Result<SeriesDetails, string>>
    getPersonFilmography: tmdbPersonId: int -> Async<Result<Filmography, string>>
}
```

### Files Modified
- `src/Server/TmdbClient.fs` - TMDB API client with caching and rate limiting
- `src/Shared/Api.fs` - Added TMDB search and detail endpoints
- `src/Shared/Domain.fs` - Added TmdbSearchResult, TmdbMovieDetails, TmdbSeriesDetails, etc.
- `src/Server/Persistence.fs` - Added tmdb_cache table for caching

### Definition of Done
- [x] Can search TMDB and display results
- [x] Can fetch full movie/series details
- [x] Credits (cast/crew) are extracted

---

## Milestone 4: Quick Capture (COMPLETED)

**Goal**: Implement the core "add to library" flow.

### Tasks

- [x] **M4.1** Create search input component with debouncing
- [x] **M4.2** Create search results dropdown with posters
- [x] **M4.3** Implement one-click add to library
- [x] **M4.4** Create "Why I Added" modal for optional note
- [x] **M4.5** Implement poster hover shine effect (CSS)
- [x] **M4.6** Add friend/tag selection during capture
- [x] **M4.7** Save to database on confirm

### Frontend Components
- `searchBar` - Main search input with debouncing
- `searchResultItem` - Dropdown with poster grid
- `quickAddModal` - Optional metadata capture with friends/tags
- `posterCard` - Reusable poster with hover shine effect

### Files Modified
- `src/Client/View.fs` - SearchBar, PosterCard, QuickAddModal components
- `src/Client/State.fs` - Search state, debouncing, AddItemToLibrary
- `src/Client/styles.css` - Poster shine effect CSS
- `src/Server/Api.fs` - libraryAddMovie, libraryAddSeries endpoints
- `src/Server/Persistence.fs` - insertLibraryEntryForMovie, insertLibraryEntryForSeries

### Definition of Done
- [x] Type to search, see TMDB results instantly
- [x] One click adds to library
- [x] Can optionally add "why I added" note
- [x] Posters have hover shine effect

---

## Milestone 5: Library View (COMPLETED)

**Goal**: Display and browse the personal library.

### Tasks

- [x] **M5.1** Create library grid view component
- [x] **M5.2** Implement filter by tag
- [x] **M5.3** Implement filter by watch status
- [x] **M5.4** Implement filter by rating
- [x] **M5.5** Implement sort options (date added, rating, title, year)
- [x] **M5.6** Create movie detail view
- [x] **M5.7** Create series detail view
- [ ] **M5.8** Implement pagination/infinite scroll (deferred - current client-side filtering handles reasonable library sizes)

### Frontend Components
- `LibraryGrid` - Main library display (in View.fs)
- `FilterBar` - Filter and sort controls (in View.fs)
- `MovieDetail` - Full movie page (in View.fs)
- `SeriesDetail` - Full series page with episode list (in View.fs)

### Files Modified
- `src/Client/Types.fs` - Added detail page routes and filter state types
- `src/Client/State.fs` - Added filter/sort state, detail view state, and messages
- `src/Client/View.fs` - Added FilterBar, MovieDetail, SeriesDetail components, and updated library grid

### Definition of Done
- [x] Can browse full library
- [x] Can filter by tag, status, rating
- [x] Can sort by various fields
- [x] Can view movie/series details

---

## Milestone 6: Friends & Tags Management (COMPLETED)

**Goal**: CRUD for friends and tags.

### Tasks

- [x] **M6.1** Create Friends list page
- [x] **M6.2** Create Friend detail page (shows watched together)
- [x] **M6.3** Create Friend add/edit modal
- [x] **M6.4** Create Tags list page
- [x] **M6.5** Create Tag detail page (shows tagged items)
- [x] **M6.6** Create Tag add/edit modal
- [ ] **M6.7** Implement bulk tagging (deferred)
- [x] **M6.8** Implement tag colors/icons

### API Contract Additions
```fsharp
type IFriendApi = {
    getAll: unit -> Async<Friend list>
    getById: int -> Async<Result<Friend, string>>
    create: CreateFriendRequest -> Async<Result<Friend, string>>
    update: UpdateFriendRequest -> Async<Result<Friend, string>>
    delete: int -> Async<Result<unit, string>>
    getWatchedWith: friendId: int -> Async<LibraryEntry list>
}

type ITagApi = {
    getAll: unit -> Async<Tag list>
    getById: int -> Async<Result<Tag, string>>
    create: CreateTagRequest -> Async<Result<Tag, string>>
    update: UpdateTagRequest -> Async<Result<Tag, string>>
    delete: int -> Async<Result<unit, string>>
    getTaggedEntries: tagId: int -> Async<LibraryEntry list>
}
```

### Files to Create/Modify
- `src/Client/Pages/Friends.fs` (new)
- `src/Client/Pages/FriendDetail.fs` (new)
- `src/Client/Pages/Tags.fs` (new)
- `src/Client/Pages/TagDetail.fs` (new)
- `src/Server/Api.fs` - Add friend and tag APIs

### Files Modified
- `src/Shared/Api.fs` - Added friend and tag CRUD API endpoints
- `src/Server/Persistence.fs` - Added getEntriesWatchedWithFriend and getEntriesWithTag
- `src/Server/Api.fs` - Implemented all friend and tag API operations
- `src/Client/Types.fs` - Added FriendDetailPage, TagDetailPage, FriendModalState, TagModalState
- `src/Client/State.fs` - Added all messages and update handlers for friends and tags management
- `src/Client/View.fs` - Added FriendsPage, FriendDetailPage, TagsPage, TagDetailPage, and modals

### Definition of Done
- [x] Can create/edit/delete friends
- [x] Can create/edit/delete tags
- [x] Can see what you've watched with each friend
- [x] Can see all items with a given tag

---

## Milestone 7: Watch Status & Progress (COMPLETED)

**Goal**: Track watch progress for movies and series.

### Tasks

- [x] **M7.1** Add watch status toggle to movie detail
- [x] **M7.2** Add "Mark as Watched" button
- [x] **M7.3** Implement series episode checkbox grid
- [x] **M7.4** Implement "Abandoned at S2E3" tracking
- [x] **M7.5** Add watch date recording
- [x] **M7.6** Create progress bar component
- [x] **M7.7** Show completion percentage on posters

### Frontend Components
- `watchStatusBadge` - Visual status indicator (in View.fs)
- `seasonEpisodeGrid` - Season/episode checkbox grid (in View.fs)
- `progressBar` - Visual progress indicator (in View.fs)

### Files Modified
- `src/Shared/Api.fs` - Added watch status API endpoints
- `src/Shared/Domain.fs` - Added AbandonRequest type
- `src/Server/Persistence.fs` - Added watch status and episode progress functions
- `src/Server/Api.fs` - Implemented watch status API handlers
- `src/Client/Types.fs` - Added AbandonModalState and modal types
- `src/Client/State.fs` - Added watch status messages and update handlers
- `src/Client/View.fs` - Added watch status components and updated detail pages

### Definition of Done
- [x] Can mark movies watched/unwatched
- [x] Can track episode-by-episode progress
- [x] Can mark series abandoned with location
- [x] Progress bars display correctly

---

## Milestone 8: Personal Rating (COMPLETED)

**Goal**: Implement the 5-tier rating system.

### Tasks

- [x] **M8.1** Create rating selector component (5 tiers)
- [x] **M8.2** Add rating to movie detail
- [x] **M8.3** Add rating to series detail
- [x] **M8.4** Display rating on poster cards
- [x] **M8.5** Filter library by rating
- [x] **M8.6** Add rating descriptions/tooltips

### Rating System
```
5 - Outstanding (Absolutely brilliant, stays with you)
4 - Entertaining (Strong craft, enjoyable, recommendable)
3 - Decent (Watchable, even if not life-changing)
2 - Meh (Didn't click, uninspiring)
1 - Waste (Waste of time)
```

### Frontend Components
- `ratingSelector` - 5-tier star selector with tooltips (inline in MovieDetail/View.fs and SeriesDetail/View.fs)
- `ratingStars` - Compact rating display on poster cards (in Components/Cards/View.fs)

### Files Modified
- `src/Client/Pages/MovieDetail/View.fs` - Rating selector with tooltips
- `src/Client/Pages/MovieDetail/State.fs` - SetRating message handler
- `src/Client/Pages/MovieDetail/Types.fs` - SetRating message type
- `src/Client/Pages/SeriesDetail/View.fs` - Rating selector with tooltips
- `src/Client/Pages/SeriesDetail/State.fs` - SetRating message handler
- `src/Client/Pages/SeriesDetail/Types.fs` - SetRating message type
- `src/Client/Pages/Library/View.fs` - Min rating filter in filter bar
- `src/Client/Pages/Library/State.fs` - Rating filter logic
- `src/Client/Components/Cards/View.fs` - Rating stars on poster cards
- `src/Client/App/State.fs` - SetRating API integration
- `src/Server/Api.fs` - librarySetRating endpoint
- `src/Server/Persistence.fs` - Rating persistence
- `src/Shared/Api.fs` - API contract for rating

### Definition of Done
- [x] Can rate any movie/series on 5-tier scale
- [x] Ratings display on detail pages and posters
- [x] Can filter by rating

---

## Milestone 9: Watch Sessions (Estimated: Day 10-11)

**Goal**: Named watch sessions for series rewatches.

### Tasks

- [ ] **M9.1** Create watch session model
- [ ] **M9.2** Create "New Watch Session" modal
- [ ] **M9.3** Track per-session episode progress
- [ ] **M9.4** Add session status (active/paused/completed)
- [ ] **M9.5** Allow tagging sessions
- [ ] **M9.6** Allow associating friends with sessions
- [ ] **M9.7** Display session list on series detail
- [ ] **M9.8** Create session detail view

### Use Case
"I'm rewatching Breaking Bad with my partner. I want to track this separately from when I watched it alone in 2015."

### Files to Create/Modify
- `src/Client/Pages/WatchSession.fs` (new)
- `src/Client/Components/SessionList.fs` (new)
- `src/Server/Api.fs` - Add session API

### Definition of Done
- Can create named watch sessions
- Sessions track progress independently
- Sessions can have tags and friends
- Session history visible on series

---

## Milestone 10: Collections & Franchises (COMPLETED)

**Goal**: Ordered collections for franchises and custom lists.

### Tasks

- [x] **M10.1** Create collection CRUD
- [x] **M10.2** Create collection detail page
- [x] **M10.3** Implement drag-and-drop ordering
- [x] **M10.4** Display collection progress (X of Y completed)
- [x] **M10.5** Create "Add to Collection" action (via API - UI to add items pending)
- [ ] **M10.6** Create pre-made franchise templates (MCU, Star Wars, etc.) - Deferred to future milestone

### Use Cases
- MCU watch order
- Star Wars timeline order
- Ghibli marathon
- Custom "Sci-Fi classics" list

### Files Created/Modified
- `src/Shared/Api.fs` - Added collection API endpoints
- `src/Server/Persistence.fs` - Added collection CRUD and item management
- `src/Server/Api.fs` - Implemented collection API handlers
- `src/Client/Pages/Collections/` - Types.fs, State.fs, View.fs
- `src/Client/Pages/CollectionDetail/` - Types.fs, State.fs, View.fs
- `src/Client/Components/CollectionModal/` - Types.fs, State.fs, View.fs
- `src/Client/App/Types.fs`, `State.fs`, `View.fs` - Wired up collections
- `src/Client/Common/Routing.fs` - Added CollectionDetailPage route

### Definition of Done
- [x] Can create ordered collections
- [x] Can reorder items via drag-and-drop
- [x] Progress bar shows completion
- [x] Can add items to collections (via API)

---

## Milestone 11: Contributor Tracking (Estimated: Day 12-13)

**Goal**: Track filmographies by director, actor, etc.

### Tasks

- [ ] **M11.1** Create contributor detail page
- [ ] **M11.2** Fetch filmography from TMDB
- [ ] **M11.3** Show "seen X of Y films" progress
- [ ] **M11.4** Highlight gaps (films not in library)
- [ ] **M11.5** Create "Discover" section (unseen films)
- [ ] **M11.6** Link contributors from movie/series detail

### Frontend Components
- `FilmographyGrid` - Shows all works by contributor
- `FilmographyProgress` - Visual completion tracker

### Files to Create/Modify
- `src/Client/Pages/ContributorDetail.fs` (new)
- `src/Client/Components/FilmographyGrid.fs` (new)

### Definition of Done
- Can view any contributor's filmography
- Shows completion percentage
- Highlights unseen works
- Links from movie/series details

---

## Milestone 12: Time Intelligence (COMPLETED)

**Goal**: Calculate and display watch time statistics.

### Tasks

- [x] **M12.1** Calculate total lifetime watch time
- [ ] **M12.2** Calculate time by tag (deferred - requires tag model changes)
- [x] **M12.3** Calculate time by year watched
- [x] **M12.4** Calculate time by franchise (via collections)
- [x] **M12.5** Show per-series time investment
- [x] **M12.6** Calculate backlog time estimate
- [x] **M12.7** Create stats dashboard component

### Stats Displayed
- Total lifetime watch time (movies + series)
- This year's watch time
- Time breakdown by year chart
- Time breakdown by rating
- Breakdown by movies vs series
- Per-series time investment (top series)
- Top collections by watched time
- Backlog stats (unwatched items + estimated time)

### Files Created/Modified
- `src/Shared/Domain.fs` - Added `SeriesTimeInvestment`, `TimeIntelligenceStats` types
- `src/Shared/Api.fs` - Added stats API endpoints
- `src/Server/Stats.fs` (new) - Pure statistics calculations
- `src/Server/Api.fs` - Implemented stats API handlers
- `src/Client/Pages/Stats/Types.fs` (new) - Page types
- `src/Client/Pages/Stats/State.fs` (new) - Page state
- `src/Client/Pages/Stats/View.fs` (new) - Dashboard UI
- `src/Client/App/Types.fs` - Added StatsPage model
- `src/Client/App/State.fs` - Added StatsMsg handler
- `src/Client/App/View.fs` - Added Stats page rendering

### Definition of Done
- [x] Dashboard shows all time statistics
- [x] Calculations are accurate
- [x] Backlog estimation works

---

## Milestone 13: Visual Timeline (Estimated: Day 14-15)

**Goal**: Chronological viewing history.

### Tasks

- [ ] **M13.1** Create timeline component
- [ ] **M13.2** Group by month/year
- [ ] **M13.3** Show posters in timeline
- [ ] **M13.4** Implement infinite scroll (pagination)
- [ ] **M13.5** Add date range filter
- [ ] **M13.6** Show watch session markers

### Frontend Components
- `Timeline` - Main chronological view
- `TimelineEntry` - Individual entry in timeline
- `DateRangeFilter` - Filter by time period

### Files to Create/Modify
- `src/Client/Pages/Timeline.fs` (new)
- `src/Client/Components/Timeline.fs` (new)

### Definition of Done
- Can browse history chronologically
- Shows posters in timeline
- Can filter by date range

---

## Milestone 14: Year-in-Review (Estimated: Day 15-16) ✅ COMPLETE

**Goal**: Annual statistics and visualization.

### Tasks

- [x] **M14.1** Calculate year statistics
- [x] **M14.2** Create year selector
- [x] **M14.3** Display hours watched in year
- [x] **M14.4** Display rating distribution
- [ ] **M14.5** Display top tags (deferred - requires tag feature)
- [x] **M14.6** Display franchises completed (collections structure in place)
- [x] **M14.7** Create visually appealing layout
- [ ] **M14.8** Store year summaries for quick access (deferred - not needed for MVP)

### Stats per Year
- Total hours watched
- Movies vs series breakdown
- Rating distribution (how many Brilliant, ReallyGood, etc.)
- Top tags
- Completed franchises/collections
- New contributors discovered
- Friends watched with most

### Files to Create/Modify
- `src/Server/YearInReview.fs` (new)
- `src/Client/Pages/YearInReview.fs` (new)

### Definition of Done
- Can view any year's statistics
- Visual breakdown is appealing
- All past years accessible

---

## Milestone 15: Home View (Estimated: Day 16-17)

**Goal**: Dashboard landing page.

### Tasks

- [ ] **M15.1** Create "Up Next" section (watchlist)
- [ ] **M15.2** Create "Recently Watched" section
- [ ] **M15.3** Create "Continue Watching" section (in-progress series)
- [ ] **M15.4** Add quick search access
- [ ] **M15.5** Add discovery insights ("One film from completing X")
- [ ] **M15.6** Implement responsive grid layout

### Home Sections
1. **Up Next** - Movies/series you want to watch
2. **Continue Watching** - In-progress series
3. **Recently Watched** - Last 10 watched
4. **Insights** - Completion suggestions

### Files to Create/Modify
- `src/Client/Pages/Home.fs` (new)
- `src/Client/Components/HomeSection.fs` (new)

### Definition of Done
- Home page shows all sections
- Sections populated with real data
- Responsive layout works

---

## Milestone 16: Relationship Graph (Estimated: Day 17-19)

**Goal**: Interactive network visualization.

### Tasks

- [ ] **M16.1** Evaluate visualization libraries (D3-force, Cytoscape.js)
- [ ] **M16.2** Create Fable bindings for chosen library
- [ ] **M16.3** Implement node types (movie, series, friend, contributor, tag)
- [ ] **M16.4** Implement edge connections
- [ ] **M16.5** Add physics simulation (floating, drift)
- [ ] **M16.6** Implement node selection/focusing
- [ ] **M16.7** Implement node dragging
- [ ] **M16.8** Show details panel on node select
- [ ] **M16.9** Optimize performance for large graphs

### Node Types
- Movies/Series (poster thumbnails)
- Friends (avatar or initials)
- Contributors (photo or initials)
- Tags (colored circles)
- Genres (colored circles)

### Interactions
- Select node → connected nodes orbit around
- Drag nodes → physics responds
- Pan/zoom canvas
- Filter which node types to show

### Files to Create/Modify
- `src/Client/Pages/Graph.fs` (new)
- `src/Client/Components/RelationshipGraph.fs` (new)
- `src/Client/Interop/D3Force.fs` (new) - JS interop

### Definition of Done
- Graph renders all nodes
- Physics simulation runs smoothly
- Can select/drag nodes
- Connected nodes highlight on selection

---

## Milestone 17: Trakt.tv Import (Estimated: Day 19-20)

**Goal**: Import viewing history from Trakt.tv.

### Tasks

- [ ] **M17.1** Create Trakt API client
- [ ] **M17.2** Implement OAuth flow (or API key)
- [ ] **M17.3** Import watched history
- [ ] **M17.4** Map Trakt entries to TMDB IDs
- [ ] **M17.5** Handle duplicates (don't re-import)
- [ ] **M17.6** Import ratings (map to 5-tier system)
- [ ] **M17.7** Create import progress UI

### Files to Create/Modify
- `src/Server/TraktClient.fs` (new)
- `src/Client/Pages/Import.fs` (new)

### Definition of Done
- Can connect to Trakt account
- Imports watched history
- Avoids duplicates
- Maps ratings appropriately

---

## Milestone 18: Visual Polish (Estimated: Day 20-21)

**Goal**: Implement delightful micro-interactions and polish.

### Tasks

- [ ] **M18.1** Poster hover shine effect (Steam-style)
- [ ] **M18.2** Smooth page transitions
- [ ] **M18.3** Loading skeletons for async content
- [ ] **M18.4** Progress bar fill animations
- [ ] **M18.5** Toast notifications
- [ ] **M18.6** Glass/frosted effects
- [ ] **M18.7** Mobile gesture support
- [ ] **M18.8** High-quality poster loading (lazy load + blur-up)

### CSS Effects to Implement
```css
/* Poster shine effect */
.poster:hover::after {
  background: linear-gradient(
    105deg,
    transparent 40%,
    rgba(255, 255, 255, 0.2) 45%,
    transparent 50%
  );
}

/* Glass effect */
.glass {
  background: rgba(255, 255, 255, 0.1);
  backdrop-filter: blur(10px);
}
```

### Files to Create/Modify
- `src/Client/styles.css` (new)
- Various component files for animations

### Definition of Done
- Hover effects feel satisfying
- Transitions are smooth
- Loading states are polished
- Mobile feels native

---

## Milestone 19: Testing & Documentation (Estimated: Day 21-22)

**Goal**: Comprehensive tests and documentation.

### Tasks

- [ ] **M19.1** Domain logic unit tests
- [ ] **M19.2** Validation tests
- [ ] **M19.3** Persistence integration tests
- [ ] **M19.4** API tests
- [ ] **M19.5** State management tests
- [ ] **M19.6** Update README with usage docs
- [ ] **M19.7** Add inline code documentation

### Test Coverage Goals
- Domain.fs: 90%
- Validation.fs: 100%
- Persistence.fs: 80%
- State.fs: 80%

### Files to Create/Modify
- `src/Tests/DomainTests.fs`
- `src/Tests/ValidationTests.fs`
- `src/Tests/PersistenceTests.fs`
- `src/Tests/StateTests.fs`
- `README.md`

### Definition of Done
- All tests pass
- `dotnet test` succeeds
- README explains usage

---

## Milestone 20: Deployment (Estimated: Day 22-23)

**Goal**: Production-ready Docker deployment.

### Tasks

- [ ] **M20.1** Update Dockerfile for Cinemarco
- [ ] **M20.2** Configure docker-compose with Tailscale
- [ ] **M20.3** Set up environment variables for API keys
- [ ] **M20.4** Create backup script for SQLite database
- [ ] **M20.5** Test deployment on home server
- [ ] **M20.6** Document deployment process

### Environment Variables Needed
```bash
TMDB_API_KEY=xxx
TRAKT_CLIENT_ID=xxx
TRAKT_CLIENT_SECRET=xxx
TS_AUTHKEY=xxx
```

### Files to Create/Modify
- `Dockerfile`
- `docker-compose.yml`
- `.env.example`
- `backup.sh`

### Definition of Done
- Docker build succeeds
- Container runs on home server
- Accessible via Tailscale
- Backups are scheduled

---

## Summary

| Milestone | Focus Area | Estimated Effort |
|-----------|-----------|------------------|
| M0 | Foundation Reset | Day 1 |
| M1 | Domain Model | Day 2-3 |
| M2 | Database Schema | Day 3-4 |
| M3 | TMDB Integration | Day 4-5 |
| M4 | Quick Capture | Day 5-6 |
| M5 | Library View | Day 6-7 |
| M6 | Friends & Tags | Day 7-8 |
| M7 | Watch Progress | Day 8-9 |
| M8 | Personal Rating | Day 9-10 |
| M9 | Watch Sessions | Day 10-11 |
| M10 | Collections | Day 11-12 |
| M11 | Contributor Tracking | Day 12-13 |
| M12 | Time Intelligence | Day 13-14 |
| M13 | Visual Timeline | Day 14-15 |
| M14 | Year-in-Review | Day 15-16 |
| M15 | Home View | Day 16-17 |
| M16 | Relationship Graph | Day 17-19 |
| M17 | Trakt Import | Day 19-20 |
| M18 | Visual Polish | Day 20-21 |
| M19 | Testing & Docs | Day 21-22 |
| M20 | Deployment | Day 22-23 |

## Critical Path

The following milestones are dependencies and must be completed in order:

```
M0 → M1 → M2 → M3 → M4 → M5
                      ↓
                    M6, M7, M8 (can parallelize)
                      ↓
                    M9 → M10 → M11
                      ↓
                    M12 → M13 → M14 → M15
                      ↓
                    M16, M17 (can parallelize)
                      ↓
                    M18 → M19 → M20
```

## Notes for Claude CLI Implementation

When implementing each milestone:

1. **Read relevant docs first** (especially `09-QUICK-REFERENCE.md`)
2. **Follow the development order**: Domain.fs → Api.fs → Validation.fs → Domain (server) → Persistence.fs → Api (server) → State.fs → View.fs → Tests
3. **Keep domain logic pure** - No I/O in `src/Server/Domain.fs`
4. **Use RemoteData pattern** for all async operations in frontend
5. **Validate early** - All validation at API boundary
6. **Test as you go** - Write tests alongside implementation
7. **Build frequently** - Run `dotnet build` after each significant change
