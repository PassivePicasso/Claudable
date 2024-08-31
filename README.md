# Claudable

Claudable is a sophisticated desktop application designed to enhance the Claude AI experience by providing local file management and integration capabilities. This application serves as a bridge between Claude AI's web interface and your local file system, offering a seamless workflow for managing projects, artifacts, and downloads.

#### No association with Anthropic or Claude.ai
I am an independent developer and this project has no association with claude.ai 
nor Anthropic.


#### A Note about Embedded Web Browser Security
Claudable, like other applications with embedded browsers, has full access to web content within its browser component. This includes the ability to execute JavaScript and interact with logged-in sessions. 

While Claudable doesn't request or store Claude AI credentials directly, the embedded browser has the same access as a regular browser when you're logged in. This level of access is standard for embedded browser applications and extensions.

Users should exercise caution and only use embedded browser applications from trusted sources, especially when they access external web content. 

The source code of Claudable is open and concise, allowing for easy review. If concerned about security implications, users can analyze the entire codebase, including uploading it to a Claude AI project for evaluation.

It's important to remember that embedded browser applications always require a high level of trust. Use Claudable, as you would any similar application, with appropriate caution.

## Key Features

1. **Web Integration**
   - Embeds Claude AI's web interface directly into the application
   - Seamless interaction with Claude AI without leaving the app

2. **Project Structure Viewer**
   - Displays and manages local project files and directories
   - Supports drag-and-drop functionality for easy file management
   - Filter settings to customize the view of project files

3. **Artifact Management**
   - Tracks and manages artifacts created by Claude AI
   - Supports SVG artifacts with preview and export options
   - Enables associating local files with Claude AI artifacts

4. **Download Management**
   - Manages file downloads from the Claude AI interface
   - Allows drag-and-drop of downloaded files into the project structure

5. **Customizable Layout**
   - Swap panels for a personalized user experience
   - Adjustable split view between Claude AI interface and project structure

6. **File Filtering and View Modes**
   - Filter settings to exclude specific files or folders from view
   - Multiple view modes: Show All, Show Only Tracked Artifacts, Show Only Outdated Files

7. **SVG Handling**
   - Converts SVG artifacts to PNG or ICO formats
   - Drag-and-drop SVG artifacts into the project structure

8. **Project Association**
   - Associates local folders with Claude AI projects
   - Automatically loads associated project structure when switching projects in Claude AI

9. **File Synchronization**
   - Tracks differences between local files and Claude AI artifacts
   - Visual indicators for files that are newer locally or in Claude AI

10. **Custom Theming**
    - Implements a custom Claude-inspired theme for a cohesive look and feel

11. **Window State Management**
    - Saves and restores window state, including size, position, and panel configuration

12. **File System Monitoring**
    - Real-time updates to the project structure when files change on disk

## Technical Details

- Built with C# and WPF (Windows Presentation Foundation)
- Uses WebView2 for web integration
- Implements MVVM (Model-View-ViewModel) architecture
- Utilizes various WPF controls and custom styles
- Implements custom drag-and-drop functionality
- Uses SkiaSharp for SVG processing

## Getting Started

1. Clone the repository
2. Open the solution in Visual Studio
3. Restore NuGet packages
4. Build and run the application

## Usage

1. **Launch Claudable**
   - Upon startup, the application opens with Claude AI's web interface on one side and the project structure viewer on the other.

2. **Set Up a New Project**
   - Click the "Set Project Root" button in the top right corner.
   - Select the local folder that corresponds to your project.
   - The project structure will appear in the tree view on the right side.

3. **Interact with Claude AI**
   - Use the Claude AI interface on the left side as you normally would in a web browser.
   - As you work with Claude, the application will track artifacts and associate them with your local files.

4. **Manage Local Files and Artifacts**
   - The project structure viewer on the right shows your local files and their relationship to Claude AI artifacts.
   - Files tracked as artifacts are marked with an "A" icon.
   - Files that are newer locally than in Claude AI are marked with an up arrow (↑).
   - Files that are newer in Claude AI than locally are marked with a down arrow (↓).

5. **Use Filter Modes**
   - Click the filter mode button (next to "Set Project Root") to cycle through view modes:
     - "Show All": Displays all files and folders.
     - "Show Only Tracked Artifacts": Only shows files associated with Claude AI artifacts.
     - "Show Only Outdated Files": Displays files where the local version differs from the Claude AI version.

6. **Set Up Custom Filters**
   - Go to the "Filter Settings" tab at the bottom right.
   - Enter file or folder names you want to exclude from the project view.
   - Click "Add Filter" to apply the filter.
   - Filters are applied across all filter modes.

7. **Manage Downloads**
   - When you download files from Claude AI, they appear in the "Downloads" tab at the bottom right.
   - Drag and drop downloaded files from this tab into your project structure to move them to your desired location.

8. **Work with SVG Artifacts**
   - SVG files created by Claude AI appear in the "SVG Artifacts" tab at the bottom right.
   - Drag an SVG artifact into your project structure to save it:
     - By default, it saves as a PNG file.
     - Hold Ctrl while dragging to save as an ICO file.

9. **Drag and Drop to Claude AI**
   - Drag files from your project structure and drop them into the Claude AI interface to add them to your conversation or project knowledge.

10. **Automatic Project Association**
    - When you switch to a different project in Claude AI, Claudable automatically loads the associated local folder if you've set one up before.
    - If no association exists, you'll be prompted to set a local folder for the new project.

11. **Customize Layout**
    - Click the swap button (↔) in the top right corner to switch the positions of the Claude AI interface and the project structure viewer.

12. **Real-time File System Monitoring**
    - Any changes made to your project files outside of Claudable (e.g., in File Explorer or another application) are automatically reflected in the project structure viewer.

13. **Automatic Renaming**
    - If Claude AI renames an artifact, Claudable will automatically update the association with your local file, maintaining the correct relationship between your local files and Claude AI artifacts.

Remember that Claudable saves its state, including your project associations and filter settings, between sessions. This means you can pick up right where you left off when you relaunch the application.

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
