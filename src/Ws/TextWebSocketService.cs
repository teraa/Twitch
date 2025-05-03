using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Contrib.WaitAndRetry;
using Polly.Retry;
using Teraa.Twitch.Ws.Events;

namespace Teraa.Twitch.Ws;

public sealed record TextWebSocketServiceOptions(
    Uri Uri
);

public interface ITextWebSocketService : IHostedService, IDisposable
{
    void EnqueueMessage(string message);
    Task SendAsync(string message, CancellationToken cancellationToken);
    Task BeginReconnectAsync(CancellationToken cancellationToken);
}

public sealed class TextWebSocketService : ITextWebSocketService
{
    private readonly ITextWebSocketClient _client;
    private readonly TextWebSocketServiceOptions _options;
    private readonly ILogger<TextWebSocketService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _stoppingCts;
    private CancellationTokenSource? _reconnectCts;
    private readonly Channel<string> _sendChannel;
    private string? _unsentMessage;
    private Task? _connectorTask;
    private int _connectCount;

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
        _sendChannel = Channel.CreateUnbounded<string>(
            new UnboundedChannelOptions
            {
                SingleReader = true,
                SingleWriter = true,
            }
        );
    }

    public AsyncRetryPolicy ConnectRetryPolicy { get; set; } = Policy
        .Handle<Exception>(ex => ex is not OperationCanceledException)
        .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                medianFirstRetryDelay: TimeSpan.FromSeconds(1),
                retryCount: 20,
                fastFirst: true
            )
        );

    public void EnqueueMessage(string message)
    {
        // This will always succeed for an unbounded channel.
        _sendChannel.Writer.TryWrite(message);
    }

    // Sends message directly, bypassing the queue.
    // This should also be used to send messages which should not be retried after a reconnect automatically,
    // e.g. authentication messages, because these are usually initiated from the reconnect event handler
    // so we will re-send them manually there again.
    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        await _client.SendAsync(message, cancellationToken);
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

        _connectorTask = Connector(_stoppingCts.Token);

        _logger.LogInformation("Started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop called without start
        if (_connectorTask == null)
        {
            return;
        }

        try
        {
            await _client.CloseAsync(cancellationToken);
        }
        catch
        {
            // ignored
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

        await _connectorTask.WaitAsync(cancellationToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

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
        _reconnectCts?.Dispose();
        _connectorTask = null;
    }

    // Should only have one caller
    private async Task Connector(CancellationToken stoppingToken)
    {
        await Task.Yield();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Connect
                await ConnectRetryPolicy.ExecuteAsync(() => _client.ConnectAsync(_options.Uri, stoppingToken));

                // Create and save the reconnect CTS before starting the Reader and Writer tasks which could cancel it.
                _reconnectCts?.Dispose();
                _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                // Store the tasks we're executing
                // We assume these tasks yield immediately and never throw
                var readerTask = Reader(_reconnectCts.Token);
                var writerTask = Writer(_reconnectCts.Token);

                // Connect succeeded, invoke and await connected event handlers
                // We await here because we want to signal to the writer only after the connected handlers run.
                // That way the client can send messages before the writer resumes consuming the send queue.
                await InvokeAsync(new ConnectedEvent(this, _connectCount++), stoppingToken);


                // Wait for something to call BeginReconnect
                try
                {
                    await Task.Delay(-1, _reconnectCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }

                // Reconnect or stop was requested.
                // These tasks are completed already since the cancel token that was passed to them was cancelled
                // if we got to this point
                await readerTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
                await writerTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            catch (OperationCanceledException)
            {
                // ignored
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in connector task");
            }
        }
    }

    public async Task BeginReconnectAsync(CancellationToken cancellationToken)
    {
        await BeginReconnect(RequestSource.External, cancellationToken);
    }

    private async Task BeginReconnect(RequestSource source, CancellationToken cancellationToken)
    {
        // log who requested reconnect
        _logger.LogDebug("Reconnect request from {Source}", source);

        // First close then cancel the token. This will try to gracefully close the WebSocket by
        // sending the close frame first. If we cancel first, the reader task will abort the WebSocket
        // and prevent us from sending the close frame.
        // CloseAsync method doesn't throw unless cancelled.
        try
        {
            await _client.CloseAsync(cancellationToken);
        }
        catch
        {
            // ignored
        }

        // cancel connection CTS
        try
        {
            _reconnectCts!.Cancel();
        }
        catch
        {
            // ignored
        }
    }

    private async Task Reader(CancellationToken cancellationToken)
    {
        await Task.Yield();
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _client.ReceiveAsync(cancellationToken);

                if (result.Type is TextWebSocketReceiveResultType.ClosedUnexpectedly)
                {
                    _logger.LogInformation("WebSocket closed unexpectedly");
                    await BeginReconnect(RequestSource.Reader, cancellationToken);
                    break;
                }

                if (result.Type is TextWebSocketReceiveResultType.CloseMessageReceived)
                {
                    _logger.LogInformation("Received close message");
                    await BeginReconnect(RequestSource.Reader, cancellationToken);
                    break;
                }

                _logger.LogInformation("Received message");
                _ = InvokeAsync(new MessageReceivedEvent(this, result.Message!), cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Either the connect TCS or the stopping token was cancelled, in any case we should break out.
        }
        catch (Exception ex)
        {
            // Reconnect
            _logger.LogError(ex, "Error in reader task");
            await BeginReconnect(RequestSource.Reader, cancellationToken);
        }
    }

    private async Task Writer(CancellationToken cancellationToken)
    {
        await Task.Yield();

        try
        {
            // Retry sending the unsent message from last session
            if (_unsentMessage is not null)
            {
                await _client.SendAsync(_unsentMessage, cancellationToken);
                _unsentMessage = null;
            }

            await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogTrace("Sending: {Message}", message);

                // If send fails, we need to retry sending this message after reconnect,
                // unless its cancel exception.
                _unsentMessage = message;
                await _client.SendAsync(message, cancellationToken);
                _unsentMessage = null;
            }
        }
        catch (OperationCanceledException)
        {
            // Either the connect TCS or the stopping token was cancelled, in any case we should break out.
        }
        catch (Exception ex)
        {
            // Reconnect
            _logger.LogError(ex, "Error sending message");
            await BeginReconnect(RequestSource.Writer, cancellationToken);
        }
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

    private enum RequestSource
    {
        Reader,
        Writer,
        External
    }
}
