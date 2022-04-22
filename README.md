# ginger-talk

## Description

ginger-talk is a distributed, peer-to-peer chat network written using Akkling (Akka.Net).

## CLI Use (Basic)

There are two ways to get your client connected to the chat cluster:

1. Manually forward the port used for cluster communication to allow incoming TCP connections to the client machine (default port mapping is `9110:9110`)
2. Pass the `-upnp`/`--upnpforwarding` flag to automatically forward the configured ports on your NAT device (router)

**‼️** If `9110:9110` is not the port mapping you'd like to use, supply the `-pi`/`--internalport` and/or `-pe`/`--externalport` parameters to change them as
desired.

## CLI Use (Full)

    ginger-talk.exe [--help] [--internalport | -pi <int>] [--externalport | -pe <int>] [--internalinterface | -il <string>] [--externalinterface | -ip <string>] [--systemname | -s <string>] [--seednodedomain | -sn <string>] [--seednodeport | -sp <int>]
    [--upnpforwarding | -upnp]
    
    OPTIONS:
    
        --internalport, -pi <int>
                              specify the internal port on which to listen. Default is 9110

        --externalport, -pe <int>
                              specify the external port on which to listen. Default is 9110

        --internalinterface, -il <string>
                              specify the interface on which to listen locally. Default is your local IP address

        --externalinterface, -ip <string>
                              specify the interface of domain name to which messages will be received. Default is your external IP address

        --systemname, -s <string>
                              specify the name of the actor system to connect to. Default is 'ginger-talk'

        --seednodedomain, -sn <string>
                              specify the domain on which the seed node is running. Default is '3.143.109.10'

        --seednodeport, -sp <int>
                              specify the port on which the seed node is listening. Default is 9110

        --upnpforwarding, -upnp
                              use this flag to (try to) forward the required ports using UPnP/NAT. Default is disabled

        --help                display this list of options.

## Projects

### GingerTalk.Client

A client for the chat program, this bootstraps the system on your network and connects you to the cluster

### GingerTalk.Server

A specialized version of the client designed to run as a seed node on a known IP and port which can be reached by all clients

### GingerTalk.Lib

Contains the logic used by all clients, including the actor system