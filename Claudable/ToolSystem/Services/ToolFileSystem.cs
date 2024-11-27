using System.IO;
using System.IO.Abstractions;
using System.Security;
using Claudable.ToolSystem.Interfaces;

namespace Claudable.ToolSystem.Services;

public class ToolFileSystem : IToolFileSystem
{
    private readonly IFileSystem _fileSystem;
    private readonly string _rootPath;

    public ToolFileSystem(IFileSystem fileSystem, string rootPath)
    {
        _fileSystem = fileSystem;
        _rootPath = rootPath;
    }

    public Task<bool> FileExistsAsync(string path)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<string>> ListFilesAsync(string path)
    {
        throw new NotImplementedException();
    }

    public async Task<string> ReadFileAsync(string path)
    {
        var safePath = GetSafePath(path);
        return await _fileSystem.File.ReadAllTextAsync(safePath);
    }

    public Task WriteFileAsync(string path, string content)
    {
        throw new NotImplementedException();
    }

    private string GetSafePath(string path)
    {
        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, path));
        if (!fullPath.StartsWith(_rootPath))
        {
            throw new SecurityException("Path traversal not allowed");
        }
        return fullPath;
    }
}