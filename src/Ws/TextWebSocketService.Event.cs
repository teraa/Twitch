namespace Teraa.Twitch.Ws;

public interface ITextWebSocketEvent
{
    ITextWebSocketService Service { get; }
}

public sealed record ConnectedEvent(
    ITextWebSocketService Service,
    int ConnectCount
) : ITextWebSocketEvent;

public sealed record MessageReceivedEvent(
    ITextWebSocketService Service,
    string Message
) : ITextWebSocketEvent;
