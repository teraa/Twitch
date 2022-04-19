using MediatR;

namespace Teraa.Twitch.PubSub.Notifications;

public record Connected : INotification;
public record MessageReceived(string Message) : INotification;
