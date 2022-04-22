module ActorSystem

open Settings.Akkling
open Akka.Actor

///The actor system for the application
let system = createSystem ()
