using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Claudable.ViewModels
{
    public class ArtifactViewModel : INotifyPropertyChanged
    {
        private string _fileName;
        private string uuid;
        private string content;
        private DateTime createdAt;
        private string projectUuid;
        private ProjectFile _projectFile;

        [JsonProperty("file_name")]
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("uuid")]
        public string Uuid
        {
            get => uuid;
            set
            {
                uuid = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("content")]
        public string Content
        {
            get => content;
            set
            {
                content = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("created_at")]
        public DateTime CreatedAt
        {
            get => createdAt;
            set
            {
                createdAt = value;
                OnPropertyChanged();
            }
        }

        [JsonProperty("project_uuid")]
        public string ProjectUuid
        {
            get => projectUuid;
            set
            {
                projectUuid = value;
                OnPropertyChanged();
            }
        }

        [JsonIgnore]
        public bool HasLocalFile => ProjectFile != null;

        [JsonIgnore]
        public ProjectFile ProjectFile
        {
            get => _projectFile;
            set
            {
                _projectFile = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasLocalFile));
            }
        }
        public ICommand UntrackArtifactCommand { get; private set; }

        public ArtifactViewModel()
        {
            UntrackArtifactCommand = new RelayCommand(UntrackArtifact);
        }
        public async void UntrackArtifact()
        {
            try
            {
                if (ProjectFile != null)
                    ProjectFile.UntrackArtifact();
                else
                    await WebViewManager.Instance.DeleteArtifact(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error untracking artifact: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}