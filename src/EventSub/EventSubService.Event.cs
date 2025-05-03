namespace Teraa.Twitch.EventSub;

public interface IEventSubEvent
{
    IEventSubService Service { get; }
}

public sealed record ConnectedEvent(
    IEventSubService Service
) : IEventSubEvent;
