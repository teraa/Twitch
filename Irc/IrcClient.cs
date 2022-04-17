using Teraa.Irc;

namespace Twitch.Irc;

public interface IIrcClient
{
    event Func<Message, ValueTask>? MessageReceived;
    void EnqueueMessage(Message message);
}

public class IrcClient : IIrcClient, IDisposable
{
    private readonly WsClient _client;

    public IrcClient(WsClient client)
    {
        _client = client;
    }

    public event Func<Message, ValueTask>? MessageReceived;

    public void EnqueueMessage(Message message)
    {
        // TODO
        throw new NotImplementedException();
    }

    public async Task ReceiverAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        while (true)
        {
            string? rawMessage = await _client.ReceiveAsync(cancellationToken)
                .ConfigureAwait(false);

            if (rawMessage is null)
                break;

            Message message = Message.Parse(rawMessage);

            // TODO
            if (MessageReceived is { } func)
                await func(message).ConfigureAwait(false);
        }
    }

    public async Task SenderAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        // System.Threading.Channels
    }

    public void Dispose()
    {
        _client.Dispose();
    }
}
