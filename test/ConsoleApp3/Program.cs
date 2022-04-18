using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Teraa.Irc;
using Twitch.Irc;
using Twitch.Irc.Notifications;

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
    .AddSingleton<IClient>(new WsClient())
    .AddSingleton<IrcClientOptions>()
    .AddSingleton<IIrcClient, IrcClient>()
    .BuildServiceProvider();

var client = services.GetRequiredService<IIrcClient>();
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
            Console.WriteLine("Stopped!");
            break;
        default:
            if (!Message.TryParse(line, out var message))
            {
                Console.WriteLine("Invalid message format.");
                continue;
            }
            client.EnqueueMessage(message);
            break;
    }
}

await client.StopAsync();

public class MessageHandler : INotificationHandler<MessageReceived>
{
    private readonly IIrcClient _client;

    public MessageHandler(IIrcClient client)
    {
        _client = client;
    }

    public Task Handle(MessageReceived received, CancellationToken cancellationToken)
    {
        if (received.Message is {Command: Command.PONG, Content.Text: "throw"})
            throw new ArgumentException("pong");

        if (received.Message is {Command: Command.PING})
            _client.EnqueueMessage(new Message {Command = Command.PONG});

        return Task.CompletedTask;
    }
}

public class ConnectedHandler : INotificationHandler<Connected>
{
    private readonly IIrcClient _client;
    private readonly ILogger<ConnectedHandler> _logger;

    public ConnectedHandler(IIrcClient client, ILogger<ConnectedHandler> logger)
    {
        _client = client;
        _logger = logger;
    }

    public Task Handle(Connected notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connected!");
        _client.EnqueueMessage(Message.Parse("nick justinfan1"));

        return Task.CompletedTask;
    }
}
