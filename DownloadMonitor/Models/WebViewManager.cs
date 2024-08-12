using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;

public class WebViewManager
{
    private WebView2 _webView;
    private string _lastVisitedUrl;

    public string LastVisitedUrl => _lastVisitedUrl;

    public WebViewManager(WebView2 webView, string initialUrl)
    {
        _webView = webView;
        _lastVisitedUrl = initialUrl;
    }

    public async Task InitializeAsync()
    {
        var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileMonitor", "WebView2Data");
        var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

        await _webView.EnsureCoreWebView2Async(webView2Environment);
        _webView.Source = new Uri(_lastVisitedUrl);

        ConfigureWebViewSettings();
        SetupWebViewEventHandlers();
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
    }
    private void WebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
    {
        _lastVisitedUrl = _webView.Source.ToString();
    }
    public void Navigate(string url)
    {
        _webView.Source = new Uri(url);
    }
}