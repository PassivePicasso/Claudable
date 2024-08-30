using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Claudable.ViewModels
{
    public class FilterViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<string> _filters;
        public ObservableCollection<string> Filters
        {
            get => _filters;
            set
            {
                _filters = value;
                OnPropertyChanged();
            }
        }

        private string _newFilter;
        public string NewFilter
        {
            get => _newFilter;
            set
            {
                _newFilter = value;
                OnPropertyChanged();
            }
        }

        public ICommand AddFilterCommand { get; private set; }
        public ICommand RemoveFilterCommand { get; private set; }

        public FilterViewModel()
        {
            Filters = new ObservableCollection<string>();
            AddFilterCommand = new RelayCommand(AddFilter, CanAddFilter);
            RemoveFilterCommand = new RelayCommand<string>(RemoveFilter);
        }

        private bool CanAddFilter()
        {
            return !string.IsNullOrWhiteSpace(NewFilter);
        }

        private void AddFilter()
        {
            if (CanAddFilter())
            {
                Filters.Add(NewFilter.Trim());
                NewFilter = string.Empty;
            }
        }

        private void RemoveFilter(string filter)
        {
            Filters.Remove(filter);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
