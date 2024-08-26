using Claudable.Models;

namespace Claudable.ViewModels
{
    public class ProjectFolder : FileSystemItem
    {
        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged();
            }
        }

        public ProjectFolder(string name, string fullPath, FileSystemItem parent = null)
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
        public void ApplyFilter(string[] filters, FilterMode filterMode)
        {
            bool shouldExclude = filters.Any(filter => Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            if (shouldExclude)
            {
                IsVisible = false;
                foreach (var child in Children)
                {
                    child.IsVisible = false;
                }
            }
            else
            {
                foreach (var child in Children)
                {
                    if (child is ProjectFolder folder)
                    {
                        folder.ApplyFilter(filters, filterMode);
                    }
                    else if (child is ProjectFile file)
                    {
                        bool isFiltered = filters.Any(filter => file.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                        file.IsVisible = !isFiltered && ShouldShowFile(file, filterMode);
                    }
                }

                IsVisible = Children.Any(c => c.IsVisible);
            }

            // Expand the folder if it has visible children
            IsExpanded = Children.Any(c => c.IsVisible);
            OnPropertyChanged(nameof(IsExpanded));
            OnPropertyChanged(nameof(Children));
        }

        private bool ShouldShowFile(ProjectFile file, FilterMode filterMode)
        {
            switch (filterMode)
            {
                case FilterMode.ShowOnlyTrackedArtifacts:
                    return file.IsTrackedAsArtifact;
                case FilterMode.ShowOnlyOutdatedFiles:
                    return file.IsTrackedAsArtifact && file.IsLocalNewer;
                default:
                    return true;
            }
        }
    }
}