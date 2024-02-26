using System.Text.Json;
using JetBrains.Annotations;
using MediatR;
using Teraa.Twitch.PubSub.Messages.ChannelUnbanRequests;
using Teraa.Twitch.PubSub.Messages.ChatModeratorActions;
using Teraa.Twitch.PubSub.Messages.LowTrustUsers;
using Teraa.Twitch.PubSub.Messages.Shoutout;
using Teraa.Twitch.PubSub.Topics;

namespace Teraa.Twitch.PubSub.Notifications;

[PublicAPI] public record Connected : INotification;
[PublicAPI] public record PayloadReceived(JsonDocument Payload, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record UnknownPayloadReceived(JsonDocument Payload, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record ResponseReceived(string Error, string Nonce, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record PongReceived(DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record MessageReceived(string Topic, JsonDocument Message, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record UnknownMessageReceived(string Topic, JsonDocument Message, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record ReconnectReceived(DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record ChatModeratorActionReceived(ChatModeratorActionsTopic Topic, IModeratorAction Action, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record ChannelUnbanRequestReceived(ChannelUnbanRequestsTopic Topic, IUnbanRequest Request, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record ShoutoutReceived(ShoutoutTopic Topic, Shoutout Shoutout, DateTimeOffset ReceivedAt) : INotification;
[PublicAPI] public record LowTrustUserTreatmentUpdateReceived(LowTrustUsersTopic Topic, TreatmentUpdate TreatmentUpdate, DateTimeOffset ReceivedAt) : INotification;
