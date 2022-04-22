module Settings



module private Program =
    open System
    open Argu
    open FSharpx.Result

    type ProgramSettings =
        { InternalPort: int
          ExternalPort: int
          InternalInterface: string
          ExternalInterface: string
          SystemName: string
          SeedNodeDomain: string
          SeedNodePort: int
          UPnPForwarding: bool }

    type private CliArguments =
        | [<AltCommandLine("-pi")>] InternalPort of int
        | [<AltCommandLine("-pe")>] ExternalPort of int
        | [<AltCommandLine("-il")>] InternalInterface of string
        | [<AltCommandLine("-ip")>] ExternalInterface of string
        | [<AltCommandLine("-s")>] SystemName of string
        | [<AltCommandLine("-sn")>] SeedNodeDomain of string
        | [<AltCommandLine("-sp")>] SeedNodePort of int
        | [<AltCommandLine("-upnp")>] UPnPForwarding
        interface IArgParserTemplate with
            member this.Usage =
                match this with
                | InternalPort _ -> "specify the internal port on which to listen. Default is 9110"
                | ExternalPort _ -> "specify the external port on which to listen. Default is 9110"
                | InternalInterface _ ->
                    "specify the interface on which to listen locally. Default is your local IP address"
                | ExternalInterface _ ->
                    "specify the interface of domain name to which messages will be received. Default is your external IP address"
                | SystemName _ -> "specify the name of the actor system to connect to. Default is 'ginger-talk'"
                | SeedNodeDomain _ -> "specify the domain on which the seed node is running. Default is '3.143.109.10'"
                | SeedNodePort _ -> "specify the port on which the seed node is listening. Default is 9110"
                | UPnPForwarding ->
                    "use this flag to (try to) forward the required ports using UPnP/NAT. Default is disabled"

    type private SettingsBuilder =
        { InternalPort: int option
          ExternalPort: int option
          InternalInterface: string option
          ExternalInterface: string option
          SystemName: string option
          SeedNodeDomain: string option
          SeedNodePort: int option
          UPnPForwarding: bool }
        static member Default() =
            { InternalPort = None
              ExternalPort = None
              InternalInterface = None
              ExternalInterface = None
              SystemName = None
              SeedNodeDomain = None
              SeedNodePort = None
              UPnPForwarding = false }

    let private applyArg builder (arg: CliArguments) : SettingsBuilder =
        match arg with
        | InternalPort port -> { builder with InternalPort = Some port }
        | ExternalPort port -> { builder with ExternalPort = Some port }
        | InternalInterface it -> { builder with InternalInterface = Some it }
        | ExternalInterface it -> { builder with ExternalInterface = Some it }
        | SystemName name -> { builder with SystemName = Some name }
        | SeedNodeDomain domain -> { builder with SeedNodeDomain = Some domain }
        | SeedNodePort port -> { builder with SeedNodePort = Some port }
        | UPnPForwarding -> { builder with UPnPForwarding = true }

    let private finalizeBuilder builder : Result<ProgramSettings, string> =
        result {
            let! internalInterface =
                if builder.InternalInterface.IsSome then
                    Ok builder.InternalInterface.Value
                else
                    Networking.IpAddress.retrieveLocalAddress ()

            let! externalInterface =
                if builder.ExternalInterface.IsSome then
                    Ok builder.ExternalInterface.Value
                else
                    Networking.IpAddress.retrieveExternalAddress ()

            return
                { InternalPort = Option.defaultValue 9110 builder.InternalPort
                  ExternalPort = Option.defaultValue 9110 builder.ExternalPort
                  InternalInterface = internalInterface
                  ExternalInterface = externalInterface
                  SystemName = Option.defaultValue "ginger-talk" builder.SystemName
                  SeedNodeDomain = Option.defaultValue "3.143.109.10" builder.SeedNodeDomain
                  SeedNodePort = Option.defaultValue 9110 builder.SeedNodePort
                  UPnPForwarding = builder.UPnPForwarding }
        }

    let programSettings =
        let args = Environment.GetCommandLineArgs() |> Array.skip 1

        let parser = ArgumentParser.Create<CliArguments>(programName = "ginger-talk.exe")

        if Array.contains "--help" args then
            printfn $"{parser.PrintUsage()}"
            exit 0

        (parser.Parse args).GetAllResults()
        |> List.fold applyArg (SettingsBuilder.Default())
        |> finalizeBuilder
        |> function
            | Ok settings -> settings
            | Error message ->
                printfn $"Failed to configure network. Shutting down.\r\Error: {message}"
                exit 1

module Akkling =
    open Program
    open Akkling

    let private hocon =
        $"""
akka {{
    stdout-loglevel = ERROR
    loglevel = ERROR
    log-config-on-start = on
    actor {{
        provider = cluster
        extensions = ["Akka.Cluster.Tools.PublishSubscribe.DistributedPubSubExtensionProvider,Akka.Cluster.Tools"]
        serializers {{
            hyperion = "Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion"
            akka-pubsub = "Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools"
        }}
        serialization-bindings {{
            "Akka.Cluster.Tools.PublishSubscribe.IDistributedPubSubMessage, Akka.Cluster.Tools" = akka-pubsub
            "Akka.Cluster.Tools.PublishSubscribe.Internal.SendToOneSubscriber, Akka.Cluster.Tools" = akka-pubsub
        }}
        serialization-identifiers {{
            "Akka.Cluster.Tools.PublishSubscribe.Serialization.DistributedPubSubMessageSerializer, Akka.Cluster.Tools" = 9
        }}
    }}
    remote {{
        dot-netty.tcp {{
            public-hostname = {programSettings.ExternalInterface}
            hostname = {programSettings.InternalInterface}
            public-port = {programSettings.ExternalPort}
            port = {programSettings.InternalPort}
        }}
    }}
    cluster {{
        auto-down-unreachable-after = 5s
        seed-nodes = [ "akka.tcp://{programSettings.SystemName}@{programSettings.SeedNodeDomain}:{programSettings.SeedNodePort}/" ]
        roles = ["chat"]
        pub-sub{{
            role = "chat"
        }}
        log-info-verbose = on
    }}
    persistence {{
        journal.plugin = "akka.persistence.journal.inmem"
        snapshot-store.plugin = "akka.persistence.snapshot-store.local"
    }}
}}
"""

    let private createConfiguration () = Configuration.parse hocon

    let createSystem () =
        let create () =
            createConfiguration ()
            |> System.create programSettings.SystemName

        match programSettings.UPnPForwarding with
        | false -> create ()
        | true ->
            let deletePortMapping =
                Networking.PortFowarding.forwardPort programSettings.InternalPort programSettings.ExternalPort

            let system = create ()
            system.RegisterOnTermination deletePortMapping
            system
