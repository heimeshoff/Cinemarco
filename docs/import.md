# Cinemarco Import Format

Use this guide to convert movie/series watch history into JSON for Cinemarco import. The JSON file can be imported at `/import-json` in the application.

---

## For AI/LLM Assistants

When a user asks you to convert their watch history into Cinemarco format:

1. **Parse the user's data** (CSV, text, spreadsheet, etc.)
2. **Look up IMDB IDs** for each title when possible (search IMDB or use known IDs)
3. **Convert ratings** using the Rating Conversion Guide below
4. **Format dates** as `YYYY-MM-DD`
5. **Suggest collections** based on patterns you detect:
   - Franchise sequences (trilogies, Marvel movies, etc.)
   - Same director or actor
   - Same genre or decade
   - Viewing occasions mentioned by user
6. **Output valid JSON** following the schema below

**Always prefer IMDB IDs** (`tt` prefix) over TMDB IDs for item identification - they're unique across movies and TV shows.

---

## JSON Structure

```json
{
  "items": [
    // ... movie and series entries
  ],
  "collections": [
    // ... optional: organize items into collections
  ]
}
```

The `items` array contains all movies and series. The optional `collections` array groups items into named collections.

## Movie Entry

```json
{
  "title": "The Matrix",
  "year": 1999,
  "type": "movie",
  "tmdb_id": 603,
  "imdb_id": "tt0133093",
  "watched": ["2020-03-15", "2023-08-20"],
  "rating": "Outstanding",
  "notes": "Mind-blowing sci-fi",
  "watched_with": ["John", "Jane"]
}
```

### Movie Fields

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `title` | Yes | string | Movie title |
| `type` | Yes | string | Must be `"movie"` |
| `id` | No | string | Custom ID for referencing in collections (e.g., `"matrix-1"`) |
| `year` | No | number | Release year (helps TMDB matching) |
| `tmdb_id` | No | number | TMDB ID for exact matching |
| `imdb_id` | No | string | IMDB ID (e.g., `"tt0133093"`) for exact matching |
| `watched` | No | array | ISO dates (`YYYY-MM-DD`) when watched. Multiple dates = rewatches |
| `rating` | No | string | See rating values below |
| `notes` | No | string | Personal notes about the movie |
| `watched_with` | No | array | Friend names (will match existing or create new) |
| `source` | No | string | Where this data came from (e.g., "Excel export") |

## Series Entry

```json
{
  "title": "Breaking Bad",
  "year": 2008,
  "type": "series",
  "tmdb_id": 1396,
  "imdb_id": "tt0903747",
  "seasons": [
    { "season": 1, "watched": "2022-01-15" },
    { "season": 2, "watched": "2022-02-20" }
  ],
  "episodes": [
    { "season": 3, "episode": 1, "watched": "2022-03-01" },
    { "season": 3, "episode": 2, "watched": "2022-03-02" }
  ],
  "rating": "Outstanding",
  "notes": "Best TV show ever",
  "watched_with": ["Sarah"]
}
```

### Series Fields

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `title` | Yes | string | Series title |
| `type` | Yes | string | Must be `"series"`, `"tv"`, or `"show"` |
| `id` | No | string | Custom ID for referencing in collections (e.g., `"breaking-bad"`) |
| `year` | No | number | First air year (helps TMDB matching) |
| `tmdb_id` | No | number | TMDB ID for exact matching |
| `imdb_id` | No | string | IMDB ID (e.g., `"tt0903747"`) for exact matching |
| `seasons` | No | array | Season-level watch data (marks all episodes in season) |
| `episodes` | No | array | Episode-level watch data (specific episodes only) |
| `rating` | No | string | See rating values below |
| `notes` | No | string | Personal notes about the series |
| `watched_with` | No | array | Friend names |
| `source` | No | string | Where this data came from |

### Season Object

```json
{
  "season": 1,
  "watched": "2022-01-15"
}
```

When you specify a season as watched, **all episodes in that season** will be marked as watched with the provided date.

### Episode Object

```json
{
  "season": 3,
  "episode": 5,
  "watched": "2022-03-15"
}
```

## Collections

Collections group related movies and series together. Items are referenced by their `id` field (which you assign in the items array) or by `imdb_id`/`tmdb_id`.

### Collection Entry

```json
{
  "name": "Nolan's Mind-Benders",
  "description": "Christopher Nolan films that mess with your perception of reality",
  "items": [
    { "imdb_id": "tt1375666" },
    { "imdb_id": "tt0816692" },
    { "imdb_id": "tt0482571" },
    { "id": "memento" }
  ]
}
```

### Collection Fields

| Field | Required | Type | Description |
|-------|----------|------|-------------|
| `name` | Yes | string | Collection name (e.g., "Marvel Cinematic Universe", "80s Classics") |
| `description` | No | string | Brief description of the collection theme |
| `items` | Yes | array | References to movies/series in this collection |

### Item Reference in Collections

Each item in a collection's `items` array can reference a movie/series in three ways:

| Reference Type | Example | Description |
|----------------|---------|-------------|
| `imdb_id` | `{ "imdb_id": "tt1375666" }` | Reference by IMDB ID (recommended) |
| `tmdb_id` + `type` | `{ "tmdb_id": 603, "type": "movie" }` | Reference by TMDB ID (requires type since IDs overlap) |
| `id` | `{ "id": "my-movie-id" }` | Reference by custom ID assigned in items array |

**Important**: When using `tmdb_id` in collection references, you MUST include `type` ("movie" or "series") because TMDB uses different ID spaces for movies and TV shows.

### Using Custom IDs

To use custom `id` references, assign an `id` field to items:

```json
{
  "items": [
    {
      "id": "matrix-1",
      "title": "The Matrix",
      "type": "movie",
      "imdb_id": "tt0133093",
      "watched": ["1999-03-31"]
    },
    {
      "id": "matrix-2",
      "title": "The Matrix Reloaded",
      "type": "movie",
      "imdb_id": "tt0234215",
      "watched": ["2003-05-15"]
    }
  ],
  "collections": [
    {
      "name": "The Matrix Trilogy",
      "items": [
        { "id": "matrix-1" },
        { "id": "matrix-2" }
      ]
    }
  ]
}
```

### Collection Examples

**Franchise Collection:**
```json
{
  "name": "Star Wars - Original Trilogy",
  "description": "Episodes IV, V, VI",
  "items": [
    { "imdb_id": "tt0076759" },
    { "imdb_id": "tt0080684" },
    { "imdb_id": "tt0086190" }
  ]
}
```

**Director Collection:**
```json
{
  "name": "Denis Villeneuve",
  "description": "Films by Denis Villeneuve",
  "items": [
    { "imdb_id": "tt1895587" },
    { "imdb_id": "tt0808279" },
    { "imdb_id": "tt3659388" },
    { "imdb_id": "tt1160419" }
  ]
}
```

**Mixed Movies and Series:**
```json
{
  "name": "DC Extended Universe",
  "items": [
    { "imdb_id": "tt0770828", "type": "movie" },
    { "imdb_id": "tt0451279", "type": "movie" },
    { "imdb_id": "tt8712204", "type": "series" }
  ]
}
```

**Thematic Collection:**
```json
{
  "name": "Time Loop Stories",
  "description": "Movies and shows featuring time loops",
  "items": [
    { "imdb_id": "tt0107048" },
    { "imdb_id": "tt1637688" },
    { "imdb_id": "tt2364235" },
    { "imdb_id": "tt7661390" }
  ]
}
```

## Rating Values

Ratings must be one of these exact strings (case-insensitive):

| Value | Meaning |
|-------|---------|
| `"Outstanding"` | Absolutely brilliant, stays with you |
| `"Entertaining"` | Strong craft, enjoyable, recommendable |
| `"Decent"` | Watchable, even if not life-changing |
| `"Meh"` | Didn't click, uninspiring |
| `"Waste"` | Waste of time |

### Rating Conversion Guide

When converting from other rating systems, use this mapping:

| Source Rating | Cinemarco Rating |
|--------------|------------------|
| 9-10/10, 5/5 stars, "loved it", "amazing", "masterpiece" | `"Outstanding"` |
| 7-8/10, 4/5 stars, "really good", "great", "excellent" | `"Entertaining"` |
| 5-6/10, 3/5 stars, "okay", "fine", "average" | `"Decent"` |
| 3-4/10, 2/5 stars, "boring", "meh", "disappointing" | `"Meh"` |
| 1-2/10, 1/5 stars, "terrible", "hated it", "avoid" | `"Waste"` |

If no rating is provided, leave out the `rating` field entirely.

## Date Format

Always use ISO 8601 format: `YYYY-MM-DD`

Examples:
- `"2023-08-15"` (August 15, 2023)
- `"2020-01-01"` (January 1, 2020)

## Complete Example

```json
{
  "items": [
    {
      "id": "inception",
      "title": "Inception",
      "year": 2010,
      "type": "movie",
      "imdb_id": "tt1375666",
      "watched": ["2010-07-20", "2015-03-12", "2023-01-05"],
      "rating": "Outstanding",
      "notes": "Gets better every rewatch",
      "watched_with": ["Mike"]
    },
    {
      "id": "interstellar",
      "title": "Interstellar",
      "year": 2014,
      "type": "movie",
      "imdb_id": "tt0816692",
      "watched": ["2014-11-10"],
      "rating": "Outstanding"
    },
    {
      "title": "The Office",
      "year": 2005,
      "type": "series",
      "imdb_id": "tt0386676",
      "seasons": [
        { "season": 1, "watched": "2018-06-01" },
        { "season": 2, "watched": "2018-06-15" },
        { "season": 3, "watched": "2018-07-01" }
      ],
      "rating": "Entertaining",
      "watched_with": ["Sarah", "Tom"]
    },
    {
      "title": "Stranger Things",
      "type": "series",
      "imdb_id": "tt4574334",
      "episodes": [
        { "season": 1, "episode": 1, "watched": "2023-10-01" },
        { "season": 1, "episode": 2, "watched": "2023-10-02" },
        { "season": 1, "episode": 3, "watched": "2023-10-03" }
      ]
    },
    {
      "title": "Barbie",
      "year": 2023,
      "type": "movie",
      "tmdb_id": 346698,
      "watched": ["2023-07-21"],
      "rating": "Entertaining"
    }
  ],
  "collections": [
    {
      "name": "Christopher Nolan",
      "description": "Mind-bending films by Christopher Nolan",
      "items": [
        { "id": "inception" },
        { "id": "interstellar" }
      ]
    },
    {
      "name": "Comfort Rewatches",
      "description": "Shows and movies I return to when I need a pick-me-up",
      "items": [
        { "imdb_id": "tt0386676" },
        { "imdb_id": "tt4574334" }
      ]
    }
  ]
}
```

## Tips

1. **If no year is known**, omit the `year` field - TMDB can still match by title
2. **If watch date is unknown**, use an empty array `[]` for `watched` or omit the field
3. **Multiple watch dates** for movies create separate watch sessions (rewatches)
4. **Use `seasons`** when you watched entire seasons at once
5. **Use `episodes`** for tracking individual episode progress
6. **You can combine** `seasons` and `episodes` in the same series entry
7. **Friend names** will be matched to existing friends (case-insensitive) or new friends will be created
8. **IMDB ID is recommended** for best matching accuracy - find it in IMDB URLs (e.g., `https://imdb.com/title/tt0133093/` → `"tt0133093"`)
9. **TMDB ID** also provides exact matching - find it on themoviedb.org URLs
10. **Matching priority**: TMDB ID → IMDB ID → title + year search
11. **Collections are optional** - you can import items without any collections
12. **Collection items reference the items array** - items must be defined in `items` before being referenced in `collections`
13. **Prefer IMDB IDs in collections** - they're unique across movies and series, unlike TMDB IDs
14. **Use custom `id` fields** when the same item appears in multiple collections for cleaner references
15. **Collection ideas**: director filmographies, franchises, decades, genres, viewing moods, awards lists

## Handling Edge Cases

- **Unknown watch date**: Omit `watched` or use empty array - item will be added to library without watch sessions
- **Partial season**: Use `episodes` array instead of `seasons` to specify exactly which episodes
- **Friend with nickname**: Use their full name as stored in Cinemarco, or their nickname
- **Duplicate entries**: If a movie/series already exists in your library, only new watch sessions will be added
- **Item in multiple collections**: Just reference it in each collection - the same movie/series can belong to many collections
- **Collection reference not found**: If a collection references an item not in the `items` array, it will be skipped with a warning
- **Empty collection**: Collections with no valid item references will be skipped

## Validation

The import will:
1. Parse the JSON and validate structure
2. Match each item with TMDB (you can resolve ambiguous matches)
3. Validate collection references point to existing items
4. Show a preview of what will be imported (items and collections)
5. Let you confirm before importing

Items that can't be matched with TMDB can be skipped during the preview step. Collections referencing unmatched items will show warnings.
