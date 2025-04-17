using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using JetBrains.Annotations;

namespace Teraa.Twitch.EventSub;

[PublicAPI]
public sealed class TextWebSocketClient : IDisposable
{
    private readonly ClientWebSocket _client;
    private StreamReader? _sr;
    private readonly SemaphoreSlim _sendSem = new(1, 1);

    public TextWebSocketClient(ClientWebSocket client)
    {
        _client = client;
    }

    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // Other states not covered: Connecting, Closed, Aborted.
            // Connecting state will only ever happen during the call to ConnectAsync, never before nor after.
            // If it's Closed or Aborted then we don't need to do anything anyway.
            if (_client.State is not (
                WebSocketState.Open or
                WebSocketState.CloseReceived or
                WebSocketState.CloseSent))
            {
                return;
            }

            await _sendSem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // This will send a message to close the socket. If we're sending something else concurrently,
                // one of the two calls will throw because that is not a supported operation.
                // So we use a semaphore to synchronize these calls.
                await _client.CloseAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _sendSem.Release();
            }
        }
        finally
        {
            _sr?.Dispose();
            _sr = null;
        }
    }

    public async Task<ReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
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

                try
                {
                    result = await _client.ReceiveAsync(buffer, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (WebSocketException ex)
                    when (ex is {WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely})
                {
                    return ReceiveResult.Close;
                }

                writer.Advance(result.Count);

                if (result.MessageType is WebSocketMessageType.Close)
                    return ReceiveResult.Close;
            } while (!result.EndOfMessage);

            await writer.FlushAsync(cancellationToken)
                .ConfigureAwait(false);

            ms.Seek(0, SeekOrigin.Begin);
        }

        // New line delimited messages. We may receive multiple text messages in a single websocket message.
        string? message = await _sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        if (_sr.EndOfStream)
        {
            _sr.Dispose();
            _sr = null;
        }

        return new ReceiveResult(false, message);
    }

    public async Task SendAsync(string message, CancellationToken cancellationToken = default)
    {
        int length = Encoding.GetByteCount(message);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            ArraySegment<byte> segment = new(buffer, 0, length);
            Encoding.GetBytes(message, segment);

            await _sendSem.WaitAsync(cancellationToken).ConfigureAwait(false);
            await _client.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            _sendSem.Release();
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _sr?.Dispose();
        _sendSem.Dispose();
    }


    public readonly record struct ReceiveResult(bool IsClose, string? Message)
    {
        public static ReceiveResult Close { get; } = new(true, null);
    }
}
