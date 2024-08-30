using Claudable.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels
{
    public class ProjectFile : FileSystemItem, INotifyPropertyChanged
    {
        private ArtifactViewModel _associatedArtifact;
        private DateTime _localLastModified;
        private DateTime _artifactLastModified;
        private bool _isLocalNewer;
        private bool _isTrackedAsArtifact;

        public ArtifactViewModel AssociatedArtifact
        {
            get => _associatedArtifact;
            set
            {
                _associatedArtifact = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTrackedAsArtifact));
                OnPropertyChanged(nameof(IsLocalNewer));
            }
        }

        public DateTime LocalLastModified
        {
            get => _localLastModified;
            set
            {
                _localLastModified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLocalNewer));
            }
        }

        public DateTime ArtifactLastModified
        {
            get => _artifactLastModified;
            set
            {
                _artifactLastModified = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLocalNewer));
            }
        }

        public bool IsLocalNewer => LocalLastModified > ArtifactLastModified;

        public bool IsTrackedAsArtifact => AssociatedArtifact != null;

        public ProjectFile(string name, string fullPath, FileSystemItem parent = null) : base()
        {
            Name = name;
            FullPath = fullPath;
            Parent = parent;
            LocalLastModified = System.IO.File.GetLastWriteTime(fullPath);
        }

        public void UpdateFromArtifact(ArtifactViewModel artifact)
        {
            AssociatedArtifact = artifact;
            ArtifactLastModified = artifact.CreatedAt;
            OnPropertyChanged(nameof(IsTrackedAsArtifact));
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}