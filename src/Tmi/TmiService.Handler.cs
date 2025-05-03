namespace Teraa.Twitch.Tmi;

public interface ITmiEventHandler;

public interface ITmiEventHandler<in TEvent> : ITmiEventHandler
    where TEvent : ITmiEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken);
}
