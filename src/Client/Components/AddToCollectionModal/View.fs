module Components.AddToCollectionModal.View

open Feliz
open Common.Types
open Shared.Domain
open Types
open Components.Modal.View
module Icons = Components.Icons


let view (model: Model) (dispatch: Msg -> unit) =
    let isDisabled = model.IsSubmitting || model.IsCreatingCollection
    let searchText = model.SearchText.Trim().ToLowerInvariant()

    wrapper {
        OnClose = fun () -> if not isDisabled then dispatch Close
        CanClose = not isDisabled
        MaxWidth = Some "max-w-md"
        Children = [
            // Header
            header "Manage Collections" (Some $"Select collections for \"{model.ItemTitle}\"") (not isDisabled) (fun () -> dispatch Close)

            // Body
            body [
                // Error message
                errorAlert model.Error

                match model.Collections with
                | Loading ->
                    Html.div [
                        prop.className "flex justify-center py-8"
                        prop.children [
                            Html.span [ prop.className "loading loading-spinner loading-md" ]
                        ]
                    ]

                | Failure err ->
                    Html.div [
                        prop.className "alert alert-error"
                        prop.text $"Error loading collections: {err}"
                    ]

                | Success collections ->
                    Html.div [
                        prop.className "space-y-4"
                        prop.children [
                            // Search input
                            Html.div [
                                prop.className "relative"
                                prop.children [
                                    Html.div [
                                        prop.className "absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none"
                                        prop.children [ Icons.search ]
                                    ]
                                    Html.input [
                                        prop.type' "text"
                                        prop.className "input input-bordered w-full pl-10"
                                        prop.placeholder "Search or create collection..."
                                        prop.value model.SearchText
                                        prop.onChange (fun (s: string) -> dispatch (SearchTextChanged s))
                                        prop.disabled isDisabled
                                        prop.autoFocus true
                                    ]
                                ]
                            ]

                            // Filtered collections list
                            let filteredCollections =
                                if searchText.Length = 0 then collections
                                else collections |> List.filter (fun c -> c.Name.ToLowerInvariant().Contains(searchText))

                            let hasExactMatch =
                                filteredCollections
                                |> List.exists (fun c -> c.Name.Trim().ToLowerInvariant() = searchText)

                            Html.div [
                                prop.className "space-y-2 max-h-64 overflow-y-auto"
                                prop.children [
                                    // Show create option if search text doesn't match any existing collection
                                    if searchText.Length > 0 && not hasExactMatch then
                                        Html.button [
                                            prop.type' "button"
                                            prop.className "w-full flex items-center gap-3 p-3 rounded-lg transition-all text-left bg-success/20 hover:bg-success/30 border-2 border-dashed border-success/50"
                                            prop.onClick (fun _ -> dispatch CreateAndAddCollection)
                                            prop.disabled isDisabled
                                            prop.children [
                                                Html.div [
                                                    prop.className "flex items-center justify-center w-6 h-6 rounded bg-success/30 text-success"
                                                    prop.children [ Icons.plus ]
                                                ]
                                                Html.div [
                                                    prop.className "flex-1"
                                                    prop.children [
                                                        Html.p [
                                                            prop.className "font-medium text-success"
                                                            prop.children [
                                                                Html.text "Create \""
                                                                Html.span [ prop.className "font-bold"; prop.text model.NewCollectionName ]
                                                                Html.text "\""
                                                            ]
                                                        ]
                                                        Html.p [
                                                            prop.className "text-xs text-success/70"
                                                            prop.text "New collection"
                                                        ]
                                                    ]
                                                ]
                                                if model.IsCreatingCollection then
                                                    Html.span [ prop.className "loading loading-spinner loading-sm text-success" ]
                                            ]
                                        ]

                                    if List.isEmpty filteredCollections && searchText.Length = 0 then
                                        Html.div [
                                            prop.className "text-center py-6 text-base-content/60"
                                            prop.children [
                                                Html.p [ prop.text "No collections yet" ]
                                                Html.p [ prop.className "text-sm mt-1"; prop.text "Type a name above to create one" ]
                                            ]
                                        ]
                                    elif List.isEmpty filteredCollections then
                                        Html.div [
                                            prop.className "text-center py-4 text-base-content/50 text-sm"
                                            prop.text "No matching collections"
                                        ]
                                    else
                                        for collection in filteredCollections do
                                            let isSelected = model.SelectedCollectionIds.Contains collection.Id
                                            let wasInitial = model.InitialCollectionIds.Contains collection.Id
                                            Html.button [
                                                prop.type' "button"
                                                prop.className (
                                                    "w-full flex items-center gap-3 p-3 rounded-lg transition-all text-left " +
                                                    if isSelected then "bg-primary/20 hover:bg-primary/30 ring-2 ring-primary/50"
                                                    else "bg-base-200 hover:bg-base-300"
                                                )
                                                prop.onClick (fun _ -> dispatch (ToggleCollection collection.Id))
                                                prop.disabled isDisabled
                                                prop.children [
                                                    // Checkbox
                                                    Html.div [
                                                        prop.className (
                                                            "flex items-center justify-center w-5 h-5 rounded border-2 transition-all " +
                                                            if isSelected then "bg-primary border-primary text-primary-content"
                                                            else "border-base-content/30"
                                                        )
                                                        prop.children [
                                                            if isSelected then Icons.check
                                                        ]
                                                    ]
                                                    Html.div [
                                                        prop.className "flex-1 min-w-0"
                                                        prop.children [
                                                            Html.p [
                                                                prop.className "font-medium truncate"
                                                                prop.text collection.Name
                                                            ]
                                                            match collection.Description with
                                                            | Some desc ->
                                                                Html.p [
                                                                    prop.className "text-xs truncate text-base-content/50"
                                                                    prop.text desc
                                                                ]
                                                            | None -> Html.none
                                                        ]
                                                    ]
                                                    // Show badge if item was already in this collection
                                                    if wasInitial && isSelected then
                                                        Html.span [
                                                            prop.className "badge badge-sm badge-ghost"
                                                            prop.text "already in"
                                                        ]
                                                ]
                                            ]
                                ]
                            ]

                            // Summary of changes
                            let toAdd = model.SelectedCollectionIds - model.InitialCollectionIds
                            let toRemove = model.InitialCollectionIds - model.SelectedCollectionIds
                            let hasChanges = not (Set.isEmpty toAdd && Set.isEmpty toRemove)

                            if hasChanges then
                                Html.div [
                                    prop.className "text-xs text-base-content/60 pt-2 border-t border-base-content/10"
                                    prop.children [
                                        if not (Set.isEmpty toAdd) then
                                            Html.span [
                                                prop.className "text-success"
                                                prop.text $"+{Set.count toAdd} collection(s)"
                                            ]
                                        if not (Set.isEmpty toAdd) && not (Set.isEmpty toRemove) then
                                            Html.text " · "
                                        if not (Set.isEmpty toRemove) then
                                            Html.span [
                                                prop.className "text-error"
                                                prop.text $"-{Set.count toRemove} collection(s)"
                                            ]
                                    ]
                                ]

                            // Submit button
                            let buttonText =
                                if hasChanges then "Save Changes"
                                elif Set.isEmpty model.SelectedCollectionIds then "Done"
                                else "Done"
                            submitButton buttonText model.IsSubmitting (fun () -> dispatch Submit)
                        ]
                    ]

                | NotAsked -> Html.none
            ]
        ]
    }
