using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.EventSub;

public class EventSubServiceOptions
{
    public Uri Uri { get; set; } = new("wss://eventsub.wss.twitch.tv/ws");
}

public interface IEventSubService : IHostedService
{
    internal TimeSpan KeepaliveTimeout { get; set; }
    internal DateTimeOffset LastKeepaliveAt { get; set; }

    internal Task InvokeAsync(IEventSubEvent evt, CancellationToken cancellationToken);
    internal Task InvokeAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : IEventSubEvent;
}

public class EventSubService : BackgroundService, IEventSubService
{
    private readonly EventSubServiceOptions _options;
    private readonly ITextWebSocketService _ws;
    private readonly ILogger<EventSubService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private DateTimeOffset _lastKeepalive;
    private TimeSpan _keepaliveTimeout;

    public EventSubService(
        IOptions<EventSubServiceOptions> options,
        ITextWebSocketService ws,
        ILogger<EventSubService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _ws = ws;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    DateTimeOffset IEventSubService.LastKeepaliveAt
    {
        get => _lastKeepalive;
        set => _lastKeepalive = value;
    }

    TimeSpan IEventSubService.KeepaliveTimeout
    {
        get => _keepaliveTimeout;
        set => _keepaliveTimeout = value;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _ws.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_keepaliveTimeout == TimeSpan.Zero)
            {
                _logger.LogDebug("Keepalive timeout not yet set, skipping keepalive check");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                continue;
            }

            
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _ws.StopAsync(cancellationToken);
    }

    async Task IEventSubService.InvokeAsync(IEventSubEvent evt, CancellationToken cancellationToken)
    {
        await (evt switch
        {
            {} => Task.CompletedTask,
            _ => throw new NotImplementedException(),
        });
    }

    async Task IEventSubService.InvokeAsync<TEvent>(TEvent evt, CancellationToken cancellationToken)
    {
        await Task.Yield();

        IEnumerable<IEventSubEventHandler<TEvent>> handlers;

        using var scope = _scopeFactory.CreateScope();

        try
        {
            handlers = scope.ServiceProvider.GetServices<IEventSubEventHandler<TEvent>>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving handlers from the service provider");
            return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(evt, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking {HandlerType} handler for {EventType} event",
                    handler.GetType(),
                    typeof(TEvent));
            }
        }
    }
}
