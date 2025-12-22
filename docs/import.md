# Cinemarco Import Format

Use this guide to convert movie/series watch history into JSON for Cinemarco import. The JSON file can be imported at `/import-json` in the application.

## JSON Structure

```json
{
  "items": [
    // ... movie and series entries
  ]
}
```

## Movie Entry

```json
{
  "title": "The Matrix",
  "year": 1999,
  "type": "movie",
  "tmdb_id": 603,
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
| `year` | No | number | Release year (helps TMDB matching) |
| `tmdb_id` | No | number | TMDB ID for exact matching |
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
| `year` | No | number | First air year (helps TMDB matching) |
| `tmdb_id` | No | number | TMDB ID for exact matching |
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
      "title": "Inception",
      "year": 2010,
      "type": "movie",
      "watched": ["2010-07-20", "2015-03-12", "2023-01-05"],
      "rating": "Outstanding",
      "notes": "Gets better every rewatch",
      "watched_with": ["Mike"]
    },
    {
      "title": "The Office",
      "year": 2005,
      "type": "series",
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
8. **TMDB ID** is optional but provides exact matching - find it on themoviedb.org URLs

## Handling Edge Cases

- **Unknown watch date**: Omit `watched` or use empty array - item will be added to library without watch sessions
- **Partial season**: Use `episodes` array instead of `seasons` to specify exactly which episodes
- **Friend with nickname**: Use their full name as stored in Cinemarco, or their nickname
- **Duplicate entries**: If a movie/series already exists in your library, only new watch sessions will be added

## Validation

The import will:
1. Parse the JSON and validate structure
2. Match each item with TMDB (you can resolve ambiguous matches)
3. Show a preview of what will be imported
4. Let you confirm before importing

Items that can't be matched with TMDB can be skipped during the preview step.
