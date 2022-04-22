using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Irc;
using Teraa.Twitch.PubSub;
using Teraa.Twitch.PubSub.Payloads;
using Teraa.Twitch.Tmi;
using Teraa.Twitch.Tmi.Notifications;

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

var client = services.GetRequiredService<PubSubService>();
// var client = services.GetRequiredService<TmiService>();
await client.StartAsync();

string? line;
while ((line = Console.ReadLine()) is not null)
{
    switch (line)
    {
        case "c":
            if (client.IsStarted) break;
            await client.StartAsync();
            break;
        case "d":
            if (!client.IsStarted) break;
            await client.StopAsync();
            break;
        default:
            // if (!Message.TryParse(line, out var message))
            // {
            //     Console.WriteLine("Invalid message format.");
            //     continue;
            // }
            // client.EnqueueMessage(message);
            client.EnqueueMessage(line);
            break;
    }
}

if (client.IsStarted)
    await client.StopAsync();

public class MessageHandler : INotificationHandler<MessageReceived>
{
    public Task Handle(MessageReceived received, CancellationToken cancellationToken)
    {
        if (received.Message is {Command: Command.PONG, Content.Text: "throw"})
            throw new ArgumentException("pong");

        return Task.CompletedTask;
    }
}

public class ConnectedHandler : INotificationHandler<Connected>
{
    private readonly TmiService _tmi;

    public ConnectedHandler(TmiService tmi)
    {
        _tmi = tmi;
    }

    public Task Handle(Connected notification, CancellationToken cancellationToken)
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
