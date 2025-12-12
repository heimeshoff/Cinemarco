module Pages.FriendDetail.View

open Feliz
open Browser.Dom
open Browser.Types
open Common.Types
open Shared.Domain
open Types
open Components.Icons
open Components.Cards.View

/// Render a friend avatar (large, round, clickable to edit)
let private friendAvatarLarge (friend: Friend) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "relative group cursor-pointer"
        prop.onClick (fun _ -> dispatch (OpenProfileImageModal friend))
        prop.children [
            // Avatar container with round shape
            Html.div [
                prop.className "avatar"
                prop.children [
                    match friend.AvatarUrl with
                    | Some url ->
                        Html.div [
                            prop.className "w-20 h-20 rounded-full ring ring-primary/30 ring-offset-base-100 ring-offset-2 overflow-hidden"
                            prop.children [
                                Html.img [
                                    prop.src $"/images/avatars{url}"
                                    prop.alt friend.Name
                                    prop.className "w-full h-full object-cover"
                                ]
                            ]
                        ]
                    | None ->
                        // Placeholder with initials
                        Html.div [
                            prop.className "avatar placeholder"
                            prop.children [
                                Html.div [
                                    prop.className "w-20 h-20 rounded-full bg-primary text-primary-content flex items-center justify-center"
                                    prop.children [
                                        Html.span [
                                            prop.className "text-3xl font-semibold"
                                            prop.text (friend.Name.Substring(0, 1).ToUpperInvariant())
                                        ]
                                    ]
                                ]
                            ]
                        ]
                ]
            ]
            // Edit overlay on hover
            Html.div [
                prop.className "absolute inset-0 rounded-full bg-black/50 opacity-0 group-hover:opacity-100 transition-opacity flex items-center justify-center"
                prop.children [
                    Html.span [
                        prop.className "text-white w-6 h-6"
                        prop.children [ camera ]
                    ]
                ]
            ]
        ]
    ]

/// Inline editable name component for friend detail
[<ReactComponent>]
let private InlineEditableName (friend: Friend) (model: Model) (dispatch: Msg -> unit) =
    let inputRef = React.useRef<HTMLInputElement option>(None)

    // Focus the input when we start editing
    React.useEffect(
        (fun () ->
            if model.IsEditingName then
                inputRef.current |> Option.iter (fun el -> el.focus(); el.select())
        ),
        [| box model.IsEditingName |]
    )

    if model.IsEditingName then
        Html.div [
            prop.className "flex items-center gap-3"
            prop.children [
                Html.input [
                    prop.ref inputRef
                    prop.className "input input-bordered text-2xl font-bold h-12 w-64"
                    prop.value model.EditingName
                    prop.disabled model.IsSaving
                    prop.onChange (fun v -> dispatch (UpdateEditingName v))
                    prop.onKeyDown (fun e ->
                        if e.key = "Enter" then
                            e.preventDefault()
                            dispatch SaveFriendName
                        elif e.key = "Escape" then
                            e.preventDefault()
                            dispatch CancelEditing
                    )
                    prop.onBlur (fun _ ->
                        // Save on blur if not already saving
                        if not model.IsSaving then
                            dispatch SaveFriendName
                    )
                ]
                if model.IsSaving then
                    Html.span [ prop.className "loading loading-spinner loading-sm" ]
            ]
        ]
    else
        Html.h2 [
            prop.className "text-2xl font-bold cursor-pointer hover:text-primary transition-colors"
            prop.onClick (fun _ -> dispatch (StartEditingName friend.Name))
            prop.title "Click to edit name"
            prop.text friend.Name
        ]

let view (model: Model) (friend: Friend option) (dispatch: Msg -> unit) =
    Html.div [
        prop.className "space-y-6"
        prop.children [
            // Back button - uses browser history for proper navigation
            Html.button [
                prop.className "btn btn-ghost btn-sm gap-2"
                prop.onClick (fun _ -> window.history.back())
                prop.children [
                    Html.span [ prop.className "w-4 h-4"; prop.children [ arrowLeft ] ]
                    Html.span [ prop.text "Back" ]
                ]
            ]

            match friend with
            | Some f ->
                // Friend header
                Html.div [
                    prop.className "flex items-center gap-4"
                    prop.children [
                        // Clickable avatar
                        friendAvatarLarge f dispatch

                        Html.div [
                            prop.className "flex-1"
                            prop.children [
                                // Editable name
                                InlineEditableName f model dispatch

                                match f.Nickname with
                                | Some nick ->
                                    Html.p [
                                        prop.className "text-base-content/60"
                                        prop.text nick
                                    ]
                                | None -> Html.none
                            ]
                        ]

                        // View in Graph button
                        Html.div [
                            prop.className "tooltip tooltip-bottom detail-tooltip"
                            prop.custom ("data-tip", "View in Graph")
                            prop.children [
                                Html.button [
                                    prop.className "detail-action-btn"
                                    prop.onClick (fun _ -> dispatch ViewInGraph)
                                    prop.children [
                                        Html.span [ prop.className "w-5 h-5"; prop.children [ graph ] ]
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]

                // Watched together section
                Html.h3 [
                    prop.className "text-xl font-semibold mt-8"
                    prop.text "Watched Together"
                ]

                match model.Entries with
                | Loading ->
                    Html.div [
                        prop.className "flex justify-center py-12"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-lg" ]
                        ]
                    ]
                | Success entries when List.isEmpty entries ->
                    Html.div [
                        prop.className "text-center py-12 text-base-content/60"
                        prop.text "No entries watched with this friend yet."
                    ]
                | Success entries ->
                    Html.div [
                        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
                        prop.children [
                            for entry in entries do
                                let (title, releaseDate) =
                                    match entry.Media with
                                    | LibraryMovie m -> (m.Title, m.ReleaseDate)
                                    | LibrarySeries s -> (s.Name, s.FirstAirDate)
                                libraryEntryCard entry (fun id isMovie ->
                                    if isMovie then dispatch (ViewMovieDetail (id, title, releaseDate))
                                    else dispatch (ViewSeriesDetail (id, title, releaseDate)))
                        ]
                    ]
                | Failure err ->
                    Html.div [
                        prop.className "alert alert-error"
                        prop.text err
                    ]
                | NotAsked -> Html.none

            | None ->
                Html.div [
                    prop.className "text-center py-12"
                    prop.text "Friend not found"
                ]
        ]
    ]
