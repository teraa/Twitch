﻿using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Irc;
using Teraa.Twitch.PubSub;
using Teraa.Twitch.PubSub.Payloads;
using Teraa.Twitch.Tmi;
using Teraa.Twitch.Ws;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();

var services = new ServiceCollection()
    .AddLogging(configure =>
    {
        configure.AddSerilog();
    })
    .AddMediatR(typeof(Program))
    .AddTmiService()
    .Configure<TmiServiceOptions>(options =>
    {
        // options.Uri = new Uri("ws://localhost:5033/ws");
        options.PingInterval = TimeSpan.FromSeconds(10);
        options.MaxPongDelay = TimeSpan.FromSeconds(1);
    })
    .AddPubSubService()
    .Configure<PubSubServiceOptions>(options =>
    {
        // options.Uri = new Uri("ws://localhost:5033/ws");
    })
    .BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true,
    });

WsService svc;

// svc = services.GetRequiredService<PubSubService>();
svc = services.GetRequiredService<TmiService>();

await svc.StartAsync(default);

string? line;
while ((line = Console.ReadLine()) is not null)
{
    switch (line)
    {
        case "c":
            if (svc.IsStarted) break;
            await svc.StartAsync(default);
            break;
        case "d":
            if (!svc.IsStarted) break;
            await svc.StopAsync(default);
            break;
        default:
            if (svc is TmiService tmi)
            {
                if (!Message.TryParse(line, out var message))
                {
                    Console.WriteLine("Invalid message format.");
                    continue;
                }
                tmi.EnqueueMessage(message);
            }
            else
            {
                svc.EnqueueMessage(line);
            }
            break;
    }
}

if (svc.IsStarted)
    await svc.StopAsync(default);

public class MessageHandler : INotificationHandler<Teraa.Twitch.Tmi.Notifications.MessageReceived>
{
    public Task Handle(Teraa.Twitch.Tmi.Notifications.MessageReceived received, CancellationToken cancellationToken)
    {
        if (received.Message is {Command: Command.PONG, Content.Text: "throw"})
            throw new ArgumentException("pong");

        return Task.CompletedTask;
    }
}

public class ConnectedHandler : INotificationHandler<Teraa.Twitch.Tmi.Notifications.Connected>
{
    private readonly TmiService _tmi;

    public ConnectedHandler(TmiService tmi)
    {
        _tmi = tmi;
    }

    public Task Handle(Teraa.Twitch.Tmi.Notifications.Connected notification, CancellationToken cancellationToken)
    {
        _tmi.EnqueueMessage(Message.Parse("nick justinfan1"));

        return Task.CompletedTask;
    }
}

public class PubSubConnectedHandler : INotificationHandler<Teraa.Twitch.PubSub.Notifications.Connected>
{
    private readonly PubSubService _pubSub;

    public PubSubConnectedHandler(PubSubService pubSub)
    {
        _pubSub = pubSub;
    }

    public Task Handle(Teraa.Twitch.PubSub.Notifications.Connected notification, CancellationToken cancellationToken)
    {
        _pubSub.EnqueueMessage(Payload.CreatePingRequest());
        _pubSub.EnqueueMessage(Payload.CreateListenRequest(new List<string>{"topic"}, "token"));
        return Task.CompletedTask;
    }
}
