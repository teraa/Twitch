using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Irc;
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
    .AddSingleton<IClient>(new WsClient())
    .Configure<TmiServiceOptions>(options =>
    {
        options.Uri = new Uri("ws://localhost:5033/ws");
    })
    .AddSingleton<TmiService>()
    .BuildServiceProvider();

var client = services.GetRequiredService<TmiService>();
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
    private readonly TmiService _tmi;

    public MessageHandler(TmiService tmi)
    {
        _tmi = tmi;
    }

    public Task Handle(MessageReceived received, CancellationToken cancellationToken)
    {
        if (received.Message is {Command: Command.PONG, Content.Text: "throw"})
            throw new ArgumentException("pong");

        if (received.Message is {Command: Command.PING})
            _tmi.EnqueueMessage(new Message {Command = Command.PONG});

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
