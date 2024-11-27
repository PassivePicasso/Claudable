# WPF MVVM Architectural Guidelines

## Preface for Claude

As the lead developer on this project, I should use this document to:

1. Guide discussions about architectural decisions by referencing specific sections
2. Ask clarifying questions when architectural choices deviate from these guidelines
3. Suggest updates to this document as the architecture evolves
4. Provide concrete examples from the codebase when explaining concepts
5. Focus on maintaining consistency with existing patterns while improving them
6. Identify areas where the current implementation could be better aligned with these guidelines

When interacting with developers, I should:

1. Reference specific sections of this document when providing feedback
2. Acknowledge good implementations that follow these guidelines
3. Suggest incremental improvements rather than complete rewrites
4. Explain the reasoning behind architectural decisions
5. Be open to discussing and evolving these guidelines based on practical experience
6. Use code examples from the current codebase to illustrate points

## 1. Project Structure

### 1.1 Core Layers

The application should be organized into the following layers:

- **Models**: Business logic and data structures
- **ViewModels**: Presentation logic and state management
- **Views**: UI elements and user interaction
- **Services**: Reusable business operations and external integrations
- **Utilities**: Helper classes and extension methods

### 1.2 Folder Organization

```
Claudable/
├── Models/          # Business models and data structures
├── ViewModels/      # MVVM ViewModels
├── Views/           # XAML Views and code-behind
├── Services/        # Business services and integrations
├── Utilities/       # Helper classes and extensions
├── Controls/        # Custom WPF controls
├── Converters/      # Value converters
└── Resources/       # Shared resources (styles, templates)
```

### 1.3 File Naming Conventions

- Views: `[Name]View.xaml`
- ViewModels: `[Name]ViewModel.cs`
- Models: `[Name].cs`
- Services: `[Name]Service.cs`
- Interfaces: `I[Name].cs`

## 2. MVVM Implementation

### 2.1 ViewModels

#### Core Principles

1. Implement `INotifyPropertyChanged`
2. Use `RelayCommand` for commands
3. Maintain clean separation from View
4. Handle all business logic
5. Avoid code-behind dependencies

#### Standard Implementation

```csharp
public class ExampleViewModel : INotifyPropertyChanged
{
    private string _property;
    public string Property
    {
        get => _property;
        set
        {
            _property = value;
            OnPropertyChanged();
        }
    }

    public ICommand ExampleCommand { get; }

    public ExampleViewModel()
    {
        ExampleCommand = new RelayCommand(ExecuteExample, CanExecuteExample);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public event PropertyChangedEventHandler PropertyChanged;
}
```

### 2.2 Views

#### Guidelines

1. Minimize code-behind
2. Use XAML-only bindings
3. Implement proper resource organization
4. Follow the single responsibility principle
5. Use user controls for reusable UI components

#### Example Structure

```xaml
<Window x:Class="Claudable.Views.ExampleView"
        xmlns:vm="clr-namespace:Claudable.ViewModels">
    
    <Window.DataContext>
        <vm:ExampleViewModel/>
    </Window.DataContext>

    <Grid>
        <!-- UI Elements -->
    </Grid>
</Window>
```

### 2.3 Models

1. Pure data models without UI logic
2. Implement validation when needed
3. Use data annotations for validation
4. Keep models serializable
5. Implement proper equality comparison

## 3. Services and Dependency Injection

### 3.1 Service Guidelines

1. Define interfaces for all services
2. Use dependency injection
3. Follow single responsibility principle
4. Implement proper disposal patterns
5. Use async/await for I/O operations

### 3.2 Service Implementation

```csharp
public interface IExampleService
{
    Task<Result> OperationAsync();
}

public class ExampleService : IExampleService
{
    private readonly IDependency _dependency;

    public ExampleService(IDependency dependency)
    {
        _dependency = dependency;
    }

    public async Task<Result> OperationAsync()
    {
        // Implementation
    }
}
```

## 4. Data Binding

### 4.1 Best Practices

1. Use proper binding modes
2. Implement value conversion when needed
3. Use proper UpdateSourceTrigger
4. Handle validation properly
5. Use proper string format bindings

### 4.2 Binding Examples

```xaml
<!-- Value Binding -->
<TextBox Text="{Binding Property, UpdateSourceTrigger=PropertyChanged}"/>

<!-- Command Binding -->
<Button Command="{Binding ExampleCommand}"
        CommandParameter="{Binding Parameter}"/>

<!-- Collection Binding -->
<ListBox ItemsSource="{Binding Items}"
         SelectedItem="{Binding SelectedItem}"/>
```

## 5. Resource Management

### 5.1 Organization

1. Separate resource dictionaries by purpose
2. Use merged dictionaries effectively
3. Implement proper theming
4. Follow naming conventions
5. Use static resources when possible

### 5.2 Structure

```xaml
<ResourceDictionary>
    <!-- Colors -->
    <Color x:Key="PrimaryColor">#FF1234</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource PrimaryColor}"/>

    <!-- Styles -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <!-- Style settings -->
    </Style>
</ResourceDictionary>
```

## 6. Error Handling and Logging

### 6.1 Guidelines

1. Implement proper exception handling
2. Use async/await properly
3. Implement logging
4. Handle UI thread synchronization
5. Provide user feedback

### 6.2 Implementation

```csharp
try
{
    await Operation();
}
catch (SpecificException ex)
{
    // Handle specific case
    _logger.LogError(ex, "Operation failed");
    await ShowErrorMessage(ex.Message);
}
catch (Exception ex)
{
    // Handle general case
    _logger.LogError(ex, "Unexpected error");
    await ShowErrorMessage("An unexpected error occurred");
}
```

## 7. Testing

### 7.1 Testing Strategy

1. Unit test ViewModels
2. Test commands and properties
3. Mock services and dependencies
4. Test validation logic
5. Implement integration tests

### 7.2 Example Tests

```csharp
[Fact]
public void PropertyChange_ShouldRaisePropertyChangedEvent()
{
    // Arrange
    var vm = new ExampleViewModel();
    var raised = false;
    vm.PropertyChanged += (s, e) => raised = true;

    // Act
    vm.Property = "new value";

    // Assert
    Assert.True(raised);
}
```

## 8. Performance Considerations

### 8.1 Guidelines

1. Implement proper collection virtualization
2. Use efficient data binding
3. Handle large datasets properly
4. Implement proper UI virtualization
5. Use async operations for I/O

### 8.2 Implementation

```xaml
<ListBox VirtualizingStackPanel.IsVirtualizing="True"
         VirtualizingStackPanel.VirtualizationMode="Recycling"
         ScrollViewer.IsDeferredScrollingEnabled="True">
    <!-- Items -->
</ListBox>
```

## 9. Documentation

### 9.1 Requirements

1. Document public APIs
2. Include usage examples
3. Document architectural decisions
4. Maintain README
5. Document configuration requirements

### 9.2 Example

```csharp
/// <summary>
/// Manages application state and handles state persistence.
/// </summary>
/// <remarks>
/// Implements the singleton pattern for application-wide state management.
/// </remarks>
public class StateManager
{
    // Implementation
}
```

## 10. Security Considerations

### 10.1 Guidelines

1. Implement proper input validation
2. Handle sensitive data properly
3. Implement proper authentication
4. Use secure communication
5. Follow principle of least privilege

### 10.2 Implementation

```csharp
public class SecurityService
{
    private readonly ISecureStorage _storage;

    public async Task<bool> ValidateUserAsync(string input)
    {
        // Validation implementation
    }
}
```