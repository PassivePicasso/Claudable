using Newtonsoft.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels
{
    public class ArtifactViewModel : INotifyPropertyChanged
    {
        private string _fileName;
        private string uuid;
        private string content;
        private DateTime createdAt;
        private string projectUuid;

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

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
