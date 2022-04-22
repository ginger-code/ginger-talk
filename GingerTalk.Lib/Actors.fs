[<RequireQualifiedAccess>]
module Actors

open Akka.Cluster.Tools.PublishSubscribe
open Akkling
open Akkling.Cluster
open Message
open ClientState

let clientInput (clientStateManager: IActorRef<Msg>) (msg: Msg option) =
    match msg with
    | None -> Ignore
    | Some msg ->
        clientStateManager <<! msg
        Ignore

let clientStateManager (mailbox: Actor<Msg>) =
    let publish user topic msg =
        mailbox.Parent()
        <! (Outgoing <| SentMessage.create user topic msg)

    let rec loop state =
        actor {
            let! msg = mailbox.Receive()
            let state = update publish msg state
            printState state
            return! loop state
        }

    loop init


let messageReceived state (mailbox: Actor<TimestampedMessage>) =

    let rec loop () =
        actor {
            let! message = mailbox.Receive()
            state <! ReceiveMessage message
            return! loop ()
        }

    loop ()

let subscriptionsChanged state (mailbox: Actor<Subscription>) =
    let rec loop () =
        actor {
            match! mailbox.Receive() with
            | Sub topic -> state <! Join topic
            | Unsub topic -> state <! Leave topic

            return! loop ()
        }

    loop ()

let chatNodeActor (mailbox: Actor<obj>) =
    let mdr =
        DistributedPubSub.Get(mailbox.System).Mediator

    (typed mdr) <! Put(untyped mailbox.Self)

    let clientState =
        spawn mailbox "client-state"
        <| props clientStateManager

    let _clientInput =
        spawnAnonymous mailbox
        <| props (actorOf (clientInput clientState))

    ///Handles incoming, timestamped messages
    let messageReceived =
        spawnAnonymous mailbox
        <| props (messageReceived clientState)

    ///Handles changes in subscriptions
    let subscriptionsChanged =
        spawnAnonymous mailbox
        <| props (subscriptionsChanged clientState)

    let rec loop () =
        actor {
            match! mailbox.Receive() with
            | :? SubscribeAck as ack ->
                subscriptionsChanged
                <! (Sub <| Topic ack.Subscribe.Topic)

                return! loop ()
            | :? UnsubscribeAck as ack ->
                subscriptionsChanged
                <! (Unsub <| Topic ack.Unsubscribe.Topic)
            | :? Msg as msg -> clientState <! msg
            | :? ChatCommand as dps ->
                match dps with
                | Subscription subscription ->
                    match subscription with
                    | Sub (Topic topic) ->
                        typed mdr
                        <! Subscribe(topic, untyped mailbox.Self)
                    | Unsub (Topic topic) ->
                        typed mdr
                        <! Unsubscribe(topic, untyped mailbox.Self)
                | Incoming message ->
                    messageReceived
                    <! TimestampedMessage.timestamp message
                | Outgoing message ->
                    let (Topic topic) = message.Topic
                    typed mdr <! Publish(topic, Incoming <| message)
            | :? LifecycleEvent as _ -> return! loop ()
            | _ -> ()

            return! loop ()
        }

    loop ()
