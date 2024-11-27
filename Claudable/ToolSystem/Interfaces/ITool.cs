using Claudable.ToolSystem.Models;

namespace Claudable.ToolSystem.Interfaces;

public interface ITool
{
    string Id { get; }
    string Name { get; }
    string Content { get; }
    string Type { get; }
    DateTime CreatedAt { get; }
    DateTime LastModified { get; }
    bool IsOpen { get; set; }
    bool IsRunning { get; }
    ToolState State { get; }
    string WorkingDirectory { get; }
}