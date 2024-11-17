using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;

namespace Claudable.Windows
{
    public partial class ArtifactViewer : Window
    {
        private ArtifactViewerOptions _options;

        public ArtifactViewer(ArtifactViewerOptions options)
        {
            InitializeComponent();
            _options = options;
            ArtifactTitle.Text = _options.Title;
            Loaded += ArtifactViewer_Loaded;
        }

        private async void ArtifactViewer_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeWebView();
            DisplayContent();
        }

        private async Task InitializeWebView()
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Claudable",
                "ArtifactViewerData");

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await ContentViewer.EnsureCoreWebView2Async(env);
        }
        private string DetectLanguage()
        {
            string extension = Path.GetExtension(_options.Title).ToLowerInvariant();
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
                _ => ""
            };
        }

        private void DisplayContent()
        {
            string language = DetectLanguage();
            string languageClass = !string.IsNullOrEmpty(language) ? $"class=\"language-{language}\"" : "";

            string htmlContent = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css"">
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js""></script>
                    <!-- Add commonly used language packs -->
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/csharp.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/xml.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/javascript.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/css.min.js""></script>
                    <style>
                        body {{
                            font-family: 'Söhne', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                            margin: 20px;
                            background-color: #2d2d2a;
                            color: #ceccc5;
                        }}
                        pre code {{
                            font-family: 'Consolas', 'Courier New', monospace;
                            padding: 15px !important;
                            border-radius: 5px;
                            font-size: 14px;
                            line-height: 1.5;
                        }}
                        .file-info {{
                            margin-bottom: 10px;
                            padding: 8px;
                            background-color: #1a1915;
                            border-radius: 4px;
                            font-size: 12px;
                            color: #888;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""file-info"">
                        {_options.Title}
                    </div>
                    <pre><code {languageClass}>{System.Web.HttpUtility.HtmlEncode(_options.Content)}</code></pre>
                    <script>
                        document.addEventListener('DOMContentLoaded', (event) => {{
                            document.querySelectorAll('pre code').forEach((el) => {{
                                hljs.highlightElement(el);
                            }});
                        }});
                    </script>
                </body>
                </html>";

            ContentViewer.NavigateToString(htmlContent);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }


    public class ArtifactViewerOptions
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}