using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Irc.Parsing;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.Tmi;

[PublicAPI]
public class TmiServiceOptions
{
    public Uri Uri { get; set; } = new("wss://irc-ws.chat.twitch.tv:443");

    public IMessageParser MessageParser { get; set; } = new MessageParser
    {
        CommandParser = new FastCommandParser(),
        TagsParser = new TagsParser(),
    };

    public TimeSpan PingInterval { get; set; } = TimeSpan.FromMinutes(4);

    public TimeSpan MaxPongDelay { get; set; } = TimeSpan.FromSeconds(10);
}

public interface ITmiClient
{
    void EnqueueMessage(IMessage message);
}

public interface ITmiService : IHostedService
{
    void EnqueueMessage(IMessage message);
    internal Task InvokeAsync<TEvent>(TEvent evt, CancellationToken cancellationToken) where TEvent : ITmiEvent;
    internal DateTimeOffset LastPongAt { get; set; }
    internal IMessageParser MessageParser { get; }
};

[PublicAPI]
public sealed class TmiService : BackgroundService, ITmiService
{
    private readonly TmiServiceOptions _options;
    private readonly ITextWebSocketService _ws;
    private readonly ILogger<TmiService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private DateTimeOffset _lastPong;

    public TmiService(
        IOptions<TmiServiceOptions> options,
        ITextWebSocketService ws,
        ILogger<TmiService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _ws = ws;
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    DateTimeOffset ITmiService.LastPongAt { get => _lastPong; set => _lastPong = value; }

    IMessageParser ITmiService.MessageParser => _options.MessageParser;

    public void EnqueueMessage(IMessage message)
        => _ws.EnqueueMessage(_options.MessageParser.ToString(message));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var enqueuedAt = DateTimeOffset.UtcNow;
            EnqueueMessage(new Message(Command.PING));

            await Task.Delay(_options.MaxPongDelay, stoppingToken);

            if (_lastPong < enqueuedAt)
            {
                _logger.LogWarning("No PONG received within {Time}, reconnecting", _options.MaxPongDelay);
                await _ws.BeginReconnectAsync(stoppingToken);
            }

            await Task.Delay(_options.PingInterval - _options.MaxPongDelay, stoppingToken);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _ws.StartAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);
        await _ws.StopAsync(cancellationToken);
    }

    async Task ITmiService.InvokeAsync<TEvent>(TEvent evt, CancellationToken cancellationToken)
    {
        await Task.Yield();

        IEnumerable<ITmiEventHandler<TEvent>> handlers;

        using var scope = _scopeFactory.CreateScope();

        try
        {
            handlers = scope.ServiceProvider.GetServices<ITmiEventHandler<TEvent>>();
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
