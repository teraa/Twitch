using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Twitch.Tmi.Notifications;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

public class TmiServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
}

[PublicAPI]
public sealed class TmiService : WsService
{
    private readonly IPublisher _publisher;
    private readonly ILogger<TmiService> _logger;

    public TmiService(
        IWsClient client,
        IPublisher publisher,
        IOptions<TmiServiceOptions> options,
        ILogger<TmiService> logger)
        : base(client, options, logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public void EnqueueMessage(Message message)
        => EnqueueMessage(message.ToString());

    protected override async ValueTask HandleConnectAsync(CancellationToken cancellationToken)
    {
        await PublishAsync(new Connected(), cancellationToken);
    }

    protected override async ValueTask HandleReceivedAsync(string rawMessage, CancellationToken cancellationToken)
    {
        INotification notification;

        if (Message.TryParse(rawMessage, out var message))
        {
            switch (message)
            {
                case {Command: Command.RECONNECT}:
                    _ = ReconnectAsync(cancellationToken);
                    break;

                case {Command: Command.PING}:
                    EnqueueMessage(new Message
                    {
                        Command = Command.PONG,
                    });
                    break;
            }

            notification = new MessageReceived(message);
        }
        else
        {
            _logger.LogTrace("Parse failed: {Message}", rawMessage);

            notification = new UnknownMessageReceived(rawMessage);
        }

        await PublishAsync(notification, cancellationToken);
    }

    private async Task PublishAsync(INotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.Publish(notification, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {Notification}", notification.GetType().Name);
        }
    }
}
