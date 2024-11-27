# React Tools Implementation Specification

## 1. Core Dependencies

```xml
<ItemGroup>
    <!-- JavaScript Engine -->
    <PackageReference Include="JavaScriptEngineSwitcher.Core" Version="3.23.1" />
    <PackageReference Include="JavaScriptEngineSwitcher.V8" Version="3.23.2" />
    <PackageReference Include="JavaScriptEngineSwitcher.V8.Native.win-x64" Version="3.23.2" />
    
    <!-- React Integration -->
    <PackageReference Include="React.Core" Version="5.2.12" />
    
    <!-- File System Abstraction -->
    <PackageReference Include="System.IO.Abstractions" Version="19.2.87" />
</ItemGroup>
```

## 2. Model Layer

```csharp
public interface ITool
{
    string Id { get; }
    string Name { get; }
    string Content { get; }
    string Type { get; }
    DateTime CreatedAt { get; }
    DateTime LastModified { get; }
    bool IsRunning { get; }
    ToolState State { get; }
    string WorkingDirectory { get; }
}

public enum ToolState
{
    Initializing,
    Running,
    Error,
    Stopped
}

public class Tool : ITool, INotifyPropertyChanged
{
    private readonly string _baseDirectory;
    private ToolState _state;
    
    public Tool(string id, string name, string content)
    {
        Id = id;
        Name = name;
        Content = content;
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Claudable",
            "Tools",
            id
        );
    }
    
    // INotifyPropertyChanged implementation
}
```

## 3. Services Layer

### 3.1 File System Service

```csharp
public interface IToolFileSystem
{
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
    Task<bool> FileExistsAsync(string path);
    Task<IEnumerable<string>> ListFilesAsync(string path);
}

public class ToolFileSystem : IToolFileSystem
{
    private readonly IFileSystem _fileSystem;
    private readonly string _rootPath;

    public ToolFileSystem(IFileSystem fileSystem, string rootPath)
    {
        _fileSystem = fileSystem;
        _rootPath = rootPath;
    }

    public async Task<string> ReadFileAsync(string path)
    {
        var safePath = GetSafePath(path);
        return await _fileSystem.File.ReadAllTextAsync(safePath);
    }

    private string GetSafePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));
        if (!fullPath.StartsWith(_rootPath))
        {
            throw new SecurityException("Path traversal not allowed");
        }
        return fullPath;
    }
}
```

### 3.2 React Environment Service

```csharp
public interface IReactEnvironment
{
    Task InitializeAsync();
    Task<string> RenderComponentAsync(string componentCode);
    Task<bool> ValidateComponentAsync(string componentCode);
}

public class ReactEnvironment : IReactEnvironment
{
    private readonly IJavaScriptEngine _jsEngine;
    private readonly IReactEnvironment _reactEnv;
    private readonly IToolFileSystem _fileSystem;

    public ReactEnvironment(
        IJavaScriptEngine jsEngine,
        IReactEnvironment reactEnv,
        IToolFileSystem fileSystem)
    {
        _jsEngine = jsEngine;
        _reactEnv = reactEnv;
        _fileSystem = fileSystem;
    }

    public async Task InitializeAsync()
    {
        await _reactEnv.InitializeAsync();
        await InitializeFileSystem();
    }

    private async Task InitializeFileSystem()
    {
        // Inject file system API into JS environment
        await _jsEngine.EvaluateAsync(@"
            window.fs = {
                readFile: async (path, options = {}) => {
                    return await Bridge.readFile(path, options);
                }
            };
        ");
    }
}
```

### 3.3 Tool Service

```csharp
public interface IToolService
{
    Task<Tool> CreateToolAsync(ArtifactViewModel artifact);
    Task StartToolAsync(Tool tool);
    Task StopToolAsync(Tool tool);
    Task<IEnumerable<Tool>> GetToolsAsync();
}

public class ToolService : IToolService
{
    private readonly IReactEnvironment _reactEnv;
    private readonly IToolFileSystem _fileSystem;
    
    public async Task<Tool> CreateToolAsync(ArtifactViewModel artifact)
    {
        // Validate component
        await _reactEnv.ValidateComponentAsync(artifact.Content);
        
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
    
    private async Task InitializeToolDirectory(Tool tool)
    {
        await _fileSystem.WriteFileAsync(
            Path.Combine(tool.WorkingDirectory, "Tool.tsx"),
            tool.Content
        );
    }
}
```

## 4. View Model Layer

```csharp
public class ToolViewModel : INotifyPropertyChanged
{
    private readonly Tool _tool;
    private readonly IReactEnvironment _reactEnv;
    private readonly IToolFileSystem _fileSystem;
    
    public ToolViewModel(
        Tool tool,
        IReactEnvironment reactEnv,
        IToolFileSystem fileSystem)
    {
        _tool = tool;
        _reactEnv = reactEnv;
        _fileSystem = fileSystem;
        
        StartCommand = new RelayCommand(Start, CanStart);
        StopCommand = new RelayCommand(Stop, CanStop);
    }
    
    public ICommand StartCommand { get; }
    public ICommand StopCommand { get; }
    
    private async Task Start()
    {
        try
        {
            await _reactEnv.InitializeAsync();
            _tool.State = ToolState.Running;
        }
        catch (Exception ex)
        {
            _tool.State = ToolState.Error;
            // Handle error
        }
    }
}
```

## 5. View Layer

```csharp
public partial class ToolWindow : Window
{
    private readonly ToolViewModel _viewModel;
    
    public ToolWindow(Tool tool)
    {
        InitializeComponent();
        
        _viewModel = new ToolViewModel(
            tool,
            new ReactEnvironment(
                JsEngineSwitcher.Current.CreateEngine(),
                ReactEnvironment.Current,
                new ToolFileSystem(new FileSystem(), tool.WorkingDirectory)
            )
        );
        
        DataContext = _viewModel;
    }
    
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await InitializeWebView();
    }
    
    private async Task InitializeWebView()
    {
        await WebViewFactory.InitializeWebView(ToolWebView);
        
        // Initialize bridge
        await ToolWebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(@"
            window.Bridge = {
                readFile: (path, options) => window.chrome.webview.postMessage({
                    type: 'fs',
                    operation: 'readFile',
                    path: path,
                    options: options
                })
            };
        ");
        
        // Handle messages
        ToolWebView.CoreWebView2.WebMessageReceived += HandleWebMessage;
    }
}
```

## 6. Implementation Steps

1. Add NuGet dependencies
2. Implement Model layer
3. Implement Services layer
4. Update ViewModels
5. Update Views
6. Add error handling
7. Implement logging
8. Add unit tests
9. Add integration tests

## 7. Testing Strategy

```csharp
public class ToolServiceTests
{
    private readonly IFileSystem _mockFileSystem;
    private readonly IReactEnvironment _mockReactEnv;
    private readonly IToolService _service;
    
    [Fact]
    public async Task CreateTool_ValidComponent_CreatesToolWithFileSystem()
    {
        // Arrange
        var artifact = new ArtifactViewModel
        {
            FileName = "TestComponent.tsx",
            Content = "valid component code"
        };
        
        // Act
        var tool = await _service.CreateToolAsync(artifact);
        
        // Assert
        Assert.NotNull(tool);
        Assert.True(await _mockFileSystem.File.ExistsAsync(
            Path.Combine(tool.WorkingDirectory, "Tool.tsx")
        ));
    }
}
```
