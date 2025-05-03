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

public interface IEventSubService : IHostedService;

public class EventSubService : IEventSubService
{
    private readonly EventSubServiceOptions _options;
    private readonly ITextWebSocketService _ws;
    private readonly ILogger<EventSubService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

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

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _ws.StartAsync(cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _ws.StopAsync(cancellationToken);
    }
}
