using System.Text.Json;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Twitch.PubSub.Notifications;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.PubSub;

public class PubSubServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://pubsub-edge.twitch.tv");
    public Func<IServiceProvider, IPublisher> PublisherFactory { get; set; } = x => x.GetRequiredService<IPublisher>();
}

[PublicAPI]
public class PubSubService : WsService
{
    private readonly PubSubServiceOptions _options;
    private readonly ILogger<PubSubService> _logger;
    private readonly IServiceProvider _services;

    public PubSubService(
        IWsClient client,
        IOptions<PubSubServiceOptions> options,
        ILogger<PubSubService> logger,
        IServiceProvider services)
        : base(client, options, logger)
    {
        _options = options.Value;
        _logger = logger;
        _services = services;
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
