using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Teraa.Irc;

namespace Twitch.Irc;

public interface IIrcClient
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void EnqueueMessage(Message message);
}

public record UnknownMessageNotification(string Message) : INotification;
public record MessageNotification(Message Message) : INotification;

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
            if (IsStarted) return;

        await _client.ConnectAsync(_options.Uri, cancellationToken)
            .ConfigureAwait(false);

            _cts = new CancellationTokenSource();
            _receiverTask = ReceiverAsync(_cts.Token);
            _senderTask = SenderAsync(_cts.Token);
            IsStarted = true;
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
            if (!IsStarted) return;

        await _client.DisconnectAsync(cancellationToken)
            .ConfigureAwait(false);

        IsStarted = false;

        _cts.Cancel();

            try { await _receiverTask; }
            catch (OperationCanceledException) { }

            try { await _senderTask; }
            catch (OperationCanceledException) { }

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

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                string? rawMessage = await _client.ReceiveAsync(cancellationToken)
                    .ConfigureAwait(false);

                if (rawMessage is null)
                    break; // End of socket

                bool parsed = Message.TryParse(rawMessage, out var message);

                try
                {
                    if (parsed)
                        await _mediator.Publish(new MessageNotification(message), cancellationToken)
                            .ConfigureAwait(false);
                    else
                        await _mediator.Publish(new UnknownMessageNotification(rawMessage), cancellationToken)
                            .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error publishing received message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
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

        await foreach (var message in _sendChannel.Reader.ReadAllAsync(cancellationToken)
                           .ConfigureAwait(false))
        {
            string rawMessage = message.ToString();
            await _client.SendAsync(rawMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        _logger.LogDebug("Sender completed");
    }

    public void Dispose()
    {
        _sem.Dispose();
        _cts?.Dispose();
    }
}
