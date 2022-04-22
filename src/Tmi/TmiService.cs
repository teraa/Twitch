using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Twitch.Tmi.Notifications;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

public class TmiServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
    public Func<IServiceProvider, IPublisher> PublisherFactory { get; set; } = x => x.GetRequiredService<IPublisher>();
}

[PublicAPI]
public sealed class TmiService : WsService
{
    private readonly TmiServiceOptions _options;
    private readonly ILogger<TmiService> _logger;
    private readonly IServiceProvider _services;

    public TmiService(
        IWsClient client,
        IOptions<TmiServiceOptions> options,
        ILogger<TmiService> logger,
        IServiceProvider services)
        : base(client, options, logger)
    {
        _options = options.Value;
        _logger = logger;
        _services = services;
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
            _logger.LogTrace("Unknown message: {Message}", rawMessage);

            notification = new UnknownMessageReceived(rawMessage);
        }

        await PublishAsync(notification, cancellationToken);
    }

    private async Task PublishAsync(INotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var publisher = _options.PublisherFactory(scope.ServiceProvider);
            await publisher.Publish(notification, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {Notification}", notification.GetType().Name);
        }
    }
}
