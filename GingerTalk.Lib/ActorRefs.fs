module ActorRefs

open Akkling
open ActorSystem
open ClientState
open Message




///Handles cluster pub-sub dispatch of subscriptions and messages
let chatNode: IActorRef<ChatCommand> =
    typed //retype actor reference
    <| untyped ( //remove generic from actor
        spawn system "chat-node"
        <| props (Actors.chatNodeActor)
    )


let subscribe topic = chatNode <! (Subscription <| Sub topic)

let unsubscribe topic =
    chatNode <! (Subscription <| Unsub topic)

let sendInput msg =
    let mailbox: IActorRef<Msg> =
        retype chatNode

    match msg with
    | None -> ()
    | Some msg -> mailbox <! msg
