using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<IActionResult> Post()
        {
            if (!this.HttpContext.WebSockets.IsWebSocketRequest)
            {
                return this.BadRequest();
            }

            using (var webSocket = await this.HttpContext.WebSockets.AcceptWebSocketAsync())
            {
                using (var jsonRpc = new JsonRpc(new WebSocketMessageHandler(webSocket)))
                {
                    jsonRpc.AddLocalRpcTarget(new SocketServer());
                    jsonRpc.StartListening();
                    await jsonRpc.Completion;
                }
            }

            return new EmptyResult();
        }
    }
}
