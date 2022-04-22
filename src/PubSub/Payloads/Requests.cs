﻿using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace Teraa.Twitch.PubSub.Payloads;

[PublicAPI]
public enum PayloadType
{
    PING,
    PONG,
    LISTEN,
    UNLISTEN,
    RESPONSE,
    MESSAGE,
    RECONNECT,
}

[PublicAPI]
public record Payload(
    PayloadType Type,
    string Nonce = "")
{
    public static Payload<ListenPayloadData> CreateListenRequest(
        IReadOnlyList<string> topics,
        string authToken,
        string nonce = "")
        => new(PayloadType.LISTEN, new ListenPayloadData(topics, authToken), nonce);

    public static Payload CreatePingRequest(string nonce = "")
        => new(PayloadType.PING, nonce);

    public static Payload<UnlistenPayloadData> CreateUnlistenRequest(
        IReadOnlyList<string> topics,
        string nonce = "")
        => new(PayloadType.UNLISTEN, new UnlistenPayloadData(topics));
}

[PublicAPI]
public record Payload<TData>(PayloadType Type, TData Data, string Nonce = "")
    : Payload(Type, Nonce);

[PublicAPI]
public record ListenPayloadData(IReadOnlyList<string> Topics,
    [property: JsonPropertyName("auth_token")] string AuthToken);

[PublicAPI]
public record UnlistenPayloadData(IReadOnlyList<string> Topics);
