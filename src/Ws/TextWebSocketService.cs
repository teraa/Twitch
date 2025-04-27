using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Teraa.Twitch.Ws;

public sealed record TextWebSocketServiceOptions(
    Uri Uri
);

public interface ITextWebSocketService : IDisposable;

public sealed class TextWebSocketService : IHostedService, ITextWebSocketService
{
    private readonly ITextWebSocketClient _client;
    private readonly TextWebSocketServiceOptions _options;
    private readonly ILogger<TextWebSocketService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private Task? _readerTask;
    private CancellationTokenSource? _stoppingCts;
    private readonly SemaphoreSlim _reconnectSem = new(1, 1);
    private readonly Channel<string> _sendChannel;

    public TextWebSocketService(
        ITextWebSocketClient client,
        IOptions<TextWebSocketServiceOptions> options,
        ILogger<TextWebSocketService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    // Sends message directly, bypassing the queue.
    // This should also be used to send messages which should not be retried after a reconnect automatically,
    // e.g. authentication messages, because these are usually initiated from the reconnect event handler
    // so we will re-send them manually there again.
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        await _client.SendAsync(message, cancellationToken);
    }

    private async Task Reconnect(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _reconnectSem.WaitAsync(stoppingToken);
            try
            {
                await ReconnectInternal(stoppingToken);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reconnecting");
            }
            finally
            {
                _reconnectSem.Release();
            }
        }
    }

    private async Task ReconnectInternal(CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        // We can only get here from reader task itself,
        // it is guaranteed that it's not null here and there is no race conditions or concurrent access.
        // This task should be completed at this point since we call reconnect on the exit of read task only.
        await _readerTask!.WaitAsync(stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        await _client.CloseAsync(stoppingToken);

        // Connect then store the reader task same as in StartAsync
        await _client.ConnectAsync(_options.Uri, stoppingToken);
        _readerTask = Reader(stoppingToken);

        // Reconnect succeeded, invoke reconnected event handlers
        _ = InvokeAsync(new ReconnectEvent(this), stoppingToken);
    }

    private async Task InvokeAsync<TEvent>(TEvent evt, CancellationToken cancellationToken)
        where TEvent : ITextWebSocketEvent
    {
        await Task.Yield();

        IEnumerable<ITextWebSocketEventHandler<TEvent>> handlers;

        using var scope = _scopeFactory.CreateScope();

        try
        {
            handlers = scope.ServiceProvider.GetServices<ITextWebSocketEventHandler<TEvent>>();
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invoking {HandlerType} handler for {EventType} event",
                    handler.GetType(),
                    typeof(TEvent));
            }
        }
    }

    private async Task Reader(CancellationToken stoppingToken)
    {
        await Task.Yield();
        try
        {
            await ReaderInternal(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in reader task");
            _ = Reconnect(stoppingToken);
        }
    }

    private async Task ReaderInternal(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = await _client.ReceiveAsync(stoppingToken);

            if (result.Type is TextWebSocketReceiveResultType.ClosedUnexpectedly)
            {
                _logger.LogInformation("WebSocket closed unexpectedly");
                _ = Reconnect(stoppingToken);
                break;
            }

            if (result.Type is TextWebSocketReceiveResultType.CloseMessageReceived)
            {
                _logger.LogInformation("Received close message");
                _ = Reconnect(stoppingToken);
                break;
            }

            _logger.LogInformation("Received message");
            _ = InvokeAsync(new MessageReceivedEvent(this, result.Message!), stoppingToken);
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            _stoppingCts?.Cancel();
        }
        catch
        {
            // ignored
        }

        // Create linked token to allow cancelling executing task from provided token
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await _client.ConnectAsync(_options.Uri, cancellationToken);

        // Store the task we're executing
        _readerTask = Reader(_stoppingCts.Token);

        _logger.LogInformation("Started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop called without start
        if (_readerTask == null)
        {
            return;
        }

        try
        {
            // First close then cancel the token. This will try to gracefully close the WebSocket by
            // sending the close frame first. If we cancel first, the reader task will abort the WebSocket
            // and prevent us from sending the close frame.
            await _client.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing client");
        }

        try
        {
            // Signal cancellation to the executing method
            _stoppingCts!.Cancel();
        }
        catch
        {
            // ignored
        }

        await _readerTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("Stopped");
    }

    public void Dispose()
    {
        try
        {
            _stoppingCts?.Cancel();
        }
        catch
        {
            // ignored
        }

        _client.Dispose();
        _stoppingCts?.Dispose();
        _reconnectSem.Dispose();
        _readerTask = null;
    }
}

public interface ITextWebSocketEvent
{
    ITextWebSocketService Service { get; }
}

public sealed record ReconnectEvent(
    ITextWebSocketService Service
) : ITextWebSocketEvent;

public sealed record MessageReceivedEvent(
    ITextWebSocketService Service,
    string Message
) : ITextWebSocketEvent;

public interface ITextWebSocketEventHandler;

public interface ITextWebSocketEventHandler<in TEvent> : ITextWebSocketEventHandler
    where TEvent : ITextWebSocketEvent
{
    ValueTask HandleAsync(TEvent evt, CancellationToken cancellationToken);
}
