using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace WebSocketServer.Middleware
{
    public interface IWebSocketServerConnectionManager
    {
        public ConcurrentDictionary<string, WebSocket> GetAllSockets();
        public string AddSocket(WebSocket socket);
    }
}