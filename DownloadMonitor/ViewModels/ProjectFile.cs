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
                UpdateArtifactStatus();
            }
        }

        public DateTime LocalLastModified
        {
            get => _localLastModified;
            set
            {
                _localLastModified = value;
                OnPropertyChanged();
                UpdateVersionComparison();
            }
        }

        public DateTime ArtifactLastModified
        {
            get => _artifactLastModified;
            set
            {
                _artifactLastModified = value;
                OnPropertyChanged();
                UpdateVersionComparison();
            }
        }

        public bool IsLocalNewer
        {
            get => _isLocalNewer;
            private set
            {
                _isLocalNewer = value;
                OnPropertyChanged();
            }
        }

        public bool IsTrackedAsArtifact
        {
            get => _isTrackedAsArtifact;
            private set
            {
                _isTrackedAsArtifact = value;
                OnPropertyChanged();
            }
        }

        public ProjectFile(string name, string fullPath) : base()
        {
            Name = name;
            FullPath = fullPath;
            LocalLastModified = System.IO.File.GetLastWriteTime(fullPath);
        }

        private void UpdateVersionComparison()
        {
            IsLocalNewer = LocalLastModified > ArtifactLastModified;
        }

        private void UpdateArtifactStatus()
        {
            IsTrackedAsArtifact = AssociatedArtifact != null;
        }

        public void UpdateFromArtifact(ArtifactViewModel artifact)
        {
            AssociatedArtifact = artifact;
            ArtifactLastModified = artifact.CreatedAt;
            UpdateArtifactStatus();
            UpdateVersionComparison();
        }

        public new event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}