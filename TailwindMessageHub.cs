using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace TailwindCSS.Blazor;

public class TailwindMessageHub(ILogger<TailwindMessageHub> logger) : ITailwindMessageHub, IAsyncDisposable, IDisposable
{
    private readonly List<WebSocket> _connectedSockets = new();

    public event Action<string>? MessageReceived;

    public void SendMessage(string message)
    {
        MessageReceived?.Invoke(message);
        BroadcastToWebSocketsAsync(message);
    }


    public Task AddWebSocketAsync(WebSocket webSocket)
    {
        _connectedSockets.Add(webSocket);
        logger.LogInformation("WebSocket connection added. Total connections: {Count}", _connectedSockets.Count);
        return Task.CompletedTask;
    }

    public Task RemoveWebSocketAsync(WebSocket webSocket)
    {
        _connectedSockets.Remove(webSocket);
        logger.LogInformation("WebSocket connection removed. Total connections: {Count}", _connectedSockets.Count);
        return Task.CompletedTask;
    }

    private async void BroadcastToWebSocketsAsync(string message)
    {
        var messageBytes = Encoding.UTF8.GetBytes(message);
        var socketsToRemove = new List<WebSocket>();

        foreach (var socket in _connectedSockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                try
                {
                    await socket.SendAsync(
                        new ArraySegment<byte>(messageBytes),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to send message to WebSocket");
                    socketsToRemove.Add(socket);
                }
            }
            else
            {
                socketsToRemove.Add(socket);
            }
        }

        // Clean up closed connections
        foreach (var socket in socketsToRemove)
        {
            _connectedSockets.Remove(socket);
        }
    }

    public async Task SentMessageToWebSocketsAsync(WebSocket socket, string type, string message)
    {
        try
        {
            var messageObject = new { type, message };
            var jsonMessage = System.Text.Json.JsonSerializer.Serialize(messageObject);
            var messageBytes = Encoding.UTF8.GetBytes(jsonMessage);
            await socket.SendAsync(
                new ArraySegment<byte>(messageBytes),
                WebSocketMessageType.Text,
                true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send message to WebSocket");
        }
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("TailwindMessageHub DisposeAsync...");
        foreach (var socket in _connectedSockets.ToList())
        {
            if (socket.State == WebSocketState.Open)
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Hub is shutting down",
                    CancellationToken.None);
            }

            _connectedSockets.Remove(socket);
        }
        logger.LogInformation("TailwindMessageHub DisposeAsync Done");
    }

    public void Dispose()
    {
        logger.LogInformation("TailwindMessageHub Disposing...");
        DisposeAsync().AsTask().Wait();
        logger.LogInformation("TailwindMessageHub Disposed");
    }

    public async Task CloseAllConnectionsAsync()
    {
        var closeTasks = new List<Task>();

        foreach (var webSocket in _connectedSockets.ToList())
        {
            if (webSocket.State == WebSocketState.Open)
            {
                closeTasks.Add(CloseWebSocketSafely(webSocket));
            }
        }

        await Task.WhenAll(closeTasks);
    }

    private async Task CloseWebSocketSafely(WebSocket webSocket)
    {
        try
        {
            await webSocket.CloseAsync(
                WebSocketCloseStatus.NormalClosure,
                "Application shutting down",
                CancellationToken.None);
        }
        catch (Exception)
        {
            // Ignore exceptions during shutdown
        }
    }

}