using System.Net.WebSockets;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace TailwindCSS.Blazor;

public static class TailwindCSSBlazorExtensions
{
    public static void AddTailwindCSS(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration.GetSection(TailwindWatcherOptions.SectionName);
        builder.Services.AddSingleton<ITailwindMessageHub, TailwindMessageHub>();
        if (!config.Exists())
        {
            builder.Services.Configure<TailwindWatcherOptions>(options =>
            {
                options.InputPath = "app.css";
                options.OutputFileName = "app.css";
            });
        }
        else
        {
            builder.Services.Configure<TailwindWatcherOptions>(config);
        }
        builder.Services.AddHostedService<TailwindWatcherService>();
    }

    public static void UseTailwindCSS(this WebApplication app)
    {
        app.UseWebSockets();
        //WebSocket endpoint
        app.Map("/ws", async (HttpContext context, ITailwindMessageHub messageHub) =>
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await HandleWebSocketConnection(webSocket, messageHub, app);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        });
    }

   static async Task HandleWebSocketConnection(WebSocket webSocket, ITailwindMessageHub messageHub,WebApplication app
   )
    {
        // Get application lifetime for shutdown handling
        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        // Register shutdown handler to close all WebSocket connections
        lifetime.ApplicationStopping.Register(async () =>
        {
            var messageHub = app.Services.GetRequiredService<ITailwindMessageHub>();
            await messageHub.CloseAllConnectionsAsync();
        });


    var options = app.Services.GetRequiredService<IOptions<TailwindWatcherOptions>>();

    // Add this connection to the message hub
    var outputFileName = options.Value.OutputFileName;
        await messageHub.AddWebSocketAsync(webSocket);
        // Sent CSS file name to a client
        await messageHub.SentMessageToWebSocketsAsync(webSocket, "__CSS_FILE__", outputFileName);

        WebSocketReceiveResult? receiveResult = null;

        try
        {
            var buffer = new byte[1024 * 4];
            receiveResult = await webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer), CancellationToken.None);

            while (!receiveResult.CloseStatus.HasValue)
            {
                // Handle incoming messages from client if needed
                var message = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                // Process client messages here if needed


                receiveResult = await webSocket.ReceiveAsync(
                    new ArraySegment<byte>(buffer), CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            // Log exception
        }
        finally
        {
            // Remove connection from message hub
            await messageHub.RemoveWebSocketAsync(webSocket);

            if (webSocket.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(
                    receiveResult?.CloseStatus ?? WebSocketCloseStatus.NormalClosure,
                    receiveResult?.CloseStatusDescription,
                    CancellationToken.None);
            }
        }
    }
}