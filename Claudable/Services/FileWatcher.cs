using Claudable.ViewModels;
using System.IO;

namespace Claudable.Services
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private ProjectFolder _rootFolder;
        private Action _updateProjectStructure;
        private readonly object _lockObject = new object();
        private Timer _batchTimer;
        private volatile bool _changesPending;
        private const int BATCH_DELAY_MS = 500; // Batch changes for 500ms

        public FileWatcher(ProjectFolder rootFolder, Action updateProjectStructure)
        {
            _rootFolder = rootFolder;
            _updateProjectStructure = updateProjectStructure;
            InitializeWatcher();
            _batchTimer = new Timer(OnBatchTimerElapsed);
        }

        private void InitializeWatcher()
        {
            _watcher = new FileSystemWatcher(_rootFolder.FullPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemChanged;
            _watcher.Changed += OnFileChanged;
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lockObject)
            {
                _changesPending = true;
                _batchTimer.Change(BATCH_DELAY_MS, Timeout.Infinite);
            }
        }

        private void OnBatchTimerElapsed(object state)
        {
            if (_changesPending)
            {
                _changesPending = false;
                _updateProjectStructure();
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            UpdateProjectFile(e.FullPath);
        }

        private void UpdateProjectFile(string fullPath)
        {
            var projectFile = FindProjectFile(_rootFolder, fullPath);
            if (projectFile != null)
            {
                projectFile.LocalLastModified = File.GetLastWriteTime(fullPath);
            }
        }

        private ProjectFile FindProjectFile(ProjectFolder folder, string fullPath)
        {
            foreach (var item in folder.Children)
            {
                if (item is ProjectFile file && file.FullPath == fullPath)
                {
                    return file;
                }
                else if (item is ProjectFolder subFolder)
                {
                    var result = FindProjectFile(subFolder, fullPath);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _batchTimer?.Dispose();
        }
    }
}