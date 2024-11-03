using Claudable.Utilities;
using Claudable.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;

namespace Claudable
{
    public class WebViewManager : IDisposable
    {
        public static WebViewManager Instance { get; private set; }
        private string _baseUrl = "https://api.claude.ai";

        private readonly Regex DataCollector = new Regex(@"^https://api\.claude\.ai/api/organizations/(?<organizationId>.*?)/projects/(?<projectId>.*?)/docs$");
        private readonly WebView2 _webView;
        private string _lastVisitedUrl;
        private string _currentProjectUrl;
        private string _lastDocsResponse;
        private string _activeOrganizationUuid;
        private string _activeProjectUuid;
        private readonly Debouncer _reloadDebouncer;

        public string LastVisitedUrl => _lastVisitedUrl;
        public string CurrentProjectUrl => _currentProjectUrl;

        public event EventHandler<string> DocsReceived;
        public event EventHandler<string> ProjectChanged;

        public WebViewManager(WebView2 webView, string initialUrl)
        {
            _webView = webView ?? throw new ArgumentNullException(nameof(webView));
            _lastVisitedUrl = initialUrl;
            if (Instance != null)
                throw new InvalidOperationException("Cannot instantiate more than 1 WebViewManager");
            Instance = this;
            _reloadDebouncer = new Debouncer(() => Application.Current.Dispatcher.Invoke(_webView.Reload));
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

        public async Task<ArtifactViewModel> CreateArtifact(string fileName, string content)
        {
            if (string.IsNullOrEmpty(_activeOrganizationUuid) || string.IsNullOrEmpty(_activeProjectUuid))
            {
                throw new InvalidOperationException("Active organization or project UUID is not set.");
            }

            string url = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{_activeProjectUuid}/docs";

            var payload = new
            {
                file_name = fileName,
                content = content
            };

            string script = $@"
                async function createArtifact(url, payload) {{
                    try {{
                        const response = await fetch(url, {{
                            method: 'POST',
                            headers: {{
                                'authority': 'api.claude.ai',
                                'origin': 'https://claude.ai',
                                'accept': '*/*',
                                'accept-language': 'en-US,en;q=0.9',
                                'anthropic-client-sha': '',
                                'anthropic-client-version': '',
                                'content-type': 'application/json'
                            }},
                            credentials: 'include',
                            body: JSON.stringify(payload)
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
                createArtifact('{url}', {JsonConvert.SerializeObject(payload)});
            ";

            try
            {
                string resultJson = await _webView.CoreWebView2.ExecuteScriptAsync(script);
                var artifact = JsonConvert.DeserializeObject<ArtifactViewModel>(resultJson);

                if (artifact != null)
                {
                    artifact.ProjectUuid = _activeProjectUuid;
                    _reloadDebouncer.Debounce();
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
            if (string.IsNullOrEmpty(_activeOrganizationUuid))
            {
                throw new InvalidOperationException("Active organization UUID is not set.");
            }

            string deleteUrl = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{artifact.ProjectUuid}/docs/{artifact.Uuid}";
            string docsUrl = $"https://api.claude.ai/api/organizations/{_activeOrganizationUuid}/projects/{artifact.ProjectUuid}/docs";

            string script = @"
                async function deleteArtifactAndFetchDocs(deleteUrl, docsUrl) {
                    try {
                        const deleteResponse = await fetch(deleteUrl, {
                            method: 'DELETE',
                            headers: {
                                'authority': 'api.claude.ai',
                                'origin': 'https://claude.ai',
                                'accept': '*/*',
                                'accept-language': 'en-US,en;q=0.9',
                                'anthropic-client-sha': '',
                                'anthropic-client-version': '',
                                'content-type': 'application/json'
                            },
                            credentials: 'include'
                        });

                        if (!deleteResponse.ok) {
                            throw new Error(`Delete HTTP error! status: ${deleteResponse.status}`);
                        }

                        const docsResponse = await fetch(docsUrl, {
                            method: 'GET',
                            headers: {
                                'authority': 'api.claude.ai',
                                'origin': 'https://claude.ai',
                                'accept': '*/*',
                                'accept-language': 'en-US,en;q=0.9',
                                'anthropic-client-sha': '',
                                'anthropic-client-version': '',
                                'content-type': 'application/json'
                            },
                            credentials: 'include'
                        });

                        if (!docsResponse.ok) {
                            throw new Error(`Docs HTTP error! status: ${docsResponse.status}`);
                        }
                    } catch (error) {
                        console.error('Error:', error);
                        throw error;
                    }
                }
                deleteArtifactAndFetchDocs('" + deleteUrl + @"', '" + docsUrl + @"');
            ";

            try
            {
                await _webView.CoreWebView2.ExecuteScriptAsync(script);
                _reloadDebouncer.Debounce();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error in delete operation: {e.Message}");
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
                            {
                                _activeOrganizationUuid = match.Groups["organizationId"].Value;
                                _activeProjectUuid = match.Groups["projectId"].Value;
                            }

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

        public void Dispose()
        {
            _reloadDebouncer?.Dispose();
        }
    }
}