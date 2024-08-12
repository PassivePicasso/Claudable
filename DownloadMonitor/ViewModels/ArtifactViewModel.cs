using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Claudable.ViewModels
{
    public class ArtifactViewModel : INotifyPropertyChanged
    {
        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set
            {
                _fileName = value;
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
