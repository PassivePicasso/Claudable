using Claudable.Models;
using System.Collections.ObjectModel;

namespace Claudable.ViewModels
{
    public class ProjectFolder : FileSystemItem
    {
        private bool _isExpanded;
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
        }

        public void ApplyFilter(FilterMode filterMode)
        {
            FilteredChildren.Clear();

            foreach (var child in Children)
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

            IsVisible = FilteredChildren.Count > 0;
        }
    }
}