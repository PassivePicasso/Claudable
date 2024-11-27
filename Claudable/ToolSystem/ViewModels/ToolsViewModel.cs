using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Claudable.ToolSystem.Models;
using Claudable.ToolSystem.Services;
using Claudable.ViewModels;

namespace Claudable.ToolSystem.ViewModels;

public class ToolsViewModel : INotifyPropertyChanged
{
    private readonly ToolManager _toolManager;
    private ObservableCollection<Tool> _tools;

    public ObservableCollection<Tool> Tools
    {
        get => _tools;
        set
        {
            _tools = value;
            OnPropertyChanged();
        }
    }

    public ICommand OpenToolCommand { get; }
    public ICommand RemoveToolCommand { get; }

    public ToolsViewModel()
    {
        _toolManager = new ToolManager();
        Tools = _toolManager.Tools;

        OpenToolCommand = new RelayCommand<Tool>(OpenTool);
        RemoveToolCommand = new RelayCommand<Tool>(RemoveTool);
    }

    private void OpenTool(Tool tool)
    {
        if (tool != null)
        {
            _toolManager.OpenTool(tool);
        }
    }

    private async void RemoveTool(Tool tool)
    {
        if (tool != null)
        {
            await _toolManager.RemoveTool(tool);
        }
    }

    public async Task CacheTool(ArtifactViewModel artifact)
    {
        await _toolManager.CacheTool(artifact);
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}