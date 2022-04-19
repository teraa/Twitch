using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Twitch.Tmi.Notifications;

namespace Teraa.Twitch.Tmi;

public class TmiServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
}

public class TmiService : IHostedService, IDisposable
{
    private readonly IClient _client;
    private readonly Channel<Message> _sendChannel;
    private readonly IPublisher _publisher;
    private readonly TmiServiceOptions _options;
    private readonly ILogger<TmiService> _logger;
    private readonly SemaphoreSlim _sem;
    private Task? _receiverTask, _senderTask;
    private CancellationTokenSource? _cts;
    private bool _isReconnecting;

    public TmiService(IClient client, IPublisher publisher, IOptions<TmiServiceOptions> options, ILogger<TmiService> logger)
    {
        _client = client;
        _publisher = publisher;
        _options = options.Value;
        _logger = logger;

        _sem = new SemaphoreSlim(1, 1);
        _sendChannel = Channel.CreateUnbounded<Message>(new()
        {
            SingleReader = true,
            SingleWriter = true,
        });
    }

    [MemberNotNullWhen(true, nameof(_cts))]
    public bool IsStarted { get; private set; }

    public void EnqueueMessage(Message message)
    {
        bool success = _sendChannel.Writer.TryWrite(message);
        Debug.Assert(success);
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
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
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _sem.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (!IsStarted)
                throw new InvalidOperationException("Not started");

            await DisconnectAsync(cancellationToken)
                .ConfigureAwait(false);

            _cts.Cancel();
            _cts.Dispose();
            await StopInternalAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            IsStarted = false;
            _sem.Release();
        }
    }

    private async Task StartInternalAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsStarted);

        if (_client.IsConnected) return;

        await _client.ConnectAsync(_options.Uri, cancellationToken)
            .ConfigureAwait(false);

        _logger.LogDebug("Connected");

        _receiverTask = ReceiverAsync(_cts.Token);
        _senderTask = SenderAsync(_cts.Token);

        _ = Task.Run(async () =>
        {
            try
            {
                await _publisher.Publish(new Connected(), cancellationToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing {Notification}", nameof(Connected));
            }
        }, cancellationToken);
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

        _logger.LogDebug("Disconnected");
    }

    private async Task ReconnectAsync(CancellationToken cancellationToken)
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
            _isReconnecting = false;
            _logger.LogError(ex, "Error reconnecting");
        }
    }

    private async Task ReconnectInternalAsync(CancellationToken cancellationToken)
    {
        Debug.Assert(IsStarted);

        await _sem.WaitAsync(cancellationToken);
        try
        {
            if (_isReconnecting)
            {
                _logger.LogDebug("Concurrent reconnect request");
                return;
            }

            _isReconnecting = true;
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
            cancellationToken = _cts.Token;
        }
        finally
        {
            _sem.Release();
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

            const int max = 7;
            int delaySeconds = 1 << (attempt > max ? max : attempt);
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
            _isReconnecting = false;
            return;
        }

        await _sem.WaitAsync(cancellationToken);
        try
        {
            _isReconnecting = false;
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

                if (receiveResult.Message is null)
                {
                    _logger.LogInformation("Received null");
                    continue;
                }

                bool parsed = Message.TryParse(receiveResult.Message, out var message);

                _logger.LogTrace("Received: {Parsed}, {Message}",
                    parsed, receiveResult.Message);

                INotification notification = parsed
                    ? new MessageReceived(message)
                    : new UnknownMessageReceived(receiveResult.Message);

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
                string rawMessage = message.ToString();
                _logger.LogTrace("Sending: {Message}", rawMessage);
                await _client.SendAsync(rawMessage, cancellationToken);
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

    public void Dispose()
    {
        _sem.Dispose();
        _cts?.Dispose();
    }
}
