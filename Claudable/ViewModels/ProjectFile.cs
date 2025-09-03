using Claudable.Models;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Claudable.ViewModels;

public class ProjectFile : FileSystemItem, INotifyPropertyChanged
{
    private ArtifactViewModel? _associatedArtifact;
    private DateTime _localLastModified;
    private DateTime _artifactLastModified;
    private bool _isLocalNewer;

    public new ArtifactViewModel? AssociatedArtifact
    {
        get => _associatedArtifact;
        set
        {
            if (_associatedArtifact != value)
            {
                _associatedArtifact = value;
                OnPropertyChanged();
                UpdateIsLocalNewer();
                OnPropertyChanged(nameof(IsTrackedAsArtifact));
            }
        }
    }

    public DateTime LocalLastModified
    {
        get => _localLastModified;
        set
        {
            if (_localLastModified != value)
            {
                _localLastModified = value;
                OnPropertyChanged();
                UpdateIsLocalNewer();
            }
        }
    }

    public DateTime ArtifactLastModified
    {
        get => _artifactLastModified;
        set
        {
            if (_artifactLastModified != value)
            {
                _artifactLastModified = value;
                OnPropertyChanged();
                UpdateIsLocalNewer();
            }
        }
    }

    public bool IsLocalNewer
    {
        get => _isLocalNewer;
        private set
        {
            if (_isLocalNewer != value)
            {
                _isLocalNewer = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsTrackedAsArtifact => AssociatedArtifact != null;

    public ICommand TrackArtifactCommand { get; private set; }
    public ICommand UntrackArtifactCommand { get; private set; }

    public ProjectFile(string name, string fullPath, FileSystemItem? parent = null) : base()
    {
        Name = name;
        FullPath = fullPath;
        Parent = parent;
        LocalLastModified = System.IO.File.GetLastWriteTimeUtc(fullPath);
        TrackArtifactCommand = new RelayCommand(TrackArtifact);
        UntrackArtifactCommand = new RelayCommand(UntrackArtifact);
    }

    private void UpdateIsLocalNewer()
    {
        if (AssociatedArtifact == null)
        {
            IsLocalNewer = false;
            return;
        }

        // Ensure both timestamps are in UTC for comparison
        var localUtc = LocalLastModified.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(LocalLastModified, DateTimeKind.Local).ToUniversalTime()
            : LocalLastModified.ToUniversalTime();

        var artifactUtc = ArtifactLastModified.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(ArtifactLastModified, DateTimeKind.Local).ToUniversalTime()
            : ArtifactLastModified.ToUniversalTime();

        IsLocalNewer = localUtc > artifactUtc;
        if (Parent is ProjectFolder folder)
            folder.NotifyFileStatusChanged();
    }

    public void UpdateFromArtifact(ArtifactViewModel artifact)
    {
        if (artifact == null)
        {
            AssociatedArtifact = null;
            ArtifactLastModified = DateTime.MinValue;
            return;
        }

        AssociatedArtifact = artifact;

        // Ensure the artifact timestamp is properly handled
        var artifactTime = artifact.CreatedAt;
        if (artifactTime.Kind == DateTimeKind.Unspecified)
        {
            artifactTime = DateTime.SpecifyKind(artifactTime, DateTimeKind.Utc);
        }
        else if (artifactTime.Kind == DateTimeKind.Local)
        {
            artifactTime = artifactTime.ToUniversalTime();
        }

        ArtifactLastModified = artifactTime;
        UpdateIsLocalNewer();
    }

    private async void TrackArtifact()
    {
        try
        {
            if (WebViewManager.Instance == null)
            {
                throw new InvalidOperationException("WebView manager is not initialized.");
            }

            var content = await File.ReadAllTextAsync(FullPath);
            var artifact = await WebViewManager.Instance.CreateArtifact(Name, content);
            if (artifact != null)
            {
                UpdateFromArtifact(artifact);
            }
        }
        catch (InvalidOperationException ex)
        {
            // Show user-friendly message for project context issues
            System.Windows.MessageBox.Show(
                $"Unable to track artifact: {ex.Message}\n\nPlease ensure you are in a Claude project before tracking files.",
                "Project Context Required",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error tracking artifact: {ex.Message}");
            System.Windows.MessageBox.Show(
                $"Error tracking artifact: {ex.Message}",
                "Error",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    public async void UntrackArtifact()
    {
        if (AssociatedArtifact != null)
        {
            try
            {
                if (WebViewManager.Instance == null)
                {
                    throw new InvalidOperationException("WebView manager is not initialized.");
                }

                await WebViewManager.Instance.DeleteArtifact(AssociatedArtifact);
                AssociatedArtifact = null;
                ArtifactLastModified = DateTime.MinValue;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error untracking artifact: {ex.Message}");
            }
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected override void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}