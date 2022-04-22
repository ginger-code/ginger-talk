open Message
open Akkling
open ActorRefs
open ClientState

subscribe <| Topic "general"
sendInput
<| Some(Msg.ChangeTopic(Topic "general"))

Application.waitForShutdown ()
|> Async.RunSynchronously
