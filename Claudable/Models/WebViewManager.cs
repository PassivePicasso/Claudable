using Claudable.Services;
using Claudable.Utilities;
using Claudable.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;

namespace Claudable;

public class WebViewManager : IDisposable
{
    public static WebViewManager Instance { get; private set; }
    private string _baseUrl = "https://api.claude.ai";

    private readonly Regex ProjectUrl = new Regex(@"^https:\/\/claude\.ai\/project\/(?<projectId>.*?)$");
    private readonly Regex DocsUrl = new Regex(@".*?\/api\/organizations\/(?<organizationId>.*?)\/projects\/(?<projectId>.*?)\/docs(?:\/(?<artifactId>.*?))?$");
    private readonly Regex DataCollector = new Regex(@".*?/api/organizations/(?<organizationId>.*?)/projects/(?<projectId>.+?)/.*");
    private readonly WebView2 _webView;
    private string _lastVisitedUrl;
    private string _currentProjectUrl;
    private string _lastDocsResponse;
    private readonly Debouncer _reloadDebouncer;
    private readonly Debouncer _fetchDocsDebouncer;

    private string _activeProjectUuid;
    private string _activeOrganizationUuid;

    public string LastVisitedUrl => _lastVisitedUrl;
    public string CurrentProjectUrl => _currentProjectUrl;

    public event EventHandler<string> DocsReceived;
    public event EventHandler<string> ProjectChanged;
    public event EventHandler<string> ArtifactDeleted;

    public WebViewManager(WebView2 webView, string initialUrl)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _lastVisitedUrl = initialUrl;
        if (Instance != null)
            throw new InvalidOperationException("Cannot instantiate more than 1 WebViewManager");
        Instance = this;
        _reloadDebouncer = new Debouncer(() =>
        {
            if (!ProjectUrl.IsMatch(LastVisitedUrl)) return;
            _webView.Reload();
        }, 1500);
        _fetchDocsDebouncer = new Debouncer(() => _ = FetchProjectDocs(), 250);
    }

    public async Task InitializeAsync()
    {
        await WebViewFactory.InitializeWebView(_webView);
        _webView.Source = new Uri(_lastVisitedUrl);
        SetupWebViewEventHandlers();
    }

    private void SetupWebViewEventHandlers()
    {
        _webView.SourceChanged += WebView_SourceChanged;
        _webView.CoreWebView2.WebResourceResponseReceived += ProcessWebViewResponse;
        _webView.CoreWebView2.NavigationCompleted += CheckForProjectChange;
        _webView.CoreWebView2.FrameNavigationCompleted += CheckForProjectChange;
        _webView.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
    }

    private void CoreWebView2_HistoryChanged(object? sender, object e) => CheckForProjectChange();

    private async void ProcessWebViewResponse(object? sender, CoreWebView2WebResourceResponseReceivedEventArgs args)
    {
        var url = args.Request.Uri;
        if (!url.Contains("claude")) return;

        var match = DataCollector.Match(url);
        if (match.Success)
        {
            _activeOrganizationUuid = match.Groups["organizationId"].Value;
            _activeProjectUuid = match.Groups["projectId"].Value;
        }

        var uri = new Uri(url);
        var schemeHostPath = $"https://{uri.Host}{uri.AbsolutePath}";
        switch (uri)
        {
            case var _ when DocsUrl.IsMatch(url) && args.Request.Method == "GET":
                {
                    try
                    {
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
            case var _ when DocsUrl.IsMatch(url) && args.Request.Method == "DELETE":
                {
                    var docMatch = DocsUrl.Match(url);
                    var artifactId = docMatch.Groups["artifactId"].Value;
                    ArtifactDeleted?.Invoke(this, artifactId);
                }
                break;
        }
    }

    public async Task<ArtifactViewModel> CreateArtifact(string fileName, string content)
    {
        string url = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{_activeProjectUuid}/docs";

        var payload = new
        {
            file_name = fileName,
            content = content
        };

        string script = RESTCall("TrackArtifact", "POST", url, payload);

        try
        {
            string resultJson = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            var artifact = JsonConvert.DeserializeObject<ArtifactViewModel>(resultJson);

            _reloadDebouncer.Debounce();
            _fetchDocsDebouncer.Debounce();
            if (artifact != null)
            {
                artifact.ProjectUuid = _activeProjectUuid;
                return artifact;
            }

            throw new Exception("Failed to create artifact: Invalid response");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error creating artifact: {e.Message}");
            throw;
        }
    }

    public async Task DeleteArtifact(ArtifactViewModel artifact)
    {
        string deleteUrl = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{artifact.ProjectUuid}/docs/{artifact.Uuid}";

        string script = RESTCall("UntrackArtifact", "DELETE", deleteUrl);

        try
        {
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
            Console.Write(result);
            _reloadDebouncer.Debounce();
            _fetchDocsDebouncer.Debounce();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error in delete operation: {e.Message}");
            throw;
        }
    }

    private async Task FetchProjectDocs()
    {
        string docsUrl = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{_activeProjectUuid}/docs";

        string script = RESTCall("fetchDocs", "GET", docsUrl);

        try
        {
            await _webView.CoreWebView2.ExecuteScriptAsync(script);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error fetching docs: {e.Message}");
            throw;
        }
    }

    private string RESTCall(string functionName, string method, string url, object payload = null)
    {
        var includePayload = payload != null;
        string script = $@"
                async function {functionName}(url{(includePayload ? ", payload" : "")}) {{
                    try {{
                        const response = await fetch(url, {{
                            method: '{method}',
                            headers: {{
                                'authority': 'api.claude.ai',
                                'origin': 'https://claude.ai',
                                'accept': '*/*',
                                'accept-language': 'en-US,en;q=0.9',
                                'anthropic-client-sha': '',
                                'anthropic-client-version': '',
                                'content-type': 'application/json'
                            }},
                            credentials: 'include'{(includePayload
                        ? @", 
                            body: JSON.stringify(payload)"
                        : "")}
                        }});

                        if (!response.ok) {{
                            throw new Error(`HTTP error! status: ${{response.status}}`);
                        }}

                        return await response.json();
                    }} catch (error) {{
                        console.error('Error:', error);
                        throw error;
                    }}
                }}
                {functionName}('{url}'{(includePayload ? $", {JsonConvert.SerializeObject(payload)}" : "")});
            ";
        return script;
    }

    public void Navigate(string url)
    {
        _webView.Source = new Uri(url);
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

    public void Dispose()
    {
        _reloadDebouncer?.Dispose();
        _fetchDocsDebouncer?.Dispose();
    }
}