using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DownloadMonitor.Models;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace DownloadMonitor.ViewModels
{
    public class FileTrackingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private ObservableCollection<TrackedFile> _trackedFiles;
        public ObservableCollection<TrackedFile> TrackedFiles
        {
            get => _trackedFiles;
            set
            {
                _trackedFiles = value;
                OnPropertyChanged();
            }
        }

        private string _selectedFolder;
        public string SelectedFolder
        {
            get => _selectedFolder;
            set
            {
                _selectedFolder = value;
                OnPropertyChanged();
            }
        }

        public ICommand BrowseFolderCommand { get; private set; }
        public ICommand MoveAndRenameFilesCommand { get; private set; }
        public ICommand DeleteSelectedFilesCommand { get; private set; }

        public FileTrackingViewModel()
        {
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
                    // Handle or log the exception
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

        public void AddFile(string filePath)
        {
            if (!TrackedFiles.Any(f => f.FullPath == filePath))
            {
                TrackedFiles.Add(new TrackedFile(filePath));
            }
        }

        public void RemoveFile(string filePath)
        {
            var fileToRemove = TrackedFiles.FirstOrDefault(f => f.FullPath == filePath);
            if (fileToRemove != null)
            {
                TrackedFiles.Remove(fileToRemove);
            }
        }

        public void UpdateFile(string filePath)
        {
            var existingFile = TrackedFiles.FirstOrDefault(f => f.FullPath == filePath);
            if (existingFile != null)
            {
                existingFile.LastModified = File.GetLastWriteTime(filePath);
            }
            else
            {
                AddFile(filePath);
            }
        }

        public void Clear()
        {
            TrackedFiles.Clear();
        }

        public FileTrackingState GetState()
        {
            return new FileTrackingState
            {
                TrackedFiles = TrackedFiles.ToList(),
                SelectedFolder = SelectedFolder
            };
        }

        public void SetState(FileTrackingState state)
        {
            TrackedFiles = new ObservableCollection<TrackedFile>(state.TrackedFiles);
            SelectedFolder = state.SelectedFolder;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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