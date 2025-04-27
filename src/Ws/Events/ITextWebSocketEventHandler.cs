namespace Teraa.Twitch.Ws.Events;

public interface ITextWebSocketEventHandler;

public interface ITextWebSocketEventHandler<in TEvent> : ITextWebSocketEventHandler
    where TEvent : ITextWebSocketEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken);
}
