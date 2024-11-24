using System;
using System.IO;
using System.Threading;
using Claudable.ViewModels;

namespace Claudable.Services
{
    public class FileWatcher : IDisposable
    {
        private readonly FileSystemWatcher _watcher;
        private readonly ProjectFolder _rootFolder;
        private readonly Timer _debounceTimer;
        private volatile bool _changesPending;
        private readonly object _syncLock = new object();

        public FileWatcher(ProjectFolder rootFolder, Action updateProjectStructure)
        {
            _rootFolder = rootFolder;
            _watcher = new FileSystemWatcher(rootFolder.FullPath)
            {
                NotifyFilter = NotifyFilters.FileName |          // For renames and create/delete
                              NotifyFilters.DirectoryName |      // For folder renames
                              NotifyFilters.LastWrite,           // For content changes
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            // Single timer for debouncing all changes
            _debounceTimer = new Timer(_ =>
            {
                if (!_changesPending) return;

                try
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        lock (_syncLock)
                        {
                            updateProjectStructure?.Invoke();
                            _changesPending = false;
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating project structure: {ex}");
                }
            });

            // Simple handlers that just mark changes as pending
            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemChanged;
            _watcher.Changed += OnFileModified;
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            // File/folder was created, deleted, or renamed
            // Mark changes pending and reset the debounce timer
            ScheduleUpdate();
        }

        private void OnFileModified(object sender, FileSystemEventArgs e)
        {
            // Only care about LastWrite changes for files (not directories)
            if (!Directory.Exists(e.FullPath))
            {
                ScheduleUpdate();
            }
        }

        private void ScheduleUpdate()
        {
            lock (_syncLock)
            {
                _changesPending = true;
                _debounceTimer.Change(200, Timeout.Infinite);  // 200ms debounce
            }
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }
    }
}