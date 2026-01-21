module Pages.Styleguide.Types

/// Model for the Styleguide page (stateless - just displays components)
type Model = unit

/// Messages for the Styleguide page
type Msg =
    | NoOp

/// External messages (none needed for this page)
type ExternalMsg =
    | NoOp

module Model =
    let empty = ()
