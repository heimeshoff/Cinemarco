module Common.Routing

open System
open Shared.Domain

/// View mode for Year in Review page
type YearInReviewViewMode =
    | Overview
    | MoviesOnly
    | SeriesOnly

/// Page routes for navigation - uses semantic slugs
type Page =
    | HomePage
    | LibraryPage
    | MovieDetailPage of slug: string
    | SeriesDetailPage of slug: string
    | SessionDetailPage of slug: string
    | FriendsPage
    | FriendDetailPage of slug: string
    | ContributorsPage
    | ContributorDetailPage of slug: string * TmdbPersonId option  // Tracked use slug only, untracked include TMDB ID
    | CollectionsPage
    | CollectionDetailPage of slug: string
    | StatsPage
    | YearInReviewPage of year: int option * viewMode: YearInReviewViewMode  // Optional year, defaults to current year
    | TimelinePage
    | GraphPage
    | ImportPage
    | CachePage
    | NotFoundPage

module Page =
    /// Extract TMDB ID from the end of a slug (e.g., "tom-hanks-31" -> Some (31, "tom-hanks"))
    /// Returns the TMDB ID and the slug without the ID
    let private extractTmdbIdAndSlug (slugWithId: string) : (int * string) option =
        let lastDash = slugWithId.LastIndexOf('-')
        if lastDash > 0 && lastDash < slugWithId.Length - 1 then
            match Int32.TryParse(slugWithId.Substring(lastDash + 1)) with
            | true, id -> Some (id, slugWithId.Substring(0, lastDash))
            | _ -> None
        else
            None

    /// Generate URL from page - uses semantic slugs
    let toUrl = function
        | HomePage -> "/"
        | LibraryPage -> "/library"
        | MovieDetailPage slug -> $"/movie/{slug}"
        | SeriesDetailPage slug -> $"/series/{slug}"
        | SessionDetailPage slug -> $"/session/{slug}"
        | FriendsPage -> "/friends"
        | FriendDetailPage slug -> $"/friend/{slug}"
        | ContributorsPage -> "/contributors"
        | ContributorDetailPage (slug, Some (TmdbPersonId id)) -> $"/contributor/{slug}-{id}"  // Untracked: include ID
        | ContributorDetailPage (slug, None) -> $"/contributor/{slug}"  // Tracked: slug only
        | CollectionsPage -> "/collections"
        | CollectionDetailPage slug -> $"/collection/{slug}"
        | StatsPage -> "/stats"
        | YearInReviewPage (Some year, Overview) -> $"/year-in-review/{year}"
        | YearInReviewPage (Some year, MoviesOnly) -> $"/year-in-review/{year}/movies"
        | YearInReviewPage (Some year, SeriesOnly) -> $"/year-in-review/{year}/series"
        | YearInReviewPage (None, _) -> "/year-in-review"
        | TimelinePage -> "/timeline"
        | GraphPage -> "/graph"
        | ImportPage -> "/import"
        | CachePage -> "/cache"
        | NotFoundPage -> "/404"

    /// Parse URL path to Page
    let fromUrl (path: string) : Page =
        let path = path.TrimEnd('/')
        let segments = path.Split('/') |> Array.filter (fun s -> s <> "") |> Array.toList

        match segments with
        | [] -> HomePage
        | ["library"] -> LibraryPage
        | ["movie"; slug] -> MovieDetailPage slug
        | ["series"; slug] -> SeriesDetailPage slug
        | ["session"; slug] -> SessionDetailPage slug
        | ["friends"] -> FriendsPage
        | ["friend"; slug] -> FriendDetailPage slug
        | ["contributors"] -> ContributorsPage
        | ["contributor"; slugOrSlugWithId] ->
            // Try to extract TMDB ID from the end (e.g., "tom-hanks-31")
            // If no ID found, it's a tracked contributor accessed by slug only
            match extractTmdbIdAndSlug slugOrSlugWithId with
            | Some (id, slug) -> ContributorDetailPage (slug, Some (TmdbPersonId id))  // Untracked: slug + ID
            | None -> ContributorDetailPage (slugOrSlugWithId, None)  // Tracked: slug only
        | ["collections"] -> CollectionsPage
        | ["collection"; slug] -> CollectionDetailPage slug
        | ["stats"] -> StatsPage
        | ["year-in-review"] -> YearInReviewPage (None, Overview)
        | ["year-in-review"; yearStr] ->
            match Int32.TryParse yearStr with
            | true, year -> YearInReviewPage (Some year, Overview)
            | _ -> NotFoundPage
        | ["year-in-review"; yearStr; "movies"] ->
            match Int32.TryParse yearStr with
            | true, year -> YearInReviewPage (Some year, MoviesOnly)
            | _ -> NotFoundPage
        | ["year-in-review"; yearStr; "series"] ->
            match Int32.TryParse yearStr with
            | true, year -> YearInReviewPage (Some year, SeriesOnly)
            | _ -> NotFoundPage
        | ["timeline"] -> TimelinePage
        | ["graph"] -> GraphPage
        | ["import"] -> ImportPage
        | ["cache"] -> CachePage
        | ["404"] -> NotFoundPage
        | _ -> NotFoundPage

    let toString = function
        | HomePage -> "Home"
        | LibraryPage -> "Library"
        | MovieDetailPage _ -> "Movie"
        | SeriesDetailPage _ -> "Series"
        | SessionDetailPage _ -> "Session"
        | FriendsPage -> "Friends"
        | FriendDetailPage _ -> "Friend"
        | ContributorsPage -> "Contributors"
        | ContributorDetailPage _ -> "Contributor"
        | CollectionsPage -> "Collections"
        | CollectionDetailPage _ -> "Collection"
        | StatsPage -> "Stats"
        | YearInReviewPage _ -> "Year in Review"
        | TimelinePage -> "Timeline"
        | GraphPage -> "Graph"
        | ImportPage -> "Import"
        | CachePage -> "Cache"
        | NotFoundPage -> "Not Found"

/// Browser history operations
module Router =
    open Browser.Dom
    open Fable.Core.JsInterop

    /// Get current URL path
    let getCurrentPath () =
        window.location.pathname

    /// Push a new URL to browser history
    let pushUrl (url: string) =
        window.history.pushState(null, "", url)

    /// Replace current URL in browser history (doesn't add to history stack)
    let replaceUrl (url: string) =
        window.history.replaceState(null, "", url)

    /// Navigate to a page and update URL
    let navigateTo (page: Page) =
        let url = Page.toUrl page
        pushUrl url
        page

    /// Parse current URL and return the page
    let parseCurrentUrl () =
        getCurrentPath () |> Page.fromUrl

    /// Set up popstate event listener for back/forward navigation
    let onUrlChange (callback: Page -> unit) =
        window.addEventListener("popstate", fun _ ->
            let page = parseCurrentUrl ()
            callback page
        )
