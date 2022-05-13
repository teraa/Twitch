using System.Text.Json.Serialization;
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
public record Payload(PayloadType Type, string? Nonce = null)
{
    public static Payload CreatePing()
        => new(PayloadType.PING);

    public static Payload<ListenPayloadData> CreateListen(
        IReadOnlyList<string> topics,
        string authToken,
        string? nonce = null)
        => new(PayloadType.LISTEN, new ListenPayloadData(topics, authToken), nonce);

    public static Payload<UnlistenPayloadData> CreateUnlisten(
        IReadOnlyList<string> topics,
        string? nonce = null)
        => new(PayloadType.UNLISTEN, new UnlistenPayloadData(topics), nonce);
}

[PublicAPI]
public record Payload<TData>(
    PayloadType Type,
    TData Data,
    string? Nonce = null
) : Payload(Type, Nonce);

[PublicAPI]
public record ListenPayloadData(
    [property: JsonPropertyName("topics")] IReadOnlyList<string> Topics,
    [property: JsonPropertyName("auth_token")] string AuthToken);

[PublicAPI]
public record UnlistenPayloadData(
    [property: JsonPropertyName("topics")] IReadOnlyList<string> Topics);
