using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace DownloadMonitor.ViewModels
{
    public class TrackedFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _fullPath;
        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(PascalCaseFileName));
            }
        }

        public string FileName => Path.GetFileName(FullPath);

        public string PascalCaseFileName => ToPascalCase(Path.GetFileNameWithoutExtension(FullPath)) + Path.GetExtension(FullPath);

        private DateTime _lastModified;
        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public TrackedFile(string fullPath)
        {
            FullPath = fullPath;
            LastModified = File.GetLastWriteTime(fullPath);
        }

        private string ToPascalCase(string input)
        {
            input = input.Replace("_", " ").Replace("-", " ");
            return string.Join("", input.Split(' ')
                .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}