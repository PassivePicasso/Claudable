using System;
using System.IO;
using Claudable.ViewModels;

namespace Claudable.Services
{
    public class FileWatcher : IDisposable
    {
        private FileSystemWatcher _watcher;
        private ProjectFolder _rootFolder;
        private Action _updateProjectStructure;

        public FileWatcher(ProjectFolder rootFolder, Action updateProjectStructure)
        {
            _rootFolder = rootFolder;
            _updateProjectStructure = updateProjectStructure;
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

            _watcher.Created += OnFileSystemChanged;
            _watcher.Deleted += OnFileSystemChanged;
            _watcher.Renamed += OnFileSystemChanged;
            _watcher.Changed += OnFileChanged;
        }

        private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
        {
            _updateProjectStructure();
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