using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft;
using Microsoft.AspNetCore.Mvc;
using StreamJsonRpc;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SocketController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get(bool useJsonRpc)
        {
            if (!this.HttpContext.WebSockets.IsWebSocketRequest)
            {
                return this.BadRequest();
            }

            using (var webSocket = await this.HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                if (useJsonRpc)
                {
                    using (var jsonRpc = new JsonRpc(new WebSocketMessageHandler(webSocket)))
                    {
                        jsonRpc.AddLocalRpcTarget(new SocketServer());
                        jsonRpc.StartListening();
                        await jsonRpc.Completion;
                    }
                }
                else
                {
                    var buffer = new byte[100];
                    while (true)
                    {
                        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, buffer.Length), CancellationToken.None);
                        if (result.CloseStatus.HasValue)
                        {
                            await webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "ok", CancellationToken.None);
                            break;
                        }

                        await webSocket.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), WebSocketMessageType.Binary, true, CancellationToken.None);
                    }
                }
            }

            return new EmptyResult();
        }
    }
}
