using JetBrains.Annotations;
using MediatR;
using Teraa.Irc;

namespace Teraa.Twitch.Tmi.Notifications;

[PublicAPI] public record MessageReceived(Message Message) : INotification;
[PublicAPI] public record UnknownMessageReceived(string Message) : INotification;
[PublicAPI] public record Connected : INotification;
