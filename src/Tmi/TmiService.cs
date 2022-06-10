using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Irc.Parsing;
using Teraa.Twitch.Tmi.Notifications;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public class TmiServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new("wss://irc-ws.chat.twitch.tv:443");

    public Func<IServiceProvider, IPublisher> PublisherFactory { get; set; } = x => x.GetRequiredService<IPublisher>();

    public IMessageParser MessageParser { get; set; } = new MessageParser();

    public TimeSpan PingInterval { get; set; } = TimeSpan.FromMinutes(4);

    public TimeSpan MaxPongDelay { get; set; } = TimeSpan.FromSeconds(10);
}

[PublicAPI]
public sealed class TmiService : WsService
{
    private readonly TmiServiceOptions _options;
    private readonly ILogger<TmiService> _logger;
    private readonly IServiceProvider _services;
    private DateTimeOffset _lastPongAt;

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

    public void EnqueueMessage(IMessage message)
        => EnqueueMessage(message.ToString()!); // TODO: fix in upstream

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsReconnecting)
            {
                var enqueuedAt = DateTimeOffset.UtcNow;
                EnqueueMessage(new Message(Command.PING));

                await Task.Delay(_options.MaxPongDelay, stoppingToken);

                if (_lastPongAt < enqueuedAt)
                {
                    _logger.LogWarning("No PONG received within {Time}, reconnecting", _options.MaxPongDelay);
                    _ = ReconnectAsync(stoppingToken);
                }
            }

            await Task.Delay(_options.PingInterval - _options.MaxPongDelay, stoppingToken);
        }
    }

    protected override async ValueTask HandleConnectAsync(CancellationToken cancellationToken)
    {
        await PublishAsync(new Connected(), cancellationToken);
    }

    protected override async ValueTask HandleReceivedAsync(string rawMessage, CancellationToken cancellationToken)
    {
        INotification notification;

        if (_options.MessageParser.TryParse(rawMessage, out var message))
        {
            switch (message)
            {
                case {Command: Command.RECONNECT}:
                    _ = ReconnectAsync(cancellationToken);
                    break;

                case {Command: Command.PING}:
                    EnqueueMessage(new Message(Command.PONG));
                    break;

                case {Command: Command.PONG}:
                    _lastPongAt = DateTimeOffset.UtcNow;
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
