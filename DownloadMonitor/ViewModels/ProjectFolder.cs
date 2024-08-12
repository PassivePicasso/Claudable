using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels
{
    public class ProjectFolder : INotifyPropertyChanged
    {
        private string _name;
        private string _fullPath;
        private ObservableCollection<ProjectFolder> _subFolders;
        private ObservableCollection<string> _files;
        private bool _isVisible = true;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public string FullPath
        {
            get => _fullPath;
            set { _fullPath = value; OnPropertyChanged(); }
        }

        public ObservableCollection<ProjectFolder> SubFolders
        {
            get => _subFolders;
            set { _subFolders = value; OnPropertyChanged(); }
        }

        public ObservableCollection<string> Files
        {
            get => _files;
            set { _files = value; OnPropertyChanged(); }
        }

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public ProjectFolder(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
            SubFolders = new ObservableCollection<ProjectFolder>();
            Files = new ObservableCollection<string>();
        }

        public void ApplyFilter(string[] filters)
        {
            bool shouldExclude = filters.Any(filter => Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            if (shouldExclude)
            {
                // If this folder matches the filter, exclude it and all its descendants
                IsVisible = false;
                foreach (var subFolder in SubFolders)
                {
                    subFolder.IsVisible = false;
                }
            }
            else
            {
                IsVisible = true;

                // Filter files
                foreach (var file in Files)
                {
                    bool fileVisible = !filters.Any(filter => file.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    // Note: We're not removing files, just conceptually marking them as invisible.
                    // In the UI binding, we'll filter out invisible files.
                }

                // Recursively apply filter to subfolders
                foreach (var subFolder in SubFolders)
                {
                    subFolder.ApplyFilter(filters);
                }

                // If all subfolders are invisible and there are no visible files, make this folder invisible
                IsVisible = SubFolders.Any(sf => sf.IsVisible) || Files.Any(f => !filters.Any(filter => f.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}