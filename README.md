# Claudable

Claudable is a desktop application that enhances the Claude AI experience by providing local file management and integration capabilities.

**Note:** This is a side project. If someone wants to pick this up and take the wheel, feel free; otherwise, I'll only be updating it as I run into issues I want to streamline.

Interestingly, the code for this project was primarily written by Claude itself, and the Claudable project was developed using Claudable as soon as the interface allowed for it - a case of the tool being used to improve itself!

## Features

- **Web Integration**: Embeds Claude AI's web interface directly into the application.
- **Project Structure Viewer**: Displays and manages local project files and directories.
- **Artifact Management**: Tracks and manages artifacts created by Claude AI.
- **SVG Handling**: Converts SVG artifacts to PNG or ICO formats.
- **Download Management**: Manages file downloads from the Claude AI interface.
- **Customizable Layout**: Allows swapping panels for a personalized user experience.
- **File Filtering**: Provides options to filter and view specific file types or statuses.

## Key Functionalities

### Drag and Drop

- **Project Structure to Claude AI**: Drag files from the Project Structure view and drop them into the Claude AI interface to add them to Project Knowledge or Chat files.
- **SVG Artifacts to Project Structure**: Drag SVG artifacts from the SVG Artifacts tab and drop them into folders in the Project Structure view.
  - Default: Saves as PNG
  - Hold Ctrl while dropping: Saves as ICO

### Filter Settings

The Filter Settings tab allows you to define filters that control which files and folders are displayed in the Project Structure view. You can add multiple filters, and the system will hide any files or folders that match these filters.

### Project Structure View Modes

The Project Structure view has three modes:

1. **Show All**: Displays all files and folders.
2. **Show Only Tracked Artifacts**: Only shows files that are tracked as artifacts by Claude AI.
3. **Show Only Outdated Files**: Displays only tracked artifacts where the local file is newer than the version Claude AI knows about.

## Technologies Used

- C# / .NET
- WPF (Windows Presentation Foundation)
- WebView2 for web integration
- JSON for data serialization
- SVG.Skia for SVG processing

## Getting Started

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build and run the application

## Project Structure

- `MainWindow.xaml` / `MainWindow.xaml.cs`: Main application window and logic
- `MainViewModel.cs`: Primary view model handling application state and logic
- `WebViewManager.cs`: Manages WebView2 control and interactions with Claude AI
- `ArtifactManager.cs`: Handles artifact-related operations
- `ProjectFolder.cs` / `ProjectFile.cs`: Represent the local file system structure
- `FileWatcher.cs`: Monitors local file system changes
- `SVGRasterizer.cs`: Converts SVG to PNG/ICO formats

## Configuration

The application saves its state in `appstate.json` in the application directory. Project associations are stored in the user's local application data folder.

## Contributing

While this is a side project, contributions are welcome if you find it useful and want to improve it. Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License.

## Acknowledgements

- Claude AI by Anthropic
- Microsoft for WebView2
- SkiaSharp and SVG.Skia libraries

