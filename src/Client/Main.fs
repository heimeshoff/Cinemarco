module Main

open Elmish
open Elmish.React
open Elmish.HMR

// Import Tailwind CSS
Fable.Core.JsInterop.importSideEffects "./styles.css"

// Import ambient color system for dynamic projector backdrop
Fable.Core.JsInterop.importSideEffects "./ambientColor.js"

// Start the Elmish application
Program.mkProgram App.State.init App.State.update App.View.view
|> Program.withReactSynchronous "root"
|> Program.run
