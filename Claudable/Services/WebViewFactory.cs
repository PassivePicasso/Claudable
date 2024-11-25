using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.IO;

namespace Claudable.Services;

public class WebViewFactory
{
    private static CoreWebView2Environment _sharedEnvironment;
    private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);
    private static readonly string _userDataFolder;

    static WebViewFactory()
    {
        _userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Claudable",
            "WebView2Data");
    }

    public static async Task<WebView2> CreateWebView()
    {
        var webView = new WebView2();
        await InitializeWebView(webView);
        return webView;
    }

    public static async Task InitializeWebView(WebView2 webView)
    {
        var environment = await GetOrCreateEnvironment();
        await webView.EnsureCoreWebView2Async(environment);
        ConfigureWebView(webView);
    }

    private static async Task<CoreWebView2Environment> GetOrCreateEnvironment()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_sharedEnvironment == null)
            {
                _sharedEnvironment = await CoreWebView2Environment.CreateAsync(null, _userDataFolder);
            }
            return _sharedEnvironment;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private static void ConfigureWebView(WebView2 webView)
    {
        var settings = webView.CoreWebView2.Settings;
        settings.AreDefaultScriptDialogsEnabled = true;
        settings.IsScriptEnabled = true;
        settings.AreDefaultContextMenusEnabled = true;
        settings.AreDevToolsEnabled = true;
        settings.IsWebMessageEnabled = true;
        settings.AreBrowserAcceleratorKeysEnabled = true;
        settings.IsPinchZoomEnabled = true;
        settings.IsSwipeNavigationEnabled = true;
        settings.IsGeneralAutofillEnabled = true;
        settings.IsPasswordAutosaveEnabled = true;
        settings.IsStatusBarEnabled = true;
    }
}