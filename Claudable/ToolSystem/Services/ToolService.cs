using Claudable.ToolSystem.Interfaces;
using Claudable.ToolSystem.Models;
using Claudable.ViewModels;
using React;
using System.IO;

namespace Claudable.ToolSystem.Services;

public class ToolService : IToolService
{
    private readonly IReactEnvironment _reactEnv;
    private readonly IToolFileSystem _fileSystem;

    public async Task<Tool> CreateToolAsync(ArtifactViewModel artifact)
    {
        // Create tool
        var tool = new Tool(
            Guid.NewGuid().ToString(),
            Path.GetFileNameWithoutExtension(artifact.FileName),
            artifact.Content
        );

        // Initialize file system
        await InitializeToolDirectory(tool);

        return tool;
    }

    public Task<IEnumerable<Tool>> GetToolsAsync()
    {
        throw new NotImplementedException();
    }

    public Task StartToolAsync(Tool tool)
    {
        throw new NotImplementedException();
    }

    public Task StopToolAsync(Tool tool)
    {
        throw new NotImplementedException();
    }

    private async Task InitializeToolDirectory(Tool tool)
    {
        await _fileSystem.WriteFileAsync(
            Path.Combine(tool.WorkingDirectory, "Tool.tsx"),
            tool.Content
        );
    }
}