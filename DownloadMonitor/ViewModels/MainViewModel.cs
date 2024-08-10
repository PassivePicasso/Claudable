using DownloadMonitor.Models;
using Newtonsoft.Json;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DownloadMonitor.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private bool _isPanelsSwapped;
        private FileTrackingViewModel _downloadMonitorViewModel;
        private FileTrackingViewModel _fileChangeMonitorViewModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public FileTrackingViewModel DownloadMonitorViewModel
        {
            get => _downloadMonitorViewModel;
            set
            {
                _downloadMonitorViewModel = value;
                OnPropertyChanged();
            }
        }
        public FileTrackingViewModel FileChangeMonitorViewModel
        {
            get => _fileChangeMonitorViewModel;
            set
            {
                _fileChangeMonitorViewModel = value;
                OnPropertyChanged();
            }
        }
        public bool IsPanelsSwapped
        {
            get => _isPanelsSwapped;
            set
            {
                if (_isPanelsSwapped != value)
                {
                    _isPanelsSwapped = value;
                    OnPropertyChanged();
                }
            }
        }
        public ICommand SaveStateCommand { get; private set; }
        public ICommand LoadStateCommand { get; private set; }

        public MainViewModel()
        {
            DownloadMonitorViewModel = new FileTrackingViewModel();
            FileChangeMonitorViewModel = new FileTrackingViewModel();

            SaveStateCommand = new RelayCommand(SaveState);
            LoadStateCommand = new RelayCommand(LoadState);
        }

        private void SaveState()
        {
            var state = new AppState
            {
                DownloadMonitorState = DownloadMonitorViewModel.GetState(),
                FileChangeMonitorState = FileChangeMonitorViewModel.GetState(),
                IsPanelsSwapped = IsPanelsSwapped
            };

            string json = JsonConvert.SerializeObject(state);
            File.WriteAllText("appstate.json", json);
        }

        private void LoadState()
        {
            if (File.Exists("appstate.json"))
            {
                string json = File.ReadAllText("appstate.json");
                var state = JsonConvert.DeserializeObject<AppState>(json);

                DownloadMonitorViewModel.SetState(state.DownloadMonitorState);
                FileChangeMonitorViewModel.SetState(state.FileChangeMonitorState);
                IsPanelsSwapped = state.IsPanelsSwapped;
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
