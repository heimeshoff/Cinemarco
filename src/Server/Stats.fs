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

    // Calculate series minutes from actual episode watch data (not count * default runtime)
    // This gives accurate totals based on real episode runtimes
    let seriesMinutesTotal =
        match filterByYear with
        | Some year ->
            episodeWatchData
            |> List.filter (fun (_, _, watchedDate) -> watchedDate.Year = year)
            |> List.sumBy (fun (_, runtime, _) -> runtime)
        | None ->
            episodeWatchData
            |> List.sumBy (fun (_, runtime, _) -> runtime)

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

// =====================================
// Year-in-Review Calculations
// =====================================

/// Get available years that have watch data
let getAvailableYears
    (entries: LibraryEntry list)
    (movieSessions: MovieWatchSession list)
    (episodeWatchData: (EntryId * int * DateTime) list)
    : AvailableYears =

    // Years from movie sessions
    let movieYears =
        movieSessions
        |> List.map (fun s -> s.WatchedDate.Year)

    // Years from episode watches
    let episodeYears =
        episodeWatchData
        |> List.map (fun (_, _, date) -> date.Year)

    // Also consider DateLastWatched from entries for backwards compatibility
    let entryYears =
        entries
        |> List.choose (fun e -> e.DateLastWatched |> Option.map (fun d -> d.Year))

    let allYears =
        movieYears @ episodeYears @ entryYears
        |> List.distinct
        |> List.sort

    {
        Years = allYears
        EarliestYear = List.tryHead allYears
        LatestYear = List.tryLast allYears
    }

/// Calculate year-in-review statistics for a specific year
let calculateYearInReviewStats
    (year: int)
    (entries: LibraryEntry list)
    (getWatchedEpisodeCount: EntryId -> int)
    (movieSessions: MovieWatchSession list)
    (episodeWatchData: (EntryId * int * DateTime) list)
    (friends: Friend list)
    (friendsByEntry: EntryId -> FriendId list)
    (completedCollections: Collection list)
    : YearInReviewStats =

    // Build entry ID to entry lookup
    let entryById =
        entries
        |> List.map (fun e -> (e.Id, e))
        |> Map.ofList

    // Find movie sessions from this year
    let yearMovieSessions =
        movieSessions
        |> List.filter (fun s -> s.WatchedDate.Year = year)

    // Find episode watches from this year
    let yearEpisodeWatches =
        episodeWatchData
        |> List.filter (fun (_, _, date) -> date.Year = year)

    // Unique movie entry IDs watched this year
    let movieEntryIds =
        yearMovieSessions
        |> List.map (fun s -> s.EntryId)
        |> List.distinct

    // Unique series entry IDs with episodes watched this year
    let seriesEntryIds =
        yearEpisodeWatches
        |> List.map (fun (entryId, _, _) -> entryId)
        |> List.distinct

    // Movies watched this year (with their runtime)
    let moviesWatchedEntries =
        movieEntryIds
        |> List.choose (fun id -> Map.tryFind id entryById)

    let movieMinutes =
        yearMovieSessions
        |> List.choose (fun session ->
            Map.tryFind session.EntryId entryById
            |> Option.bind (fun entry ->
                match entry.Media with
                | LibraryMovie m -> m.RuntimeMinutes
                | _ -> None))
        |> List.sum

    // Series watched this year
    let seriesWatchedEntries =
        seriesEntryIds
        |> List.choose (fun id -> Map.tryFind id entryById)

    // Episode minutes this year
    let episodesWatched = List.length yearEpisodeWatches
    let seriesMinutes =
        yearEpisodeWatches
        |> List.sumBy (fun (_, runtime, _) -> runtime)

    // Rating distribution from entries watched this year
    let entriesWatchedThisYear =
        (moviesWatchedEntries @ seriesWatchedEntries)
        |> List.distinctBy (fun e -> e.Id)

    let ratingDistribution =
        entriesWatchedThisYear
        |> List.fold (fun dist entry ->
            RatingDistribution.add entry.PersonalRating dist
        ) RatingDistribution.empty

    // Calculate average rating
    let ratedEntries =
        entriesWatchedThisYear
        |> List.choose (fun e -> e.PersonalRating |> Option.map PersonalRating.toInt)

    let averageRating =
        if List.isEmpty ratedEntries then None
        else Some (List.averageBy float ratedEntries)

    // Top rated entries (4 or 5 stars)
    let topRated =
        entriesWatchedThisYear
        |> List.filter (fun e ->
            match e.PersonalRating with
            | Some Outstanding | Some Entertaining -> true
            | _ -> false)
        |> List.sortByDescending (fun e ->
            e.PersonalRating
            |> Option.map PersonalRating.toInt
            |> Option.defaultValue 0)
        |> List.truncate 10

    // Count watches per friend
    let friendIdToFriend =
        friends
        |> List.map (fun f -> (f.Id, f))
        |> Map.ofList

    let friendWatchCounts =
        entriesWatchedThisYear
        |> List.collect (fun entry -> friendsByEntry entry.Id)
        |> List.countBy id
        |> List.choose (fun (friendId, count) ->
            Map.tryFind friendId friendIdToFriend
            |> Option.map (fun friend -> { Friend = friend; WatchCount = count }))
        |> List.sortByDescending (fun fwc -> fwc.WatchCount)
        |> List.truncate 5

    let hasData =
        not (List.isEmpty yearMovieSessions && List.isEmpty yearEpisodeWatches)

    // Build a map of entryId -> watched date for movies
    let movieWatchDateByEntry =
        yearMovieSessions
        |> List.groupBy (fun s -> s.EntryId)
        |> List.map (fun (entryId, sessions) ->
            let maxDate = sessions |> List.map (fun s -> s.WatchedDate) |> List.max
            (entryId, maxDate))
        |> Map.ofList

    // All movies watched this year (sorted by watched date)
    let allMovies =
        moviesWatchedEntries
        |> List.sortBy (fun e ->
            movieWatchDateByEntry
            |> Map.tryFind e.Id
            |> Option.defaultValue System.DateTime.MinValue)

    // Build a map of entryId -> latest episode watched date from episode watch data
    let lastEpisodeWatchDateByEntry =
        episodeWatchData
        |> List.groupBy (fun (entryId, _, _) -> entryId)
        |> List.map (fun (entryId, episodes) ->
            let maxDate = episodes |> List.map (fun (_, _, date) -> date) |> List.max
            (entryId, maxDate))
        |> Map.ofList

    // All series watched this year with finished/abandoned flags
    // A series is "finished this year" if:
    // - All episodes have been watched (watchedCount >= totalEpisodes)
    // - The last episode was watched in this year (based on actual episode watch dates)
    // A series is "abandoned this year" if:
    // - WatchStatus is Abandoned
    // - The last episode was watched in this year
    let allSeries =
        seriesWatchedEntries
        |> List.map (fun entry ->
            let lastWatchInYear =
                lastEpisodeWatchDateByEntry
                |> Map.tryFind entry.Id
                |> Option.map (fun d -> d.Year = year)
                |> Option.defaultValue false
            let finishedThisYear =
                match entry.Media with
                | LibrarySeries series ->
                    let watchedCount = getWatchedEpisodeCount entry.Id
                    let allEpisodesWatched = watchedCount >= series.NumberOfEpisodes && series.NumberOfEpisodes > 0
                    allEpisodesWatched && lastWatchInYear
                | _ -> false
            let abandonedThisYear =
                match entry.WatchStatus with
                | Abandoned _ -> lastWatchInYear
                | _ -> false
            { Entry = entry; FinishedThisYear = finishedThisYear; AbandonedThisYear = abandonedThisYear })
        |> List.sortBy (fun s ->
            // Sort order: Finished (0), Normal (1), Abandoned (2), then alphabetically
            let categoryOrder =
                if s.FinishedThisYear then 0
                elif s.AbandonedThisYear then 2
                else 1
            let name =
                match s.Entry.Media with
                | LibrarySeries series -> series.Name
                | _ -> ""
            (categoryOrder, name))

    {
        Year = year
        TotalMinutes = movieMinutes + seriesMinutes
        MovieMinutes = movieMinutes
        SeriesMinutes = seriesMinutes
        MoviesWatched = List.length movieEntryIds
        SeriesWatched = List.length seriesEntryIds
        EpisodesWatched = episodesWatched
        RatingDistribution = ratingDistribution
        CollectionsCompleted = completedCollections
        TopFriends = friendWatchCounts
        TopRated = topRated
        AverageRating = averageRating
        HasData = hasData
        AllMovies = allMovies
        AllSeries = allSeries
    }
