using DownloadMonitor.Models;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace DownloadMonitor.ViewModels
{
    public class FileTrackingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<TrackedFile> _trackedFiles;
        private ObservableCollection<TrackedFile> _allTrackedFiles;
        private List<string> _currentFilters = new List<string>();
        private string _selectedFolder;
        private bool _hasChanges;

        public ObservableCollection<TrackedFile> TrackedFiles
        {
            get => _trackedFiles;
            set
            {
                _trackedFiles = value;
                OnPropertyChanged();
            }
        }

        public string SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        public ICommand BrowseFolderCommand { get; private set; }
        public ICommand MoveAndRenameFilesCommand { get; private set; }
        public ICommand DeleteSelectedFilesCommand { get; private set; }

        public FileTrackingViewModel()
        {
            _allTrackedFiles = new ObservableCollection<TrackedFile>();
            TrackedFiles = new ObservableCollection<TrackedFile>();
            BrowseFolderCommand = new RelayCommand(BrowseFolder);
            MoveAndRenameFilesCommand = new RelayCommand(MoveAndRenameFiles, CanMoveAndRenameFiles);
            DeleteSelectedFilesCommand = new RelayCommand(DeleteSelectedFiles, CanDeleteSelectedFiles);
        }

        private void BrowseFolder()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                SelectedFolder = dialog.FolderName;
            }
        }

        private bool CanMoveAndRenameFiles()
        {
            return !string.IsNullOrEmpty(SelectedFolder) && TrackedFiles.Any();
        }

        private void MoveAndRenameFiles()
        {
            foreach (var file in TrackedFiles.ToList())
            {
                string destinationPath = Path.Combine(SelectedFolder, file.PascalCaseFileName);

                try
                {
                    File.Move(file.FullPath, destinationPath);
                    RemoveFile(file.FullPath);
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Error moving file {file.FileName}: {ex.Message}");
                }
            }

            System.Windows.MessageBox.Show("Files moved and renamed successfully!");
        }

        private bool CanDeleteSelectedFiles()
        {
            return TrackedFiles.Any(f => f.IsSelected);
        }

        private void DeleteSelectedFiles()
        {
            var selectedFiles = TrackedFiles.Where(f => f.IsSelected).ToList();
            if (selectedFiles.Count == 0)
            {
                System.Windows.MessageBox.Show("Please select files to delete.");
                return;
            }

            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete {selectedFiles.Count} file(s)?", "Confirm Delete", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
            if (result == System.Windows.MessageBoxResult.Yes)
            {
                foreach (var file in selectedFiles)
                {
                    try
                    {
                        File.Delete(file.FullPath);
                        RemoveFile(file.FullPath);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.MessageBox.Show($"Error deleting file {file.FileName}: {ex.Message}");
                    }
                }
            }
        }

        public void ApplyFilters(IEnumerable<string> filters)
        {
            _currentFilters = filters.Select(f => f.ToLowerInvariant()).ToList();

            TrackedFiles = new ObservableCollection<TrackedFile>(
                _allTrackedFiles.Where(file => !IsExcludedByFilters(file))
            );
        }
        private bool IsExcludedByFilters(TrackedFile file)
        {
            string fullPath = file.FullPath.ToLowerInvariant();
            return _currentFilters.Any(filter => fullPath.Contains(filter));
        }

        public void AddFile(string filePath)
        {
            var newFile = new TrackedFile(filePath);
            _allTrackedFiles.Add(newFile);
            if (!IsExcludedByFilters(newFile))
            {
                TrackedFiles.Add(newFile);
            }
            HasChanges = true;
        }

        public void RemoveFile(string filePath)
        {
            var fileToRemove = _allTrackedFiles.FirstOrDefault(f => f.FullPath == filePath);
            if (fileToRemove != null)
            {
                _allTrackedFiles.Remove(fileToRemove);
                TrackedFiles.Remove(fileToRemove);
                HasChanges = true;
            }
        }

        public void UpdateFile(string filePath)
        {
            var existingFile = _allTrackedFiles.FirstOrDefault(f => f.FullPath == filePath);
            if (existingFile != null)
            {
                existingFile.LastModified = File.GetLastWriteTime(filePath);
                if (!IsExcludedByFilters(existingFile) && !TrackedFiles.Contains(existingFile))
                {
                    TrackedFiles.Add(existingFile);
                }
                else if (IsExcludedByFilters(existingFile) && TrackedFiles.Contains(existingFile))
                {
                    TrackedFiles.Remove(existingFile);
                }
                HasChanges = true;
            }
            else
            {
                AddFile(filePath);
            }
        }

        public void Clear()
        {
            _allTrackedFiles.Clear();
            TrackedFiles.Clear();
            HasChanges = false;
        }

        public FileTrackingState GetState()
        {
            return new FileTrackingState
            {
                TrackedFiles = _allTrackedFiles.ToList(),
                SelectedFolder = SelectedFolder,
            };
        }

        public void SetState(FileTrackingState state)
        {
            _allTrackedFiles = new ObservableCollection<TrackedFile>(state.TrackedFiles);
            SelectedFolder = state.SelectedFolder;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}