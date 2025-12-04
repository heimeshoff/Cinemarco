module Common.Components.SectionHeader.Types

open Feliz

type ActionType =
    | NoAction
    | Link of text: string * icon: ReactElement option
    | Button of text: string * icon: ReactElement option

type HeaderSize =
    | Large   // text-2xl
    | Medium  // text-xl
    | Small   // text-lg

type Model = {
    Title: string
    Action: ActionType
    Size: HeaderSize
}

module Model =
    let create title = { Title = title; Action = NoAction; Size = Medium }
    let large model = { model with Size = Large }
    let small model = { model with Size = Small }
    let withLink text icon model = { model with Action = Link (text, icon) }
    let withButton text icon model = { model with Action = Button (text, icon) }
