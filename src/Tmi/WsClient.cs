using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using JetBrains.Annotations;

namespace Teraa.Twitch.Tmi;

public interface IWsClient
{
    bool IsConnected { get; }
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default);
    Task DisconnectAsync(CancellationToken cancellationToken = default);
    Task<ReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default);
    Task SendAsync(string message, CancellationToken cancellationToken = default);
}

[PublicAPI]
public class WsClient : IWsClient, IDisposable
{
    private ClientWebSocket? _client;
    private StreamReader? _sr;

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
            if (_client.State is WebSocketState.Open or WebSocketState.CloseReceived or WebSocketState.CloseSent)
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, default, cancellationToken)
                    .ConfigureAwait(false);
        }
        finally
        {
            _client.Dispose();
            _client = null;

            _sr?.Dispose();
            _sr = null;
        }
    }

    public async Task<ReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Client is not connected.");

        if (_sr is null)
        {
            // Disposed when StreamReader is disposed
            MemoryStream ms = new();

            // Disposed on end of stream or call to Dispose()
            _sr = new StreamReader(ms, Encoding);

            PipeWriter writer = PipeWriter.Create(ms);

            ValueWebSocketReceiveResult result;
            do
            {
                Memory<byte> buffer = writer.GetMemory(512);

                result = await _client.ReceiveAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);

                writer.Advance(result.Count);

                if (result.MessageType is WebSocketMessageType.Close)
                    return new ReceiveResult(true, null);
            } while (!result.EndOfMessage);

            await writer.FlushAsync(cancellationToken)
                .ConfigureAwait(false);

            ms.Seek(0, SeekOrigin.Begin);
        }

        string? message = await _sr.ReadLineAsync().ConfigureAwait(false);
        // TODO: what happens when there is multiple newlines in a row without other characters
        Debug.Assert(message is {Length: > 0});

        if (_sr.EndOfStream)
        {
            _sr.Dispose();
            _sr = null;
        }

        return new ReceiveResult(false, message);
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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _client?.Dispose();
            _sr?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
