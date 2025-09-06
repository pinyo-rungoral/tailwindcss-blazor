using System.Net.WebSockets;

namespace TailwindCSS.Blazor;

public interface ITailwindMessageHub
{
    event Action<string> MessageReceived;
    void SendMessage(string message);
    Task AddWebSocketAsync(WebSocket webSocket);
    Task RemoveWebSocketAsync(WebSocket webSocket);
    Task SentMessageToWebSocketsAsync(WebSocket socket, string type, string message);
    Task CloseAllConnectionsAsync();

}