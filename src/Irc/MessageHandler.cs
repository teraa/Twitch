using Teraa.Irc;

namespace Twitch.Irc;

public interface IMessageHandler
{
    ValueTask HandleAsync(Message message, CancellationToken cancellationToken = default);
}

public class MessageHandler : IMessageHandler
{
    public ValueTask HandleAsync(Message message, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
