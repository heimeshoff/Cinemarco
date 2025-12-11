module Pages.Stats.Types

open System
open Common.Types
open Shared.Domain

type Model = {
    Stats: RemoteData<TimeIntelligenceStats>
}

type Msg =
    | LoadStats
    | StatsLoaded of Result<TimeIntelligenceStats, string>
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | ViewCollection of collectionId: CollectionId * name: string

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | NavigateToCollection of collectionId: CollectionId * name: string

module Model =
    let empty = {
        Stats = NotAsked
    }
