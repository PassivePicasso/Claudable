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

            string htmlContent = GetHtmlContent(languageClass);

            ContentViewer.NavigateToString(htmlContent);
        }

        private string GetHtmlContent(string languageClass)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset='utf-8'>
                    <link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css"">
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/csharp.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/xml.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/javascript.min.js""></script>
                    <script src=""https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/css.min.js""></script>
                    <style>
                        html, body {{
                            margin: 0;
                            padding: 0;
                            height: 100vh;
                            overflow: auto;
                            background-color: #2d2d2a;
                            color: #ceccc5;
                            font-family: 'Söhne', 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                        }}
                        pre {{
                            margin: 0;
                            white-space:pre-wrap;
                            height: 100vh;
                            box-sizing: border-box;
                        }}
                        pre code {{
                            margin: 0;
                            font-family: 'Consolas', 'Courier New', monospace;
                            font-size: 14px;
                            line-height: 1.5;
                        }}
                        code {{
                            padding: 8px !important;
                        }}
                        #codezone {{
                            overflow: visible;
                        }}
                    </style>
                </head>
                <body>
                    <div>
                        <pre><code {languageClass}>{System.Web.HttpUtility.HtmlEncode(_options.Content)}</code></pre>
                    </div>
                    <script>
                        document.addEventListener('DOMContentLoaded', (event) => {{
                            document.querySelectorAll('pre code').forEach((el) => {{
                                hljs.highlightElement(el);
                            }});
                        }});
                    </script>
                </body>
                </html>";
        }
    }


    public class ArtifactViewerOptions
    {
        public string Title { get; set; }
        public string Content { get; set; }
    }
}