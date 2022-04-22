module Application

open Akkling
open Akka.Actor
open System
open Spectre.Console

let private sendMessage: Async<unit> =
    AnsiConsole.MarkupLine
        "Commands: [green]/user <name>[/]: Change username, [yellow]/join <topic>[/]: Join a topic, [blue]/topic <topic>[/]: Change topic channel"

    let rec loop () =
        async {
            if Console.IsInputRedirected then
                do! Async.Sleep 10000
                return! loop ()
            else
                let input = Console.ReadLine()

                let msg = ClientState.parseMsg ActorRefs.subscribe ActorRefs.unsubscribe input

                ActorRefs.sendInput msg
                return! loop ()
        }

    loop ()

///Sends a shutdown request to the actor system
let terminateSystem _ =
    async {
        let! _ =
            CoordinatedShutdown
                .Get(ActorSystem.system)
                .Run(CoordinatedShutdown.ClrExitReason.Instance)
            |> Async.AwaitTask

        return ()
    }

Console.CancelKeyPress.Add (fun _ ->
    printfn "Ctrl-C pressed. Attempting graceful shutdown before exiting."

    try
        terminateSystem () |> Async.RunSynchronously
        exit 0
    with
    | _ ->
        printfn "Failed to shut down system gracefully."
        exit 1)

AppDomain.CurrentDomain.DomainUnload.Add (fun _ ->
    printfn "Terminating actor system due to shutdown."

    try
        terminateSystem () |> Async.RunSynchronously
        exit 0
    with
    | _ ->
        printfn "Failed to shut down system gracefully."
        exit 1)

///Completes when the actor system has fully terminated
let waitForShutdown () =
    let awaitSome (task: Async<'a>) : Async<'a option> =
        async {
            let! x = task
            return Some x
        }

    async {
        let! completed =
            (Seq.map awaitSome >> Async.Choice)
                (
                    seq {
                        ActorSystem.system.WhenTerminated
                        |> Async.AwaitTask

                        sendMessage
                    }
                )

        return Option.get completed
    }
