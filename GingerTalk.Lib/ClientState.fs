module ClientState

open Message

type State =
    { User: User
      OpenTopic: Topic option
      Subscriptions: Map<Topic, TimestampedMessage list> }

let init =
    { User = User "Some <| Nerd"
      OpenTopic = None
      Subscriptions = Map.empty }

type Msg =
    | ReceiveMessage of TimestampedMessage
    | MessageSubmitted
    | UsernameChanged of User
    | ChangeTopic of Topic
    | Join of Topic
    | Leave of Topic
    | Pub of string
    | NoOp


let update publish (msg: Msg) (state: State) : State =
    match msg with
    | ReceiveMessage message ->
        { state with
            Subscriptions = state.Subscriptions.Add(message.Topic, state.Subscriptions.[message.Topic] @ [ message ]) }
    | MessageSubmitted -> state
    | UsernameChanged user -> { state with User = user }
    | ChangeTopic topic -> { state with OpenTopic = Some topic }
    | Join topic -> { state with Subscriptions = state.Subscriptions.Add(topic, []) }
    | Leave topic -> { state with Subscriptions = state.Subscriptions.Remove topic }
    | Pub message ->
        match state.OpenTopic with
        | Some topic ->
            publish state.User topic (Message message)
            state
        | None -> state
    | NoOp -> state

let parseMsg subscribe unsubscribe (line: string) : Msg option =
    if line = null then
        None
    else if line.StartsWith("/user ") then
        let user = line.Substring(String.length "/user ")

        Some <| (UsernameChanged <| User user)
    else if line.StartsWith("/join ") then
        let topic = Topic <| line.Substring(String.length "/join ")

        subscribe topic
        Some <| ChangeTopic topic
    else if line.StartsWith("/leave ") then
        let topic = Topic <| line.Substring(String.length "/leave ")

        unsubscribe topic
        None
    else if line.StartsWith("/topic ") then
        line.Substring(String.length "/topic ")
        |> Topic
        |> ChangeTopic
        |> Some
    else
        Some <| Pub line

open Spectre.Console

let printState (state: State) =
    AnsiConsole.Clear()

    AnsiConsole.MarkupLine
        "Commands: [green]/user <name>[/]: Change username, [yellow]/join <topic>[/]: Join a topic, [blue]/topic <topic>[/]: Change topic channel"

    match state.OpenTopic with
    | None ->
        AnsiConsole.MarkupLine
            "You are not currently viewing a topic. Type [green]/user <name>[/] to change your username, [yellow]/join <topic>[/] to join a topic or [blue]/topic <topic>[/] to change topics"
    | Some (Topic topic) ->
        AnsiConsole.MarkupLine $"[yellow]Topic: {topic}[/]"
        AnsiConsole.MarkupLine $"[blue]Username: {state.User}[/]"
        AnsiConsole.MarkupLine "[aqua]~[/][fuchsia]~[/][aqua]~[/][fuchsia]~[/][aqua]~[/]"

        match state.Subscriptions.ContainsKey <| Topic topic with
        | false -> ()
        | true ->
            state.Subscriptions.[Topic topic]
            |> Seq.iter (fun message ->
                AnsiConsole.MarkupLine
                    $"([blue]{message.Timestamp}[/]) [yellow]{message.User}[/]: [white]{message.Message}[/]")

        AnsiConsole.MarkupLine "[grey]Say something![/]"
