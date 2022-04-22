open Akkling
open ActorRefs
open ClientState
open Message

subscribe <| Topic "general"
subscribe <| Topic "secrets"
subscribe <| Topic "role-playing"
subscribe <| Topic "synthesizers"

sendInput
<| Some(Msg.ChangeTopic(Topic "general"))
sendInput
<| Some(Msg.UsernameChanged(User "Sir Verr"))

Application.waitForShutdown ()
|> Async.RunSynchronously
