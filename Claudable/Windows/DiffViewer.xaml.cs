using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;
using Claudable.ViewModels;
using Microsoft.Web.WebView2.Wpf;

namespace Claudable.Windows
{
    public partial class DiffViewer : Window
    {
        private readonly ProjectFile _projectFile;
        private readonly string _localContent;
        private readonly string _artifactContent;

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

            UpdateHeader();
            Loaded += DiffViewer_Loaded;
        }

        private void UpdateHeader()
        {
            FileNameText.Text = $"Comparing versions of: {_projectFile.Name}";
            LocalTimestampText.Text = _projectFile.LocalLastModified.ToString("g");
            ArtifactTimestampText.Text = _projectFile.ArtifactLastModified.ToString("g");
        }

        private async void DiffViewer_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            LoadDiffEditor();
        }

        private async Task InitializeWebView()
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Claudable",
                "DiffViewerData");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await DiffWebViewer.EnsureCoreWebView2Async(env);
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
            string htmlContent = $@"
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
        #container {{
            width: 100vw;
            height: 100vh;
        }}
    </style>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/editor/editor.main.min.css'>
</head>
<body>
    <div id='container'></div>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs/loader.min.js'></script>
    <script>
        require.config({{ paths: {{ vs: 'https://cdnjs.cloudflare.com/ajax/libs/monaco-editor/0.45.0/min/vs' }} }});

        require(['vs/editor/editor.main'], function() {{
            // Define custom theme
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
                renderOverviewRuler: false,
                scrollbar: {{
                    useShadows: false,
                    verticalHasArrows: true,
                    horizontalHasArrows: true,
                    vertical: 'visible',
                    horizontal: 'visible'
                }}
            }});

            var originalModel = monaco.editor.createModel({JsonEscapeString(_localContent)}, '{language}');
            var modifiedModel = monaco.editor.createModel({JsonEscapeString(_artifactContent)}, '{language}');
            
            diffEditor.setModel({{
                original: originalModel,
                modified: modifiedModel
            }});
        }});
    </script>
</body>
</html>";

            DiffWebViewer.CoreWebView2.NavigateToString(htmlContent);
        }

        private string JsonEscapeString(string str)
        {
            return System.Text.Json.JsonSerializer.Serialize(str);
        }

        private async void SyncToArtifact_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(
                "Are you sure you want to update this file to match the artifact version? This will overwrite your local changes.",
                "Confirm Sync",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    await File.WriteAllTextAsync(_projectFile.FullPath, _artifactContent);
                    _projectFile.LocalLastModified = DateTime.Now;
                    UpdateHeader();
                    MessageBox.Show("File has been successfully synchronized with the artifact version.", "Sync Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error synchronizing file: {ex.Message}", "Sync Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}