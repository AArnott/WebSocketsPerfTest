using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Server;
using StreamJsonRpc;

namespace Client
{
    class Program
    {
        private const string serviceBaseUrl = "http://localhost:5000";

        static async Task Main(string[] args)
        {
            await DoWebSocketsAsync();
        }

        private static async Task DoWebSocketsAsync()
        {
            var socket = new ClientWebSocket();
            var socketUrl = new UriBuilder(serviceBaseUrl + "/api/socket");
            socketUrl.Scheme = "ws://";
            await socket.ConnectAsync(socketUrl.Uri, CancellationToken.None);
            var jsonRpc = new JsonRpc(new WebSocketMessageHandler(socket));
            var server = jsonRpc.Attach<ISocketServer>();
            jsonRpc.StartListening();
            for (int i = 0; i < 10000; i++)
            {
                await server.HiAsync("Andrew");
            }

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None);
        }
    }
}
