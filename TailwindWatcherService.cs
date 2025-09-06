using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TailwindCSS.Blazor;

public class TailwindWatcherService : BackgroundService
{
    private readonly ILogger<TailwindWatcherService> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly TailwindWatcherOptions _options;
    private readonly ITailwindMessageHub _messageHub;
    private Process? _tailwindProcess;

    public TailwindWatcherService(
        ILogger<TailwindWatcherService> logger, 
        IWebHostEnvironment env,
        IOptions<TailwindWatcherOptions> options,
        ITailwindMessageHub messageHub)
    {
        _logger = logger;
        _env = env;
        _options = options.Value;
        _messageHub = messageHub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var shouldRun = (_env.IsDevelopment() && _options.EnableInDevelopment) ||
                       (_env.IsProduction() && _options.EnableInProduction);

        if (!shouldRun)
        {
            _logger.LogInformation("Tailwind watcher skipped - disabled for {Environment} environment", _env.EnvironmentName);
            return;
        }

        try
        {
            await StartTailwindWatcher(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Tailwind watcher service");
        }
    }

    private async Task StartTailwindWatcher(CancellationToken stoppingToken)
    {
        var arguments = BuildTailwindArguments();
        var projectRootDir = Directory.GetParent(_env.ContentRootPath); // /usr/name/src/blazor1
        var startInfo = new ProcessStartInfo
        {
            FileName = "npx",
            Arguments = arguments,
            WorkingDirectory = projectRootDir.FullName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        _logger.LogInformation("Starting Tailwind watcher at working dir {wkd} with command: npx {Arguments}",projectRootDir, arguments);

        _tailwindProcess = new Process { StartInfo = startInfo };

        // Handle output and error streams
        _tailwindProcess.OutputDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                _logger.LogInformation("Tailwind: {Message}", args.Data);
                
                // Send message to WebSocket connections
                _messageHub.SendMessage(JsonSerializer.Serialize(new
                {
                    type = "tailwind-output",
                    message = args.Data,
                    timestamp = DateTime.UtcNow
                }));
            }
        };

        _tailwindProcess.ErrorDataReceived += (sender, args) =>
        {
            if (!string.IsNullOrEmpty(args.Data))
            {
                // Check if it's actually an error or just informational output
                var isActualError = args.Data.ToLower().Contains("error") || 
                                   args.Data.ToLower().Contains("failed") ||
                                   args.Data.ToLower().Contains("cannot");
                
                if (isActualError)
                {
                    _logger.LogError("Tailwind Error: {Message}", args.Data);
                    _messageHub.SendMessage(JsonSerializer.Serialize(new
                    {
                        type = "tailwind-error",
                        message = args.Data,
                        timestamp = DateTime.UtcNow
                    }));
                }
                else
                {
                    // Treat as informational output
                    _logger.LogInformation("Tailwind: {Message}", args.Data);
                    _messageHub.SendMessage(JsonSerializer.Serialize(new
                    {
                        type = "tailwind-output", // Changed from "tailwind-error"
                        message = args.Data,
                        timestamp = DateTime.UtcNow
                    }));
                }
            }
        };

        _tailwindProcess.Start();
        _tailwindProcess.BeginOutputReadLine();
        _tailwindProcess.BeginErrorReadLine();

        _logger.LogInformation("Tailwind CSS watcher started with command: npx {Arguments}", arguments);

        // Send startup message
        _messageHub.SendMessage(JsonSerializer.Serialize(new
        {
            type = "tailwind-started",
            message = "Tailwind CSS watcher started",
            timestamp = DateTime.UtcNow
        }));

        // Wait for the process to exit or cancellation
        try
        {
            await _tailwindProcess.WaitForExitAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Tailwind watcher service was cancelled");
        }

        if (_tailwindProcess.HasExited)
        {
            _logger.LogInformation("Tailwind process exited with code: {ExitCode}", _tailwindProcess.ExitCode);
            
            _messageHub.SendMessage(JsonSerializer.Serialize(new
            {
                type = "tailwind-stopped",
                message = $"Tailwind process exited with code: {_tailwindProcess.ExitCode}",
                timestamp = DateTime.UtcNow
            }));
        }
    }

    private string BuildTailwindArguments()
    {
        var webRootDir = _env.ContentRootPath; // /usr/name/src/blazor1/blazor1
        var inputPath = Path.Combine(webRootDir, _options.InputPath); // /usr/name/src/blazor1/blazor1/app.css;
        var outputPath = Path.Combine(webRootDir, "wwwroot", _options.OutputFileName ?? "app.css"); // /usr/name/src/blazor1/wwwroot/app.css;
        var args = new List<string>
        {
            "@tailwindcss/cli",
            "-i", inputPath,
            "-o", outputPath,
            "--watch"
        };

        if (!string.IsNullOrEmpty(_options.ConfigPath))
        {
            args.AddRange(new[] { "--config", _options.ConfigPath ?? "tailwind.config.js" });
        }

        if (_options.EnableMinify)
        {
            args.Add("--minify");
        }

        if (_options.AdditionalArguments.Any())
        {
            args.AddRange(_options.AdditionalArguments);
        }

        return string.Join(" ", args);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Tailwind watcher service");

        if (_tailwindProcess != null && !_tailwindProcess.HasExited)
        {
            try
            {
                _tailwindProcess.Kill(true); // Kill the process tree
                await _tailwindProcess.WaitForExitAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping Tailwind process");
            }
        }

        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        _logger.LogInformation("Tailwind watcher service is disposing...");
        _tailwindProcess?.Dispose();
        base.Dispose();
        _logger.LogInformation("Tailwind watcher service disposed");
    }
}