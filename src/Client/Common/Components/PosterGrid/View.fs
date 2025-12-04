module Common.Components.PosterGrid.View

open Feliz

/// Responsive grid container for poster cards
/// Default: 2/3/4/5/6 columns at sm/md/lg/xl breakpoints
let view (children: ReactElement list) =
    Html.div [
        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
        prop.children children
    ]

/// Grid with custom gap
let viewWithGap (gap: int) (children: ReactElement list) =
    Html.div [
        prop.className $"grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-{gap}"
        prop.children children
    ]

/// Grid with fixed number of columns
let viewFixed (columns: int) (children: ReactElement list) =
    Html.div [
        prop.className $"grid grid-cols-{columns} gap-4"
        prop.children children
    ]
