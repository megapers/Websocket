using System.Net.WebSockets;
using System.Text;

namespace WebSocketServer.Middleware
{
    public class WebSocketServerMiddleware : IMiddleware
    {
        private readonly IWebSocketServerConnectionManager _manager;

        public WebSocketServerMiddleware(IWebSocketServerConnectionManager manager)
        {
            _manager = manager;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            // if (context.WebSockets.IsWebSocketRequest)
            // {
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("Websocket Connected");

            string ConnID = _manager.AddSocket(webSocket);

            await ReceiveMessage(webSocket, async (result, buffer) =>
            {
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    Console.WriteLine("Message Received");
                    Console.WriteLine($"Message: {Encoding.UTF8.GetString(buffer, 0, result.Count)}");
                    return;
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    Console.WriteLine("Received Close message");
                    return;
                }
            });
            // }
            // else
            // {
            //     Console.WriteLine("Hello from the 2nd request delegate.");
            //     await next(context);
            // }
        }

        private async Task ReceiveMessage(WebSocket socket, Action<WebSocketReceiveResult, byte[]> handleMessage)
        {
            var buffer = new byte[1024 * 4];

            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(buffer: new ArraySegment<byte>(buffer),
                cancellationToken: CancellationToken.None);

                handleMessage(result, buffer);
            }
        }
    }
}