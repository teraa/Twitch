using System.Text.Json;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Twitch.PubSub.Notifications;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.PubSub;

public class PubSubServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://pubsub-edge.twitch.tv");
}

[PublicAPI]
public class PubSubService : WsService
{
    private readonly ILogger<PubSubService> _logger;
    private readonly IPublisher _publisher;

    public PubSubService(
        IWsClient client,
        IOptions<PubSubServiceOptions> options,
        ILogger<PubSubService> logger,
        IPublisher publisher)
        : base(client, options, logger)
    {
        _logger = logger;
        _publisher = publisher;
    }

    protected override async ValueTask HandleConnectAsync(CancellationToken cancellationToken)
    {
        await PublishAsync(new Connected(), cancellationToken);
    }

    protected override async ValueTask HandleReceivedAsync(string message, CancellationToken cancellationToken)
    {
        await PublishAsync(new MessageReceived(message), cancellationToken);
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
