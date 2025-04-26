using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Teraa.Twitch.Ws;

public interface ITextWebSocketClient : IDisposable
{
    Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
    Task<TextWebSocketReceiveResult> ReceiveAsync(CancellationToken cancellationToken);
    Task SendAsync(string message, CancellationToken cancellationToken);
}

[PublicAPI]
public sealed class TextWebSocketClient : ITextWebSocketClient
{
    private ClientWebSocket _client;
    private StreamReader? _sr;
    private readonly SemaphoreSlim _sendSem = new(1, 1);
    private readonly Func<ClientWebSocket> _clientFactory;
    private readonly Lock _stateLock = new();
    private readonly ILogger<TextWebSocketClient> _logger;

    public TextWebSocketClient(
        Func<ClientWebSocket> clientFactory,
        ILogger<TextWebSocketClient> logger)
    {
        _client = clientFactory();
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public Encoding Encoding { get; set; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Connecting to {Uri}", uri);
        lock (_stateLock)
        {
            if (_client.State != WebSocketState.None)
            {
                _client.Dispose();
                _client = _clientFactory();
                _logger.LogDebug("Created a new client");
            }
            else
            {
                _logger.LogDebug("Skipped creating a new client");
            }
        }

        await _client.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Connected to {Uri}", uri);
    }

    // This method should never throw, except maybe when cancelled while waiting on semaphore.
    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Closing WebSocket");
        try
        {
            if (_client.State is WebSocketState.Closed or WebSocketState.Aborted)
            {
                // We don't need to do any cleaning up
                _logger.LogDebug("WebSocket already closed ({State}), skipping sending close frame", _client.State);
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

                _logger.LogDebug("Sent close frame");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error sending close frame");
                _client.Abort();
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
    private async Task<TextWebSocketReceiveResultType> ReceiveMessage(PipeWriter writer, CancellationToken cancellationToken)
    {
        ValueWebSocketReceiveResult result;
        do
        {
            Memory<byte> buffer = writer.GetMemory();

            try
            {
                _logger.LogDebug("Waiting to receive a new message");

                result = await _client.ReceiveAsync(buffer, cancellationToken)
                    .ConfigureAwait(false);

                _logger.LogDebug("Received message, type: {MessageType}, size: {Count}, EndOfMessage: {EndOfMessage}",
                    result.MessageType,
                    result.Count,
                    result.EndOfMessage
                );
            }
            catch (WebSocketException ex)
                when (ex is {WebSocketErrorCode: WebSocketError.ConnectionClosedPrematurely})
            {
                _logger.LogDebug("WebSocket connection closed prematurely");
                return TextWebSocketReceiveResultType.ClosedUnexpectedly;
            }

            writer.Advance(result.Count);

            if (result.MessageType is WebSocketMessageType.Close)
            {
                return TextWebSocketReceiveResultType.CloseMessageReceived;
            }
        } while (!result.EndOfMessage);

        return TextWebSocketReceiveResultType.Regular;
    }

    /// <summary>
    /// Receives a message until the end of line.
    /// </summary>
    public async Task<TextWebSocketReceiveResult> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (_sr is null)
        {
            // Disposed when StreamReader is disposed
            MemoryStream ms = new();

            // Disposed on end of stream or call to Dispose()
            _sr = new StreamReader(ms, Encoding);

            PipeWriter writer = PipeWriter.Create(ms);

            var result = await ReceiveMessage(writer, cancellationToken)
                .ConfigureAwait(false);

            await writer.FlushAsync(cancellationToken)
                .ConfigureAwait(false);

            if (result is
                TextWebSocketReceiveResultType.ClosedUnexpectedly or
                TextWebSocketReceiveResultType.CloseMessageReceived)
            {
                // We're entering one of the close states and there is only (possibly) incomplete data
                // that we already received, so we will discard this data and dispose of the stream.
                // The stream might be null here in case we got here by calling CloseAsync.
                _sr?.Dispose();
                _sr = null;

                return new TextWebSocketReceiveResult(result, null);
            }

            // We're done writing to the stream with pipe writer,
            // seek to the beginning before reading with stream reader.
            ms.Seek(0, SeekOrigin.Begin);
        }
        else
        {
            _logger.LogDebug("Consuming previously received message");
        }

        // New line delimited messages. We may receive multiple text messages in a single websocket message.
        string? message = await _sr.ReadLineAsync(cancellationToken).ConfigureAwait(false);

        // We won't be using this stream anymore if we reached its end.
        if (_sr.EndOfStream)
        {
            _logger.LogDebug("Reached end of message");
            _sr.Dispose();
            _sr = null;
        }

        return new TextWebSocketReceiveResult(TextWebSocketReceiveResultType.Regular, message);
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

                _logger.LogDebug("Sent a message of {Count} bytes", length);
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
}

public readonly record struct TextWebSocketReceiveResult(
    TextWebSocketReceiveResultType Type,
    string? Message
);

public enum TextWebSocketReceiveResultType
{
    Regular,
    CloseMessageReceived,
    ClosedUnexpectedly,
}
