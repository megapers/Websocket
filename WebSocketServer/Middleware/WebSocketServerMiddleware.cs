using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;
using System.Linq;

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
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine("Message Received");
                    Console.WriteLine($"Message: {message}");
                    await RouteJSONMessageAsync(message);
                    return;
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    string id = _manager.GetAllSockets().FirstOrDefault(s => s.Value == webSocket).Key;

                    Console.WriteLine("Received Close message");
                    _manager.GetAllSockets().TryRemove(id, out WebSocket sock);

                    await sock.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                    
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

        public async Task RouteJSONMessageAsync(string message)
        {
            var routeObj = JsonConvert.DeserializeObject<dynamic>(message);
            var guid = routeObj.To.ToString();

            if (Guid.TryParse(guid, out Guid guidOutput))
            {
                Console.WriteLine("Targeted");
                var sock = _manager.GetAllSockets().FirstOrDefault(s => s.Key == guid);

                if (sock.Value != null)
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                        WebSocketMessageType.Text, true, CancellationToken.None);
                    }

                }
                else
                {
                    Console.WriteLine("Invalid recipient");
                }
            }
            else
            {
                Console.WriteLine("Broadcast");
                foreach (var sock in _manager.GetAllSockets())
                {
                    if (sock.Value.State == WebSocketState.Open)
                    {
                        await sock.Value.SendAsync(Encoding.UTF8.GetBytes(routeObj.Message.ToString()),
                        WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                }
            }
        }
    }
}