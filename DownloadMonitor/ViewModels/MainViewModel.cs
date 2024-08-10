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
        private int _selectedTabIndex;
        private FileTrackingViewModel _downloadMonitorViewModel;
        private FileTrackingViewModel _fileChangeMonitorViewModel;
        private FilterViewModel _filterViewModel;

        public event PropertyChangedEventHandler PropertyChanged;

        public FilterViewModel FilterViewModel
        {
            get => _filterViewModel;
            set
            {
                _filterViewModel = value;
                OnPropertyChanged();
            }
        }
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
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set
            {
                if (_selectedTabIndex != value)
                {
                    _selectedTabIndex = value;
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
            FilterViewModel = new FilterViewModel();

            SaveStateCommand = new RelayCommand(SaveState);
            LoadStateCommand = new RelayCommand(LoadState);

            FilterViewModel.ApplyFiltersCommand = new RelayCommand(() =>
            {
                DownloadMonitorViewModel.ApplyFilters(FilterViewModel.Filters);
                FileChangeMonitorViewModel.ApplyFilters(FilterViewModel.Filters);
            });
        }
        private void SaveState()
        {
            var state = new AppState
            {
                DownloadMonitorState = DownloadMonitorViewModel.GetState(),
                FileChangeMonitorState = FileChangeMonitorViewModel.GetState(),
                IsPanelsSwapped = IsPanelsSwapped,
                SelectedTabIndex = SelectedTabIndex,
                Filters = FilterViewModel.Filters.ToArray()
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
                SelectedTabIndex = state.SelectedTabIndex;
                FilterViewModel.Filters = new System.Collections.ObjectModel.ObservableCollection<string>(state.Filters ?? Array.Empty<string>());

                DownloadMonitorViewModel.ApplyFilters(FilterViewModel.Filters);
                FileChangeMonitorViewModel.ApplyFilters(FilterViewModel.Filters);
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}