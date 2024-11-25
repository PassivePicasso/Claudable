using Claudable.Services;
using Claudable.ViewModels;
using System.IO;
using System.Windows;

namespace Claudable.Windows;
public partial class DiffViewer : Window
{
    private readonly ProjectFile _projectFile;
    private readonly string _localContent;
    private readonly string _artifactContent;
    private readonly bool _isLocalNewer;

    public static async Task ShowDiffDialog(ProjectFile projectFile)
    {
        try
        {
            string localContent = await File.ReadAllTextAsync(projectFile.FullPath);
            string artifactContent = projectFile.AssociatedArtifact?.Content ?? "";

            var diffViewer = new DiffViewer(projectFile, localContent, artifactContent);
            diffViewer.Owner = Application.Current.MainWindow;
            diffViewer.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file contents: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public DiffViewer(ProjectFile projectFile, string localContent, string artifactContent)
    {
        InitializeComponent();
        _projectFile = projectFile;
        _localContent = localContent;
        _artifactContent = artifactContent;
        _isLocalNewer = _projectFile.LocalLastModified > _projectFile.ArtifactLastModified;

        UpdateHeader();
        Loaded += DiffViewer_Loaded;
    }

    private void UpdateHeader()
    {
        FileNameText.Text = $"Comparing versions of: {_projectFile.Name}";
    }

    private async void DiffViewer_Loaded(object sender, RoutedEventArgs e)
    {
        await WebViewFactory.InitializeWebView(DiffWebViewer);
        LoadDiffEditor();
    }

    private string DetectLanguage()
    {
        string extension = Path.GetExtension(_projectFile.Name).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".html" => "html",
            ".css" => "css",
            ".xml" => "xml",
            ".json" => "json",
            ".md" => "markdown",
            ".py" => "python",
            ".rb" => "ruby",
            ".java" => "java",
            ".cpp" => "cpp",
            ".h" => "cpp",
            ".sql" => "sql",
            ".sh" => "bash",
            ".yaml" => "yaml",
            ".yml" => "yaml",
            ".php" => "php",
            ".rs" => "rust",
            ".go" => "go",
            ".swift" => "swift",
            _ => "plaintext"
        };
    }

    private void LoadDiffEditor()
    {
        string language = DetectLanguage();
        string htmlContent = GetHtmlContent(language);
        DiffWebViewer.NavigateToString(htmlContent);
    }

    private string GetHtmlContent(string language)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{
            margin: 0;
            padding: 0;
            background-color: #2d2d2a;
            overflow: hidden;
        }}
        #root {{
            width: 100vw;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }}
        #title {{
            display: flex;
            flex-direction: row;
        }}
        #container {{
            flex-grow: 1;
        }}
        .diff-title {{
            flex-grow: 1;
            color: #ceccc5;
            background-color: #1a1915;
            padding: 4px 12px;
            border-radius: 0 0 4px 4px;
            font-family: 'Söhne', 'Segoe UI', sans-serif;
            font-size: 12px;
            z-index: 1000;
        }}
    </style>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/editor/editor.main.min.css'>
</head>
<body>
    <div id='root'>
        <div id='title'>
            <div class='diff-title' id='original-title'>{(_isLocalNewer ? "Artifact Version" : "Local Version")} ({(_isLocalNewer ? _projectFile.ArtifactLastModified : _projectFile.LocalLastModified):g})</div>
            <div class='diff-title' id='modified-title'>{(_isLocalNewer ? "Local Version" : "Artifact Version")} ({(_isLocalNewer ? _projectFile.LocalLastModified : _projectFile.ArtifactLastModified):g})</div>
        </div>
        <div id='container'></div>
    </div>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/loader.min.js'></script>
    <script>
        require.config({{ paths: {{ vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' }} }});

        require(['vs/editor/editor.main'], function() {{
            monaco.editor.defineTheme('claudeTheme', {{
                base: 'vs-dark',
                inherit: true,
                rules: [],
                colors: {{
                    'editor.background': '#2d2d2a',
                    'editor.foreground': '#ceccc5',
                    'editor.lineHighlightBackground': '#53524c',
                    'editorLineNumber.foreground': '#666660',
                    'editorGutter.background': '#1a1915',
                    'diffEditor.insertedTextBackground': '#23372330',
                    'diffEditor.removedTextBackground': '#73524c30',
                }}
            }});

            var diffEditor = monaco.editor.createDiffEditor(document.getElementById('container'), {{
                theme: 'claudeTheme',
                language: '{language}',
                automaticLayout: true,
                readOnly: true,
                renderSideBySide: true,
                fontSize: 14,
                lineHeight: 21,
                minimap: {{ enabled: false }},
                scrollBeyondLastLine: false,
                folding: true,
                renderOverviewRuler: true,
                overviewRulerLanes: 2,
                overviewRulerBorder: true,
                scrollbar: {{
                    useShadows: false,
                    verticalHasArrows: true,
                    horizontalHasArrows: true,
                    vertical: 'visible',
                    horizontal: 'visible',
                    verticalScrollbarSize: 14,
                    verticalSliderSize: 14,
                    scrollByPage: true
                }}
            }});

            var originalContent = {(_isLocalNewer ? JsonEscapeString(_artifactContent) : JsonEscapeString(_localContent))};
            var modifiedContent = {(_isLocalNewer ? JsonEscapeString(_localContent) : JsonEscapeString(_artifactContent))};

            var originalModel = monaco.editor.createModel(originalContent, '{language}');
            var modifiedModel = monaco.editor.createModel(modifiedContent, '{language}');
            
            diffEditor.setModel({{
                original: originalModel,
                modified: modifiedModel
            }});
        }});
    </script>
</body>
</html>";
    }

    private string JsonEscapeString(string str)
    {
        return System.Text.Json.JsonSerializer.Serialize(str);
    }
}