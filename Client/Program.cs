using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
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
            int iterations = 10000;
            await TimeItAsync("HTTP         ", i => DoHttpCallsAsync(i), iterations);
            await TimeItAsync("WS (json-rpc)", i => DoJsonRpcOverWebSocketsAsync(i), iterations);
            await TimeItAsync("WS (raw)     ", i => DoRawBinaryOverWebSocketsAsync(i), iterations);
        }

        private static async Task TimeItAsync(string name, Func<int, Task> operation, int iterations)
        {
            var sw = Stopwatch.StartNew();
            await operation(iterations);
            sw.Stop();
            System.Console.WriteLine($"{name} took {sw.Elapsed} for {iterations} iterations. {(double)sw.ElapsedMilliseconds / iterations}ms per iteration.");
        }

        private static async Task DoHttpCallsAsync(int iterations)
        {
            var client = new HttpClient();
            var httpUrl = new Uri(serviceBaseUrl + "/api/http");
            for (int i = 0; i < iterations; i++)
            {
                var response = await client.PostAsync(httpUrl, new FormUrlEncodedContent(new Dictionary<string, string> { { "character", "Andrew" } }));
                response.EnsureSuccessStatusCode();
            }
        }

        private static async Task DoJsonRpcOverWebSocketsAsync(int iterations)
        {
            var socket = new ClientWebSocket();
            var socketUrl = new UriBuilder(serviceBaseUrl + "/api/socket?useJsonRpc=true");
            socketUrl.Scheme = "ws://";
            await socket.ConnectAsync(socketUrl.Uri, CancellationToken.None);
            using (var jsonRpc = new JsonRpc(new WebSocketMessageHandler(socket)))
            {
                var server = jsonRpc.Attach<ISocketServer>();
                jsonRpc.StartListening();
                for (int i = 0; i < iterations; i++)
                {
                    await server.HiAsync("Andrew");
                }

                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None);
            }
        }

        private static async Task DoRawBinaryOverWebSocketsAsync(int iterations)
        {
            var socket = new ClientWebSocket();
            var socketUrl = new UriBuilder(serviceBaseUrl + "/api/socket?useJsonRpc=false");
            socketUrl.Scheme = "ws://";
            await socket.ConnectAsync(socketUrl.Uri, CancellationToken.None);
            var buffer = new byte[100];
            for (int i = 0; i < iterations; i++)
            {
                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, 20), WebSocketMessageType.Binary, true, CancellationToken.None);
                await socket.ReceiveAsync(new ArraySegment<byte>(buffer, 0, 20), CancellationToken.None);
            }

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None);
        }
    }
}
