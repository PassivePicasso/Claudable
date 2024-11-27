namespace Claudable.ToolSystem.Interfaces;

public interface IToolFileSystem
{
    Task<string> ReadFileAsync(string path);
    Task WriteFileAsync(string path, string content);
    Task<bool> FileExistsAsync(string path);
    Task<IEnumerable<string>> ListFilesAsync(string path);
}