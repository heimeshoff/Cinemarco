module Common.Components.ErrorState.Types

type Model = {
    Message: string
    Context: string option  // e.g., "loading library"
}

module Model =
    let create msg = { Message = msg; Context = None }
    let withContext ctx model = { model with Context = Some ctx }
