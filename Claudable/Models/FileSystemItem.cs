using Claudable.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Claudable.Models
{
    public class FileSystemItem : INotifyPropertyChanged
    {
        private string _name;
        private string _fullPath;
        private bool _isVisible = true;
        private ArtifactViewModel _associatedArtifact;
        private FileSystemItem? _parent;

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

        public bool IsVisible
        {
            get => _isVisible;
            set { _isVisible = value; OnPropertyChanged(); }
        }

        public ArtifactViewModel AssociatedArtifact
        {
            get => _associatedArtifact;
            set { _associatedArtifact = value; OnPropertyChanged(); }
        }

        public FileSystemItem? Parent
        {
            get => _parent;
            set { _parent = value; OnPropertyChanged(); }
        }
        public ObservableCollection<FileSystemItem> Children { get; } = new ObservableCollection<FileSystemItem>();

        public bool IsFolder => this is ProjectFolder;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}