module Pages.SessionDetail.Types

open System
open Common.Types
open Shared.Domain

type Model = {
    SessionId: SessionId
    SessionData: RemoteData<WatchSessionWithProgress>
    SeasonDetails: Map<int, TmdbSeasonDetails>
    LoadingSeasons: Set<int>
    IsAddingFriend: bool
}

type Msg =
    | LoadSession
    | SessionLoaded of Result<WatchSessionWithProgress, string>
    | LoadSeasonDetails of int
    | SeasonDetailsLoaded of int * Result<TmdbSeasonDetails, string>
    | ToggleEpisodeWatched of seasonNum: int * episodeNum: int * isWatched: bool
    | MarkEpisodesUpTo of seasonNum: int * episodeNum: int * isWatched: bool
    | MarkSeasonWatched of seasonNum: int
    | UpdateStatus of SessionStatus
    | ToggleFriend of FriendId
    | AddNewFriend of string
    | FriendCreated of Result<Friend, string>
    | DeleteSession
    | ActionResult of Result<WatchSession, string>
    | EpisodeActionResult of Result<EpisodeProgress list, string>
    | DeleteResult of Result<unit, string>
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToSeries of entryId: EntryId * name: string * firstAirDate: DateTime option
    | ShowNotification of message: string * isSuccess: bool
    | SessionDeleted of SessionId
    | FriendCreatedInline of Friend

module Model =
    let create sessionId = {
        SessionId = sessionId
        SessionData = NotAsked
        SeasonDetails = Map.empty
        LoadingSeasons = Set.empty
        IsAddingFriend = false
    }
