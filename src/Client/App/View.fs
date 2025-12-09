module App.View

open Feliz
open Common.Types
open Common.Routing
open Shared.Domain
open Types
open Browser.Types

/// Render the current page content
let private pageContent (model: Model) (dispatch: Msg -> unit) =
    let friends = RemoteData.defaultValue [] model.Friends

    match model.CurrentPage with
    | HomePage ->
        match model.HomePage with
        | Some pageModel ->
            Pages.Home.View.view pageModel (HomeMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | LibraryPage ->
        match model.LibraryPage with
        | Some pageModel ->
            Pages.Library.View.view pageModel (LibraryMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | MovieDetailPage _ ->
        match model.MovieDetailPage with
        | Some pageModel ->
            Pages.MovieDetail.View.view pageModel friends (MovieDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | SeriesDetailPage _ ->
        match model.SeriesDetailPage with
        | Some pageModel ->
            Pages.SeriesDetail.View.view pageModel friends (SeriesDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | SessionDetailPage _ ->
        match model.SessionDetailPage with
        | Some pageModel ->
            Pages.SessionDetail.View.view pageModel friends (SessionDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | FriendsPage ->
        match model.FriendsPage with
        | Some pageModel ->
            Pages.Friends.View.view pageModel (FriendsMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | FriendDetailPage _ ->
        match model.FriendDetailPage with
        | Some pageModel ->
            let friendId = pageModel.FriendId
            let friend = friends |> List.tryFind (fun f -> f.Id = friendId)
            Pages.FriendDetail.View.view pageModel friend (FriendDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | ContributorsPage ->
        match model.ContributorsPage with
        | Some pageModel ->
            Pages.Contributors.View.view pageModel (ContributorsMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | CachePage ->
        match model.CachePage with
        | Some pageModel ->
            Pages.Cache.View.view pageModel (CacheMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | CollectionsPage ->
        match model.CollectionsPage with
        | Some pageModel ->
            Pages.Collections.View.view pageModel (CollectionsMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | CollectionDetailPage _ ->
        match model.CollectionDetailPage with
        | Some pageModel ->
            Pages.CollectionDetail.View.view pageModel (CollectionDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | ContributorDetailPage _ ->
        match model.ContributorDetailPage with
        | Some pageModel ->
            Pages.ContributorDetail.View.view pageModel (ContributorDetailMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | StatsPage ->
        match model.StatsPage with
        | Some pageModel ->
            Pages.Stats.View.view pageModel (StatsMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | YearInReviewPage _ ->
        match model.YearInReviewPage with
        | Some pageModel ->
            Pages.YearInReview.View.view pageModel (YearInReviewMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | TimelinePage ->
        match model.TimelinePage with
        | Some pageModel ->
            Pages.Timeline.View.view pageModel (TimelineMsg >> dispatch)
        | None ->
            Html.div [ prop.className "loading loading-spinner" ]

    | GraphPage
    | ImportPage ->
        Html.div [
            prop.className "text-center py-16"
            prop.children [
                Html.h2 [
                    prop.className "text-2xl font-bold mb-4"
                    prop.text (Page.toString model.CurrentPage)
                ]
                Html.p [
                    prop.className "text-base-content/60"
                    prop.text "Coming soon..."
                ]
            ]
        ]

    | NotFoundPage ->
        Pages.NotFound.View.view ()

/// Render the active modal
let private modalContent (model: Model) (dispatch: Msg -> unit) =
    let friends = RemoteData.defaultValue [] model.Friends

    match model.Modal with
    | NoModal -> Html.none

    | SearchModal modalModel ->
        Components.SearchModal.View.view modalModel (SearchModalMsg >> dispatch)

    | FriendModal modalModel ->
        Components.FriendModal.View.view modalModel (FriendModalMsg >> dispatch)

    | AbandonModal modalModel ->
        Components.AbandonModal.View.view modalModel (AbandonModalMsg >> dispatch)

    | ConfirmDeleteModal modalModel ->
        Components.ConfirmModal.View.view modalModel (ConfirmModalMsg >> dispatch)

    | WatchSessionModal modalModel ->
        Components.WatchSessionModal.View.view modalModel friends (WatchSessionModalMsg >> dispatch)

    | MovieWatchSessionModal modalModel ->
        Components.MovieWatchSessionModal.View.view modalModel friends (MovieWatchSessionModalMsg >> dispatch)

    | CollectionModal modalModel ->
        Components.CollectionModal.View.view modalModel (CollectionModalMsg >> dispatch)

    | AddToCollectionModal modalModel ->
        Components.AddToCollectionModal.View.view modalModel (AddToCollectionModalMsg >> dispatch)

    | ProfileImageModal (modalModel, friendId) ->
        Components.ProfileImageEditor.View.View
            modalModel
            (ProfileImageModalMsg >> dispatch)
            (fun base64 -> dispatch (ProfileImageConfirmed (friendId, base64)))

/// Browser history listener component - handles back/forward navigation
[<ReactComponent>]
let private BrowserHistoryListener (dispatch: Msg -> unit) (children: ReactElement) =
    React.useEffect(fun () ->
        let handler _ =
            let page = Router.parseCurrentUrl ()
            dispatch (UrlChanged page)

        Browser.Dom.window.addEventListener("popstate", handler)

        // Cleanup
        React.createDisposable(fun () ->
            Browser.Dom.window.removeEventListener("popstate", handler)
        )
    , [||])

    children

/// Global keyboard shortcut handler component
[<ReactComponent>]
let private KeyboardShortcuts (model: Model) (dispatch: Msg -> unit) (children: ReactElement) =
    React.useEffect(fun () ->
        let handler (e: Event) =
            let ke = e :?> KeyboardEvent
            // Ctrl+K or Cmd+K to open search
            if (ke.ctrlKey || ke.metaKey) && ke.key = "k" then
                ke.preventDefault()
                // Only open if no modal is currently open
                if model.Modal = NoModal then
                    dispatch OpenSearchModal

        Browser.Dom.document.addEventListener("keydown", handler)

        // Cleanup
        React.createDisposable(fun () ->
            Browser.Dom.document.removeEventListener("keydown", handler)
        )
    , [| box model.Modal |])

    children

/// Check if the current page is a detail page (movie, series, or session)
let private isDetailPage (page: Page) =
    match page with
    | MovieDetailPage _ | SeriesDetailPage _ | SessionDetailPage _ -> true
    | _ -> false

/// Animated backdrop component - "The Projector Room"
let private animatedBackdrop (currentPage: Page) =
    let isFocused = isDetailPage currentPage
    let beamClass = if isFocused then "projector-beam projector-beam-focused" else "projector-beam"
    let glowClass = if isFocused then "projector-glow projector-glow-focused" else "projector-glow"

    Html.div [
        prop.className "animated-backdrop"
        prop.children [
            // Main projector light beam from top-right
            Html.div [ prop.className beamClass ]

            // Diffused glow around the beam source
            Html.div [ prop.className glowClass ]

            // Ambient light bounce from below
            Html.div [ prop.className "ambient-light" ]

            // Floating dust particles in the beam
            Html.div [
                prop.className "dust-container"
                prop.children [
                    for _ in 1..15 do
                        Html.div [ prop.className "dust" ]
                ]
            ]

            // Cinema vignette effect
            Html.div [ prop.className "backdrop-vignette" ]

            // Subtle film grain for authenticity
            Html.div [ prop.className "film-grain" ]
        ]
    ]

/// Main view
let view (model: Model) (dispatch: Msg -> unit) =
    let onNavigate = fun page -> dispatch (NavigateTo page)
    let onSearch = fun () -> dispatch OpenSearchModal

    BrowserHistoryListener dispatch (KeyboardShortcuts model dispatch (Html.div [
        prop.className "min-h-screen bg-transparent"
        prop.children [
            // Animated backdrop
            animatedBackdrop model.CurrentPage

            // Sidebar (desktop)
            Components.Layout.View.sidebar
                model.Layout
                model.CurrentPage
                onNavigate
                onSearch

            // Mobile nav
            Components.Layout.View.mobileNav
                model.Layout
                model.CurrentPage
                onNavigate
                onSearch
                (LayoutMsg >> dispatch)

            // Mobile menu drawer
            Components.Layout.View.mobileMenuDrawer
                model.Layout
                model.CurrentPage
                onNavigate
                (LayoutMsg >> dispatch)

            // Main content
            Html.main [
                prop.className "lg:pl-64 min-h-screen relative"
                prop.children [
                    Html.div [
                        prop.className "p-4 lg:p-8 pb-24 lg:pb-8"
                        prop.children [
                            pageContent model dispatch
                        ]
                    ]
                ]
            ]

            // Modal
            modalContent model dispatch

            // Notification
            Components.Notification.View.view model.Notification (NotificationMsg >> dispatch)
        ]
    ]))
