module Message

open System

type User =
    | User of string
    override this.ToString() =
        match this with
        | User u -> u

type Topic =
    | Topic of string
    override this.ToString() =
        match this with
        | Topic t -> t

type Message =
    | Message of string
    override this.ToString() =
        match this with
        | Message m -> m

type Subscription =
    | Sub of Topic
    | Unsub of Topic

type SentMessage =
    { User: User
      Topic: Topic
      Message: Message }

module SentMessage =
    let create user topic message =
        { User = user
          Topic = topic
          Message = message }

type TimestampedMessage =
    { User: User
      Topic: Topic
      Message: Message
      Timestamp: DateTimeOffset }

module TimestampedMessage =
    let timestamp (message: SentMessage) =
        { User = message.User
          Topic = message.Topic
          Message = message.Message
          Timestamp = DateTimeOffset.Now }

type ChatCommand =
    | Subscription of Subscription
    | Incoming of SentMessage
    | Outgoing of SentMessage
