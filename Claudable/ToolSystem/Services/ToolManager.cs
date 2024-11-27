using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Claudable.ToolSystem.Models;
using Claudable.ViewModels;
using Claudable.Windows;

namespace Claudable.ToolSystem.Services;

public class ToolManager : INotifyPropertyChanged
{
    private readonly string _toolsDirectory;
    private ObservableCollection<Tool> _tools;
    private readonly Dictionary<string, ToolWindow> _openTools;

    public ObservableCollection<Tool> Tools
    {
        get => _tools;
        private set
        {
            _tools = value;
            OnPropertyChanged();
        }
    }

    public ToolManager()
    {
        _toolsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Claudable",
            "Tools");
        _openTools = new Dictionary<string, ToolWindow>();
        Tools = new ObservableCollection<Tool>();
        InitializeToolsDirectory();
        LoadTools();
    }

    private void InitializeToolsDirectory()
    {
        if (!Directory.Exists(_toolsDirectory))
        {
            Directory.CreateDirectory(_toolsDirectory);
        }
    }

    private void LoadTools()
    {
        var toolFiles = Directory.GetFiles(_toolsDirectory, "*.json");
        foreach (var file in toolFiles)
        {
            try
            {
                var json = File.ReadAllText(file);
                var tool = JsonSerializer.Deserialize<Tool>(json);
                if (tool != null)
                {
                    Tools.Add(tool);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading tool: {ex.Message}");
            }
        }
    }

    public async Task CacheTool(ArtifactViewModel artifact)
    {
        if (!artifact.FileName.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase))
            return;

        var tool = new Tool()
        {
            Id = Guid.NewGuid().ToString(),
            Name = Path.GetFileNameWithoutExtension(artifact.FileName),
            Content = artifact.Content,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow,
            IsOpen = false
        };

        var filePath = Path.Combine(_toolsDirectory, $"{tool.Id}.json");
        var json = JsonSerializer.Serialize(tool);
        await File.WriteAllTextAsync(filePath, json);

        Tools.Add(tool);
    }

    public void OpenTool(Tool tool)
    {
        if (_openTools.TryGetValue(tool.Id, out var window))
        {
            window.Activate();
            return;
        }

        var toolWindow = new ToolWindow(tool);
        toolWindow.Closed += (s, e) =>
        {
            tool.IsOpen = false;
            _openTools.Remove(tool.Id);
        };

        tool.IsOpen = true;
        _openTools[tool.Id] = toolWindow;
        toolWindow.Show();
    }

    public async Task RemoveTool(Tool tool)
    {
        if (_openTools.TryGetValue(tool.Id, out var window))
        {
            window.Close();
        }

        var filePath = Path.Combine(_toolsDirectory, $"{tool.Id}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        Tools.Remove(tool);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}