module Networking.PortFowarding

open System
open System.Net
open System.Net.Http
open System.Net.Sockets
open System.Net.NetworkInformation
open FSharpx.Result
open Mono.Nat
open FSharp.Json
open Spectre.Console


///Retrieves the UPnP-enabled NAT gateway for the local network
///Will fail if the local network does not have a NAT gateway, or UPnP is not enabled
let private retrieveGateway () =
    let getDefaultGateway () =
        NetworkInterface.GetAllNetworkInterfaces()
        |> Array.filter (fun n ->
            n.OperationalStatus = OperationalStatus.Up
            && n.NetworkInterfaceType
               <> NetworkInterfaceType.Loopback)
        |> Seq.collect (fun n ->
            n.GetIPProperties().GatewayAddresses
            |> Seq.map (fun a -> a.Address))
        |> Seq.head

    ///Performs a multicast search on the local network for the NAT device
    ///The device must be the gateway for the client
    let natSearch gateway =
        let mutable devices = []

        NatUtility.DeviceFound.Add(fun e -> devices <- e.Device :: devices)

        async {
            do
                NatUtility.StartDiscovery [|
                    NatProtocol.Upnp
                |]

            do! Async.Sleep(TimeSpan.FromMilliseconds 500)
            do NatUtility.StopDiscovery()
        }
        |> Async.RunSynchronously

        devices
        |> List.filter (fun dev -> dev.DeviceEndpoint.Address = gateway)
        |> List.tryHead
        |> function
            | Some router -> router
            | None ->
                failwith
                    "Router with UPnP functionality not detected. Please manually forward your ports to use ginger-talk"

    let run = getDefaultGateway >> natSearch

    run ()

let private mapPort (internalPort: int) (externalPort: int) (device: INatDevice) =
    let run () =
        Mapping(Protocol.Tcp, internalPort, externalPort, 645000, "ginger-talk")
        |> device.CreatePortMap

    run ()

let private closePort (device: INatDevice) (portMapping: Mapping) () =
    device.DeletePortMap portMapping |> ignore
    AnsiConsole.MarkupLine $"[green]Removed UPnP-forwarded port[/]"


let forwardPort (internalPort: int) (externalPort: int) =
    AnsiConsole.MarkupLine $"[chartreuse1]Attempting to automatically forward port [green]{internalPort}[/] to [green]{externalPort}[/][/]"
    let device = retrieveGateway ()

    let portMapping =
        mapPort internalPort externalPort device
    AnsiConsole.MarkupLine $"[green]Port forwarded successfully![/]"
    closePort device portMapping
