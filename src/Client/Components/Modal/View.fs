module Components.Modal.View

open Feliz

/// Standard modal wrapper component with consistent backdrop and styling
/// Usage: Modal.wrapper { IsOpen = true; OnClose = fun () -> dispatch Close; Children = [...] }
type ModalProps = {
    OnClose: unit -> unit
    CanClose: bool
    MaxWidth: string option
    Children: ReactElement list
}

/// Default modal props
let defaultProps = {
    OnClose = fun () -> ()
    CanClose = true
    MaxWidth = Some "max-w-md"
    Children = []
}

/// Render a modal with consistent styling
let wrapper (props: ModalProps) =
    let maxWidthClass = props.MaxWidth |> Option.defaultValue "max-w-md"

    Html.div [
        prop.className "fixed inset-0 z-50 flex items-center justify-center p-4"
        prop.children [
            // Backdrop with blur effect
            Html.div [
                prop.className "modal-backdrop"
                prop.onClick (fun _ -> if props.CanClose then props.OnClose())
            ]
            // Modal content with gradient background and shadow
            Html.div [
                prop.className $"modal-content w-full {maxWidthClass} max-h-[90vh] overflow-y-auto"
                prop.children props.Children
            ]
        ]
    ]

/// Modal header with title and close button
let header (title: string) (subtitle: string option) (canClose: bool) (onClose: unit -> unit) =
    Html.div [
        prop.className "p-6 border-b border-base-300/30"
        prop.children [
            Html.h2 [
                prop.className "text-xl font-bold"
                prop.text title
            ]
            match subtitle with
            | Some sub ->
                Html.p [
                    prop.className "text-sm text-base-content/60 mt-1"
                    prop.text sub
                ]
            | None -> Html.none
            Html.button [
                prop.className "absolute top-4 right-4 btn btn-circle btn-sm btn-ghost"
                prop.onClick (fun _ -> onClose())
                prop.disabled (not canClose)
                prop.text "✕"
            ]
        ]
    ]

/// Modal body container
let body (children: ReactElement list) =
    Html.div [
        prop.className "p-6 space-y-4"
        prop.children children
    ]

/// Modal footer container
let footer (children: ReactElement list) =
    Html.div [
        prop.className "p-4 border-t border-base-300/30 flex items-center justify-end gap-2"
        prop.children children
    ]

/// Error alert for modals
let errorAlert (message: string option) =
    match message with
    | Some err ->
        Html.div [
            prop.className "alert alert-error"
            prop.text err
        ]
    | None -> Html.none

/// Form field wrapper
let formField (label: string) (required: bool) (children: ReactElement list) =
    Html.div [
        prop.className "form-control"
        prop.children [
            Html.label [
                prop.className "label"
                prop.children [
                    Html.span [
                        prop.className "label-text"
                        prop.text (if required then $"{label} *" else label)
                    ]
                ]
            ]
            yield! children
        ]
    ]

/// Submit button with loading state
let submitButton (text: string) (isLoading: bool) (onClick: unit -> unit) =
    Html.button [
        prop.className "btn btn-primary w-full"
        prop.onClick (fun _ -> onClick())
        prop.disabled isLoading
        prop.children [
            if isLoading then
                Html.span [ prop.className "loading loading-spinner loading-sm" ]
            else
                Html.span [ prop.text text ]
        ]
    ]
