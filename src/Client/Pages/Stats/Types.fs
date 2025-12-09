module Pages.Stats.Types

open Common.Types
open Shared.Domain

type Model = {
    Stats: RemoteData<TimeIntelligenceStats>
}

type Msg =
    | LoadStats
    | StatsLoaded of Result<TimeIntelligenceStats, string>
    | ViewMovieDetail of entryId: EntryId * title: string
    | ViewSeriesDetail of entryId: EntryId * name: string
    | ViewCollection of collectionId: CollectionId * name: string

type ExternalMsg =
    | NoOp
    | NavigateToMovieDetail of entryId: EntryId * title: string
    | NavigateToSeriesDetail of entryId: EntryId * name: string
    | NavigateToCollection of collectionId: CollectionId * name: string

module Model =
    let empty = {
        Stats = NotAsked
    }
