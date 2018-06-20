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
            await TimeItAsync("HTTP", i => DoHttpCallsAsync(i), iterations);
            await TimeItAsync("WS  ", i => DoWebSocketsAsync(i), iterations);
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

        private static async Task DoWebSocketsAsync(int iterations)
        {
            var socket = new ClientWebSocket();
            var socketUrl = new UriBuilder(serviceBaseUrl + "/api/socket");
            socketUrl.Scheme = "ws://";
            await socket.ConnectAsync(socketUrl.Uri, CancellationToken.None);
            var jsonRpc = new JsonRpc(new WebSocketMessageHandler(socket));
            var server = jsonRpc.Attach<ISocketServer>();
            jsonRpc.StartListening();
            for (int i = 0; i < iterations; i++)
            {
                await server.HiAsync("Andrew");
            }

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None);
        }
    }
}
