using Claudable.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

public class WebViewManager
{
    public static WebViewManager Instance { get; private set; }
    private string _baseUrl = "https://api.claude.ai";

    Regex DataCollector = new Regex(@"^https://api\.claude\.ai/api/organizations/(?<organizationId>.*?)/projects/(?<projectId>.*?)/docs$");
    private WebView2 _webView;
    private string _lastVisitedUrl;
    private string _currentProjectUrl;
    private string _lastDocsResponse;
    private string _activeOrganizationUuid;

    public string LastVisitedUrl => _lastVisitedUrl;
    public string CurrentProjectUrl => _currentProjectUrl;

    public event EventHandler<string> DocsReceived;
    public event EventHandler<string> ProjectChanged;

    public WebViewManager(WebView2 webView, string initialUrl)
    {
        _webView = webView;
        _lastVisitedUrl = initialUrl;
        if (Instance != null)
            throw new InvalidOperationException("Cannot instantiate more than 1 WebViewManager");
        Instance = this;
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
    public async Task DeleteArtifact(ArtifactViewModel artifact)
    {
        if (string.IsNullOrEmpty(_activeOrganizationUuid))
        {
            throw new InvalidOperationException("Active organization UUID is not set.");
        }

        string url = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{artifact.ProjectUuid}/docs/{artifact.Uuid}";

        // Create a JavaScript function to perform the DELETE request
        string script = @"
                async function deleteArtifact(url) {
                    try {
                        const response = await fetch(url, {
                            method: 'DELETE',
                            headers: {
                                'authority': 'api.claude.ai',
                                'origin': 'https://claude.ai',
                                'accept': '*/*',
                                'accept-language': 'en-US,en;q=0.9'
                            },
                            credentials: 'include'  // Important: Includes cookies for authentication
                        });
                        if (!response.ok) {
                            throw new Error(`HTTP error! status: ${response.status}`);
                        }
                        // For 204 No Content responses, return success object
                        if (response.status === 204) {
                            return { success: true };
                        }
                
                        return await response.json();
                    } catch (error) {
                        console.error('Error:', error);
                        throw error;
                    }
                }
                deleteArtifact('" + url + @"');
            ";
        try
        {
            // Execute the script in the WebView2
            string resultJson = await _webView.CoreWebView2.ExecuteScriptAsync(script);

            // Parse the result
            var result = JsonConvert.DeserializeObject<DeleteArtifactResult>(resultJson);

            // Check if the deletion was successful
            if (result?.Success == true)
            {
                //OnArtifactDeleted(artifact);
                _webView.Reload();
            }
            else
            {
                throw new Exception($"Failed to delete artifact: {result}");
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error deleting artifact: {e.Message}");
            throw;
        }
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
                    try
                    {
                        var path = args.Request.Uri;
                        var match = DataCollector.Match(path);
                        if (match.Success)
                            _activeOrganizationUuid = match.Groups["organizationId"].Value;

                        var response = args.Response;
                        var stream = await response.GetContentAsync();
                        using var reader = new StreamReader(stream);
                        _lastDocsResponse = reader.ReadToEnd();
                        DocsReceived?.Invoke(this, _lastDocsResponse);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
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
    public class DeleteArtifactResult
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
    }
}
