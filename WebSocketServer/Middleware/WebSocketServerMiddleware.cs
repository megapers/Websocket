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
            WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
            Console.WriteLine("Websocket Connected");

            string ConnID = _manager.AddSocket(webSocket);
            await SendConnIDAsync(webSocket, ConnID);

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
        }

        private async Task SendConnIDAsync(WebSocket socket, string connId)
        {
            var buffer = Encoding.UTF8.GetBytes("ConnID: " + connId);
            await socket.SendAsync(
                buffer,
                WebSocketMessageType.Text,
                endOfMessage: true,
                cancellationToken: CancellationToken.None);
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