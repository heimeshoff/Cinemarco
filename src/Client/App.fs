module App

open Elmish
open Elmish.React
open Elmish.HMR

// Import Tailwind CSS
Fable.Core.JsInterop.importSideEffects "./styles.css"

// Start the Elmish application
Program.mkProgram State.init State.update View.view
|> Program.withReactSynchronous "root"
|> Program.run
