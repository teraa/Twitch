// See https://aka.ms/new-console-template for more information

using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

Console.WriteLine("Hello, World!");

var cts = new CancellationTokenSource();
var pipe = new Pipe();

var readTask = Task.Run(async () =>
{
    await Task.Yield();


    while(!cts.IsCancellationRequested)
    {
        var reader = pipe.Reader;

        var result = await reader.ReadAsync(cts.Token);

        if (result.IsCanceled)
        {
            Console.WriteLine("Read canceled");
            break;
        }

        var message = Encoding.UTF8.GetString(result.Buffer);
        Console.WriteLine($"Read: {message}");
        reader.AdvanceTo(result.Buffer.End);

        if (result.IsCompleted)
        {
            Console.WriteLine("Read completed");
            break;
        }
    }

    Console.WriteLine("Read task finished");
});


var writeTask = Task.Run(async () =>
{
    await Task.Yield();

    while (!cts.IsCancellationRequested)
    {
        var writer = pipe.Writer;

        Console.WriteLine("Write/Flush/cOmplete/cAncel <message>: ");
        var input = Console.ReadLine();
        if (input is null)
            break;

        var parts = input.Split(" ", 2);
        switch (parts)
        {
            case ["W", var message]:
                var bytes = Encoding.UTF8.GetBytes(message);
                writer.Write(bytes);
                break;
            case ["F"]:
                var result = await writer.FlushAsync();
                Console.WriteLine($"Flush result - Canceled: {result.IsCanceled}, Completed: {result.IsCompleted}");
                break;
            case ["O"]:
                writer.Complete();
                break;
            case ["A"]:
                writer.CancelPendingFlush();
                break;
            default:
                Console.WriteLine("Unknown input");
                break;
        }
    }
});

await writeTask;
cts.Cancel();
await readTask;
