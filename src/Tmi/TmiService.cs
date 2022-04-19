﻿using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Irc;
using Teraa.Twitch.Tmi.Notifications;

namespace Teraa.Twitch.Tmi;

public class TmiServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new Uri("wss://irc-ws.chat.twitch.tv:443");
}

[PublicAPI]
public sealed class TmiService : WsService
{
    private readonly IPublisher _publisher;
    private readonly ILogger<TmiService> _logger;

    public TmiService(
        IWsClient client,
        IPublisher publisher,
        IOptions<TmiServiceOptions> options,
        ILogger<TmiService> logger)
        : base(client, options, logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public void EnqueueMessage(Message message)
        => EnqueueMessage(message.ToString());

    protected override async ValueTask HandleConnectAsync(CancellationToken cancellationToken)
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
    }

    protected override async ValueTask HandleReceivedAsync(ReceiveResult receiveResult, CancellationToken cancellationToken)
    {
        if (receiveResult.Message is null)
        {
            _logger.LogInformation("Received null");
            return;
        }

        bool parsed = Message.TryParse(receiveResult.Message, out var message);

        _logger.LogTrace("Received: {Parsed}, {Message}",
            parsed, receiveResult.Message);

        if (parsed)
        {
            switch (message)
            {
                case {Command: Command.RECONNECT}:
                    _ = ReconnectAsync(cancellationToken);
                    break;

                case {Command: Command.PING}:
                    EnqueueMessage(new Message
                    {
                        Command = Command.PONG,
                    });
                    break;
            }
        }

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
