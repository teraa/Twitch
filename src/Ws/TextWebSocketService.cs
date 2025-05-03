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
}

public sealed class TextWebSocketService : ITextWebSocketService
{
    private readonly ITextWebSocketClient _client;
    private readonly TextWebSocketServiceOptions _options;
    private readonly ILogger<TextWebSocketService> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private CancellationTokenSource? _stoppingCts;
    private CancellationTokenSource? _reconnectCts;
    private TaskCompletionSource _connectedTcs;
    private readonly Channel<string> _sendChannel;
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

        // This needs to be a field so that StartAsync can wait until the connection establishes.
        // Otherwise, it could have been a local variable passed around to other methods.
        _connectedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
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
        await _connectedTcs.Task;

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

        _connectedTcs.TrySetCanceled(CancellationToken.None);

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

        // We can start writer right away because it will wait for connected TCS to complete before writing.
        var writerTask = Writer(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Connect
                await ConnectRetryPolicy.ExecuteAsync(() => _client.ConnectAsync(_options.Uri, stoppingToken));

                // Create and save the reconnect CTS before starting the Reader task which could cancel it.
                // Theoretically, the Writer task could have tried to use it already since we started it already,
                // but it can't since it will have blocked on waiting for connected TCS to complete.
                _reconnectCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);

                // Store the tasks we're executing
                // We assume these tasks yield immediately and never throw
                var readerTask = Reader(_reconnectCts.Token);

                // Connect succeeded, invoke and await connected event handlers
                // We await here because we want to signal to the writer only after the connected handlers run.
                // That way the client can send messages before the writer resumes consuming the send queue.
                await InvokeAsync(new ConnectedEvent(this, _connectCount++), stoppingToken);

                // Signal to the writer that it can resume consuming the send queue.
                // Also signal to the StartAsync that it can return, in case this is the first connection attempt.
                _connectedTcs.TrySetResult();


                // Wait for something to call BeginReconnect
                try
                {
                    await Task.Delay(-1, _reconnectCts.Token);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
                if (stoppingToken.IsCancellationRequested)
                    break;


                // Reconnect:
                // If we got here, it means something requested the reconnect,
                // so we will begin closing procedure before reconnecting

                // Writer will observe a new TCS which will stop it from sending data until we reconnect.
                _connectedTcs = new TaskCompletionSource();

                // This task is completed already since the cancel token that was passed to it was cancelled
                // if we got to this point
                await readerTask.WaitAsync(stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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

        // If we get here, it means stoppingToken was cancelled so the writer task here is done already.
        await writerTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
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

    private async Task Writer(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await foreach (var message in _sendChannel.Reader.ReadAllAsync(stoppingToken))
        {
            _logger.LogTrace("Sending: {Message}", message);

            // Repeatedly try to send the same message until we succeed
            while (!stoppingToken.IsCancellationRequested)
            {
                // If send fails, we need to retry sending this message after reconnect,
                // unless its cancel exception.
                try
                {
                    // Wait until the client is connected
                    await _connectedTcs.Task;
                    await _client.SendAsync(message, stoppingToken);
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Either the connect TCS or the stopping token was cancelled, in any case we should break out.
                    break;
                }
                catch (Exception ex)
                {
                    // Reconnect and retry
                    _logger.LogError(ex, "Error sending message");
                    await BeginReconnect(RequestSource.Writer, stoppingToken);
                }
            }
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
    }
}
