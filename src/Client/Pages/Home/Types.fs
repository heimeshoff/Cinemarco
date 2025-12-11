module Pages.Home.Types

open System
open Common.Types
open Shared.Domain

type TraktSyncState =
    | SyncIdle
    | SyncChecking
    | Syncing
    | SyncComplete of TraktSyncResult
    | SyncError of string

type TmdbHealthState =
    | TmdbNotChecked
    | TmdbChecking
    | TmdbConnected
    | TmdbError of string

type Model = {
    Library: RemoteData<LibraryEntry list>
    TraktSync: TraktSyncState
    TraktStatus: TraktSyncStatus option  // Cached status for display
    TmdbHealth: TmdbHealthState
}

type Msg =
    | LoadLibrary
    | LibraryLoaded of Result<LibraryEntry list, string>
    | ViewMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | ViewSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | ViewLibrary
    | ViewYearInReview
    // Trakt sync messages
    | CheckTraktSync
    | TraktSyncStatusReceived of Result<TraktSyncStatus, string>
    | TraktSyncCompleted of Result<TraktSyncResult, string>
    | DismissSyncNotification
    | ManualSync  // User clicked sync button
    // TMDB health check messages
    | CheckTmdbHealth
    | TmdbHealthReceived of Result<string, string>

type ExternalMsg =
    | NoOp
    | NavigateToLibrary
    | NavigateToMovieDetail of entryId: EntryId * title: string * releaseDate: DateTime option
    | NavigateToSeriesDetail of entryId: EntryId * name: string * firstAirDate: DateTime option
    | NavigateToYearInReview

module Model =
    let empty = {
        Library = NotAsked
        TraktSync = SyncIdle
        TraktStatus = None
        TmdbHealth = TmdbNotChecked
    }
