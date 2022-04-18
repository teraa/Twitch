using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Teraa.Irc;
using Twitch.Irc.Notifications;

namespace Twitch.Irc;

public interface IIrcClient
{
    bool IsStarted { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void EnqueueMessage(Message message);
}

public class IrcClientOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
}

public class IrcClient : IHostedService, IIrcClient, IDisposable
{
    private readonly IClient _client;
    private readonly Channel<Message> _sendChannel;
    private readonly IMediator _mediator;
    private readonly IrcClientOptions _options;
    private readonly ILogger<IrcClient> _logger;
    private readonly SemaphoreSlim _sem;
    private Task? _receiverTask, _senderTask;
    private CancellationTokenSource? _cts;

    public IrcClient(IClient client, IMediator mediator, IrcClientOptions options, ILogger<IrcClient> logger)
    {
        _client = client;
        _mediator = mediator;
        _options = options;
        _logger = logger;

        _sem = new SemaphoreSlim(1, 1);
        _sendChannel = Channel.CreateUnbounded<Message>(new()
        {
            SingleReader = true,
            SingleWriter = true,
        });
    }

    [MemberNotNullWhen(true, nameof(_cts), nameof(_receiverTask), nameof(_senderTask))]
    public bool IsStarted { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _sem.WaitAsync(cancellationToken)
            .ConfigureAwait(false);
        try
        {
            if (IsStarted)
                throw new InvalidOperationException("Already started");

            IsStarted = true;

            await _client.ConnectAsync(_options.Uri, cancellationToken)
                .ConfigureAwait(false);

            _cts = new CancellationTokenSource();
            _receiverTask = ReceiverAsync(_cts.Token);
            _senderTask = SenderAsync(_cts.Token);

            _ = Task.Run(async () =>
            {
                try
                {
                    await _mediator.Publish(new Connected(), cancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing connected notification");
                }
            }, cancellationToken);
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

            IsStarted = false;

            try
            {
                await _client.DisconnectAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disconnecting the client");
            }

            _cts.Cancel();

            await _receiverTask;
            await _senderTask;

            _receiverTask = null;
            _senderTask = null;
        }
        finally
        {
            _sem.Release();
        }
    }

    public void EnqueueMessage(Message message)
    {
        bool success = _sendChannel.Writer.TryWrite(message);
        Debug.Assert(success);
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

                _logger.LogTrace("Received: {Message}, {Parsed}", receiveResult.Message, parsed);

                try
                {
                    if (parsed)
                        await _mediator.Publish(new MessageReceived(message), cancellationToken);
                    else
                        await _mediator.Publish(new UnknownMessageReceived(receiveResult.Message), cancellationToken);
                }
                catch (OperationCanceledException) { }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing message received notification");
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Receiver error");
        }

        _logger.LogDebug("Receiver completed");

        // if (!cancellationToken.IsCancellationRequested) { /* reconnect */ }
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

        // if (!cancellationToken.IsCancellationRequested) { /* reconnect */ }
    }

    public void Dispose()
    {
        _sem.Dispose();
        _cts?.Dispose();
    }
}
