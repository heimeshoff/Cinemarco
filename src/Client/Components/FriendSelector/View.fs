module Components.FriendSelector.View

open Feliz
open Shared.Domain
open Components.Icons

/// Props for the FriendSelector component
type FriendSelectorProps = {
    /// All available friends
    AllFriends: Friend list
    /// Currently selected friend IDs
    SelectedFriends: FriendId list
    /// Called when a friend is toggled (selected or deselected)
    OnToggle: FriendId -> unit
    /// Called when user wants to add a new friend with the given name
    OnAddNew: string -> unit
    /// Called when Enter is pressed with no search text (optional submit action)
    OnSubmit: (unit -> unit) option
    /// Whether the component is disabled
    IsDisabled: bool
    /// Placeholder text for the search input
    Placeholder: string
    /// Whether at least one friend is required
    IsRequired: bool
    /// Whether to auto-focus the input on mount
    AutoFocus: bool
}

/// Default props
let defaultProps = {
    AllFriends = []
    SelectedFriends = []
    OnToggle = fun _ -> ()
    OnAddNew = fun _ -> ()
    OnSubmit = None
    IsDisabled = false
    Placeholder = "Search friends..."
    IsRequired = false
    AutoFocus = false
}

/// Dropdown item type for keyboard navigation
type private DropdownItem =
    | AddNewItem of string
    | FriendItem of Friend

/// Reusable friend selector with search, pills, and add-new functionality
[<ReactComponent>]
let FriendSelector (props: FriendSelectorProps) =
    let (searchText, setSearchText) = React.useState ""
    let (isFocused, setIsFocused) = React.useState false
    let (highlightedIndex, setHighlightedIndex) = React.useState 0
    let inputRef = React.useInputRef()

    // Get selected friend objects
    let selectedFriends =
        props.SelectedFriends
        |> List.choose (fun fid ->
            props.AllFriends |> List.tryFind (fun f -> f.Id = fid))

    // Filter friends by search text (case-insensitive)
    let filteredFriends =
        if String.length searchText < 1 then
            props.AllFriends
        else
            let searchLower = searchText.ToLowerInvariant()
            props.AllFriends
            |> List.filter (fun f ->
                f.Name.ToLowerInvariant().Contains(searchLower) ||
                (f.Nickname |> Option.map (fun n -> n.ToLowerInvariant().Contains(searchLower)) |> Option.defaultValue false))

    // Check if search text matches any existing friend exactly
    let exactMatch =
        let searchLower = searchText.ToLowerInvariant().Trim()
        if searchLower.Length = 0 then
            true
        else
            props.AllFriends
            |> List.exists (fun f -> f.Name.ToLowerInvariant() = searchLower)

    // Whether to show the "Add new" option
    let showAddNew = searchText.Trim().Length > 0 && not exactMatch

    // Build the dropdown items list for keyboard navigation
    let dropdownItems =
        [
            if showAddNew then yield AddNewItem (searchText.Trim())
            for friend in filteredFriends do yield FriendItem friend
        ]

    let itemCount = List.length dropdownItems

    // Reset highlighted index when items change
    React.useEffect((fun () ->
        setHighlightedIndex 0
    ), [| box itemCount; box searchText |])

    // Auto-focus input on mount (only if AutoFocus is true)
    React.useEffect((fun () ->
        if props.AutoFocus then
            inputRef.current |> Option.iter (fun el -> el.focus())
    ), [||])

    // Remove a selected friend (clicking the X on a pill)
    let removeFriend friendId =
        props.OnToggle friendId

    // Helper to refocus input after a delay (allows React to re-render first)
    let refocusInput () =
        Browser.Dom.window.setTimeout((fun () ->
            inputRef.current |> Option.iter (fun el -> el.focus())
        ), 50) |> ignore

    // Add a friend (clicking on a friend in the list)
    let addFriend friendId =
        props.OnToggle friendId
        setSearchText ""
        setHighlightedIndex 0
        refocusInput()

    // Add new friend
    let addNewFriend () =
        let name = searchText.Trim()
        if name.Length > 0 then
            props.OnAddNew name
            setSearchText ""
            setHighlightedIndex 0
            refocusInput()

    // Select the currently highlighted item
    let selectHighlighted () =
        if highlightedIndex >= 0 && highlightedIndex < itemCount then
            match List.tryItem highlightedIndex dropdownItems with
            | Some (AddNewItem _) -> addNewFriend ()
            | Some (FriendItem friend) -> addFriend friend.Id
            | None -> ()

    // Handle keyboard navigation
    let handleKeyDown (e: Browser.Types.KeyboardEvent) =
        match e.key with
        | "ArrowDown" ->
            e.preventDefault()
            if itemCount > 0 then
                let newIdx = (highlightedIndex + 1) % itemCount
                setHighlightedIndex newIdx
        | "ArrowUp" ->
            e.preventDefault()
            if itemCount > 0 then
                let newIdx = if highlightedIndex <= 0 then itemCount - 1 else highlightedIndex - 1
                setHighlightedIndex newIdx
        | "Enter" ->
            e.preventDefault()
            if isFocused && itemCount > 0 then
                // Dropdown visible with items - select the highlighted item
                selectHighlighted ()
            elif searchText.Trim().Length = 0 then
                // No text and no items - trigger submit if available
                match props.OnSubmit with
                | Some submit -> submit ()
                | None -> ()
        | "Escape" ->
            e.preventDefault()
            setSearchText ""
            setIsFocused false
            setHighlightedIndex 0
        | "Backspace" ->
            // When backspace is pressed with empty search text, remove the last pill
            if searchText.Length = 0 && not (List.isEmpty props.SelectedFriends) then
                let lastFriendId = List.last props.SelectedFriends
                props.OnToggle lastFriendId
                refocusInput()
        | _ -> ()

    Html.div [
        prop.className "relative"
        prop.children [
            // Selected friends as pills + search input
            Html.div [
                prop.className (
                    "flex flex-wrap items-center gap-2 p-2 border rounded-lg bg-base-100 " +
                    if isFocused then "border-primary ring-1 ring-primary" else "border-base-300"
                )
                prop.children [
                    // Pills for selected friends
                    for friend in selectedFriends do
                        Html.span [
                            prop.key (FriendId.value friend.Id |> string)
                            prop.className "badge badge-primary gap-1 pl-3"
                            prop.children [
                                Html.span [ prop.text friend.Name ]
                                Html.button [
                                    prop.className "btn btn-ghost btn-xs btn-circle -mr-1"
                                    prop.onClick (fun e ->
                                        e.stopPropagation()
                                        if not props.IsDisabled then removeFriend friend.Id)
                                    prop.disabled props.IsDisabled
                                    prop.children [
                                        Html.span [
                                            prop.className "w-3 h-3"
                                            prop.children [ close ]
                                        ]
                                    ]
                                ]
                            ]
                        ]

                    // Search input
                    Html.input [
                        prop.ref inputRef
                        prop.type' "text"
                        prop.className "flex-1 min-w-[120px] bg-transparent outline-none text-sm"
                        prop.placeholder props.Placeholder
                        prop.value searchText
                        prop.onChange setSearchText
                        prop.onFocus (fun _ -> setIsFocused true)
                        prop.onBlur (fun _ ->
                            // Delay to allow click events on dropdown items
                            Browser.Dom.window.setTimeout((fun () -> setIsFocused false), 150) |> ignore)
                        prop.onKeyDown handleKeyDown
                        prop.disabled props.IsDisabled
                    ]
                ]
            ]

            // Validation message
            if props.IsRequired && List.isEmpty props.SelectedFriends then
                Html.p [
                    prop.className "text-sm text-base-content/60 mt-1"
                    prop.text "Select at least one friend"
                ]

            // Dropdown with filtered friends (z-[100] to overlay above modal, max-h for ~5 items)
            if isFocused then
                Html.div [
                    prop.className "absolute left-0 right-0 top-full mt-1 z-[100] rounded-lg shadow-xl border border-base-300 max-h-[220px] overflow-y-auto"
                    prop.style [ style.backgroundColor "#1d232a" ] // Solid dark background matching modal
                    prop.children [
                        // "Add new" option
                        if showAddNew then
                            let addNewIndex = 0
                            let isHighlighted = highlightedIndex = addNewIndex
                            Html.button [
                                prop.className "w-full px-4 py-3 text-left flex items-center gap-2 text-primary border-b border-base-300"
                                prop.style [
                                    if isHighlighted then style.backgroundColor "#3d4451" // Visible highlight color
                                ]
                                prop.onMouseDown (fun e ->
                                    e.preventDefault()
                                    addNewFriend())
                                prop.children [
                                    Html.span [
                                        prop.className "w-4 h-4"
                                        prop.children [ plus ]
                                    ]
                                    Html.span [
                                        prop.text $"Add \"{searchText.Trim()}\" as new friend"
                                    ]
                                ]
                            ]

                        // Filtered friends list
                        if List.isEmpty filteredFriends then
                            if not showAddNew then
                                Html.div [
                                    prop.className "px-4 py-3 text-base-content/50 text-sm"
                                    prop.text "No friends found"
                                ]
                        else
                            let startIndex = if showAddNew then 1 else 0
                            for i, friend in List.indexed filteredFriends do
                                let itemIndex = startIndex + i
                                let isSelected = List.contains friend.Id props.SelectedFriends
                                let isHighlighted = highlightedIndex = itemIndex
                                Html.button [
                                    prop.key (FriendId.value friend.Id |> string)
                                    prop.className "w-full px-4 py-2 text-left flex items-center justify-between"
                                    prop.style [
                                        if isHighlighted then style.backgroundColor "#3d4451" // Visible highlight color
                                        elif isSelected then style.backgroundColor "rgba(101, 163, 13, 0.1)" // Subtle green for selected
                                    ]
                                    prop.onMouseDown (fun e ->
                                        e.preventDefault()
                                        addFriend friend.Id)
                                    prop.children [
                                        Html.div [
                                            Html.span [
                                                prop.className "font-medium"
                                                prop.text friend.Name
                                            ]
                                            match friend.Nickname with
                                            | Some nick ->
                                                Html.span [
                                                    prop.className "text-base-content/50 text-sm ml-2"
                                                    prop.text $"({nick})"
                                                ]
                                            | None -> Html.none
                                        ]
                                        if isSelected then
                                            Html.span [
                                                prop.className "w-4 h-4 text-primary"
                                                prop.children [ check ]
                                            ]
                                    ]
                                ]
                    ]
                ]
        ]
    ]
