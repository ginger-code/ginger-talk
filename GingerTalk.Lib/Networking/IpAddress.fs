
module Networking.IpAddress

open System.Net
open System.Net.Http
open System.Net.Sockets
open FSharp.Json


let retrieveLocalAddress () =
    try
        Dns.GetHostEntry(Dns.GetHostName()).AddressList
        |> Array.find (fun x -> x.AddressFamily = AddressFamily.InterNetwork)
        |> fun ip -> ip.ToString()
        |> Ok
    with
    | _ ->
        "Could not retrieve local network address"
        |> Error

type ExternalAddress = { ip: string }

let retrieveExternalAddress () =
    use client = new HttpClient()

    try
        task {
            let! response = client.GetAsync("https://api.ipify.org?format=json")
            let! body = response.Content.ReadAsStringAsync()

            let json = Json.deserialize<ExternalAddress> body

            return json.ip
        }
        |> Async.AwaitTask
        |> Async.RunSynchronously
        |> Ok
    with
    | _ -> "Could not retrieve external address" |> Error
