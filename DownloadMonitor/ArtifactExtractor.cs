using Microsoft.Web.WebView2.Wpf;

public static class ArtifactExtractor
{
    public static async Task<List<string>> ExtractArtifactFileNames(WebView2 webView)
    {
        string script = @"
            function extractArtifacts() {
                const artifactSection = document.querySelector('.col-span-5.hidden.pl-12.pr-4.lg\\:block');
                if (!artifactSection) {
                    return null;
                }
                
                const fileElements = artifactSection.querySelectorAll('[testid]');
                const fileNames = Array.from(fileElements).map(el => el.getAttribute('testid'));
                return fileNames;
            }
            extractArtifacts();
        ";

        try
        {
            var result = await webView.ExecuteScriptAsync(script);
            if (result == "null")
            {
                return null; // Artifact section not found
            }

            var fileNames = System.Text.Json.JsonSerializer.Deserialize<List<string>>(result);
            return fileNames;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error extracting artifacts: {ex.Message}");
            return null;
        }
    }
}
