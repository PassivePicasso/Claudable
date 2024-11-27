using Claudable.ToolSystem.Interfaces;
using EmbedIO;
using EmbedIO.Actions;
using EmbedIO.WebApi;
using Microsoft.Extensions.Logging;
using System.IO;

namespace Claudable.ToolSystem.Services;

public class ToolHostingService : IToolHostingService, IDisposable
{
    private WebServer _server;
    private readonly string _toolsDirectory;
    private readonly string _wwwRoot;
    private bool _isDisposed;
    private readonly ILogger _logger;
    private readonly Random _random = new Random();

    public int Port { get; private set; }

    public ToolHostingService(string toolsDirectory, ILogger logger)
    {
        _toolsDirectory = toolsDirectory;
        _logger = logger;
        _wwwRoot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Claudable",
            "ToolServer");

        // Ensure directories exist
        Directory.CreateDirectory(_wwwRoot);
        Directory.CreateDirectory(Path.Combine(_wwwRoot, "tools"));
    }

    public async Task StartAsync()
    {
        // Try ports in 5000-5999 range until we find an available one
        for (int i = 0; i < 1000; i++)
        {
            Port = 5000 + _random.Next(1000);
            try
            {
                _server = CreateWebServer(Port);
                await _server.RunAsync();
                _logger.LogInformation($"Tool server started on port {Port}");
                return;
            }
            catch (Exception ex) when (ex.Message.Contains("address already in use"))
            {
                _logger.LogWarning($"Port {Port} in use, trying another...");
                continue;
            }
        }
        throw new Exception("Could not find available port for tool server");
    }

    private WebServer CreateWebServer(int port)
    {
        var server = new WebServer(o => o
                .WithUrlPrefix($"http://localhost:{port}/")
                .WithMode(HttpListenerMode.EmbedIO))
            .WithWebApi("/api", c => c
                .WithController<ToolController>())
            .WithStaticFolder("/", _wwwRoot, true)
            .WithModule(new ActionModule("/", HttpVerbs.Any, ctx => ctx.SendDataAsync(new { Message = "Tool Server Running" })));

        // Add CORS support
        server.WithCors();

        return server;
    }

    public string GetToolUrl(string toolId) => $"http://localhost:{Port}/tools/{toolId}";

    public async Task StopAsync()
    {
        if (_server != null)
        {
            await _server.StopAsync();
            _logger.LogInformation("Tool server stopped");
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                _server?.Dispose();
            }
            _isDisposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
