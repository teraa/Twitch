namespace Teraa.Twitch.EventSub;

public interface IEventSubEventHandler;

public interface IEventSubEventHandler<in TEvent> : IEventSubEventHandler
    where TEvent : IEventSubEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken);
}
