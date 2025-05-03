namespace Teraa.Twitch.Ws;

public interface ITextWebSocketEventHandler;

public interface ITextWebSocketEventHandler<in TEvent> : ITextWebSocketEventHandler
    where TEvent : ITextWebSocketEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken);
}
