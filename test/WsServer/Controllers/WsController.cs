using Microsoft.AspNetCore.Mvc;

namespace WsServer.Controllers;

[ApiController]
[Route("[controller]")]
public class WsController : ControllerBase
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

        var buffer = new byte[1024 * 4];
        var receiveResult = await ws.ReceiveAsync(
            new ArraySegment<byte>(buffer), CancellationToken.None);

        while (!receiveResult.CloseStatus.HasValue)
        {
            await ws.SendAsync(
                new ArraySegment<byte>(buffer, 0, receiveResult.Count),
                receiveResult.MessageType,
                receiveResult.EndOfMessage,
                CancellationToken.None);

            receiveResult = await ws.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);
        }

        await ws.CloseAsync(
            receiveResult.CloseStatus.Value,
            receiveResult.CloseStatusDescription,
            CancellationToken.None);
    }
}
