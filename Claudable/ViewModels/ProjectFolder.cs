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

            foreach (var child in Children)
            {
                switch (child)
                {
                    case ProjectFile file:
                        var shouldAdd = filterMode switch
                        {
                            FilterMode.ShowOnlyTrackedArtifacts => file.IsTrackedAsArtifact,
                            FilterMode.ShowOnlyOutdatedFiles => file.IsTrackedAsArtifact && file.IsLocalNewer,
                            _ => true,
                        };
                        if (shouldAdd)
                        {
                            FilteredChildren.Add(child);
                        }
                        break;
                    case ProjectFolder folder:
                        folder.ApplyFilter(filterMode);
                        if (folder.FilteredChildren.Count > 0)
                            FilteredChildren.Add(child);
                        break;
                }
            }

            IsVisible = FilteredChildren.Count > 0;
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