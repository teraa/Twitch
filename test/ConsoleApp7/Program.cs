using System.Net;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Teraa.Irc.Parsing;
using Teraa.Twitch.Tmi;
using Teraa.Twitch.Ws;
using ConnectedEventHandler = Teraa.Twitch.Tmi.ITmiEventHandler<Teraa.Twitch.Tmi.ConnectedEvent>;
using ConnectedEvent = Teraa.Twitch.Tmi.ConnectedEvent;
using MessageReceivedEventHandler = Teraa.Twitch.Tmi.ITmiEventHandler<Teraa.Twitch.Tmi.MessageReceivedEvent>;
using MessageReceivedEvent = Teraa.Twitch.Tmi.MessageReceivedEvent;

// using ConnectedEventHandler = Teraa.Twitch.Ws.Events.ITextWebSocketEventHandler<Teraa.Twitch.Ws.Events.ConnectedEvent>;
// using ConnectedEvent = Teraa.Twitch.Ws.Events.ConnectedEvent;
// using MessageReceivedEventHandler = Teraa.Twitch.Ws.Events.ITextWebSocketEventHandler<Teraa.Twitch.Ws.Events.MessageReceivedEvent>;
// using MessageReceivedEvent = Teraa.Twitch.Ws.Events.MessageReceivedEvent;

var logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
    )
    .MinimumLevel.Verbose()
    .CreateLogger();

var services = new ServiceCollection()
    .AddLogging(builder => builder.AddSerilog(logger))
    // .AddSingleton<TextWebSocketService>()
    .AddTmiService()
    .Configure<TextWebSocketClientOptions>(
        options => options.ClientFactory = () => new ClientWebSocket()
        {
            Options =
            {
                Proxy = new WebProxy("127.0.0.1", 8080),
            },
        }
    )
    .AddTmiEventHandler<MessageReceivedEvent, MessageReceivedHandler>()
    .AddTmiEventHandler<ConnectedEvent, ConnectedHandler>()
    // .AddTextWebSocketEventHandler<MessageReceivedEvent, MessageReceivedHandler>()
    // .AddTextWebSocketEventHandler<ConnectedEvent, ConnectedHandler>()
    .BuildServiceProvider(new ServiceProviderOptions
    {
        ValidateScopes = true,
        ValidateOnBuild = true,
    });

var ws = services.GetRequiredService<ITmiService>();
// var ws = services.GetRequiredService<ITextWebSocketService>();

logger.Information("Ready");

var messageParser = new MessageParser();

string? line;
while ((line = Console.ReadLine()) is not null)
{
    switch (line)
    {
        case "c":
            await ws.StartAsync(CancellationToken.None);
            logger.Information("StartAsync completed");
            break;
        case "d":
            await ws.StopAsync(CancellationToken.None);
            logger.Information("StopAsync completed");
            break;
        case null:
            return;
        default:
            // await ws.SendAsync(line);

            if (!messageParser.TryParse(line, out var message))
            {
                Console.WriteLine("Invalid message format.");
                continue;
            }

            ws.EnqueueMessage(message);

            // ws.EnqueueMessage(line);
            break;
    }
}

sealed file class MessageReceivedHandler : MessageReceivedEventHandler
{
    private readonly ILogger<MessageReceivedHandler> _logger;

    public MessageReceivedHandler(ILogger<MessageReceivedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(MessageReceivedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received: {Message}", evt.Message);
        return ValueTask.CompletedTask;
    }
}

sealed file class ConnectedHandler : ConnectedEventHandler
{
    private readonly ILogger<ConnectedHandler> _logger;

    public ConnectedHandler(ILogger<ConnectedHandler> logger)
    {
        _logger = logger;
    }

    public ValueTask HandleAsync(ConnectedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Connected");
        return ValueTask.CompletedTask;
    }
}
