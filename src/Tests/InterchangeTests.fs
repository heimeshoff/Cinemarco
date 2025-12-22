module InterchangeTests

open System
open Expecto
open Shared.Domain

// =====================================
// Test Helpers - Pure Functions for Interchange Logic
// =====================================

/// Module containing pure interchange logic that can be tested without I/O
module InterchangeLogic =
    /// Map Trakt rating (1-10) to Cinemarco PersonalRating (1-5)
    /// Based on TraktClient.mapTraktRating
    let mapTraktRating (traktRating: int) : PersonalRating =
        match traktRating with
        | r when r >= 9 -> Outstanding      // 9-10 -> Outstanding
        | r when r >= 7 -> Entertaining     // 7-8 -> Entertaining
        | r when r >= 5 -> Decent           // 5-6 -> Decent
        | r when r >= 3 -> Meh              // 3-4 -> Meh
        | _ -> Waste                        // 1-2 -> Waste

    /// Detect if a day is a "binge day" (more than 4 episodes watched)
    /// Based on TraktImport binge detection threshold
    let isBingeDay (episodesOnDay: int) : bool =
        episodesOnDay > 4

    /// Determine whether to use air date or watched date for an episode
    /// Based on TraktImport.importEpisodeWatchData logic
    let chooseEpisodeDate
        (episodesOnSameDay: int)
        (watchedAt: DateTime option)
        (airDate: DateTime option)
        : DateTime option =
        match watchedAt with
        | Some watched when episodesOnSameDay > 4 ->
            // Binge day: prefer air date if available
            airDate |> Option.orElse (Some watched)
        | watchedDate ->
            // Normal watching or no air date: use watched date
            watchedDate

    /// Group episodes by date for binge detection
    let groupEpisodesByDate (episodes: (int * int * DateTime option) list) : Map<DateTime option, int> =
        episodes
        |> List.groupBy (fun (_, _, dt) -> dt |> Option.map (fun d -> d.Date))
        |> List.map (fun (date, eps) -> date, eps.Length)
        |> Map.ofList

    /// Deduplicate watched episodes by keeping earliest watch date
    /// Based on TraktClient.getWatchedShowsWithEpisodes deduplication
    let deduplicateEpisodes (episodes: (int * int * DateTime option) list) : (int * int * DateTime option) list =
        episodes
        |> List.groupBy (fun (season, ep, _) -> (season, ep))
        |> List.map (fun ((s, e), watches) ->
            let earliestWatch =
                watches
                |> List.choose (fun (_, _, w) -> w)
                |> List.sort
                |> List.tryHead
            (s, e, earliestWatch))

    /// Parse ISO 8601 date format commonly used by APIs
    let tryParseIsoDate (str: string) : DateTime option =
        match DateTime.TryParse(str, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None) with
        | true, dt -> Some dt
        | false, _ -> None

    /// TMDB cache key generation logic
    let getCacheKey (cacheType: string) (id: int) : string =
        $"{cacheType}:{id}"

    let getSearchCacheKey (searchType: string) (query: string) : string =
        $"search:{searchType}:{query.ToLowerInvariant()}"

    let getSeasonCacheKey (seriesId: int) (seasonNumber: int) : string =
        $"tv:{seriesId}:season:{seasonNumber}"

// =====================================
// Rating Mapping Tests
// =====================================

[<Tests>]
let ratingMappingTests =
    testList "Trakt Rating Mapping" [
        testCase "rating 10 maps to Outstanding" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 10
            Expect.equal result Outstanding "10 should map to Outstanding"

        testCase "rating 9 maps to Outstanding" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 9
            Expect.equal result Outstanding "9 should map to Outstanding"

        testCase "rating 8 maps to Entertaining" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 8
            Expect.equal result Entertaining "8 should map to Entertaining"

        testCase "rating 7 maps to Entertaining" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 7
            Expect.equal result Entertaining "7 should map to Entertaining"

        testCase "rating 6 maps to Decent" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 6
            Expect.equal result Decent "6 should map to Decent"

        testCase "rating 5 maps to Decent" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 5
            Expect.equal result Decent "5 should map to Decent"

        testCase "rating 4 maps to Meh" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 4
            Expect.equal result Meh "4 should map to Meh"

        testCase "rating 3 maps to Meh" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 3
            Expect.equal result Meh "3 should map to Meh"

        testCase "rating 2 maps to Waste" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 2
            Expect.equal result Waste "2 should map to Waste"

        testCase "rating 1 maps to Waste" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 1
            Expect.equal result Waste "1 should map to Waste"

        testCase "rating 0 maps to Waste" <| fun () ->
            let result = InterchangeLogic.mapTraktRating 0
            Expect.equal result Waste "0 should map to Waste (edge case)"

        testCase "negative rating maps to Waste" <| fun () ->
            let result = InterchangeLogic.mapTraktRating -1
            Expect.equal result Waste "Negative should map to Waste (edge case)"
    ]

[<Tests>]
let ratingMappingPropertyTests =
    testList "Rating Mapping Properties" [
        testProperty "all valid Trakt ratings (1-10) produce valid Cinemarco ratings" <| fun () ->
            [1..10]
            |> List.forall (fun r ->
                let mapped = InterchangeLogic.mapTraktRating r
                [Waste; Meh; Decent; Entertaining; Outstanding] |> List.contains mapped)

        testProperty "higher Trakt ratings never produce lower Cinemarco ratings" <| fun () ->
            let traktRatings = [1..10]
            let mapped = traktRatings |> List.map (fun r -> (r, PersonalRating.toInt (InterchangeLogic.mapTraktRating r)))
            mapped
            |> List.pairwise
            |> List.forall (fun ((_, m1), (_, m2)) -> m2 >= m1)
    ]

// =====================================
// Binge Detection Tests
// =====================================

[<Tests>]
let bingeDetectionTests =
    testList "Binge Detection" [
        testCase "4 episodes is NOT a binge day" <| fun () ->
            let result = InterchangeLogic.isBingeDay 4
            Expect.isFalse result "4 episodes should not trigger binge detection"

        testCase "5 episodes IS a binge day" <| fun () ->
            let result = InterchangeLogic.isBingeDay 5
            Expect.isTrue result "5 episodes should trigger binge detection"

        testCase "1 episode is NOT a binge day" <| fun () ->
            let result = InterchangeLogic.isBingeDay 1
            Expect.isFalse result "1 episode should not be a binge day"

        testCase "0 episodes is NOT a binge day" <| fun () ->
            let result = InterchangeLogic.isBingeDay 0
            Expect.isFalse result "0 episodes should not be a binge day"

        testCase "10 episodes IS a binge day" <| fun () ->
            let result = InterchangeLogic.isBingeDay 10
            Expect.isTrue result "10 episodes should definitely be a binge day"
    ]

// =====================================
// Episode Date Selection Tests
// =====================================

[<Tests>]
let episodeDateSelectionTests =
    testList "Episode Date Selection (Binge Logic)" [
        let watchedDate = Some (DateTime(2024, 1, 15, 20, 0, 0))
        let airDate = Some (DateTime(2023, 9, 10))

        testCase "non-binge day uses watched date" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 3 watchedDate airDate
            Expect.equal result watchedDate "Non-binge should use watched date"

        testCase "binge day with air date uses air date" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 6 watchedDate airDate
            Expect.equal result airDate "Binge day should use air date"

        testCase "binge day without air date falls back to watched date" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 6 watchedDate None
            Expect.equal result watchedDate "Binge day without air date should fall back"

        testCase "no watched date returns None" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 6 None airDate
            Expect.isNone result "No watched date should return None"

        testCase "exactly 4 episodes uses watched date" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 4 watchedDate airDate
            Expect.equal result watchedDate "Exactly 4 episodes is not binge"

        testCase "exactly 5 episodes uses air date" <| fun () ->
            let result = InterchangeLogic.chooseEpisodeDate 5 watchedDate airDate
            Expect.equal result airDate "Exactly 5 episodes is binge"
    ]

// =====================================
// Episode Grouping Tests
// =====================================

[<Tests>]
let episodeGroupingTests =
    testList "Episode Grouping by Date" [
        testCase "groups episodes by date correctly" <| fun () ->
            let date1 = Some (DateTime(2024, 1, 1))
            let date2 = Some (DateTime(2024, 1, 2))
            let episodes = [
                (1, 1, date1)
                (1, 2, date1)
                (1, 3, date1)
                (1, 4, date2)
                (1, 5, date2)
            ]
            let result = InterchangeLogic.groupEpisodesByDate episodes
            Expect.equal (result |> Map.find date1) 3 "Day 1 should have 3 episodes"
            Expect.equal (result |> Map.find date2) 2 "Day 2 should have 2 episodes"

        testCase "handles episodes without dates" <| fun () ->
            let date1 = Some (DateTime(2024, 1, 1))
            let episodes = [
                (1, 1, date1)
                (1, 2, None)
                (1, 3, date1)
            ]
            let result = InterchangeLogic.groupEpisodesByDate episodes
            Expect.equal (result |> Map.find date1) 2 "Day 1 should have 2 episodes"
            Expect.equal (result |> Map.find None) 1 "None group should have 1 episode"

        testCase "empty list produces empty map" <| fun () ->
            let result = InterchangeLogic.groupEpisodesByDate []
            Expect.isEmpty result "Empty list should produce empty map"

        testCase "groups by date only, ignoring time" <| fun () ->
            let morningWatch = Some (DateTime(2024, 1, 1, 8, 0, 0))
            let eveningWatch = Some (DateTime(2024, 1, 1, 20, 0, 0))
            let episodes = [
                (1, 1, morningWatch)
                (1, 2, eveningWatch)
            ]
            let result = InterchangeLogic.groupEpisodesByDate episodes
            // Both should be grouped under the same date (ignoring time)
            // The implementation maps to d.Date, so different times on same day group together
            Expect.equal result.Count 1 "Different times same day should group together"
            let dateKey = Some (DateTime(2024, 1, 1).Date)
            Expect.equal (result |> Map.find dateKey) 2 "Should have 2 episodes on that date"
    ]

// =====================================
// Episode Deduplication Tests
// =====================================

[<Tests>]
let episodeDeduplicationTests =
    testList "Episode Deduplication" [
        testCase "deduplicates by season and episode number" <| fun () ->
            let earlier = Some (DateTime(2024, 1, 1))
            let later = Some (DateTime(2024, 1, 15))
            let episodes = [
                (1, 1, later)
                (1, 1, earlier)  // Duplicate, but watched earlier
                (1, 2, later)
            ]
            let result = InterchangeLogic.deduplicateEpisodes episodes
            Expect.equal result.Length 2 "Should dedupe to 2 unique episodes"

        testCase "keeps earliest watch date" <| fun () ->
            let earlier = Some (DateTime(2024, 1, 1))
            let later = Some (DateTime(2024, 1, 15))
            let episodes = [
                (1, 1, later)
                (1, 1, earlier)
            ]
            let result = InterchangeLogic.deduplicateEpisodes episodes
            let (_, _, watchDate) = result |> List.find (fun (s, e, _) -> s = 1 && e = 1)
            Expect.equal watchDate earlier "Should keep earlier watch date"

        testCase "handles episodes without watch dates" <| fun () ->
            let withDate = Some (DateTime(2024, 1, 1))
            let episodes = [
                (1, 1, None)
                (1, 1, withDate)
            ]
            let result = InterchangeLogic.deduplicateEpisodes episodes
            let (_, _, watchDate) = result |> List.head
            Expect.equal watchDate withDate "Should keep the dated watch"

        testCase "empty list returns empty" <| fun () ->
            let result = InterchangeLogic.deduplicateEpisodes []
            Expect.isEmpty result "Empty input should return empty output"

        testCase "no duplicates returns same list" <| fun () ->
            let episodes = [
                (1, 1, Some (DateTime(2024, 1, 1)))
                (1, 2, Some (DateTime(2024, 1, 2)))
                (2, 1, Some (DateTime(2024, 1, 3)))
            ]
            let result = InterchangeLogic.deduplicateEpisodes episodes
            Expect.equal result.Length 3 "No duplicates should return all"
    ]

// =====================================
// Date Parsing Tests
// =====================================

[<Tests>]
let dateParsingTests =
    testList "ISO Date Parsing" [
        testCase "parses ISO 8601 date" <| fun () ->
            let result = InterchangeLogic.tryParseIsoDate "2024-01-15"
            Expect.isSome result "Should parse ISO date"
            Expect.equal result.Value.Year 2024 "Year should be 2024"
            Expect.equal result.Value.Month 1 "Month should be January"
            Expect.equal result.Value.Day 15 "Day should be 15"

        testCase "parses ISO 8601 datetime with Z" <| fun () ->
            let result = InterchangeLogic.tryParseIsoDate "2024-01-15T20:30:00Z"
            Expect.isSome result "Should parse ISO datetime"

        testCase "parses ISO 8601 datetime with milliseconds" <| fun () ->
            let result = InterchangeLogic.tryParseIsoDate "2024-01-15T20:30:00.123Z"
            Expect.isSome result "Should parse ISO datetime with ms"

        testCase "returns None for invalid date" <| fun () ->
            let result = InterchangeLogic.tryParseIsoDate "not-a-date"
            Expect.isNone result "Should return None for invalid"

        testCase "returns None for empty string" <| fun () ->
            let result = InterchangeLogic.tryParseIsoDate ""
            Expect.isNone result "Should return None for empty"
    ]

// =====================================
// TMDB Cache Key Tests
// =====================================

[<Tests>]
let cacheKeyTests =
    testList "TMDB Cache Key Generation" [
        testCase "movie cache key" <| fun () ->
            let result = InterchangeLogic.getCacheKey "movie" 550
            Expect.equal result "movie:550" "Movie cache key format"

        testCase "tv cache key" <| fun () ->
            let result = InterchangeLogic.getCacheKey "tv" 1396
            Expect.equal result "tv:1396" "TV cache key format"

        testCase "person cache key" <| fun () ->
            let result = InterchangeLogic.getCacheKey "person" 287
            Expect.equal result "person:287" "Person cache key format"

        testCase "search cache key lowercases query" <| fun () ->
            let result = InterchangeLogic.getSearchCacheKey "movie" "Fight Club"
            Expect.equal result "search:movie:fight club" "Search should lowercase"

        testCase "season cache key format" <| fun () ->
            let result = InterchangeLogic.getSeasonCacheKey 1396 2
            Expect.equal result "tv:1396:season:2" "Season cache key format"
    ]

// =====================================
// Domain Type Tests
// =====================================

[<Tests>]
let domainTypeTests =
    testList "Domain Types" [
        testCase "PersonalRating toInt and fromInt roundtrip" <| fun () ->
            let ratings = [Waste; Meh; Decent; Entertaining; Outstanding]
            for rating in ratings do
                let intVal = PersonalRating.toInt rating
                let backToRating = PersonalRating.fromInt intVal
                Expect.equal backToRating (Some rating) $"Roundtrip for {rating}"

        testCase "PersonalRating.fromInt returns None for invalid" <| fun () ->
            let result = PersonalRating.fromInt 0
            Expect.isNone result "0 is not a valid rating"
            let result6 = PersonalRating.fromInt 6
            Expect.isNone result6 "6 is not a valid rating"

        testCase "TmdbMovieId create and value roundtrip" <| fun () ->
            let id = TmdbMovieId 550
            let value = match id with TmdbMovieId v -> v
            Expect.equal value 550 "Should extract value correctly"

        testCase "TmdbSeriesId create and value roundtrip" <| fun () ->
            let id = TmdbSeriesId 1396
            let value = match id with TmdbSeriesId v -> v
            Expect.equal value 1396 "Should extract value correctly"

        testCase "TmdbPersonId create and value roundtrip" <| fun () ->
            let id = TmdbPersonId 287
            let value = TmdbPersonId.value id
            Expect.equal value 287 "Should extract value correctly"

        testCase "MediaType has Movie and Series cases" <| fun () ->
            let movie: MediaType = Movie
            let series: MediaType = Series
            Expect.notEqual (box movie) (box series) "Movie and Series should be different"
    ]

// =====================================
// TraktHistoryItem Tests
// =====================================

[<Tests>]
let traktHistoryItemTests =
    testList "TraktHistoryItem Structure" [
        testCase "can create movie history item" <| fun () ->
            let item: TraktHistoryItem = {
                TmdbId = 550
                MediaType = Movie
                Title = "Fight Club"
                WatchedAt = Some (DateTime(2024, 1, 15))
                TraktRating = Some 9
            }
            Expect.equal item.TmdbId 550 "TMDB ID should be set"
            Expect.equal item.MediaType Movie "Media type should be Movie"

        testCase "can create series history item" <| fun () ->
            let item: TraktHistoryItem = {
                TmdbId = 1396
                MediaType = Series
                Title = "Breaking Bad"
                WatchedAt = Some (DateTime(2024, 1, 15))
                TraktRating = None
            }
            Expect.equal item.MediaType Series "Media type should be Series"
            Expect.isNone item.TraktRating "Rating can be None"

        testCase "WatchedAt can be None" <| fun () ->
            let item: TraktHistoryItem = {
                TmdbId = 550
                MediaType = Movie
                Title = "Fight Club"
                WatchedAt = None
                TraktRating = None
            }
            Expect.isNone item.WatchedAt "WatchedAt can be None (watchlist item)"
    ]

// =====================================
// TraktWatchedSeries Tests
// =====================================

[<Tests>]
let traktWatchedSeriesTests =
    testList "TraktWatchedSeries Structure" [
        testCase "can create watched series with episodes" <| fun () ->
            let series: TraktWatchedSeries = {
                TmdbId = 1396
                Title = "Breaking Bad"
                LastWatchedAt = Some (DateTime(2024, 1, 20))
                WatchedEpisodes = [
                    { SeasonNumber = 1; EpisodeNumber = 1; WatchedAt = Some (DateTime(2024, 1, 15)) }
                    { SeasonNumber = 1; EpisodeNumber = 2; WatchedAt = Some (DateTime(2024, 1, 16)) }
                ]
                TraktRating = Some 10
            }
            Expect.equal series.WatchedEpisodes.Length 2 "Should have 2 episodes"

        testCase "watched episodes track season and episode numbers" <| fun () ->
            let episode: TraktWatchedEpisode = {
                SeasonNumber = 3
                EpisodeNumber = 5
                WatchedAt = Some (DateTime(2024, 1, 15))
            }
            Expect.equal episode.SeasonNumber 3 "Season number should be 3"
            Expect.equal episode.EpisodeNumber 5 "Episode number should be 5"

        testCase "series can have empty episode list" <| fun () ->
            let series: TraktWatchedSeries = {
                TmdbId = 1396
                Title = "Breaking Bad"
                LastWatchedAt = None
                WatchedEpisodes = []
                TraktRating = None
            }
            Expect.isEmpty series.WatchedEpisodes "Can have empty episodes (watchlist)"
    ]

// =====================================
// TraktImportOptions Tests
// =====================================

[<Tests>]
let traktImportOptionsTests =
    testList "TraktImportOptions" [
        testCase "can set all import options" <| fun () ->
            let options: TraktImportOptions = {
                ImportWatchedMovies = true
                ImportWatchedSeries = true
                ImportWatchlist = true
                ImportRatings = true
            }
            Expect.isTrue options.ImportWatchedMovies "Movies option"
            Expect.isTrue options.ImportWatchedSeries "Series option"
            Expect.isTrue options.ImportWatchlist "Watchlist option"
            Expect.isTrue options.ImportRatings "Ratings option"

        testCase "can selectively enable options" <| fun () ->
            let options: TraktImportOptions = {
                ImportWatchedMovies = true
                ImportWatchedSeries = false
                ImportWatchlist = false
                ImportRatings = true
            }
            Expect.isTrue options.ImportWatchedMovies "Movies enabled"
            Expect.isFalse options.ImportWatchedSeries "Series disabled"
            Expect.isFalse options.ImportWatchlist "Watchlist disabled"
            Expect.isTrue options.ImportRatings "Ratings enabled"
    ]

// =====================================
// ImportStatus Tests
// =====================================

[<Tests>]
let importStatusTests =
    testList "ImportStatus Structure" [
        testCase "represents import progress" <| fun () ->
            let status: ImportStatus = {
                InProgress = true
                CurrentItem = Some "Breaking Bad"
                Completed = 5
                Total = 10
                Errors = []
            }
            Expect.isTrue status.InProgress "Should be in progress"
            Expect.equal status.CurrentItem (Some "Breaking Bad") "Current item"
            Expect.equal status.Completed 5 "Completed count"
            Expect.equal status.Total 10 "Total count"

        testCase "can track errors" <| fun () ->
            let status: ImportStatus = {
                InProgress = false
                CurrentItem = None
                Completed = 8
                Total = 10
                Errors = ["Failed to import Movie X"; "Failed to import Movie Y"]
            }
            Expect.equal status.Errors.Length 2 "Should track 2 errors"

        testCase "completed state" <| fun () ->
            let status: ImportStatus = {
                InProgress = false
                CurrentItem = None
                Completed = 10
                Total = 10
                Errors = []
            }
            Expect.isFalse status.InProgress "Not in progress when done"
            Expect.isNone status.CurrentItem "No current item when done"
    ]

// =====================================
// TraktSyncResult Tests
// =====================================

[<Tests>]
let traktSyncResultTests =
    testList "TraktSyncResult Structure" [
        testCase "tracks sync metrics" <| fun () ->
            let result: TraktSyncResult = {
                NewMovieWatches = 5
                NewEpisodeWatches = 20
                UpdatedItems = 3
                Errors = []
            }
            Expect.equal result.NewMovieWatches 5 "Movie watches"
            Expect.equal result.NewEpisodeWatches 20 "Episode watches"
            Expect.equal result.UpdatedItems 3 "Updated items"

        testCase "can have errors" <| fun () ->
            let result: TraktSyncResult = {
                NewMovieWatches = 3
                NewEpisodeWatches = 10
                UpdatedItems = 0
                Errors = ["Network timeout"; "Invalid response"]
            }
            Expect.equal result.Errors.Length 2 "Should have 2 errors"
    ]

// =====================================
// TraktSyncStatus Tests
// =====================================

[<Tests>]
let traktSyncStatusTests =
    testList "TraktSyncStatus" [
        testCase "authenticated with sync history" <| fun () ->
            let status: TraktSyncStatus = {
                IsAuthenticated = true
                LastSyncAt = Some (DateTime(2024, 1, 15, 12, 0, 0))
                AutoSyncEnabled = true
            }
            Expect.isTrue status.IsAuthenticated "Should be authenticated"
            Expect.isSome status.LastSyncAt "Should have sync time"
            Expect.isTrue status.AutoSyncEnabled "Auto sync enabled"

        testCase "not authenticated" <| fun () ->
            let status: TraktSyncStatus = {
                IsAuthenticated = false
                LastSyncAt = None
                AutoSyncEnabled = false
            }
            Expect.isFalse status.IsAuthenticated "Not authenticated"
            Expect.isNone status.LastSyncAt "No sync history"
    ]

// =====================================
// Integration Scenario Tests
// =====================================

[<Tests>]
let integrationScenarioTests =
    testList "Integration Scenarios" [
        testCase "full binge day scenario" <| fun () ->
            // Simulate watching 6 episodes on the same day
            let bingeDate = DateTime(2024, 1, 15)
            let episodes = [
                (1, 1, Some bingeDate)
                (1, 2, Some bingeDate)
                (1, 3, Some bingeDate)
                (1, 4, Some bingeDate)
                (1, 5, Some bingeDate)
                (1, 6, Some bingeDate)
            ]

            let groupedByDate = InterchangeLogic.groupEpisodesByDate episodes
            let bingeGroupKey = Some bingeDate.Date
            let episodesOnBingeDay = groupedByDate |> Map.tryFind bingeGroupKey |> Option.defaultValue 0

            Expect.isTrue (InterchangeLogic.isBingeDay episodesOnBingeDay) "Should detect binge day"

            // Each episode should use air date if available
            let airDate = Some (DateTime(2023, 9, 10))
            let watchedDate = Some bingeDate
            let selectedDate = InterchangeLogic.chooseEpisodeDate episodesOnBingeDay watchedDate airDate
            Expect.equal selectedDate airDate "Should select air date on binge day"

        testCase "normal watching scenario" <| fun () ->
            // Watch 2 episodes per day over 3 days
            let day1 = DateTime(2024, 1, 15)
            let day2 = DateTime(2024, 1, 16)
            let day3 = DateTime(2024, 1, 17)
            let episodes = [
                (1, 1, Some day1)
                (1, 2, Some day1)
                (1, 3, Some day2)
                (1, 4, Some day2)
                (1, 5, Some day3)
                (1, 6, Some day3)
            ]

            let groupedByDate = InterchangeLogic.groupEpisodesByDate episodes
            for (date, count) in groupedByDate |> Map.toList do
                match date with
                | Some _ -> Expect.isFalse (InterchangeLogic.isBingeDay count) "Each day should not be binge"
                | None -> ()

        testCase "rating import scenario" <| fun () ->
            // Simulate importing rated movies
            let traktRatings = [(550, 9); (278, 10); (155, 7); (680, 5)]
            let mappedRatings =
                traktRatings
                |> List.map (fun (id, rating) -> (id, InterchangeLogic.mapTraktRating rating))

            Expect.equal (mappedRatings |> List.find (fun (id, _) -> id = 550) |> snd) Outstanding "9 -> Outstanding"
            Expect.equal (mappedRatings |> List.find (fun (id, _) -> id = 278) |> snd) Outstanding "10 -> Outstanding"
            Expect.equal (mappedRatings |> List.find (fun (id, _) -> id = 155) |> snd) Entertaining "7 -> Entertaining"
            Expect.equal (mappedRatings |> List.find (fun (id, _) -> id = 680) |> snd) Decent "5 -> Decent"

        testCase "rewatch deduplication scenario" <| fun () ->
            // User rewatched some episodes
            let firstWatch = Some (DateTime(2023, 6, 1))
            let rewatch = Some (DateTime(2024, 1, 15))
            let episodes = [
                (1, 1, firstWatch)
                (1, 1, rewatch)      // Rewatch
                (1, 2, firstWatch)
                (1, 3, rewatch)
            ]

            let deduplicated = InterchangeLogic.deduplicateEpisodes episodes
            Expect.equal deduplicated.Length 3 "Should have 3 unique episodes"

            // S01E01 should have earliest date
            let (_, _, ep1Date) = deduplicated |> List.find (fun (s, e, _) -> s = 1 && e = 1)
            Expect.equal ep1Date firstWatch "S01E01 should keep first watch date"
    ]
