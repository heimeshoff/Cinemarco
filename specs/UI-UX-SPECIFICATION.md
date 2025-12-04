# Cinemarco UI/UX Specification

This document specifies the user interface design, components, and interactions for Cinemarco.

## Design Principles

1. **Dark mode default** - Lets posters pop, reduces eye strain
2. **Poster-centric** - High-quality visuals are the hero
3. **Mobile-first, desktop-primary** - Works on phones, optimized for desktop
4. **Satisfying interactions** - Micro-animations that feel good
5. **Information density** - Show useful data without clutter
6. **Consistent patterns** - Same interactions throughout

---

## Color Palette

### DaisyUI Dark Theme Base

```css
/* Primary brand colors */
--primary: #7c3aed;       /* Purple - main actions */
--primary-focus: #6d28d9;
--primary-content: #ffffff;

--secondary: #db2777;     /* Pink - accents */
--secondary-focus: #be185d;
--secondary-content: #ffffff;

--accent: #14b8a6;        /* Teal - success states */
--accent-focus: #0d9488;
--accent-content: #ffffff;

/* Base colors */
--base-100: #1d232a;      /* Main background */
--base-200: #191e24;      /* Slightly darker */
--base-300: #15191e;      /* Card backgrounds */
--base-content: #a6adbb;  /* Text color */

/* State colors */
--success: #22c55e;
--warning: #f59e0b;
--error: #ef4444;
--info: #3b82f6;

/* Rating colors */
--rating-brilliant: #fbbf24;  /* Gold */
--rating-really-good: #22c55e; /* Green */
--rating-decent: #3b82f6;      /* Blue */
--rating-meh: #9ca3af;         /* Gray */
--rating-nope: #ef4444;        /* Red */
```

### Glass Effect

```css
.glass {
  background: rgba(255, 255, 255, 0.05);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
}
```

---

## Common Components

### Component Hierarchy

```
Page
├── SectionHeader (page title)
├── FilterChip row (filters)
└── GlassPanel (content sections)
    ├── SectionHeader (section title)
    ├── RemoteDataView (async data)
    │   ├── Loading → Skeleton/Spinner
    │   ├── Success → Content
    │   │   ├── EmptyState (if empty)
    │   │   └── Data display
    │   └── Failure → ErrorState
    └── GlassButton row (actions)
```

### Glass Variants - When to Use

| Variant | CSS Class | Usage |
|---------|-----------|-------|
| `GlassPanel.standard` | `glass` | Most content sections, cards, panels |
| `GlassPanel.strong` | `glass-strong` | Important/highlighted sections, hero areas, modals |
| `GlassPanel.subtle` | `glass-subtle` | Background panels, secondary content, nested panels |

**Decision Guide:**
- **Standard**: Default choice for any distinct content area
- **Strong**: Use when the section needs visual emphasis (featured content, primary action areas)
- **Subtle**: Use for supporting content or when nesting panels inside other glass containers

### GlassButton Variants - When to Use

| Variant | Visual | Usage |
|---------|--------|-------|
| `GlassButton.button` | Gray hover | Default action, neutral state |
| `GlassButton.success` | Green hover | Positive action (mark watched, add) |
| `GlassButton.successActive` | Green filled | Active success state (is watched) |
| `GlassButton.danger` | Red hover | Destructive action (delete, remove) |
| `GlassButton.primary` | Purple hover | Primary action (rate, favorite) |
| `GlassButton.primaryActive` | Purple filled | Active primary state (is rated) |
| `GlassButton.disabled` | Muted | Non-interactive state |

### Color Usage with DaisyUI

Use DaisyUI theme colors consistently:

| Purpose | DaisyUI Class | Example |
|---------|---------------|---------|
| Primary actions | `btn-primary`, `text-primary` | Save, Submit |
| Success states | `badge-success`, `alert-success` | Watched, Completed |
| Danger/warnings | `btn-error`, `alert-error` | Delete, Error |
| Info messages | `badge-info`, `alert-info` | In Progress |
| Neutral/ghost | `btn-ghost`, `text-base-content` | Cancel, secondary text |

**Rating Colors** (use CSS variables):
```css
--rating-outstanding: #fbbf24;   /* Gold */
--rating-entertaining: #22c55e;  /* Green */
--rating-decent: #3b82f6;        /* Blue */
--rating-meh: #9ca3af;           /* Gray */
--rating-waste: #ef4444;         /* Red */
```

### Animation Guidelines

**Transition Durations:**
- Hover effects: `150ms-200ms`
- Content changes: `200ms-300ms`
- Page transitions: `200ms-400ms`
- Progress bars: `500ms`

**Standard Transitions:**
```css
/* Hover effects */
transition: all 150ms ease-out;

/* Content fade */
transition: opacity 200ms ease-out;

/* Transform effects */
transition: transform 200ms ease-out, box-shadow 200ms ease-out;

/* Progress bars */
transition: width 500ms ease-out;
```

**Animation Patterns:**

1. **Hover lift** (cards, buttons):
```css
transform: translateY(-2px);
box-shadow: 0 10px 25px -5px rgb(0 0 0 / 0.3);
```

2. **Poster shine** (always include on poster cards):
```css
.poster-shine {
  background: linear-gradient(
    105deg,
    transparent 40%,
    rgba(255, 255, 255, 0.15) 45%,
    transparent 50%
  );
  opacity: 0;
  transition: opacity 300ms;
}
.poster-card:hover .poster-shine {
  opacity: 1;
}
```

3. **Skeleton loading**:
```css
@keyframes skeleton-shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

4. **Page enter/exit**:
```css
/* Enter */
opacity: 0 → 1
transform: translateY(10px) → translateY(0)

/* Exit */
opacity: 1 → 0
```

**Accessibility Note:**
Always respect `prefers-reduced-motion`:
```css
@media (prefers-reduced-motion: reduce) {
  * {
    animation-duration: 0.01ms !important;
    transition-duration: 0.01ms !important;
  }
}
```

---

## Typography

```css
/* Font stack */
font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;

/* Scale */
--text-xs: 0.75rem;    /* 12px */
--text-sm: 0.875rem;   /* 14px */
--text-base: 1rem;     /* 16px */
--text-lg: 1.125rem;   /* 18px */
--text-xl: 1.25rem;    /* 20px */
--text-2xl: 1.5rem;    /* 24px */
--text-3xl: 1.875rem;  /* 30px */
--text-4xl: 2.25rem;   /* 36px */
```

---

## Layout System

### Breakpoints

```css
/* TailwindCSS defaults */
sm: 640px   /* Mobile landscape */
md: 768px   /* Tablet */
lg: 1024px  /* Desktop */
xl: 1280px  /* Large desktop */
2xl: 1536px /* Extra large */
```

### Container

```css
.container {
  max-width: 1536px;
  margin: 0 auto;
  padding: 0 1rem;
}

@media (min-width: 640px) {
  .container { padding: 0 1.5rem; }
}

@media (min-width: 1024px) {
  .container { padding: 0 2rem; }
}
```

### Grid System

```fsharp
// Poster grids
"grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"

// Card grids
"grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6"

// Dashboard sections
"grid grid-cols-1 lg:grid-cols-3 gap-8"
```

---

## Navigation

### Desktop Sidebar

```
┌──────────────────────────────────────────────────────────────────┐
│ ┌──────────┐                                                     │
│ │ CINEMARCO│  ─────────────────[ Search... ]───────────────────  │
│ │          │                                                     │
│ ├──────────┤  ┌──────────────────────────────────────────────┐   │
│ │ 🏠 Home  │  │                                              │   │
│ │ 📚 Library│ │                                              │   │
│ │ 👥 Friends│ │            MAIN CONTENT AREA                 │   │
│ │ 🏷 Tags   │  │                                              │   │
│ │ 📁 Collect│ │                                              │   │
│ │ 📊 Stats  │  │                                              │   │
│ │ 📅 Timeline│ │                                              │   │
│ │ 🕸 Graph  │  │                                              │   │
│ │ ⬇ Import │  │                                              │   │
│ └──────────┘  └──────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────┘
```

### Mobile Bottom Navigation

```
┌────────────────────────────────────────┐
│                                        │
│            MAIN CONTENT                │
│                                        │
├────────────────────────────────────────┤
│  🏠    📚    🔍    📊    ☰             │
│ Home  Library Search Stats  Menu       │
└────────────────────────────────────────┘
```

### Navigation Component

```fsharp
let navigation model dispatch =
    Html.nav [
        prop.className "fixed left-0 top-0 h-full w-64 bg-base-200 border-r border-base-300 hidden lg:block"
        prop.children [
            // Logo
            Html.div [
                prop.className "p-6"
                prop.children [
                    Html.h1 [
                        prop.className "text-2xl font-bold text-primary"
                        prop.text "Cinemarco"
                    ]
                ]
            ]

            // Nav items
            Html.ul [
                prop.className "menu p-4 space-y-2"
                prop.children [
                    navItem "Home" Page.HomePage model.CurrentPage (fun _ -> dispatch (NavigateTo HomePage))
                    navItem "Library" Page.LibraryPage model.CurrentPage (fun _ -> dispatch (NavigateTo LibraryPage))
                    // ... more items
                ]
            ]
        ]
    ]
```

---

## Components

### Poster Card

The most important component - used everywhere.

```
┌─────────────────────┐
│                     │
│                     │
│    [POSTER IMAGE]   │
│                     │
│                     │
├─────────────────────┤
│ Movie Title         │
│ 2023 • ⭐ 4         │
│ ████████░░ 80%      │
└─────────────────────┘
```

```fsharp
let posterCard (entry: LibraryEntry) (dispatch: Msg -> unit) =
    let title, year, posterPath =
        match entry.Media with
        | LibraryMovie m -> m.Title, m.ReleaseDate, m.PosterPath
        | LibrarySeries s -> s.Name, s.FirstAirDate, s.PosterPath

    Html.div [
        prop.className "group relative cursor-pointer"
        prop.onClick (fun _ -> dispatch (NavigateTo (MovieDetailPage entry.Id)))
        prop.children [
            // Poster image with hover effect
            Html.div [
                prop.className "relative aspect-[2/3] rounded-lg overflow-hidden"
                prop.children [
                    Html.img [
                        prop.src (tmdbImageUrl posterPath)
                        prop.className "w-full h-full object-cover transition-transform duration-300 group-hover:scale-105"
                        prop.alt title
                    ]

                    // Hover shine effect
                    Html.div [
                        prop.className "absolute inset-0 opacity-0 group-hover:opacity-100 transition-opacity duration-300 pointer-events-none"
                        prop.style [
                            style.background "linear-gradient(105deg, transparent 40%, rgba(255,255,255,0.15) 45%, transparent 50%)"
                        ]
                    ]

                    // Status badge
                    watchStatusBadge entry.WatchStatus

                    // Rating badge
                    match entry.PersonalRating with
                    | Some rating -> ratingBadge rating
                    | None -> Html.none
                ]
            ]

            // Title and year
            Html.div [
                prop.className "mt-2"
                prop.children [
                    Html.h3 [
                        prop.className "font-medium text-sm truncate"
                        prop.text title
                    ]
                    Html.p [
                        prop.className "text-xs text-base-content/60"
                        prop.text (year |> Option.map (fun d -> d.Year.ToString()) |> Option.defaultValue "")
                    ]
                ]
            ]
        ]
    ]
```

### Poster Hover Shine Effect (CSS)

```css
/* Steam-style shine effect */
.poster-card {
  position: relative;
  overflow: hidden;
}

.poster-card::after {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 50%;
  height: 100%;
  background: linear-gradient(
    to right,
    transparent,
    rgba(255, 255, 255, 0.2),
    transparent
  );
  transform: skewX(-25deg);
  transition: left 0.5s ease-in-out;
}

.poster-card:hover::after {
  left: 150%;
}
```

### Watch Status Badge

```fsharp
let watchStatusBadge status =
    let (text, colorClass) =
        match status with
        | NotStarted -> None
        | InProgress _ -> Some ("In Progress", "badge-info")
        | Completed -> Some ("Watched", "badge-success")
        | Abandoned _ -> Some ("Dropped", "badge-error")

    match text with
    | None -> Html.none
    | Some (t, c) ->
        Html.span [
            prop.className $"absolute top-2 left-2 badge badge-sm {c}"
            prop.text t
        ]
```

### Rating Selector

5-tier rating with descriptions:

```
┌─────────────────────────────────────────────────────────────┐
│  Your Rating                                                │
│                                                             │
│  ○ Outstanding - Absolutely brilliant, stays with you      │
│  ○ Entertaining - Strong craft, enjoyable, recommendable   │
│  ○ Decent - Watchable, even if not life-changing           │
│  ○ Meh - Didn't click, uninspiring                         │
│  ○ Waste - Waste of time                                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

```fsharp
let ratingSelector (currentRating: PersonalRating option) (onRate: PersonalRating option -> unit) =
    let ratings = [
        Outstanding, "⭐", "Outstanding", "Absolutely brilliant, stays with you"
        Entertaining, "👍", "Entertaining", "Strong craft, enjoyable"
        Decent, "👌", "Decent", "Watchable"
        Meh, "😐", "Meh", "Didn't click"
        Waste, "👎", "Waste", "Waste of time"
    ]

    Html.div [
        prop.className "space-y-2"
        prop.children [
            for (rating, icon, label, description) in ratings do
                Html.label [
                    prop.className $"flex items-center gap-3 p-3 rounded-lg cursor-pointer transition-colors hover:bg-base-200 {if currentRating = Some rating then "bg-base-200 ring-2 ring-primary" else ""}"
                    prop.onClick (fun _ -> onRate (Some rating))
                    prop.children [
                        Html.span [ prop.className "text-xl"; prop.text icon ]
                        Html.div [
                            prop.children [
                                Html.span [ prop.className "font-medium"; prop.text label ]
                                Html.p [ prop.className "text-sm text-base-content/60"; prop.text description ]
                            ]
                        ]
                    ]
                ]
        ]
    ]
```

### Episode Grid

For tracking series progress:

```
┌──────────────────────────────────────────────────────────┐
│ Season 1                                    ████████ 8/8 │
│ ┌───┬───┬───┬───┬───┬───┬───┬───┐                       │
│ │ 1 │ 2 │ 3 │ 4 │ 5 │ 6 │ 7 │ 8 │                       │
│ │ ✓ │ ✓ │ ✓ │ ✓ │ ✓ │ ✓ │ ✓ │ ✓ │                       │
│ └───┴───┴───┴───┴───┴───┴───┴───┘                       │
│                                                          │
│ Season 2                                    ████░░░░ 4/8 │
│ ┌───┬───┬───┬───┬───┬───┬───┬───┐                       │
│ │ 1 │ 2 │ 3 │ 4 │ 5 │ 6 │ 7 │ 8 │                       │
│ │ ✓ │ ✓ │ ✓ │ ✓ │   │   │   │   │                       │
│ └───┴───┴───┴───┴───┴───┴───┴───┘                       │
└──────────────────────────────────────────────────────────┘
```

```fsharp
let episodeGrid (series: Series) (progress: EpisodeProgress list) dispatch =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            for season in series.Seasons do
                let seasonProgress =
                    progress
                    |> List.filter (fun p -> p.SeasonNumber = season.SeasonNumber)

                let watchedCount = seasonProgress |> List.filter (fun p -> p.IsWatched) |> List.length

                Html.div [
                    prop.className "space-y-3"
                    prop.children [
                        // Season header
                        Html.div [
                            prop.className "flex justify-between items-center"
                            prop.children [
                                Html.h3 [
                                    prop.className "font-medium"
                                    prop.text $"Season {season.SeasonNumber}"
                                ]
                                Html.span [
                                    prop.className "text-sm text-base-content/60"
                                    prop.text $"{watchedCount}/{season.EpisodeCount}"
                                ]
                            ]
                        ]

                        // Progress bar
                        Html.progress [
                            prop.className "progress progress-primary w-full"
                            prop.value watchedCount
                            prop.max season.EpisodeCount
                        ]

                        // Episode checkboxes
                        Html.div [
                            prop.className "flex flex-wrap gap-2"
                            prop.children [
                                for ep in 1 .. season.EpisodeCount do
                                    let isWatched =
                                        seasonProgress
                                        |> List.exists (fun p -> p.EpisodeNumber = ep && p.IsWatched)

                                    Html.button [
                                        prop.className $"w-10 h-10 rounded-lg border-2 flex items-center justify-center transition-colors {if isWatched then "bg-primary border-primary text-primary-content" else "border-base-300 hover:border-primary"}"
                                        prop.onClick (fun _ -> dispatch (ToggleEpisode (season.SeasonNumber, ep)))
                                        prop.children [
                                            Html.span [
                                                prop.className "text-sm"
                                                prop.text (if isWatched then "✓" else string ep)
                                            ]
                                        ]
                                    ]
                            ]
                        ]
                    ]
                ]
        ]
    ]
```

### Search Bar

Global search with instant results:

```
┌──────────────────────────────────────────────────────────┐
│ 🔍 Search movies and series...                           │
└──────────────────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────┐
│ ┌────────────────────────────────────────────────────┐   │
│ │ [POSTER] Inception (2010)                          │   │
│ │          Christopher Nolan • Sci-Fi               │   │
│ ├────────────────────────────────────────────────────┤   │
│ │ [POSTER] The Dark Knight (2008)                   │   │
│ │          Christopher Nolan • Action               │   │
│ ├────────────────────────────────────────────────────┤   │
│ │ [POSTER] Interstellar (2014)                      │   │
│ │          Christopher Nolan • Sci-Fi               │   │
│ └────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────┘
```

```fsharp
let searchBar model dispatch =
    Html.div [
        prop.className "relative"
        prop.children [
            // Input
            Html.div [
                prop.className "relative"
                prop.children [
                    Html.span [
                        prop.className "absolute left-3 top-1/2 -translate-y-1/2 text-base-content/40"
                        prop.text "🔍"
                    ]
                    Html.input [
                        prop.className "input input-bordered w-full pl-10"
                        prop.placeholder "Search movies and series..."
                        prop.value model.SearchQuery
                        prop.onChange (SearchQueryChanged >> dispatch)
                        prop.onFocus (fun _ -> dispatch OpenSearchResults)
                    ]
                ]
            ]

            // Results dropdown
            if model.ShowSearchResults then
                Html.div [
                    prop.className "absolute top-full left-0 right-0 mt-2 bg-base-200 rounded-lg shadow-2xl border border-base-300 max-h-96 overflow-y-auto z-50"
                    prop.children [
                        match model.SearchResults with
                        | Loading ->
                            Html.div [
                                prop.className "p-4 text-center"
                                prop.children [ Html.span [ prop.className "loading loading-spinner" ] ]
                            ]
                        | Success results ->
                            Html.ul [
                                prop.className "divide-y divide-base-300"
                                prop.children [
                                    for result in results do
                                        searchResultItem result dispatch
                                ]
                            ]
                        | Failure err ->
                            Html.div [
                                prop.className "p-4 text-error"
                                prop.text err
                            ]
                        | NotAsked ->
                            Html.div [
                                prop.className "p-4 text-base-content/60"
                                prop.text "Type to search..."
                            ]
                    ]
                ]
        ]
    ]
```

### Progress Bar

Visual progress indicator:

```fsharp
let progressBar (value: int) (max: int) (showLabel: bool) =
    let percentage = if max > 0 then float value / float max * 100.0 else 0.0

    Html.div [
        prop.className "flex items-center gap-2"
        prop.children [
            Html.div [
                prop.className "flex-1 h-2 bg-base-300 rounded-full overflow-hidden"
                prop.children [
                    Html.div [
                        prop.className "h-full bg-primary transition-all duration-500 ease-out"
                        prop.style [
                            style.width (length.percent percentage)
                        ]
                    ]
                ]
            ]
            if showLabel then
                Html.span [
                    prop.className "text-sm text-base-content/60 whitespace-nowrap"
                    prop.text $"{value}/{max}"
                ]
        ]
    ]
```

### Modal

```fsharp
let modal (isOpen: bool) (title: string) (onClose: unit -> unit) (content: ReactElement list) =
    Html.div [
        prop.className $"modal {if isOpen then "modal-open" else ""}"
        prop.children [
            Html.div [
                prop.className "modal-box bg-base-200 max-w-2xl"
                prop.children [
                    // Header
                    Html.div [
                        prop.className "flex justify-between items-center mb-4"
                        prop.children [
                            Html.h3 [ prop.className "text-xl font-bold"; prop.text title ]
                            Html.button [
                                prop.className "btn btn-sm btn-circle btn-ghost"
                                prop.onClick (fun _ -> onClose())
                                prop.text "✕"
                            ]
                        ]
                    ]

                    // Content
                    Html.div [
                        prop.className "space-y-4"
                        prop.children content
                    ]
                ]
            ]

            // Backdrop
            Html.div [
                prop.className "modal-backdrop bg-black/50"
                prop.onClick (fun _ -> onClose())
            ]
        ]
    ]
```

### Toast Notifications

```fsharp
let toast (toast: Toast) (onDismiss: unit -> unit) =
    let (icon, alertClass) =
        match toast.Type with
        | ToastSuccess -> "✓", "alert-success"
        | ToastError -> "✕", "alert-error"
        | ToastInfo -> "ℹ", "alert-info"
        | ToastWarning -> "⚠", "alert-warning"

    Html.div [
        prop.className $"alert {alertClass} shadow-lg"
        prop.children [
            Html.span [ prop.text icon ]
            Html.span [ prop.text toast.Message ]
            Html.button [
                prop.className "btn btn-sm btn-ghost"
                prop.onClick (fun _ -> onDismiss())
                prop.text "✕"
            ]
        ]
    ]

let toastContainer (toasts: Toast list) dispatch =
    Html.div [
        prop.className "toast toast-end toast-bottom z-50"
        prop.children [
            for t in toasts do
                toast t (fun () -> dispatch (DismissToast t.Id))
        ]
    ]
```

---

## Pages

### Home Page

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│  🔍 [Search movies and series...]                               │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ 💡 One film away from completing Denis Villeneuve        │   │
│  │    [Sicario] [Arrival] [Blade Runner 2049] [+3]         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Continue Watching                                              │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                               │
│  │     │ │     │ │     │ │     │                               │
│  │ S2E4│ │ S1E7│ │ S3E2│ │ S5E1│                               │
│  │     │ │     │ │     │ │     │                               │
│  └─────┘ └─────┘ └─────┘ └─────┘                               │
│  Breaking  Better   Shogun  The                                │
│  Bad       Call             Sopranos                           │
│                                                                 │
│  Up Next                                                        │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘               │
│                                                                 │
│  Recently Watched                                               │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘               │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Library Page

```
┌─────────────────────────────────────────────────────────────────┐
│  Library                                                        │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ [All ▼] [Status ▼] [Rating ▼] [Tags ▼] 🔍 Search...     │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘               │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  │     │ │     │ │     │ │     │ │     │ │     │               │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘ └─────┘               │
│                                                                 │
│  [Load More]                                                    │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Movie/Series Detail Page

```
┌─────────────────────────────────────────────────────────────────┐
│ ┌────────────────────────────────────────────────────────────┐  │
│ │                      BACKDROP IMAGE                        │  │
│ │                                                            │  │
│ └────────────────────────────────────────────────────────────┘  │
│                                                                 │
│  ┌─────────┐                                                    │
│  │         │  Inception                                         │
│  │ POSTER  │  2010 • 2h 28min • PG-13                          │
│  │         │                                                    │
│  │         │  ⭐ Your Rating: Really Good                       │
│  │         │  [Rate ▼]                                          │
│  └─────────┘                                                    │
│                                                                 │
│  [Mark as Watched] [Add to Collection] [+ Tag]                  │
│                                                                 │
│  Overview                                                       │
│  A thief who steals corporate secrets through the use of       │
│  dream-sharing technology is given the inverse task of          │
│  planting an idea into the mind of a C.E.O.                    │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Cast & Crew                                                    │
│  ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐ ┌─────┐                       │
│  │ 👤  │ │ 👤  │ │ 👤  │ │ 👤  │ │ 👤  │                       │
│  └─────┘ └─────┘ └─────┘ └─────┘ └─────┘                       │
│  DiCaprio Marion  Cillian Christopher ...                      │
│                   Cotillard Murphy   Nolan                     │
│                                                                 │
│  ─────────────────────────────────────────────────────────────  │
│                                                                 │
│  Why I Added                                                    │
│  Recommended by: Sarah                                          │
│  "She said it was mind-bending"                                │
│                                                                 │
│  Tags: mind-bending, sci-fi, favorites                         │
│  Watched with: Sarah, Mike                                      │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Year in Review Page

```
┌─────────────────────────────────────────────────────────────────┐
│  Year in Review                                                 │
│                                                                 │
│  [2023 ▼] [2022] [2021] [2020]                                  │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                         │   │
│  │               🎬 Your 2023 in Cinema                    │   │
│  │                                                         │   │
│  │         ┌─────────────────────────────────────┐         │   │
│  │         │                                     │         │   │
│  │         │     247 hours watched               │         │   │
│  │         │                                     │         │   │
│  │         └─────────────────────────────────────┘         │   │
│  │                                                         │   │
│  │    52 Movies        12 Series        148 Episodes       │   │
│  │                                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Rating Distribution                                            │
│  Outstanding ████████████████░░░░ 12                           │
│  Entertaining████████████████████ 24                           │
│  Decent      ██████████░░░░░░░░░░ 10                           │
│  Meh         ████░░░░░░░░░░░░░░░░ 4                            │
│  Waste       ██░░░░░░░░░░░░░░░░░░ 2                            │
│                                                                 │
│  Top Tags                                                       │
│  #sci-fi (18) #drama (15) #thriller (12)                       │
│                                                                 │
│  Completed Franchises                                           │
│  ✓ Lord of the Rings                                            │
│  ✓ Denis Villeneuve Filmography (9/9)                          │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Relationship Graph Page

```
┌─────────────────────────────────────────────────────────────────┐
│  Relationship Graph                                             │
│                                                                 │
│  [Movies ✓] [Series ✓] [Friends ✓] [Directors ✓] [Tags ✓]      │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │                                                         │   │
│  │                        ○ sci-fi                         │   │
│  │                      /   \                              │   │
│  │                  [Blade]  ○ Denis                       │   │
│  │                  Runner   Villeneuve                    │   │
│  │                 /    \      \                           │   │
│  │            [Dune]   [Arrival] ○ cozy                    │   │
│  │            /    \                                       │   │
│  │       👤 Sarah  [Inception]──○ mind-bending            │   │
│  │                  |                                      │   │
│  │              ○ Christopher                              │   │
│  │                Nolan                                    │   │
│  │                                                         │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
│  Selected: Inception (2010)                                     │
│  Connected: 3 tags, 2 friends, 5 contributors                  │
│  [View Details]                                                 │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Animations & Transitions

### Page Transitions

```css
.page-enter {
  opacity: 0;
  transform: translateY(10px);
}

.page-enter-active {
  opacity: 1;
  transform: translateY(0);
  transition: opacity 200ms ease-out, transform 200ms ease-out;
}

.page-exit {
  opacity: 1;
}

.page-exit-active {
  opacity: 0;
  transition: opacity 150ms ease-in;
}
```

### Progress Bar Fill

```css
.progress-fill {
  transition: width 500ms ease-out;
}
```

### Card Hover

```css
.card-hover {
  transition: transform 200ms ease-out, box-shadow 200ms ease-out;
}

.card-hover:hover {
  transform: translateY(-4px);
  box-shadow: 0 20px 25px -5px rgb(0 0 0 / 0.3);
}
```

### Skeleton Loading

```css
.skeleton {
  background: linear-gradient(
    90deg,
    rgba(255, 255, 255, 0.05) 0%,
    rgba(255, 255, 255, 0.1) 50%,
    rgba(255, 255, 255, 0.05) 100%
  );
  background-size: 200% 100%;
  animation: skeleton-shimmer 1.5s infinite;
}

@keyframes skeleton-shimmer {
  0% { background-position: 200% 0; }
  100% { background-position: -200% 0; }
}
```

---

## Responsive Behavior

### Mobile (< 640px)
- Single column layouts
- Bottom navigation
- Full-width cards
- Collapsed sidebars
- Touch-friendly targets (44px minimum)

### Tablet (640px - 1024px)
- 2-3 column grids
- Side navigation (collapsible)
- Floating action buttons

### Desktop (> 1024px)
- Fixed sidebar navigation
- Multi-column layouts
- Hover interactions
- Keyboard shortcuts

---

## Accessibility

1. **Keyboard navigation** - All interactive elements focusable
2. **ARIA labels** - Screen reader support
3. **Color contrast** - WCAG AA compliant
4. **Focus indicators** - Visible focus states
5. **Reduced motion** - Respect `prefers-reduced-motion`

```fsharp
// Example: Accessible button
Html.button [
    prop.className "btn btn-primary"
    prop.ariaLabel "Mark movie as watched"
    prop.onClick (fun _ -> dispatch MarkAsWatched)
    prop.children [
        Html.span [ prop.className "sr-only"; prop.text "Mark as watched" ]
        Html.span [ prop.text "✓ Watched" ]
    ]
]
```

---

## URL Routing & Deep Linking

### Requirement

Every page in the application must be directly accessible via a unique URL. Users must be able to:

1. **Bookmark any page** - Navigate directly to any movie, series, person, tag, collection, or other entity page
2. **Share URLs** - Copy and share a link to any specific page
3. **Use browser navigation** - Back/forward buttons must work correctly
4. **Refresh without losing state** - Refreshing the page must reload the same view

### URL Patterns

| Page | URL Pattern | Example |
|------|-------------|---------|
| Home | `/` | `/` |
| Library | `/library` | `/library` |
| Movie Detail | `/movie/:id` | `/movie/42` |
| Series Detail | `/series/:id` | `/series/17` |
| Friends List | `/friends` | `/friends` |
| Friend Detail | `/friend/:id` | `/friend/5` |
| Tags List | `/tags` | `/tags` |
| Tag Detail | `/tag/:id` | `/tag/12` |
| Collections List | `/collections` | `/collections` |
| Collection Detail | `/collection/:id` | `/collection/3` |
| Contributor (Person) | `/person/:id` | `/person/287` |
| Timeline | `/timeline` | `/timeline` |
| Statistics | `/stats` | `/stats` |
| Year in Review | `/year/:year` | `/year/2024` |
| Relationship Graph | `/graph` | `/graph` |
| Import | `/import` | `/import` |

### Router Implementation

```fsharp
/// Parse URL to Page
let parseUrl (segments: string list) : Page =
    match segments with
    | [] -> HomePage
    | [ "library" ] -> LibraryPage
    | [ "movie"; id ] -> MovieDetailPage (EntryId (int id))
    | [ "series"; id ] -> SeriesDetailPage (EntryId (int id))
    | [ "friends" ] -> FriendsPage
    | [ "friend"; id ] -> FriendDetailPage (FriendId (int id))
    | [ "tags" ] -> TagsPage
    | [ "tag"; id ] -> TagDetailPage (TagId (int id))
    | [ "collections" ] -> CollectionsPage
    | [ "collection"; id ] -> CollectionDetailPage (CollectionId (int id))
    | [ "person"; id ] -> ContributorDetailPage (ContributorId (int id))
    | [ "timeline" ] -> TimelinePage
    | [ "stats" ] -> StatsPage
    | [ "year"; year ] -> YearInReviewPage (int year)
    | [ "graph" ] -> GraphPage
    | [ "import" ] -> ImportPage
    | _ -> NotFoundPage

/// Generate URL from Page
let toUrl (page: Page) : string =
    match page with
    | HomePage -> "/"
    | LibraryPage -> "/library"
    | MovieDetailPage (EntryId id) -> $"/movie/{id}"
    | SeriesDetailPage (EntryId id) -> $"/series/{id}"
    | FriendsPage -> "/friends"
    | FriendDetailPage (FriendId id) -> $"/friend/{id}"
    | TagsPage -> "/tags"
    | TagDetailPage (TagId id) -> $"/tag/{id}"
    | CollectionsPage -> "/collections"
    | CollectionDetailPage (CollectionId id) -> $"/collection/{id}"
    | ContributorDetailPage (ContributorId id) -> $"/person/{id}"
    | TimelinePage -> "/timeline"
    | StatsPage -> "/stats"
    | YearInReviewPage year -> $"/year/{year}"
    | GraphPage -> "/graph"
    | ImportPage -> "/import"
    | NotFoundPage -> "/404"
```

### Navigation Behavior

1. **Internal links** - Use `NavigateTo` message, which updates both state and URL
2. **URL changes** - Listen to browser history events, parse URL, update state
3. **Initial load** - Parse URL on app start to determine initial page
4. **Invalid URLs** - Redirect to `NotFoundPage` with option to go home

### Query Parameters (Optional)

Some pages may support query parameters for filtering/state:

| Page | Query Parameters |
|------|------------------|
| Library | `?status=watched&tag=5&sort=date` |
| Timeline | `?year=2024&month=3` |
| Graph | `?center=movie:42&depth=2` |

---

## File Organization

```
src/Client/
├── App.fs                 # Main app entry
├── Types.fs              # Client-only types
├── Api.fs                # Fable.Remoting client
├── State.fs              # Model, Msg, update
├── View.fs               # Root view
├── Router.fs             # URL routing (parseUrl, toUrl, subscription)
│
├── Components/           # Reusable components
│   ├── PosterCard.fs
│   ├── SearchBar.fs
│   ├── RatingSelector.fs
│   ├── EpisodeGrid.fs
│   ├── ProgressBar.fs
│   ├── Modal.fs
│   ├── Toast.fs
│   ├── Navigation.fs
│   └── FilterBar.fs
│
├── Pages/                # Page components
│   ├── Home.fs
│   ├── Library.fs
│   ├── MovieDetail.fs
│   ├── SeriesDetail.fs
│   ├── Friends.fs
│   ├── FriendDetail.fs
│   ├── Tags.fs
│   ├── TagDetail.fs
│   ├── Collections.fs
│   ├── CollectionDetail.fs
│   ├── ContributorDetail.fs
│   ├── Timeline.fs
│   ├── Stats.fs
│   ├── YearInReview.fs
│   ├── Graph.fs
│   └── Import.fs
│
└── Styles/
    └── main.css          # Custom CSS (animations, effects)
```

---

## Notes for Implementation

1. **Start with components** - Build PosterCard, ProgressBar first
2. **Use DaisyUI classes** - Don't reinvent the wheel
3. **Test on mobile** - Check responsive behavior early
4. **Lazy load images** - Use intersection observer
5. **Debounce search** - 300ms delay on input
6. **Skeleton loading** - Show placeholders during load
7. **Error boundaries** - Handle failures gracefully
8. **Keyboard shortcuts** - Add for power users
9. **Implement routing early** - Set up Router.fs before building pages to ensure all pages are URL-addressable from the start
10. **Test deep links** - Verify each page can be accessed directly via URL and survives page refresh
