using JetBrains.Annotations;
using MediatR;

namespace Teraa.Twitch.PubSub.Notifications;

[PublicAPI] public record Connected : INotification;
[PublicAPI] public record MessageReceived(string Message) : INotification;
