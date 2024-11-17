using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels
{
    public class ArtifactManager : INotifyPropertyChanged
    {
        private WebView2 webView;
        private ObservableCollection<ArtifactViewModel> _artifacts;
        private ObservableCollection<SvgArtifactViewModel> _svgArtifacts;
        private ProjectFolder _rootProjectFolder;

        public ObservableCollection<ArtifactViewModel> Artifacts
        {
            get => _artifacts;
            set
            {
                _artifacts = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SvgArtifactViewModel> SvgArtifacts
        {
            get => _svgArtifacts;
            set
            {
                _svgArtifacts = value;
                OnPropertyChanged();
            }
        }

        public ProjectFolder RootProjectFolder
        {
            get => _rootProjectFolder;
            set
            {
                _rootProjectFolder = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ArtifactManager()
        {
            Artifacts = new ObservableCollection<ArtifactViewModel>();
            SvgArtifacts = new ObservableCollection<SvgArtifactViewModel>();
        }

        public void LoadArtifacts(string artifactsJson)
        {
            try
            {
                if (artifactsJson.StartsWith("["))
                {
                    var artifacts = JsonConvert.DeserializeObject<ArtifactViewModel[]>(artifactsJson)
                                 ?? Enumerable.Empty<ArtifactViewModel>();

                    foreach (var artifact in artifacts)
                    {
                        ProcessArtifact(artifact);
                    }

                    Artifacts = new ObservableCollection<ArtifactViewModel>(artifacts);
                }
                else
                {
                    var artifact = JsonConvert.DeserializeObject<ArtifactViewModel>(artifactsJson);
                    if (artifact != null)
                    {
                        ProcessArtifact(artifact);
                        Artifacts.Insert(0, artifact);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading artifacts: {ex.Message}");
            }
        }

        private void ProcessArtifact(ArtifactViewModel artifact)
        {
            artifact.CreatedAt = artifact.CreatedAt.ToLocalTime();
            UpdateArtifactLocalFileStatus(artifact);
            AssociateArtifactWithProjectFile(artifact);
            if (IsSvgArtifact(artifact))
            {
                UpdateOrAddSvgArtifact(artifact);
            }
        }

        private void UpdateArtifactLocalFileStatus(ArtifactViewModel artifact)
        {
            if (RootProjectFolder == null)
            {
                artifact.HasLocalFile = false;
                return;
            }

            // Search for the file in the project structure
            var projectFile = FindProjectFile(RootProjectFolder, artifact.FileName);
            artifact.HasLocalFile = projectFile != null && File.Exists(projectFile.FullPath);
        }

        private bool IsSvgArtifact(ArtifactViewModel artifact)
        {
            return artifact.FileName.EndsWith(".svg", StringComparison.OrdinalIgnoreCase);
        }

        private void UpdateOrAddSvgArtifact(ArtifactViewModel artifact)
        {
            var existingSvgArtifact = SvgArtifacts.FirstOrDefault(s => s.Uuid == artifact.Uuid);
            if (existingSvgArtifact != null)
            {
                existingSvgArtifact.Name = artifact.FileName;
                existingSvgArtifact.Content = artifact.Content;
            }
            else
            {
                var svgArtifact = new SvgArtifactViewModel
                {
                    Name = artifact.FileName,
                    Content = artifact.Content,
                    Uuid = artifact.Uuid
                };
                SvgArtifacts.Add(svgArtifact);
            }
        }

        private void AssociateArtifactWithProjectFile(ArtifactViewModel artifact)
        {
            if (RootProjectFolder == null) return;

            var projectFile = FindProjectFile(RootProjectFolder, artifact.FileName);
            if (projectFile != null)
            {
                projectFile.AssociatedArtifact = artifact;
                CheckVersionDifference(projectFile);
            }
        }

        private ProjectFile FindProjectFile(ProjectFolder folder, string fileName)
        {
            foreach (var item in folder.Children)
            {
                if (item is ProjectFile file && file.Name == fileName)
                {
                    return file;
                }
                else if (item is ProjectFolder subFolder)
                {
                    var result = FindProjectFile(subFolder, fileName);
                    if (result != null)
                    {
                        return result;
                    }
                }
            }
            return null;
        }

        public void CheckVersionDifference(ProjectFile projectFile)
        {
            if (projectFile.AssociatedArtifact == null) return;

            var fileInfo = new FileInfo(projectFile.FullPath);
            if (fileInfo.Exists)
            {
                projectFile.LocalLastModified = fileInfo.LastWriteTime;
                projectFile.ArtifactLastModified = projectFile.AssociatedArtifact.CreatedAt;
            }
        }

        public void UpdateAllArtifactsStatus()
        {
            if (RootProjectFolder == null) return;

            foreach (var artifact in Artifacts)
            {
                UpdateArtifactLocalFileStatus(artifact);
            }
        }
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}