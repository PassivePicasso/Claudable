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
    public static WebViewManager? Instance { get; private set; }
    private string _baseUrl = "https://claude.ai";

    private readonly Regex ProjectUrl = new Regex(@"^https:\/\/claude\.ai\/project\/(?<projectId>.*?)$");
    private readonly Regex DocsUrl = new Regex(@".*?\/api\/organizations\/(?<organizationId>.*?)\/projects\/(?<projectId>.*?)\/docs(?:\/(?<artifactId>.*?))?$");
    private readonly Regex DataCollector = new Regex(@".*?/api/organizations/(?<organizationId>.*?)/projects/(?<projectId>.+?)/.*");
    private readonly Regex ConversationUrl = new Regex(@".*?/api/organizations/(?<organizationId>.*?)/chat_conversations/(?<conversationId>[a-f0-9-]+)(?:\?.*)?$");
    private readonly Regex ChatPageUrl = new Regex(@"^https:\/\/claude\.ai\/chat\/(?<conversationId>[a-f0-9-]+)$");
    private readonly Regex CurrentLeafMessageUrl = new Regex(@".*?/api/organizations/(?<organizationId>.*?)/chat_conversations/(?<conversationId>[a-f0-9-]+)/current_leaf_message_uuid$");

    private readonly WebView2 _webView;
    private string? _lastVisitedUrl;
    private string? _currentProjectUrl;
    private string? _lastDocsResponse;
    private readonly Debouncer _reloadDebouncer;
    private readonly Debouncer _fetchDocsDebouncer;

    private string? _activeProjectUuid;
    private string? _activeOrganizationUuid;
    private ConversationViewModel? currentConversation;

    public string? LastVisitedUrl => _lastVisitedUrl;
    public string? CurrentProjectUrl => _currentProjectUrl;
    public ConversationViewModel? CurrentConversation
    {
        get => currentConversation;
        private set
        {
            currentConversation = value;
        }
    }

    public event EventHandler<string>? DocsReceived;
    public event EventHandler<string>? ProjectChanged;
    public event EventHandler<string>? ArtifactDeleted;
    public event EventHandler<ConversationViewModel>? ConversationReceived;
    public event EventHandler<string>? CurrentLeafMessageChanged;

    public WebViewManager(WebView2 webView, string initialUrl)
    {
        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _lastVisitedUrl = initialUrl;
        if (Instance != null)
            throw new InvalidOperationException("Cannot instantiate more than 1 WebViewManager");
        Instance = this;
        _reloadDebouncer = new Debouncer(() =>
        {
            if (LastVisitedUrl != null && !ProjectUrl.IsMatch(LastVisitedUrl)) return;
            _webView.Reload();
        }, 500);
        _fetchDocsDebouncer = new Debouncer(() => _ = FetchProjectDocs(), 250);
    }

    public async Task InitializeAsync()
    {
        await WebViewFactory.InitializeWebView(_webView);
        if (_lastVisitedUrl != null)
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
            _activeOrganizationUuid = match.Groups["organizationId"].Value;

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
            case var _ when ConversationUrl.IsMatch(url) && args.Request.Method == "GET":
                {
                    try
                    {
                        var response = args.Response;
                        var stream = await response.GetContentAsync();
                        using var reader = new StreamReader(stream);
                        var conversationJson = reader.ReadToEnd();

                        // Parse the conversation JSON
                        var conversation = JsonConvert.DeserializeObject<ConversationViewModel>(conversationJson);
                        if (conversation != null)
                        {
                            CurrentConversation = conversation;
                            ConversationReceived?.Invoke(this, conversation);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing conversation: {ex}");
                    }
                }
                break;
            case var _ when CurrentLeafMessageUrl.IsMatch(url) && args.Request.Method == "PUT":
                {
                    if (args.Response.StatusCode == 200)
                        try
                        {
                            var response = args.Response;
                            var stream = await response.GetContentAsync();
                            using var reader = new StreamReader(stream);
                            var leafMessageJson = await reader.ReadToEndAsync();

                            var leafMessageData = JsonConvert.DeserializeObject<CurrentLeafMessageUpdate>(leafMessageJson);
                            if (leafMessageData != null && !string.IsNullOrEmpty(leafMessageData.CurrentLeafMessageUuid))
                            {
                                // Update the current conversation with the new leaf message UUID
                                if (CurrentConversation != null)
                                {
                                    CurrentConversation.CurrentLeafMessageUuid = leafMessageData.CurrentLeafMessageUuid;
                                    CurrentLeafMessageChanged?.Invoke(this, leafMessageData.CurrentLeafMessageUuid);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error processing current leaf message change: {ex}");
                        }
                }
                break;
        }
    }

    public async Task<ArtifactViewModel> CreateArtifact(string fileName, string content)
    {
        // Ensure we have valid project context before creating artifacts
        if (string.IsNullOrEmpty(_activeProjectUuid) || string.IsNullOrEmpty(_activeOrganizationUuid))
        {
            throw new InvalidOperationException("No active project context. Please ensure you're in a Claude project before creating artifacts.");
        }

        // Verify the current project URL matches our stored UUIDs for additional safety
        if (!string.IsNullOrEmpty(_currentProjectUrl))
        {
            var projectMatch = ProjectUrl.Match(_currentProjectUrl);
            if (projectMatch.Success)
            {
                string urlProjectId = projectMatch.Groups["projectId"].Value;
                if (urlProjectId != _activeProjectUuid)
                {
                    // Update the UUID to match the current URL
                    _activeProjectUuid = urlProjectId;
                    Console.WriteLine($"Updated active project UUID to match current URL: {_activeProjectUuid}");
                }
            }
        }

        string url = $"https://claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{_activeProjectUuid}/docs";

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
        // Ensure we have valid project context
        if (string.IsNullOrEmpty(_activeOrganizationUuid))
        {
            throw new InvalidOperationException("No active organization context. Please ensure you're logged into Claude before deleting artifacts.");
        }

        // Use the artifact's project UUID if available, otherwise fall back to active project
        string? projectUuid = !string.IsNullOrEmpty(artifact.ProjectUuid) ? artifact.ProjectUuid : _activeProjectUuid;
        
        if (string.IsNullOrEmpty(projectUuid))
        {
            throw new InvalidOperationException("No project context available for artifact deletion.");
        }

        string deleteUrl = $"https://claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{projectUuid}/docs/{artifact.Uuid}";

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

    public string? ExportConversation()
    {
        if (CurrentConversation == null)
        {
            Console.WriteLine("No conversation available to export.");
            return null;
        }

        // Log conversation metadata for debugging
        Console.WriteLine($"Exporting conversation: {CurrentConversation.Name}");
        Console.WriteLine($"UUID: {CurrentConversation.Uuid}");
        Console.WriteLine($"Current leaf message UUID: {CurrentConversation.CurrentLeafMessageUuid}");
        Console.WriteLine($"Total messages: {CurrentConversation.Messages.Count}");

        var exporter = new ConversationExporter(CurrentConversation);
        return exporter.Export();
    }

    private async Task FetchProjectDocs()
    {
        string docsUrl = $"https://claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{_activeProjectUuid}/docs";

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

    private string RESTCall(string functionName, string method, string url, object? payload = null)
    {
        var includePayload = payload != null;
        string script = $@"
                async function {functionName}(url{(includePayload ? ", payload" : "")}) {{
                    try {{
                        const response = await fetch(url, {{
                            method: '{method}',
                            headers: {{
                                'authority': 'claude.ai',
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
                Console.WriteLine($"Project changed from '{_currentProjectUrl}' to '{newProjectUrl}'");
                _currentProjectUrl = newProjectUrl;
                ProjectChanged?.Invoke(this, _currentProjectUrl);
            }
            
            // Extract project UUID directly from the project URL path
            var projectMatch = ProjectUrl.Match(newProjectUrl);
            if (projectMatch.Success)
            {
                string newProjectUuid = projectMatch.Groups["projectId"].Value;
                if (newProjectUuid != _activeProjectUuid)
                {
                    Console.WriteLine($"Project UUID updated from '{_activeProjectUuid}' to '{newProjectUuid}'");
                    _activeProjectUuid = newProjectUuid;
                }
            }
            else
            {
                // Fallback: try the DataCollector regex for API URLs
                var match = DataCollector.Match(url);
                if (match.Success)
                {
                    string newProjectUuid = match.Groups["projectId"].Value;
                    if (newProjectUuid != _activeProjectUuid)
                    {
                        Console.WriteLine($"Project UUID updated (fallback) from '{_activeProjectUuid}' to '{newProjectUuid}'");
                        _activeProjectUuid = newProjectUuid;
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        _reloadDebouncer?.Dispose();
        _fetchDocsDebouncer?.Dispose();
    }
}