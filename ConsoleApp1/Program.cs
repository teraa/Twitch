using Twitch.Irc;

var client = new WsClient();
await client.ConnectAsync(new Uri("ws://localhost:5033/ws"));

var cts = new CancellationTokenSource();
var readTask = Task.Run(async () =>
{
    while (!cts.IsCancellationRequested)
    {
        string message = await client.ReceiveAsync(cts.Token);
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
