using Twitch.Irc;

var client = new WsClient();
// await client.ConnectAsync(new Uri("ws://localhost:5033/ws"));
await client.ConnectAsync(new Uri("wss://irc-ws.chat.twitch.tv:443"));

var cts = new CancellationTokenSource();
var readTask = Task.Run(async () =>
{
    await Task.Yield();

    while (!cts.IsCancellationRequested)
    {
        string? message = await client.ReceiveAsync(cts.Token);
        if (message is null) break;

        Console.WriteLine("> " + message);
    }
});

string? line;
while ((line = Console.ReadLine()) is not null)
{
    await client.SendAsync(line);
}

await client.DisconnectAsync();
cts.Cancel();

try
{
    await readTask;
}
catch (Exception ex)
{

}
