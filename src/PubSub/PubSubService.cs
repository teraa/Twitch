﻿using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Teraa.Twitch.PubSub.Notifications;
using Teraa.Twitch.PubSub.Payloads;
using Teraa.Twitch.PubSub.Topics;
using Teraa.Twitch.Ws;

namespace Teraa.Twitch.PubSub;

[PublicAPI]
public class PubSubServiceOptions : IWsServiceOptions
{
    public Uri Uri { get; set; } = new("wss://pubsub-edge.twitch.tv");

    public Func<IServiceProvider, IPublisher> PublisherFactory { get; set; } = x => x.GetRequiredService<IPublisher>();

    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = {new JsonStringEnumConverter()},
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public TimeSpan PingInterval { get; set; } = TimeSpan.FromMinutes(4);

    public TimeSpan MaxPongDelay { get; set; } = TimeSpan.FromSeconds(10);
}

public interface IPubSubClient
{
    void EnqueueMessage(Payload message);
}

[PublicAPI]
public class PubSubService : WsService, IPubSubClient
{
    private readonly PubSubServiceOptions _options;
    private readonly ILogger<PubSubService> _logger;
    private readonly IServiceProvider _services;
    private DateTimeOffset _lastPongAt;

    public PubSubService(
        IWsClient client,
        IOptions<PubSubServiceOptions> options,
        ILogger<PubSubService> logger,
        IServiceProvider services)
        : base(client, options, logger)
    {
        _options = options.Value;
        _logger = logger;
        _services = services;
    }

    public void EnqueueMessage(Payload message)
    {
        // Polymorphic serialization
        string json = JsonSerializer.Serialize<object>(message, _options.JsonSerializerOptions);
        EnqueueMessage(json);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (!IsReconnecting)
            {
                var enqueuedAt = DateTimeOffset.UtcNow;
                EnqueueMessage(Payload.CreatePing());

                await Task.Delay(_options.MaxPongDelay, stoppingToken);

                if (_lastPongAt < enqueuedAt)
                {
                    _logger.LogWarning("No PONG received within {Time}, reconnecting", _options.MaxPongDelay);
                    _ = ReconnectAsync(stoppingToken);
                }
            }

            await Task.Delay(_options.PingInterval - _options.MaxPongDelay, stoppingToken);
        }
    }

    protected override async ValueTask HandleConnectAsync(CancellationToken cancellationToken)
    {
        await PublishAsync(new Connected(), cancellationToken);
    }

    protected override async ValueTask HandleReceivedAsync(string rawMessage, CancellationToken cancellationToken)
    {
        using var doc = JsonDocument.Parse(rawMessage);
        var now = DateTimeOffset.UtcNow;

        await PublishAsync(new PayloadReceived(doc, now), cancellationToken);

        var elem = doc.RootElement;
        var payloadType = elem.GetProperty("type").Deserialize<PayloadType>(_options.JsonSerializerOptions);

        switch (payloadType)
        {
            case PayloadType.RECONNECT:
                _ = ReconnectAsync(cancellationToken);
                await PublishAsync(new ReconnectReceived(now), cancellationToken);
                break;

            case PayloadType.PONG:
                _lastPongAt = DateTimeOffset.UtcNow;
                await PublishAsync(new PongReceived(now), cancellationToken);
                break;

            case PayloadType.RESPONSE:
            {
                var error = elem.GetProperty("error").GetString();
                var nonce = elem.GetProperty("nonce").GetString();
                Debug.Assert(error is not null);
                Debug.Assert(nonce is not null);

                await PublishAsync(new ResponseReceived(error, nonce, now), cancellationToken);
                break;
            }

            case PayloadType.MESSAGE:
            {
                var data = elem.GetProperty("data");
                var rawTopic = data.GetProperty("topic").GetString();
                var rawInnerMessage = data.GetProperty("message").GetString();

                Debug.Assert(rawTopic is not null);
                Debug.Assert(rawInnerMessage is not null);

                using var innerMessage = JsonDocument.Parse(rawInnerMessage);
                await PublishAsync(new MessageReceived(rawTopic, innerMessage, now), cancellationToken);
                await HandleMessageAsync(rawTopic, rawInnerMessage, innerMessage, now, cancellationToken);
                break;
            }

            default:
                _logger.LogWarning("Unknown payload type: {PayloadType}", payloadType);
                await PublishAsync(new UnknownPayloadReceived(doc, now), cancellationToken);
                break;
        }
    }

    private async Task HandleMessageAsync(
        string rawTopic,
        string rawMessage,
        JsonDocument message,
        DateTimeOffset receivedAt,
        CancellationToken cancellationToken)
    {
        if (!Topic.TryParse(rawTopic, out var topic))
        {
            _logger.LogWarning("Unknown topic: {Topic}", rawTopic);
            return;
        }

        switch (topic)
        {
            case ChatModeratorActionsTopic t
                when Messages.ChatModeratorActions.Parser.TryParse(message, out var action):
                await PublishAsync(new ChatModeratorActionReceived(t, action, receivedAt), cancellationToken);
                break;

            case ChannelUnbanRequestsTopic t
                when Messages.ChannelUnbanRequests.Parser.TryParse(message, out var request):
                await PublishAsync(new ChannelUnbanRequestReceived(t, request, receivedAt), cancellationToken);
                break;

            case ShoutoutTopic t
                when Messages.Shoutout.Parser.TryParse(message, out var shoutout):
                await PublishAsync(new ShoutoutReceived(t, shoutout, receivedAt), cancellationToken);
                break;

            case LowTrustUsersTopic t
                when Messages.LowTrustUsers.Parser.TryParse(message, out var treatmentUpdate):
                await PublishAsync(new LowTrustUserTreatmentUpdateReceived(t, treatmentUpdate, receivedAt),
                    cancellationToken);
                break;

            default:
                _logger.LogWarning("Unknown message: {Message}", rawMessage);
                await PublishAsync(new UnknownMessageReceived(rawTopic, message, receivedAt), cancellationToken);
                break;
        }
    }

    private async Task PublishAsync(INotification notification, CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var publisher = _options.PublisherFactory(scope.ServiceProvider);
            await publisher.Publish(notification, cancellationToken);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {Notification}", notification.GetType().Name);
        }
    }
}
