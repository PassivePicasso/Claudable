using Claudable.Models;
using System.Collections.ObjectModel;

namespace Claudable.ViewModels
{
    public class ProjectFolder : FileSystemItem
    {
        private bool _isExpanded;
        private bool _refreshInProgress;
        private ObservableCollection<FileSystemItem> _filteredChildren = [];

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public bool RefreshInProgress
        {
            get => _refreshInProgress;
            set
            {
                _refreshInProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanRefresh));
            }
        }

        public bool CanRefresh => !RefreshInProgress && HasOutdatedFiles;

        public bool HasOutdatedFiles
        {
            get
            {
                return GetAllProjectFiles()
                    .Any(file => file.IsTrackedAsArtifact && file.IsLocalNewer);
            }
        }

        public ObservableCollection<FileSystemItem> FilteredChildren
        {
            get => _filteredChildren;
            private set
            {
                _filteredChildren = value;
                OnPropertyChanged();
            }
        }

        public ProjectFolder(string name, string fullPath, FileSystemItem? parent = null)
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            FilteredChildren = new ObservableCollection<FileSystemItem>();
        }

        public void AddChild(FileSystemItem child)
        {
            child.Parent = this;
            Children.Add(child);
            OnPropertyChanged(nameof(HasOutdatedFiles));
        }

        public void NotifyFileStatusChanged()
        {
            OnPropertyChanged(nameof(HasOutdatedFiles));
            OnPropertyChanged(nameof(CanRefresh));
            (Parent as ProjectFolder)?.NotifyFileStatusChanged();
        }

        public void ApplyFilter(FilterMode filterMode)
        {
            FilteredChildren.Clear();

            // Always add folders regardless of filter mode
            foreach (var folder in Children.OfType<ProjectFolder>())
            {
                folder.ApplyFilter(filterMode);
                FilteredChildren.Add(folder);
            }

            // Filter files based on the selected mode
            var files = Children.OfType<ProjectFile>();
            var filteredFiles = filterMode switch
            {
                FilterMode.ShowOnlyTrackedArtifacts => files.Where(file => file.IsTrackedAsArtifact),
                FilterMode.ShowOnlyOutdatedFiles => files.Where(file => file.IsTrackedAsArtifact && file.IsLocalNewer),
                _ => files
            };

            foreach (var file in filteredFiles)
            {
                FilteredChildren.Add(file);
            }

            IsVisible = true; // Always show folders
        }

        public IEnumerable<ProjectFile> GetAllProjectFiles()
        {
            foreach (var child in Children)
            {
                if (child is ProjectFile file)
                {
                    yield return file;
                }
                else if (child is ProjectFolder folder)
                {
                    foreach (var nestedFile in folder.GetAllProjectFiles())
                    {
                        yield return nestedFile;
                    }
                }
            }
        }
    }
}