using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using System.IO;

public class ToolController : WebApiController
{
    [Route(HttpVerbs.Get, "/tools/{id}")]
    public async Task GetTool(string id)
    {
        var toolPath = Path.Combine(HttpContext.Server.Configuration.WebRootPath, "tools", $"{id}.html");
        if (!File.Exists(toolPath))
        {
            throw HttpException.NotFound();
        }

        // Set content type and cache headers
        HttpContext.Response.ContentType = "text/html";
        HttpContext.Response.Headers.Add("Cache-Control", "no-cache");

        await HttpContext.SendStringAsync(await File.ReadAllTextAsync(toolPath));
    }

    [Route(HttpVerbs.Post, "/tools/{id}")]
    public async Task UpdateTool(string id)
    {
        var content = await HttpContext.GetRequestBodyAsStringAsync();
        var toolPath = Path.Combine(HttpContext.Server.Configuration.WebRootPath, "tools", $"{id}.html");
        await File.WriteAllTextAsync(toolPath, content);
        await HttpContext.SendStringAsync("Tool updated");
    }
}