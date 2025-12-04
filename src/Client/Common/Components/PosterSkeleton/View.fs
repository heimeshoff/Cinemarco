module Common.Components.PosterSkeleton.View

open Feliz

/// Single poster skeleton card
let posterSkeleton () =
    Html.div [
        prop.className "space-y-3"
        prop.children [
            Html.div [ prop.className "skeleton aspect-[2/3] rounded-lg" ]
            Html.div [ prop.className "skeleton h-4 w-3/4 rounded" ]
            Html.div [ prop.className "skeleton h-3 w-1/2 rounded" ]
        ]
    ]

/// Grid of poster skeletons for loading state
let view (count: int) =
    Html.div [
        prop.className "grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-4"
        prop.children [
            for _ in 1..count do
                posterSkeleton ()
        ]
    ]
