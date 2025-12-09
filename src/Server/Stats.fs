module Stats

open System
open Shared.Domain

/// Calculate total runtime for a movie in minutes
let private movieMinutes (movie: Movie) : int =
    movie.RuntimeMinutes |> Option.defaultValue 0

/// Calculate total runtime for a series based on watched episodes
let private seriesMinutes (series: Series) (watchedEpisodeCount: int) : int =
    let episodeRuntime = series.EpisodeRunTimeMinutes |> Option.defaultValue 45
    watchedEpisodeCount * episodeRuntime

/// Calculate total runtime for a series (all episodes)
let private seriesTotalMinutes (series: Series) : int =
    let episodeRuntime = series.EpisodeRunTimeMinutes |> Option.defaultValue 45
    series.NumberOfEpisodes * episodeRuntime

/// Get the year an entry was watched (for grouping)
let private entryWatchedYear (entry: LibraryEntry) : int option =
    entry.DateLastWatched
    |> Option.map (fun d -> d.Year)

/// Check if entry was watched in given year
let private wasWatchedInYear (year: int) (entry: LibraryEntry) : bool =
    entry.DateLastWatched
    |> Option.map (fun d -> d.Year = year)
    |> Option.defaultValue false

/// Calculate watch time stats from library entries
let calculateWatchTimeStats
    (entries: LibraryEntry list)
    (getWatchedEpisodeCount: EntryId -> int)
    (movieSessions: MovieWatchSession list)
    (episodeWatchData: (EntryId * int * DateTime) list) // (entryId, episodeRuntime, watchedDate)
    (filterByYear: int option)
    : WatchTimeStats =

    // Filter entries by year if specified (for totals we still use DateLastWatched)
    let filteredEntries =
        match filterByYear with
        | Some year -> entries |> List.filter (wasWatchedInYear year)
        | None -> entries

    // Only count watched entries
    let watchedEntries =
        filteredEntries
        |> List.filter (fun e ->
            match e.WatchStatus with
            | Completed -> true
            | InProgress _ -> true
            | _ -> false)

    let movieMinutesTotal =
        watchedEntries
        |> List.choose (fun e ->
            match e.Media with
            | LibraryMovie m when e.WatchStatus = Completed -> Some (movieMinutes m)
            | _ -> None)
        |> List.sum

    let seriesMinutesTotal =
        watchedEntries
        |> List.choose (fun e ->
            match e.Media with
            | LibrarySeries s ->
                let watchedCount = getWatchedEpisodeCount e.Id
                Some (seriesMinutes s watchedCount)
            | _ -> None)
        |> List.sum

    // Build entry -> movie runtime lookup
    let movieRuntimeByEntry =
        entries
        |> List.choose (fun e ->
            match e.Media with
            | LibraryMovie m -> Some (e.Id, movieMinutes m)
            | _ -> None)
        |> Map.ofList

    // Calculate ByYear from actual watch sessions
    // For movies: each session counts as one watch of that movie's runtime
    let movieMinutesByYear =
        movieSessions
        |> List.choose (fun session ->
            match filterByYear with
            | Some year when session.WatchedDate.Year <> year -> None
            | _ ->
                movieRuntimeByEntry
                |> Map.tryFind session.EntryId
                |> Option.map (fun runtime -> (session.WatchedDate.Year, runtime)))
        |> List.groupBy fst
        |> List.map (fun (year, items) -> (year, items |> List.sumBy snd))

    // For series: each episode watched counts with its runtime
    let episodeMinutesByYear =
        episodeWatchData
        |> List.choose (fun (_, runtime, watchedDate) ->
            match filterByYear with
            | Some year when watchedDate.Year <> year -> None
            | _ -> Some (watchedDate.Year, runtime))
        |> List.groupBy fst
        |> List.map (fun (year, items) -> (year, items |> List.sumBy snd))

    // Combine movie and episode minutes by year
    let byYear =
        movieMinutesByYear @ episodeMinutesByYear
        |> List.groupBy fst
        |> List.map (fun (year, items) -> (year, items |> List.sumBy snd))
        |> Map.ofList

    let byRating =
        watchedEntries
        |> List.choose (fun e ->
            e.PersonalRating
            |> Option.map (fun rating ->
                let minutes =
                    match e.Media with
                    | LibraryMovie m -> movieMinutes m
                    | LibrarySeries s -> seriesMinutes s (getWatchedEpisodeCount e.Id)
                (rating, minutes)))
        |> List.groupBy fst
        |> List.map (fun (rating, items) -> (rating, items |> List.sumBy snd))
        |> Map.ofList

    {
        TotalMinutes = movieMinutesTotal + seriesMinutesTotal
        MovieMinutes = movieMinutesTotal
        SeriesMinutes = seriesMinutesTotal
        ByYear = byYear
        ByRating = byRating
    }

/// Calculate backlog stats (unwatched items)
let calculateBacklogStats (entries: LibraryEntry list) : BacklogStats =
    let unwatchedEntries =
        entries
        |> List.filter (fun e -> e.WatchStatus = NotStarted)

    let estimatedMinutes =
        unwatchedEntries
        |> List.sumBy (fun e ->
            match e.Media with
            | LibraryMovie m -> movieMinutes m
            | LibrarySeries s -> seriesTotalMinutes s)

    let oldestEntry =
        unwatchedEntries
        |> List.sortBy (fun e -> e.DateAdded)
        |> List.tryHead

    {
        TotalEntries = List.length unwatchedEntries
        EstimatedMinutes = estimatedMinutes
        OldestEntry = oldestEntry
    }

/// Calculate time investment for a series
let calculateSeriesTimeInvestment
    (entry: LibraryEntry)
    (series: Series)
    (watchedEpisodeCount: int)
    : SeriesTimeInvestment =

    let totalMinutes = seriesTotalMinutes series
    let watchedMinutes = seriesMinutes series watchedEpisodeCount
    let remainingMinutes = max 0 (totalMinutes - watchedMinutes)
    let completionPercentage =
        if totalMinutes > 0 then
            (float watchedMinutes / float totalMinutes) * 100.0
        else
            0.0

    {
        Entry = entry
        Series = series
        TotalMinutes = totalMinutes
        WatchedMinutes = watchedMinutes
        RemainingMinutes = remainingMinutes
        CompletionPercentage = completionPercentage
    }

/// Get top series by time investment
let getTopSeriesByTime
    (entries: LibraryEntry list)
    (getWatchedEpisodeCount: EntryId -> int)
    (limit: int)
    : SeriesTimeInvestment list =

    entries
    |> List.choose (fun entry ->
        match entry.Media with
        | LibrarySeries series ->
            let watchedCount = getWatchedEpisodeCount entry.Id
            if watchedCount > 0 then
                Some (calculateSeriesTimeInvestment entry series watchedCount)
            else
                None
        | _ -> None)
    |> List.sortByDescending (fun s -> s.WatchedMinutes)
    |> List.truncate limit

/// Format minutes as human-readable string
let formatMinutes (minutes: int) : string =
    let hours = minutes / 60
    let mins = minutes % 60
    if hours > 0 then
        if mins > 0 then $"{hours}h {mins}m"
        else $"{hours}h"
    else
        $"{mins}m"

/// Format minutes as days, hours, minutes
let formatMinutesLong (minutes: int) : string =
    let days = minutes / (60 * 24)
    let hours = (minutes % (60 * 24)) / 60
    let mins = minutes % 60

    let pluralize count singular = if count > 1 then singular + "s" else singular
    let dayStr = pluralize days "day"
    let hourStr = pluralize hours "hour"
    let minStr = pluralize mins "minute"

    let parts =
        [
            if days > 0 then yield $"{days} {dayStr}"
            if hours > 0 then yield $"{hours} {hourStr}"
            if mins > 0 && days = 0 then yield $"{mins} {minStr}"
        ]

    match parts with
    | [] -> "0 minutes"
    | [single] -> single
    | _ -> String.Join(", ", parts)
