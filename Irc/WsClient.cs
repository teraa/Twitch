using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;

namespace Twitch.Irc;

public class WsClient : IDisposable
{
    private ClientWebSocket? _client;

    [MemberNotNullWhen(true, nameof(_client))]
    public bool IsConnected { get; private set; }

    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (IsConnected)
            throw new InvalidOperationException("Client was already connected.");

        _client = new ClientWebSocket();
        await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);

        IsConnected = true;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Client is not connected.");

        IsConnected = false;

        try
        {
            await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, default, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _client.Dispose();
            _client = null;
        }
    }

    public async Task<string?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Client is not connected.");

        await using MemoryStream ms = new();
        PipeWriter writer = PipeWriter.Create(ms);
        ValueWebSocketReceiveResult result;
        do
        {
            Memory<byte> buffer = writer.GetMemory(512);

            result = await _client.ReceiveAsync(buffer, cancellationToken)
                .ConfigureAwait(false);

            writer.Advance(result.Count);

            if (result.MessageType is WebSocketMessageType.Close)
                return null;

        } while (!result.EndOfMessage);

        await writer.FlushAsync(cancellationToken)
            .ConfigureAwait(false);

        ms.Seek(0, SeekOrigin.Begin);

        using StreamReader sr = new(ms, Encoding);

        string message = await sr.ReadToEndAsync()
            .ConfigureAwait(false);

        return message;
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Client is not connected.");

        int length = Encoding.GetByteCount(message);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            ArraySegment<byte> segment = new(buffer, 0, length);
            Encoding.GetBytes(message, segment);

            await _client.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        _client?.Dispose();
    }
}
