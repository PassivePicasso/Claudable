using System;
using System.IO;
using Claudable.ViewModels;

namespace Claudable.Services
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private ProjectFolder _rootFolder;

        public FileWatcher(ProjectFolder rootFolder)
        {
            _rootFolder = rootFolder;
            InitializeWatcher();
        }

        private void InitializeWatcher()
        {
            _watcher = new FileSystemWatcher(_rootFolder.FullPath)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnFileChanged;
            _watcher.Created += OnFileCreated;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Deleted += OnFileDeleted;
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            UpdateProjectFile(e.FullPath);
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            // Handle file creation if needed
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            // Handle file renaming if needed
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            // Handle file deletion if needed
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
            for (int i = 0; i < folder.Children.Count; i++)
            {
                Models.FileSystemItem? item = folder.Children[i];
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
        }
    }
}
