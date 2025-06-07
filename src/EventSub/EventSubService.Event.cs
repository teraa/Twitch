using Teraa.Twitch.EventSub.Messages;

namespace Teraa.Twitch.EventSub;

public interface IEventSubEvent
{
    IEventSubService Service { get; }
}

public sealed record ConnectedEvent(
    IEventSubService Service
) : IEventSubEvent;

// keepalive, reconnect, and close messages should be handled by lib.
// That means we don't need to expose them
// revocation idk

public interface IMessageReceived
{
    string MessageId { get; }
    DateTimeOffset MessageTimestamp { get; }
}

public sealed record WelcomeMessageReceived(
    IEventSubService Service,
    string MessageId,
    DateTimeOffset MessageTimestamp,
    Session Session
) : IEventSubEvent, IMessageReceived;

public sealed record KeepaliveMessageReceived(
    IEventSubService Service,
    string MessageId,
    DateTimeOffset MessageTimestamp
) : IEventSubEvent, IMessageReceived;

public sealed record NotificationMessageReceived<TEvent>(
    IEventSubService Service,
    string MessageId,
    DateTimeOffset MessageTimestamp,
    Subscription Subscription,
    TEvent Event
) : IEventSubEvent, IMessageReceived;

public sealed record RevocationMessageReceived(
    IEventSubService Service,
    string MessageId,
    DateTimeOffset MessageTimestamp,
    string SubscriptionType,
    string SubscriptionVersion,
    Subscription Subscription
) : IEventSubEvent, IMessageReceived;
