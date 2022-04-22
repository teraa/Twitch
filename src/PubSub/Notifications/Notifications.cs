using System.Text.Json;
using JetBrains.Annotations;
using MediatR;

namespace Teraa.Twitch.PubSub.Notifications;

[PublicAPI] public record Connected : INotification;
[PublicAPI] public record PayloadReceived(JsonDocument Payload) : INotification;
[PublicAPI] public record UnknownPayloadReceived(JsonDocument Payload) : INotification;
[PublicAPI] public record ResponseReceived(string Error, string Nonce) : INotification;
[PublicAPI] public record PongReceived : INotification;
[PublicAPI] public record MessageReceived(string Topic, JsonDocument Message) : INotification;
[PublicAPI] public record ReconnectReceived : INotification;
