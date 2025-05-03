using JetBrains.Annotations;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Twitch.PubSub;
using Teraa.Twitch.PubSub.Payloads;

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
    .AddMediatR(config => config.RegisterServicesFromAssemblyContaining<Program>())
    .AddPubSubService(options =>
    {
        // options.Uri = new Uri("ws://localhost:5033/ws");
    })
    .BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true,
    });

var svc = services.GetRequiredService<PubSubService>();

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
            svc.EnqueueMessage(line);
            break;
    }
}

if (svc.IsStarted)
    await svc.StopAsync(default);


[UsedImplicitly]
public class PubSubConnectedHandler : INotificationHandler<Teraa.Twitch.PubSub.Notifications.Connected>
{
    private readonly PubSubService _pubSub;

    public PubSubConnectedHandler(PubSubService pubSub)
    {
        _pubSub = pubSub;
    }

    public Task Handle(Teraa.Twitch.PubSub.Notifications.Connected notification, CancellationToken cancellationToken)
    {
        _pubSub.EnqueueMessage(Payload.CreatePing());
        _pubSub.EnqueueMessage(Payload.CreateListen(new List<string>{"topic"}, "token"));
        return Task.CompletedTask;
    }
}
