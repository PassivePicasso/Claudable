using Claudable.Models;
using Claudable.Services;
using Microsoft.Web.WebView2.Core;
using System.IO;
using System.Windows;

namespace Claudable.Windows;

public partial class ArtifactViewer : Window
{
    private readonly ArtifactViewerOptions _options;
    private bool _isRendered = true;
    private bool _isLineNumbersEnabled = true;
    private bool _isWrapEnabled = false;
    private readonly string _detectedLanguage;

    public ArtifactViewer(ArtifactViewerOptions options)
    {
        InitializeComponent();
        _options = options;
        _detectedLanguage = DetectLanguage();
        Loaded += ArtifactViewer_Loaded;
    }

    private async void ArtifactViewer_Loaded(object sender, RoutedEventArgs e)
    {
        await WebViewFactory.InitializeWebView(ContentViewer);
        ContentViewer.CoreWebView2.WebMessageReceived += HandleWebMessage;
        DisplayContent();
    }

    private void HandleWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.WebMessageAsJson;
        try
        {
            var command = System.Text.Json.JsonSerializer.Deserialize<ViewerCommand>(message);
            switch (command.action)
            {
                case "toggleRender":
                    _isRendered = !_isRendered;
                    DisplayContent();
                    break;
                case "toggleLineNumbers":
                    _isLineNumbersEnabled = !_isLineNumbersEnabled;
                    DisplayContent();
                    break;
                case "toggleWrap":
                    _isWrapEnabled = !_isWrapEnabled;
                    DisplayContent();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error handling web message: {ex.Message}");
        }
    }

    private string DetectLanguage()
    {
        string extension = Path.GetExtension(_options.FileName).ToLowerInvariant();
        return extension switch
        {
            ".cs" => "csharp",
            ".js" => "javascript",
            ".ts" => "typescript",
            ".html" => "html",
            ".css" => "css",
            ".xml" or ".xaml" or ".axaml" or ".axml" or ".appxml" or
            ".config" or ".csproj" or ".vbproj" or ".fsproj" or
            ".build" or ".targets" or ".props" or ".rdl" or
            ".settings" or ".manifest" or ".resx" or ".ruleset" or
            ".vstemplate" or ".vsixmanifest" or ".nuspec" or
            ".msbuild" or ".xsd" or ".wsdl" or ".soap" or ".svg" => "xml",
            ".json" => "json",
            ".md" or ".markdown" => "markdown",
            ".py" => "python",
            ".rb" => "ruby",
            ".java" => "java",
            ".cpp" or ".hpp" => "cpp",
            ".h" => "cpp",
            ".sql" => "sql",
            ".sh" => "bash",
            ".yaml" or ".yml" => "yaml",
            ".php" => "php",
            ".rs" => "rust",
            ".go" => "go",
            ".swift" => "swift",
            ".mmd" or ".mermaid" => "mermaid",
            _ => "plaintext"
        };
    }

    private bool IsMarkdown => _detectedLanguage == "markdown";
    private bool IsMermaid => _detectedLanguage == "mermaid";

    private void DisplayContent()
    {
        string htmlContent = GetHtmlContent();
        ContentViewer.NavigateToString(htmlContent);
    }

    private string GetHtmlContent()
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.0/github-markdown-dark.min.css'>
    <link rel='stylesheet' href='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/styles/github-dark.min.css'>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/markdown-it/13.0.2/markdown-it.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/highlight.min.js'></script>
    <script src='https://cdnjs.cloudflare.com/ajax/libs/mermaid/10.6.1/mermaid.min.js'></script>
    {GetLanguageScripts()}
    <style>
        {GetStyles()}
    </style>
</head>
<body>
    <div id='toolbar'>
        {GetToolbarButtons()}
    </div>
    <div id='content' class='{GetContentClasses()}'>
        {GetInitialContent()}
    </div>
    <script>
        {GetJavaScript()}
    </script>
</body>
</html>";
    }

    private string GetLanguageScripts()
    {
        var languages = new[]
        {
            "xml", "csharp", "javascript", "typescript", "css", "json", "markdown",
            "python", "ruby", "java", "cpp", "sql", "yaml", "bash", "php",
            "rust", "go", "swift"
        };

        return string.Join("\n", languages.Select(lang =>
            $"<script src='https://cdnjs.cloudflare.com/ajax/libs/highlight.js/11.9.0/languages/{lang}.min.js'></script>"));
    }

    private string GetStyles()
    {
        return @"
                :root {
                    --claude-bg: #2d2d2a;
                    --claude-primary: #1a1915;
                    --claude-text: #ceccc5;
                    --claude-highlight: #53524c;
                    --claude-border: #3e3e39;
                }
                html, body {
                    margin: 0;
                    padding: 0;
                    height: 100vh;
                    background-color: var(--claude-bg);
                    color: var(--claude-text);
                    font-family: 'Söhne', 'Segoe UI', sans-serif;
                    display: flex;
                    flex-direction: column;
                }
                #toolbar {
                    background-color: var(--claude-primary);
                    padding: 8px;
                    border-bottom: 1px solid var(--claude-border);
                    display: flex;
                    gap: 8px;
                }
                .toolbar-button {
                    background-color: var(--claude-bg);
                    border: 1px solid var(--claude-border);
                    color: var(--claude-text);
                    padding: 4px 8px;
                    border-radius: 4px;
                    cursor: pointer;
                    font-size: 12px;
                }
                .toolbar-button:hover {
                    background-color: var(--claude-highlight);
                }
                .toolbar-button.active {
                    background-color: var(--claude-highlight);
                    border-color: var(--claude-text);
                }
                #content {
                    flex: 1;
                    overflow: auto;
                    padding: 26px;
                }
                .markdown-body {
                    background-color: var(--claude-bg) !important;
                    padding: 16px;
                }
                pre {
                    background-color: var(--claude-primary) !important;
                    margin: 0;
                    margin: -26px;
                }
                .nowrap pre code {
                    white-space: pre;
                }
                .wrap pre code {
                    white-space: pre-wrap;
                    word-wrap: break-word;
                }
                .line-numbers pre code {
                    counter-reset: line;
                }
                .line-numbers pre code > span {
                    display: inline;
                }
                .line-numbers pre code > span:before {
                    counter-increment: line;
                    content: counter(line);
                    display: inline-block;
                    padding: 0 0.5em;
                    margin-right: 0.5em;
                    color: #666660;
                    border-right: 1px solid var(--claude-border);
                    min-width: 1.5em;
                    text-align: right;
                    -webkit-user-select: none;
                    -moz-user-select: none;
                    -ms-user-select: none;
                    user-select: none;
                }
                .mermaid {
                    background: var(--claude-bg);
                }";
    }

    private string GetToolbarButtons()
    {
        var buttons = new List<string>();

        if (IsMarkdown)
        {
            buttons.Add($@"<button class='toolbar-button{(_isRendered ? " active" : "")}' onclick='toggleRender()'>
                              {(_isRendered ? "Show Source" : "Show Rendered")}</button>");
        }

        if ((IsMarkdown || IsMermaid) && _isRendered)
            return string.Join("\n", buttons);

        buttons.Add($@"<button class='toolbar-button{(_isLineNumbersEnabled ? " active" : "")}' onclick='toggleLineNumbers()'>
                          {(_isLineNumbersEnabled ? "Hide Line Numbers" : "Show Line Numbers")}</button>");

        buttons.Add($@"<button class='toolbar-button{(_isWrapEnabled ? " active" : "")}' onclick='toggleWrap()'>
                          {(_isWrapEnabled ? "Disable Wrap" : "Enable Wrap")}</button>");

        return string.Join("\n", buttons);
    }

    private string GetContentClasses()
    {
        var classes = new List<string>();

        if (_isLineNumbersEnabled) classes.Add("line-numbers");
        if (_isWrapEnabled) classes.Add("wrap");
        else classes.Add("nowrap");

        return string.Join(" ", classes);
    }

    private string GetInitialContent()
    {
        if (IsMermaid && _isRendered)
        {
            return $@"<pre class='mermaid'>{System.Web.HttpUtility.HtmlEncode(_options.Content)}</pre>";
        }
        else if (IsMarkdown && _isRendered)
        {
            return "<div id='markdown-content'></div>";
        }
        else
        {
            string languageClass = !string.IsNullOrEmpty(_detectedLanguage) ? $"class=\"language-{_detectedLanguage}\"" : "";
            return $"<pre><code {languageClass}>{System.Web.HttpUtility.HtmlEncode(_options.Content)}</code></pre>";
        }
    }

    private string GetJavaScript()
    {
        return $@"
                const sendMessage = (action) => {{
                    window.chrome.webview.postMessage({{ action }});
                }};

                const toggleRender = () => sendMessage('toggleRender');
                const toggleLineNumbers = () => sendMessage('toggleLineNumbers');
                const toggleWrap = () => sendMessage('toggleWrap');

                const initMermaid = () => {{
                    mermaid.initialize({{ 
                        theme: 'dark',
                        darkMode: true,
                        background: '#2d2d2a'
                    }});
                }};

                const initMarkdown = () => {{
                    const md = window.markdownit({{
                        html: true,
                        linkify: true,
                        highlight: (str, lang) => {{
                            if (lang && hljs.getLanguage(lang)) {{
                                try {{
                                    return hljs.highlight(str, {{ language: lang }}).value;
                                }} catch (__) {{}}
                            }}
                            return '';
                        }}
                    }});

                    const content = {System.Text.Json.JsonSerializer.Serialize(_options.Content)};
                    document.getElementById('markdown-content').innerHTML = md.render(content);
                }};

                const initHighlight = () => {{
                    document.querySelectorAll('pre code').forEach((block) => {{
                        if (!block.className) {{
                            const result = hljs.highlightAuto(block.textContent);
                            block.innerHTML = result.value;
                            block.className = `hljs language-${{result.language}}`;
                        }} else {{
                            hljs.highlightElement(block);
                        }}

                        const lines = block.innerHTML.split('\n');
                        block.innerHTML = lines.map(line => `<span>${{line}}</span>`).join('\n');
                    }});
                }};

                window.addEventListener('DOMContentLoaded', () => {{
                    const isMermaid = {(IsMermaid ? "true" : "false")};
                    const isMarkdown = {(IsMarkdown ? "true" : "false")};
                    const isRendered = {(_isRendered ? "true" : "false")};
                    
                    if (isMermaid && isRendered) {{
                        initMermaid();
                    }} else if (isMarkdown && isRendered) {{
                        initMarkdown();
                    }} else {{
                        initHighlight();
                    }}
                }});";
    }
}
