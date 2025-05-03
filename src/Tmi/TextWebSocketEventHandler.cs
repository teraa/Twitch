using Microsoft.Extensions.Logging;
using Teraa.Irc;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;


internal class ConnectedEventHandler : ITextWebSocketEventHandler<Ws.ConnectedEvent>
{
    private readonly ITmiService _tmi;

    public ConnectedEventHandler(ITmiService tmi)
    {
        _tmi = tmi;
    }

    public async ValueTask HandleAsync(Ws.ConnectedEvent evt, CancellationToken cancellationToken)
    {
        await _tmi.InvokeAsync(new ConnectedEvent(_tmi), cancellationToken);
    }
}


internal class MessageReceivedEventHandler : ITextWebSocketEventHandler<Ws.MessageReceivedEvent>
{
    private readonly ITmiService _tmi;
    private readonly ILogger<MessageReceivedEventHandler> _logger;

    public MessageReceivedEventHandler(
        ITmiService tmi,
        ILogger<MessageReceivedEventHandler> logger)
    {
        _tmi = tmi;
        _logger = logger;
    }

    public async ValueTask HandleAsync(Ws.MessageReceivedEvent evt, CancellationToken cancellationToken)
    {
        ITmiEvent newEvt;
        var rawMessage = evt.Message;

        if (_tmi.MessageParser.TryParse(rawMessage, out var message))
        {
            switch (message)
            {
                case {Command: Command.RECONNECT}:
                    await evt.Service.BeginReconnectAsync(cancellationToken);
                    break;

                case {Command: Command.PING}:
                    _tmi.EnqueueMessage(new Message(Command.PONG));
                    break;

                case {Command: Command.PONG}:
                    _tmi.LastPongAt = DateTimeOffset.UtcNow;
                    break;
            }

            newEvt = new MessageReceivedEvent(_tmi, message);
        }
        else
        {
            _logger.LogTrace("Unknown message: {Message}", rawMessage);

            newEvt = new UnknownMessageReceivedEvent(_tmi, rawMessage);
        }

        await _tmi.InvokeAsync(newEvt, cancellationToken);
    }
}
