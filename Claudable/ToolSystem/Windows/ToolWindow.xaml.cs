using Claudable.Services;
using Claudable.ToolSystem.Models;
using Microsoft.Web.WebView2.Core;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace Claudable.Windows;

public partial class ToolWindow : Window
{
    private readonly Tool _tool;
    private bool _isDisposed;

    public ToolWindow(Tool tool)
    {
        InitializeComponent();
        _tool = tool;
        Title = $"Tool: {_tool.Name}";
        Loaded += ToolWindow_Loaded;
        Closed += ToolWindow_Closed;
    }

    private async void ToolWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await WebViewFactory.InitializeWebView(ToolWebView);
        LoadTool();
    }

    private void ToolWindow_Closed(object sender, EventArgs e)
    {
        if (!_isDisposed)
        {
            ToolWebView?.Dispose();
            _isDisposed = true;
        }
    }

    private void LoadTool()
    {
        var (processedCode, declarations) = ProcessComponentCode(_tool.Content);
        var html = GenerateHtml(processedCode, declarations);
        ToolWebView.NavigateToString(html);
    }

    private (string processedCode, string declarations) ProcessComponentCode(string code)
    {
        var importRegex = new Regex(@"import\s+(?:{[^}]*}|\w+)\s+from\s+['""]([^'""]+)['""];", RegexOptions.Multiline);
        var matches = importRegex.Matches(code);
        var declarations = new StringBuilder();

        foreach (Match match in matches)
        {
            var importStatement = match.Value;
            if (importStatement.Contains("'react'") || importStatement.Contains("\"react\""))
            {
                code = code.Replace(importStatement, "");
                continue; // React imports are handled globally
            }

            if (importStatement.Contains("@/components/ui/alert"))
            {
                code = code.Replace(importStatement, "");
                continue; // Alert components are provided globally
            }

            if (importStatement.Contains("lucide-react"))
            {
                code = code.Replace(importStatement, "");
                continue; // Icon components are provided globally
            }

            declarations.AppendLine($"// Processed: {importStatement}");
        }

        // Remove any remaining import statements
        code = importRegex.Replace(code, "");

        return (code, declarations.ToString());
    }

    private string GenerateHtml(string componentCode, string processedImports)
    {
        var html = new StringBuilder();
        html.AppendLine($@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <script crossorigin src='https://cdnjs.cloudflare.com/ajax/libs/react/18.2.0/umd/react.development.js'></script>
    <script crossorigin src='https://cdnjs.cloudflare.com/ajax/libs/react-dom/18.2.0/umd/react-dom.development.js'></script>
    <script crossorigin src='https://cdnjs.cloudflare.com/ajax/libs/babel-standalone/7.23.5/babel.min.js'></script>
    <script crossorigin src='https://cdnjs.cloudflare.com/ajax/libs/lodash.js/4.17.21/lodash.min.js'></script>
    <script crossorigin src='https://cdnjs.cloudflare.com/ajax/libs/PapaParse/5.2.0/papaparse.min.js'></script>
    <script src='https://cdn.tailwindcss.com'></script>
    
    <style>
        :root {{
            --claude-bg: #2d2d2a;
            --claude-primary: #1a1915;
            --claude-text: #ceccc5;
            --claude-highlight: #53524c;
            --claude-border: #3e3e39;
        }}
        body {{ 
            margin: 0; 
            padding: 1rem; 
            background: var(--claude-bg);
            color: var(--claude-text);
            font-family: 'Söhne', ui-sans-serif, system-ui, -apple-system, sans-serif;
        }}
        pre {{ white-space: pre-wrap; }}
        .claude-card {{
            background: var(--claude-primary);
            border: 1px solid var(--claude-border);
            border-radius: 0.5rem;
            padding: 1rem;
            margin-bottom: 1rem;
        }}
    </style>
</head>
<body>
    <div id='root'></div>

    <script>
        // Global scope setup
        const {{
            useState,
            useEffect,
            useCallback,
            useMemo,
            useRef
        }} = React;

        // Alert components
        const Alert = ({{children, className = ''}}) => 
            React.createElement('div', {{
                className: `claude-card bg-blue-900/20 border-blue-800 ${{className}}`
            }}, children);

        const AlertTitle = ({{children}}) => 
            React.createElement('div', {{
                className: 'font-bold mb-1'
            }}, children);

        const AlertDescription = ({{children}}) => 
            React.createElement('div', {{
                className: 'text-sm opacity-90'
            }}, children);

        // Icon components
        const Plus = ({{size = 24, ...props}}) => 
            React.createElement('svg', {{
                xmlns: 'http://www.w3.org/2000/svg',
                width: size,
                height: size,
                viewBox: '0 0 24 24',
                fill: 'none',
                stroke: 'currentColor',
                strokeWidth: 2,
                strokeLinecap: 'round',
                strokeLinejoin: 'round',
                ...props
            }}, React.createElement('path', {{ d: 'M12 5v14m-7-7h14' }}));

        const Minus = ({{size = 24, ...props}}) => 
            React.createElement('svg', {{
                xmlns: 'http://www.w3.org/2000/svg',
                width: size,
                height: size,
                viewBox: '0 0 24 24',
                fill: 'none',
                stroke: 'currentColor',
                strokeWidth: 2,
                strokeLinecap: 'round',
                strokeLinejoin: 'round',
                ...props
            }}, React.createElement('path', {{ d: 'M5 12h14' }}));

        const RotateCcw = ({{size = 24, ...props}}) => 
            React.createElement('svg', {{
                xmlns: 'http://www.w3.org/2000/svg',
                width: size,
                height: size,
                viewBox: '0 0 24 24',
                fill: 'none',
                stroke: 'currentColor',
                strokeWidth: 2,
                strokeLinecap: 'round',
                strokeLinejoin: 'round',
                ...props
            }}, [
                React.createElement('path', {{ key: 1, d: 'M3 2v6h6' }}),
                React.createElement('path', {{ key: 2, d: 'M3 8C3 13 7 16 12 16c5 0 9-3 9-8' }})
            ]);

        // Error Boundary Component
        class ErrorBoundary extends React.Component {{
            constructor(props) {{
                super(props);
                this.state = {{ hasError: false, error: null }};
            }}

            static getDerivedStateFromError(error) {{
                return {{ hasError: true, error }};
            }}

            render() {{
                if (this.state.hasError) {{
                    return React.createElement(Alert, {{
                        className: 'border-red-500'
                    }}, [
                        React.createElement(AlertTitle, {{}}, 'Something went wrong'),
                        React.createElement('pre', {{ 
                            className: 'text-sm opacity-75'
                        }}, this.state.error?.message)
                    ]);
                }}
                return this.props.children;
            }}
        }}

        // File system API
        window.fs = {{
            readFile: async (path, options = {{}}) => {{
                throw new Error('File system not implemented');
            }}
        }};
    </script>

    <script type='text/babel'>
        /* Processed imports:
        {processedImports}
        */
        
        // Component code
        {componentCode}

        // Initialize and render
        const root = ReactDOM.createRoot(document.getElementById('root'));
        root.render(
            React.createElement(ErrorBoundary, null,
                React.createElement(SimpleCounter)
            )
        );
    </script>
</body>
</html>");

        return html.ToString();
    }
}