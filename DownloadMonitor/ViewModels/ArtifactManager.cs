using Microsoft.Web.WebView2.Wpf;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Claudable.ViewModels
{
    public class ArtifactManager : INotifyPropertyChanged
    {
        private WebView2 webView;
        private ObservableCollection<ArtifactViewModel> _artifacts;
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
                        artifact.CreatedAt = artifact.CreatedAt.ToLocalTime();
                        AssociateArtifactWithProjectFile(artifact);
                    }

                    Artifacts = new ObservableCollection<ArtifactViewModel>(artifacts);
                }
                else
                {
                    var artifact = JsonConvert.DeserializeObject<ArtifactViewModel>(artifactsJson);
                    if (artifact != null)
                    {
                        AssociateArtifactWithProjectFile(artifact);
                        Artifacts.Insert(0, artifact);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                Console.WriteLine($"Error loading artifacts: {ex.Message}");
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


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}