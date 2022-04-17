using System.Collections.Concurrent;
using Teraa.Irc;
namespace Twitch.Irc;

public class IrcClient
{
    private readonly WsClient _client;
    private readonly PriorityQueue<Message, int> _sendQueue;

    public IrcClient(WsClient client)
    {
        _client = client;
        _sendQueue = new();
    }

    public event Func<Message, ValueTask>? MessageReceived;

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

            if (MessageReceived is { } func)
                await func(message).ConfigureAwait(false);
        }
    }

    public async Task SenderAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        // System.Threading.Channels
    }
}
