using MediatR;
using Teraa.Irc;

namespace Twitch.Irc.Notifications;

public record MessageReceived(Message Message) : INotification;
public record UnknownMessageReceived(string Message) : INotification;
public record Connected : INotification;
