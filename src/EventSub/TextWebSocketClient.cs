using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using JetBrains.Annotations;

namespace Teraa.Twitch.EventSub;

[PublicAPI]
public sealed class TextWebSocketClient : IDisposable
{
    private ClientWebSocket _client;
    private StreamReader? _sr;
    private readonly SemaphoreSlim _sendSem = new(1, 1);
    private readonly Func<ClientWebSocket> _clientFactory;
    private readonly Lock _stateLock = new();

    public TextWebSocketClient(Func<ClientWebSocket> clientFactory)
    {
        _client = clientFactory();
        _clientFactory = clientFactory;
    }

    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        lock (_stateLock)
        {
            _client.Dispose();
            _client = _clientFactory();
        }

        await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (_client.State is WebSocketState.Closed or WebSocketState.Aborted)
            {
                // We don't need to do any cleaning up
                return;
            }

            await _sendSem.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // This will send a message to close the socket. If we're sending something else concurrently,
                // one of the two calls will throw because that is not a supported operation.
                // So we use a semaphore to synchronize these calls.
                await _client.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, null, cancellationToken)
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

    /// <summary>
    /// Calls ReceiveAsync until we reach end of message or the connection gets closed.
    /// </summary>
    private async Task<ReceiveResultType> ReceiveMessage(PipeWriter writer, CancellationToken cancellationToken)
    {
        ValueWebSocketReceiveResult result;
        do
        {
            Memory<byte> buffer = writer.GetMemory();

            try
            {
                result = await _client.ReceiveAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (WebSocketException ex)
                when (ex is {WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely})
            {
                return ReceiveResultType.ClosedUnexpectedly;
            }

            writer.Advance(result.Count);

            if (result.MessageType is WebSocketMessageType.Close)
            {
                return ReceiveResultType.CloseMessageReceived;
            }
        } while (!result.EndOfMessage);

        return ReceiveResultType.Regular;
    }

    /// <summary>
    /// Receives a message until the end of line.
    /// </summary>
    public async Task<ReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_sr is null)
        {
            // Disposed when StreamReader is disposed
            MemoryStream ms = new();

            // Disposed on end of stream or call to Dispose()
            _sr = new StreamReader(ms, Encoding);

            PipeWriter writer = PipeWriter.Create(ms);

            var result = await ReceiveMessage(writer, cancellationToken);

            if (result is
                ReceiveResultType.ClosedUnexpectedly or
                ReceiveResultType.CloseMessageReceived)
            {
                // We're entering one of the close states and there is only (possibly) incomplete data
                // that we already received, so we will discard this data and dispose of the stream.
                // The stream might be null here in case we got here by calling DisconnectAsync.
                _sr?.Dispose();
                _sr = null;

                return new ReceiveResult(result, null);
            }

            await writer.FlushAsync(cancellationToken)
                .ConfigureAwait(false);

            // We're done writing to the stream with pipe writer,
            // seek to the beginning before reading with stream reader.
            ms.Seek(0, SeekOrigin.Begin);
        }

        // New line delimited messages. We may receive multiple text messages in a single websocket message.
        string? message = await _sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        // We won't be using this stream anymore if we reached its end.
        if (_sr.EndOfStream)
        {
            _sr.Dispose();
            _sr = null;
        }

        return new ReceiveResult(ReceiveResultType.Regular, message);
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
            try
            {
                await _client.SendAsync(segment, WebSocketMessageType.Text, true, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally
            {
                _sendSem.Release();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        _client.Dispose();
        _sr?.Dispose();
        _sendSem.Dispose();
    }


    public enum ReceiveResultType
    {
        Regular,
        CloseMessageReceived,
        ClosedUnexpectedly,
    }

    public readonly record struct ReceiveResult(
        ReceiveResultType Type,
        string? Message
    );
}
