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
        private readonly HashSet<string> _pendingChanges = new();
        private const int BATCH_DELAY_MS = 250; // Reduced from 500ms to 250ms for better responsiveness

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
                NotifyFilter = NotifyFilters.DirectoryName |
                             NotifyFilters.FileName |
                             NotifyFilters.LastWrite |
                             NotifyFilters.CreationTime |
                             NotifyFilters.Size,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true,
                InternalBufferSize = 65536 // Increased buffer size for better handling of rapid changes
            };

            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemChanged;
            _watcher.Changed += OnFileSystemChanged;
            _watcher.Error += OnWatcherError;
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            lock (_lockObject)
            {
                // Add the changed path to pending changes
                _pendingChanges.Add(e.FullPath);
                // Reset the timer each time a change is detected
                _batchTimer.Change(BATCH_DELAY_MS, Timeout.Infinite);
            }
        }

        private void OnWatcherError(object sender, ErrorEventArgs e)
        {
            // Log the error
            System.Diagnostics.Debug.WriteLine($"FileSystemWatcher error: {e.GetException()}");

            // Attempt to restart the watcher
            try
            {
                _watcher.EnableRaisingEvents = false;
                InitializeWatcher();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error restarting FileSystemWatcher: {ex}");
            }
        }

        private void OnBatchTimerElapsed(object state)
        {
            lock (_lockObject)
            {
                if (_pendingChanges.Count > 0)
                {
                    // Clear pending changes before processing
                    _pendingChanges.Clear();

                    try
                    {
                        // Invoke the update on the UI thread
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            _updateProjectStructure?.Invoke();
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating project structure: {ex}");
                    }
                }
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _batchTimer?.Dispose();
        }
    }
}