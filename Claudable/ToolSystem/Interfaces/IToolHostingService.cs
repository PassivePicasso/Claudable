namespace Claudable.ToolSystem.Interfaces;

public interface IToolHostingService
{
    Task StartAsync();
    Task StopAsync();
    string GetToolUrl(string toolId);
    int Port { get; }
}