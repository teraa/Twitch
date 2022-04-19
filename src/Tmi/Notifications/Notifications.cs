using MediatR;
using Teraa.Irc;

namespace Teraa.Twitch.Tmi.Notifications;

public record MessageReceived(Message Message) : INotification;
public record UnknownMessageReceived(string Message) : INotification;
public record Connected : INotification;
