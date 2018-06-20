using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Threading;
using Server;
using StreamJsonRpc;

namespace Client
{
    class Program
    {
        // private const string serviceBaseUrl = "http://websocketsperftest-west.westus2.azurecontainer.io";
        // private const string serviceBaseUrl = "http://websocketsperftest2.eastus.azurecontainer.io";
        private const string serviceBaseUrl = "http://localhost:5000";

        static async Task<int> Main(string[] args)
        {
            if (args.Length != 1 || !int.TryParse(args[0], out int iterations)) {
                await Console.Error.WriteLineAsync("Pass in the number of iterations as the only argument.");
                return 1;
            }

            // await ConcurrentWebSocketsAsync(iterations);
            await TimeItAsync("HTTP         ", i => DoHttpCallsAsync(i), iterations);
            await TimeItAsync("WS (json-rpc)", i => DoJsonRpcOverWebSocketsAsync(i), iterations);
            await TimeItAsync("WS (raw)     ", i => DoRawBinaryOverWebSocketsAsync(i), iterations);

            return 0;
        }

        private static async Task ConcurrentWebSocketsAsync(int iterations)
        {
            await TimeItAsync("Concurrent web sockets",
                async delegate
                {
                    Console.WriteLine($"Establishing {iterations} web socket connections...");
                    var ws = new Task<ClientWebSocket>[iterations];
                    int successful = 0;
                    for (int i = 0; i < ws.Length; i++)
                    {
                        ws[i] = Task.Run(async delegate
                        {
                            var socket = new ClientWebSocket();
                            var socketUrl = new UriBuilder(serviceBaseUrl + "/api/socket?useJsonRpc=true");
                            socketUrl.Scheme = "ws://";
                            while (true)
                            {
                                try
                                {
                                    await socket.ConnectAsync(socketUrl.Uri, CancellationToken.None);
                                    if (Interlocked.Increment(ref successful) % 100 == 0) Console.Write(".");
                                    return socket;
                                }
                                catch (Exception)
                                {
                                }
                            }
                        });
                    }

                    await Task.WhenAll(ws).NoThrowAwaitable();
                    Console.WriteLine();
                    Console.WriteLine(string.Join(Environment.NewLine, ws.Where(s => s.IsFaulted).Select(s => s.Exception.InnerException.Message).Distinct()));
                    Console.WriteLine($"Closing {successful} web socket connections...");
                    await Task.WhenAll(ws.Where(s => !s.IsFaulted).Select(socket =>
                        socket.Result.CloseAsync(WebSocketCloseStatus.NormalClosure, "normal", CancellationToken.None)));
                },
            1);
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
