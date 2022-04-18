using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Teraa.Irc;
using Twitch.Irc;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
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
Console.WriteLine("Connected!");

string? line;
while ((line = Console.ReadLine()) is not null)
{
    if (!Message.TryParse(line, out var message))
    {
        Console.WriteLine("Invalid message format.");
        continue;
    }

    client.EnqueueMessage(message);
}

await client.StopAsync();

public class Handler : AsyncRequestHandler<MessageRequest>
{
    protected override Task Handle(MessageRequest request, CancellationToken cancellationToken)
    {
        Console.WriteLine("recv: " + request.Message.ToString());

        return Task.CompletedTask;
    }
}
