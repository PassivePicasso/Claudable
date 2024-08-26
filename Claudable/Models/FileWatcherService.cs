using System.IO;

namespace Claudable.Services
{
    public class FileWatcherService
    {
        private FileSystemWatcher _downloadWatcher;
        private FileSystemWatcher _changeWatcher;
        private Action<string> _onFileCreated;
        private Action<string> _onFileChanged;
        private Action<string, string> _onFileRenamed;
        private Action<string> _onFileDeleted;

        public FileWatcherService(Action<string> onFileCreated, Action<string> onFileChanged, Action<string, string> onFileRenamed, Action<string> onFileDeleted)
        {
            _onFileCreated = onFileCreated;
            _onFileChanged = onFileChanged;
            _onFileRenamed = onFileRenamed;
            _onFileDeleted = onFileDeleted;
        }

        public void InitializeDownloadWatcher(string path)
        {
            _downloadWatcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*"
            };

            _downloadWatcher.Created += OnFileCreated;
            _downloadWatcher.Renamed += OnFileRenamed;
            _downloadWatcher.Deleted += OnFileDeleted;
            _downloadWatcher.EnableRaisingEvents = true;
        }

        public void InitializeChangeWatcher(string path)
        {
            _changeWatcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*",
                IncludeSubdirectories = true
            };

            _changeWatcher.Changed += OnFileChanged;
            _changeWatcher.Created += OnFileChanged;
            _changeWatcher.Renamed += OnFileRenamed;
            _changeWatcher.EnableRaisingEvents = true;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            if (!IsFileHidden(e.FullPath) && !IsUnderDotFolder(e.FullPath))
            {
                _onFileCreated(e.FullPath);
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (File.Exists(e.FullPath) && !IsFileHidden(e.FullPath) && !IsTempFile(e.FullPath) && !IsUnderDotFolder(e.FullPath))
            {
                _onFileChanged(e.FullPath);
            }
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            if (sender == _downloadWatcher)
            {
                _onFileRenamed(e.OldFullPath, e.FullPath);
            }
            else if (sender == _changeWatcher)
            {
                if (!IsFileHidden(e.FullPath) && !IsUnderDotFolder(e.FullPath))
                {
                    _onFileRenamed(e.OldFullPath, e.FullPath);
                }
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            _onFileDeleted(e.FullPath);
        }

        private bool IsFileHidden(string filePath)
        {
            try
            {
                var attr = File.GetAttributes(filePath);
                return (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
            }
            catch
            {
                return true;
            }
        }

        private bool IsTempFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".TMP", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsUnderDotFolder(string filePath)
        {
            string[] pathParts = filePath.Split(Path.DirectorySeparatorChar);
            return pathParts.Any(part => part.StartsWith(".") && part.Length > 1);
        }

        public void Dispose()
        {
            _downloadWatcher?.Dispose();
            _changeWatcher?.Dispose();
        }
    }
}
