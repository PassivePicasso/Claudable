using Claudable.ToolSystem.Models;
using Claudable.ViewModels;

namespace Claudable.ToolSystem.Interfaces;
public interface IToolService
{
    Task<Tool> CreateToolAsync(ArtifactViewModel artifact);
    Task StartToolAsync(Tool tool);
    Task StopToolAsync(Tool tool);
    Task<IEnumerable<Tool>> GetToolsAsync();
}