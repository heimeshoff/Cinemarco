module Pages.ContributorDetail.Types

open Common.Types
open Shared.Domain

/// Filter for the filmography view
type FilmographyFilter =
    | AllWorks
    | SeenWorks
    | UnseenWorks

/// Filter by media type
type MediaTypeFilter =
    | AllMedia
    | MoviesOnly
    | SeriesOnly

/// Filter by role
type RoleFilter =
    | AllRoles
    | CastOnly
    | CrewOnly

type Model = {
    TmdbPersonId: TmdbPersonId
    PersonDetails: RemoteData<TmdbPersonDetails>
    Filmography: RemoteData<TmdbFilmography>
    LibraryEntryIds: Map<int * MediaType, EntryId>  // (TmdbId, MediaType) -> EntryId for items in library
    FilmographyFilter: FilmographyFilter
    MediaTypeFilter: MediaTypeFilter
    RoleFilter: RoleFilter
}

type Msg =
    | LoadPersonDetails
    | PersonDetailsLoaded of Result<TmdbPersonDetails, string>
    | LoadFilmography
    | FilmographyLoaded of Result<TmdbFilmography, string>
    | LibraryLoaded of LibraryEntry list
    | SetFilmographyFilter of FilmographyFilter
    | SetMediaTypeFilter of MediaTypeFilter
    | SetRoleFilter of RoleFilter
    | AddToLibrary of TmdbWork
    | AddToLibraryResult of Result<LibraryEntry, string>
    | ViewMovieDetail of EntryId
    | ViewSeriesDetail of EntryId
    | GoBack

type ExternalMsg =
    | NoOp
    | NavigateBack
    | NavigateToMovieDetail of EntryId
    | NavigateToSeriesDetail of EntryId
    | ShowNotification of message: string * isSuccess: bool

module Model =
    let create personId = {
        TmdbPersonId = personId
        PersonDetails = NotAsked
        Filmography = NotAsked
        LibraryEntryIds = Map.empty
        FilmographyFilter = AllWorks
        MediaTypeFilter = AllMedia
        RoleFilter = AllRoles
    }
