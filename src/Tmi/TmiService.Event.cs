using Teraa.Irc;

namespace Teraa.Twitch.Tmi;

public interface ITmiEvent
{
    ITmiService Service { get; }
}

public sealed record ConnectedEvent(
    ITmiService Service
) : ITmiEvent;

public sealed record MessageReceivedEvent(
    ITmiService Service,
    IMessage Message
) : ITmiEvent;

public sealed record UnknownMessageReceivedEvent(
    ITmiService Service,
    string RawMessage
) : ITmiEvent;
