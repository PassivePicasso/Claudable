using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Claudable.ViewModels
{
  using Models;

  public class FilterViewModel : INotifyPropertyChanged
    {
        public static readonly char[] FolderChar = ['/', '\\'];

        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<Filter> _filters;
        public ObservableCollection<Filter> Filters
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
            Filters             = new ObservableCollection<Filter>();
            AddFilterCommand    = new RelayCommand(AddFilter, CanAddFilter);
            RemoveFilterCommand = new RelayCommand<Filter>(RemoveFilter);
        }

        private bool CanAddFilter()
        {
            return !string.IsNullOrWhiteSpace(NewFilter) && !(NewFilter.Length == 1 && FolderChar.Any(fc => fc == NewFilter[0]));
        }

        private void AddFilter()
        {
            if (CanAddFilter())
            {
                var filterValue = NewFilter.Trim().Replace('/', '\\');

                var newFilter = new Filter(filterValue);

                Filters.Add(newFilter);
                NewFilter = string.Empty;
            }
        }

        private void RemoveFilter(Filter filter)
        {
            Filters.Remove(filter);
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
