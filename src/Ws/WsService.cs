using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Teraa.Twitch.Ws;

[PublicAPI]
public interface IWsServiceOptions
{
    Uri Uri { get; set; }
}

[PublicAPI]
public abstract class WsService : BackgroundService
{
    private readonly IWsClient _client;
    private readonly Channel<string> _sendChannel;
    private readonly IWsServiceOptions _options;
    private readonly ILogger<WsService> _logger;
    private readonly SemaphoreSlim _sem;
    private Task? _receiverTask, _senderTask;
    private CancellationTokenSource? _cts;
    private DateTimeOffset _connectedAt;
    private int _fastDisconnects;

    protected WsService(IWsClient client, IOptions<IWsServiceOptions> options, ILogger<WsService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;

        _sem = new SemaphoreSlim(1, 1);
        _sendChannel = Channel.CreateUnbounded<string>(new()
        {
            SingleReader = true,
            SingleWriter = true,
        });
    }

    [MemberNotNullWhen(true, nameof(_cts))]
    public bool IsStarted { get; private set; }
    protected bool IsReconnecting { get; private set; }

    private static int GetDelaySeconds(int iteration)
    {
        const int max = 7;
        return 1 << (iteration > max ? max : iteration);
    }

    public void EnqueueMessage(string message)
    {
        bool success = _sendChannel.Writer.TryWrite(message);
        Debug.Assert(success);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.CompletedTask;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await _sem.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (IsStarted)
                throw new InvalidOperationException("Already started");

            IsStarted = true;

            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            await StartInternalAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _sem.Release();
        }

        await base.StartAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _sem.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (!IsStarted)
                throw new InvalidOperationException("Not started");

            // Properly disconnect before calling Cancel(), as that cancels the read operation
            // which terminates the connection without close handshake
            await DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);

            // Cancel needed to await tasks inside StopInternalAsync
            await _cts.CancelAsync();
            await StopInternalAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            IsStarted = false;
            _sem.Release();
        }

        await base.StopAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    protected abstract ValueTask HandleConnectAsync(CancellationToken cancellationToken);
    protected abstract ValueTask HandleReceivedAsync(string message, CancellationToken cancellationToken);

    private async Task HandleConnectWrapperAsync(CancellationToken cancellationToken)
    {
        try
        {
            await HandleConnectAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Handler}", nameof(HandleConnectAsync));
        }
    }

    private async Task HandleReceivedWrapperAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await HandleReceivedAsync(message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in {Handler}", nameof(HandleReceivedAsync));
        }
    }

    private async Task StartInternalAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsStarted);

        if (_client.IsConnected) return;

        await _client.ConnectAsync(_options.Uri, cancellationToken)
            .ConfigureAwait(false);

        _connectedAt = DateTimeOffset.UtcNow;
        _logger.LogInformation("Connected to {Uri}", _options.Uri);

        _receiverTask = ReceiverAsync(_cts.Token);
        _senderTask = SenderAsync(_cts.Token);
        _ = HandleConnectWrapperAsync(cancellationToken);
    }

    private async Task StopInternalAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsStarted);

        try
        {
            await DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting the client");
        }

        if (_receiverTask is { } receiverTask)
            await receiverTask;

        if (_senderTask is { } senderTask)
            await senderTask;
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        if (!_client.IsConnected) return;

        await _client.DisconnectAsync(cancellationToken)
            .ConfigureAwait(false);

        _logger.LogInformation("Disconnected");
    }

    protected async Task ReconnectAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        if (cancellationToken.IsCancellationRequested)
            return;

        try
        {
            await ReconnectInternalAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            IsReconnecting = false;
            if (ex is not OperationCanceledException)
                _logger.LogError(ex, "Error reconnecting");
        }
    }

    private async Task ReconnectInternalAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsStarted);

        await _sem.WaitAsync(cancellationToken);
        try
        {
            if (IsReconnecting)
            {
                _logger.LogDebug("Concurrent reconnect request");
                return;
            }

            IsReconnecting = true;
            await _cts.CancelAsync(); // TODO: this will run and throw sometimes when manually stopping
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            cancellationToken = _cts.Token;
        }
        finally
        {
            _sem.Release();
        }

        var fastDelay = TimeSpan.FromSeconds(GetDelaySeconds(_fastDisconnects));
        var fastDelayRemaining = fastDelay - (DateTimeOffset.UtcNow - _connectedAt);
        if (fastDelayRemaining > TimeSpan.Zero)
        {
            _logger.LogDebug("Fast disconnect #{Count} delay {Delay} ({RemainingDelay})",
                _fastDisconnects, fastDelay, fastDelayRemaining);
            _fastDisconnects++;
            await Task.Delay(fastDelayRemaining, cancellationToken);
        }
        else
        {
            _fastDisconnects = 0;
        }

        int attempt = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Reconnect #{Attempt} start", attempt);

            await StopInternalAsync(cancellationToken);

            try
            {
                await StartInternalAsync(cancellationToken);
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error restarting");
            }

            attempt++;

            var delaySeconds = GetDelaySeconds(attempt);
            _logger.LogDebug("Delaying reconnect #{Attempt} for {DelaySeconds}s", attempt, delaySeconds);

            try
            {
                await Task.Delay(delaySeconds * 1000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        if (cancellationToken.IsCancellationRequested)
        {
            _logger.LogDebug("Reconnect aborted");
            IsReconnecting = false;
            return;
        }

        await _sem.WaitAsync(cancellationToken);
        try
        {
            IsReconnecting = false;
        }
        finally
        {
            _sem.Release();
        }

        _logger.LogDebug("Reconnect completed");
    }

    private async Task ReceiverAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _logger.LogDebug("Receiver started");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var receiveResult = await _client.ReceiveAsync(cancellationToken);

                if (receiveResult.IsClose)
                {
                    _logger.LogInformation("Received close");
                    break;
                }

                switch (receiveResult.Message)
                {
                    case null:
                        _logger.LogTrace("Received null message");
                        continue;
                    case {Length: 0}:
                        _logger.LogTrace("Received empty message");
                        continue;
                }

                _logger.LogTrace("Received: {Message}", receiveResult.Message);

                _ = HandleReceivedWrapperAsync(receiveResult.Message, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Receiver error");
        }

        _logger.LogDebug("Receiver completed");

        _ = ReconnectAsync(cancellationToken);
    }

    private async Task SenderAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        _logger.LogDebug("Sender started");

        try
        {
            await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken))
            {
                _logger.LogTrace("Sending: {Message}", message);
                await _client.SendAsync(message, cancellationToken);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sender error");
        }

        _logger.LogDebug("Sender completed");

        _ = ReconnectAsync(cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _sem.Dispose();
            _cts?.Dispose();
        }

        base.Dispose();
    }

    public sealed override void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
