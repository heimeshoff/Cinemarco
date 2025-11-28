module Pages.NotFound.View

open Feliz
open Components.Icons

let view () =
    Html.div [
        prop.className "flex flex-col items-center justify-center min-h-[60vh] text-center"
        prop.children [
            Html.span [
                prop.className "text-8xl text-base-content/20 mb-6"
                prop.children [ warning ]
            ]
            Html.h1 [
                prop.className "text-4xl font-bold mb-2"
                prop.text "404"
            ]
            Html.p [
                prop.className "text-xl text-base-content/60 mb-8"
                prop.text "Page not found"
            ]
            Html.p [
                prop.className "text-base-content/40"
                prop.text "The page you're looking for doesn't exist or has been moved."
            ]
        ]
    ]
