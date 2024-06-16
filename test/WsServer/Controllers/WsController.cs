using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace WsServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WsController(ILogger<WsController> logger) : ControllerBase
{
    [HttpGet]
    public async Task Get()
    {
        if (!HttpContext.WebSockets.IsWebSocketRequest)
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return;
        }

        var ws = await HttpContext.WebSockets.AcceptWebSocketAsync();

        var buffer = new ArraySegment<byte>(new byte[1024 * 4]);
        var receiveResult = await ws.ReceiveAsync(buffer, CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            var segment = buffer[..receiveResult.Count];
            logger.LogDebug("Received: {Message}", Encoding.UTF8.GetString(segment));

            await ws.SendAsync(
                segment,
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await ws.ReceiveAsync(buffer, CancellationToken.None);
        }

        await ws.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
