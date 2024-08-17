using Claudable.Models;
using Claudable.Services;
using Claudable.Utilities;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Claudable.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private ProjectFolder _rootProjectFolder;
        private FilterViewModel _filterViewModel;
        private DownloadManager _downloadManager;
        private ArtifactManager _artifactManager;
        private bool _isPanelsSwapped;
        private int _selectedTabIndex;
        private FileWatcher _fileWatcher;

        public ArtifactManager ArtifactManager
        {
            get => _artifactManager;
            set
            {
                _artifactManager = value;
                OnPropertyChanged();
            }
        }
        public ProjectFolder RootProjectFolder
        {
            get => _rootProjectFolder;
            set
            {
                _rootProjectFolder = value;
                ArtifactManager.RootProjectFolder = value;
                InitializeFileWatcher();
                OnPropertyChanged();
            }
        }

        public FilterViewModel FilterViewModel
        {
            get => _filterViewModel;
            set
            {
                _filterViewModel = value;
                OnPropertyChanged();
            }
        }

        public DownloadManager DownloadManager
        {
            get => _downloadManager;
            set
            {
                _downloadManager = value;
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

        public ObservableCollection<SvgArtifactViewModel> SvgArtifacts => ArtifactManager.SvgArtifacts;

        public ICommand SetProjectRootCommand { get; private set; }
        public ICommand SaveStateCommand { get; private set; }
        public ICommand LoadStateCommand { get; private set; }
        public ICommand UpdateArtifactStatusCommand { get; private set; }
        public ICommand DropSvgArtifactCommand { get; private set; }

        public MainViewModel()
        {
            FilterViewModel = new FilterViewModel();
            DownloadManager = new DownloadManager();
            ArtifactManager = new ArtifactManager();

            SetProjectRootCommand = new RelayCommand(SetProjectRoot);
            SaveStateCommand = new RelayCommand(SaveState);
            LoadStateCommand = new RelayCommand(LoadState);
            UpdateArtifactStatusCommand = new RelayCommand(UpdateArtifactStatus);
            FilterViewModel.ApplyFiltersCommand = new RelayCommand(ApplyFilters);
            DropSvgArtifactCommand = new RelayCommand<object>(DropSvgArtifact);
        }
        private void SetProjectRoot()
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Project Root Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                string rootPath = dialog.FolderName;
                RootProjectFolder = new ProjectFolder(Path.GetFileName(rootPath), rootPath);
                LoadProjectStructure(RootProjectFolder);
                ApplyFilters();
            }
        }

        private void InitializeFileWatcher()
        {
            _fileWatcher?.Dispose();
            _fileWatcher = new FileWatcher(RootProjectFolder);
        }

        private void LoadProjectStructure(ProjectFolder folder)
        {
            try
            {
                foreach (var directory in Directory.GetDirectories(folder.FullPath))
                {
                    var subFolder = new ProjectFolder(Path.GetFileName(directory), directory, folder);
                    folder.AddChild(subFolder);
                    LoadProjectStructure(subFolder);
                }

                foreach (var file in Directory.GetFiles(folder.FullPath))
                {
                    folder.AddChild(new ProjectFile(Path.GetFileName(file), file, folder));
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.WriteLine($"Unauthorized access to {folder.FullPath}: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading folder {folder.FullPath}: {ex.Message}");
            }
        }

        private void DropSvgArtifact(object parameter)
        {
            if (parameter is Tuple<SvgArtifactViewModel, FileSystemItem, string> dropInfo)
            {
                var svgArtifact = dropInfo.Item1;
                var targetItem = dropInfo.Item2;
                var fileType = dropInfo.Item3;

                ProjectFolder targetFolder;
                if (targetItem is ProjectFile projectFile)
                {
                    targetFolder = projectFile.Parent as ProjectFolder;
                }
                else
                {
                    targetFolder = targetItem as ProjectFolder;
                }

                if (targetFolder == null)
                {
                    MessageBox.Show("Invalid drop target. Please drop on a folder or file in the project structure.");
                    return;
                }

                string fileName = Path.GetFileNameWithoutExtension(svgArtifact.Name) + "." + fileType;
                string fullPath = Path.Combine(targetFolder.FullPath, fileName);

                try
                {
                    if (fileType == "png")
                    {
                        SVGRasterizer.GenerateArtifactIcon(fullPath, svgArtifact.Content);
                    }
                    else if (fileType == "ico")
                    {
                        SVGRasterizer.GenerateArtifactIcon(fullPath, svgArtifact.Content, true);
                    }

                    // Add the new file to the project structure
                    var newFile = new ProjectFile(fileName, fullPath, targetFolder);
                    targetFolder.AddChild(newFile);

                    MessageBox.Show($"File saved successfully: {fullPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
                }
            }
        }
        private void ApplyFilters()
        {
            if (RootProjectFolder != null)
            {
                RootProjectFolder.ApplyFilter(FilterViewModel.Filters.ToArray());
                OnPropertyChanged(nameof(RootProjectFolder));
            }
        }

        private void UpdateArtifactStatus()
        {
            if (RootProjectFolder != null && ArtifactManager.Artifacts != null)
            {
                UpdateArtifactStatusRecursive(RootProjectFolder);
            }
        }

        private void UpdateArtifactStatusRecursive(ProjectFolder folder)
        {
            foreach (var item in folder.Children)
            {
                if (item is ProjectFile file)
                {
                    var artifact = ArtifactManager.Artifacts.FirstOrDefault(a => a.FileName == file.Name);
                    if (artifact != null)
                    {
                        file.UpdateFromArtifact(artifact);
                    }
                    else
                    {
                        file.AssociatedArtifact = null;
                    }
                }
                else if (item is ProjectFolder subFolder)
                {
                    UpdateArtifactStatusRecursive(subFolder);
                }
            }
        }

        private void SaveState()
        {
            var state = new AppState
            {
                IsPanelsSwapped = IsPanelsSwapped,
                SelectedTabIndex = SelectedTabIndex,
                Filters = FilterViewModel.Filters.ToArray(),
                ProjectRootPath = RootProjectFolder?.FullPath
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

                IsPanelsSwapped = state.IsPanelsSwapped;
                SelectedTabIndex = state.SelectedTabIndex;
                FilterViewModel.Filters = new ObservableCollection<string>(state.Filters ?? Array.Empty<string>());

                if (!string.IsNullOrEmpty(state.ProjectRootPath))
                {
                    RootProjectFolder = new ProjectFolder(Path.GetFileName(state.ProjectRootPath), state.ProjectRootPath);
                    LoadProjectStructure(RootProjectFolder);
                }
                ApplyFilters();
                UpdateArtifactStatus();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}