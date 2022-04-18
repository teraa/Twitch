using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.Logging;
using Teraa.Irc;

namespace Twitch.Irc;

public interface IIrcClient
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void EnqueueMessage(Message message);
}

public record MessageRequest(Message Message)
    : IRequest;

public class IrcClientOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
}

public class IrcClient : IIrcClient
{
    private readonly IClient _client;
    private readonly Channel<Message> _receiveChannel;
    private readonly Channel<Message> _sendChannel;
    private readonly IMediator _mediator;
    private readonly IrcClientOptions _options;
    private readonly ILogger<IrcClient> _logger;
    private Task? _receiverTask, _senderTask;
    private CancellationTokenSource? _cts;

    public IrcClient(IClient client, IMediator mediator, IrcClientOptions options, ILogger<IrcClient> logger)
    {
        _client = client;
        _mediator = mediator;
        _options = options;
        _logger = logger;

        _receiveChannel = Channel.CreateUnbounded<Message>(new()
        {
            SingleReader = true,
            SingleWriter = true,
        });

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
        if (IsStarted) return;

        await _client.ConnectAsync(_options.Uri, cancellationToken)
            .ConfigureAwait(false);

        _cts = new CancellationTokenSource();
        _receiverTask = ReceiverAsync(_cts.Token);
        _senderTask = SenderAsync(_cts.Token);
        IsStarted = true;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
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

    public void EnqueueMessage(Message message)
    {
        bool success = _sendChannel.Writer.TryWrite(message);
        Debug.Assert(success);
    }

    private async Task ReceiverAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();

        // TODO: Try catch
        while (true)
        {
            string? rawMessage = await _client.ReceiveAsync(cancellationToken)
                .ConfigureAwait(false);

            if (rawMessage is null)
                break; // TODO: Signal end of socket

            // TODO: try parse
            Message message = Message.Parse(rawMessage);

            // await _receiveChannel.Writer.WriteAsync(message, cancellationToken)
            //     .ConfigureAwait(false);

            await _mediator.Send(new MessageRequest(message), cancellationToken)
                .ConfigureAwait(false);
        }
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
    }
}
