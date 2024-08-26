using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;

public class WebViewManager
{
    private WebView2 _webView;
    private string _lastVisitedUrl;
    private string _currentProjectUrl;
    private string _lastDocsResponse;

    public string LastVisitedUrl => _lastVisitedUrl;
    public string CurrentProjectUrl => _currentProjectUrl;

    public event EventHandler<string> DocsReceived;
    public event EventHandler<string> ProjectChanged;

    public WebViewManager(WebView2 webView, string initialUrl)
    {
        _webView = webView;
        _lastVisitedUrl = initialUrl;
    }

    public async Task InitializeAsync()
    {
        var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Claudable", "WebView2Data");
        var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

        await _webView.EnsureCoreWebView2Async(webView2Environment);
        _webView.Source = new Uri(_lastVisitedUrl);

        ConfigureWebViewSettings();
        SetupWebViewEventHandlers();
    }

    public void Navigate(string url)
    {
        _webView.Source = new Uri(url);
    }

    private void ConfigureWebViewSettings()
    {
        _webView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
        _webView.CoreWebView2.Settings.IsScriptEnabled = true;
        _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        _webView.CoreWebView2.Settings.AreDevToolsEnabled = true;
        _webView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        _webView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
        _webView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
        _webView.CoreWebView2.Settings.IsSwipeNavigationEnabled = true;
        _webView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
        _webView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
        _webView.CoreWebView2.Settings.IsStatusBarEnabled = true;
    }
    private void SetupWebViewEventHandlers()
    {
        _webView.SourceChanged += WebView_SourceChanged;
        _webView.CoreWebView2.WebResourceResponseReceived += ProcessDocsResponse;
        _webView.CoreWebView2.NavigationCompleted += CheckForProjectChange;
        _webView.CoreWebView2.FrameNavigationCompleted += CheckForProjectChange;
        _webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
    }

    private void CoreWebView2_HistoryChanged(object? sender, object e) => CheckForProjectChange();

    private async void ProcessDocsResponse(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        var url = args.Request.Uri;
        if (!url.Contains("claude")) return;
        var uri = new Uri(url);
        var schemeHostPath = $"https://{uri.Host}{uri.AbsolutePath}";
        switch (uri)
        {
            case var _ when url.EndsWith("docs"):
                {
                    var response = args.Response;
                    var stream = await response.GetContentAsync();
                    using var reader = new StreamReader(stream);
                    _lastDocsResponse = reader.ReadToEnd();
                    DocsReceived?.Invoke(this, _lastDocsResponse);
                }
                break;
        }
    }

    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        _lastVisitedUrl = _webView.Source.ToString();
    }

    private void CheckForProjectChange(object? sender, CoreWebView2NavigationCompletedEventArgs e) => CheckForProjectChange();
    private void CheckForProjectChange()
    {
        string url = _webView.Source.ToString();
        if (!url.Contains("claude")) return;

        var uri = new Uri(url);
        if (uri.Host == "claude.ai" && uri.AbsolutePath.StartsWith("/project/"))
        {
            string newProjectUrl = $"https://{uri.Host}{uri.AbsolutePath}";
            if (newProjectUrl != _currentProjectUrl)
            {
                _currentProjectUrl = newProjectUrl;
                ProjectChanged?.Invoke(this, _currentProjectUrl);
            }
        }
    }
}
