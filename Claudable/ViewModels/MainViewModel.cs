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
        private ProjectAssociationService _projectAssociationService;
        private string _currentProjectUrl;
        private FilterMode _currentFilterMode;
        private WebViewManager _webViewManager;

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

        public FilterMode CurrentFilterMode
        {
            get => _currentFilterMode;
            set
            {
                if (_currentFilterMode != value)
                {
                    _currentFilterMode = value;
                    OnPropertyChanged();
                    ApplyFilters();
                }
            }
        }
        public WebViewManager WebViewManager
        {
            get => _webViewManager;
            set
            {
                _webViewManager = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<SvgArtifactViewModel> SvgArtifacts => ArtifactManager.SvgArtifacts;

        public ICommand SetProjectRootCommand { get; private set; }
        public ICommand SaveStateCommand { get; private set; }
        public ICommand LoadStateCommand { get; private set; }
        public ICommand UpdateArtifactStatusCommand { get; private set; }
        public ICommand DropSvgArtifactCommand { get; private set; }
        public ICommand DoubleClickTrackedArtifactCommand { get; private set; }

        public MainViewModel()
        {
            FilterViewModel = new FilterViewModel();
            DownloadManager = new DownloadManager();
            ArtifactManager = new ArtifactManager();
            _projectAssociationService = new ProjectAssociationService();

            SetProjectRootCommand = new RelayCommand(SetProjectRoot);
            SaveStateCommand = new RelayCommand(SaveState);
            LoadStateCommand = new RelayCommand(LoadState);
            UpdateArtifactStatusCommand = new RelayCommand(UpdateArtifactStatus);
            DropSvgArtifactCommand = new RelayCommand<object>(DropSvgArtifact);
            DoubleClickTrackedArtifactCommand = new RelayCommand<ProjectFile>(OnDoubleClickTrackedArtifact);
        }

        private void OnDoubleClickTrackedArtifact(ProjectFile projectFile)
        {
            if (projectFile?.AssociatedArtifact != null)
            {
                // Implement the logic to scroll to and highlight the corresponding item in the Project Knowledge section
                //_ = WebViewManager.ScrollToProjectKnowledgeItem(projectFile.AssociatedArtifact.ProjectKnowledgeId);
            }
        }
        private void InitializeFileWatcher()
        {
            _fileWatcher?.Dispose();
            if (RootProjectFolder == null || string.IsNullOrEmpty(RootProjectFolder.FullPath) || !Directory.Exists(RootProjectFolder.FullPath))
                return;

            _fileWatcher = new FileWatcher(RootProjectFolder, UpdateProjectStructure);
        }

        private void UpdateProjectStructure()
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProjectStructureRecursive(RootProjectFolder);
                UpdateArtifactStatus();
                ApplyFilters();
                OnPropertyChanged(nameof(RootProjectFolder));
            });
        }

        private void UpdateProjectStructureRecursive(ProjectFolder folder)
        {
            var currentItems = new HashSet<string>(folder.Children.Select(c => c.Name));
            var entries = Directory.EnumerateFileSystemEntries(folder.FullPath)
                .Where(filePath => !FilterViewModel.Filters.Any(filter => filePath.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0))
                .Select(Path.GetFileName);
            var directoryItems = new HashSet<string>(entries);

            // Remove items that no longer exist
            var itemsToRemove = currentItems.Except(directoryItems).ToList();
            foreach (var itemName in itemsToRemove)
            {
                var itemToRemove = folder.Children.FirstOrDefault(c => c.Name == itemName);
                if (itemToRemove != null)
                {
                    folder.Children.Remove(itemToRemove);
                }
            }

            // Add new items
            var itemsToAdd = directoryItems.Except(currentItems);
            foreach (var itemName in itemsToAdd)
            {
                var fullPath = Path.Combine(folder.FullPath, itemName);
                FileSystemItem newItem;
                if (Directory.Exists(fullPath))
                {
                    newItem = new ProjectFolder(itemName, fullPath, folder);
                    folder.Children.Add(newItem);
                    UpdateProjectStructureRecursive((ProjectFolder)newItem);
                }
                else
                {
                    newItem = new ProjectFile(itemName, fullPath, folder);
                    folder.Children.Add(newItem);
                }
                ExpandToItem(newItem);
            }

            // Recursively update existing subfolders
            foreach (var child in folder.Children.OfType<ProjectFolder>())
            {
                UpdateProjectStructureRecursive(child);
            }

            // Sort children
            var sortedChildren = folder.Children.OrderBy(c => c is ProjectFile).ThenBy(c => c.Name).ToList();
            folder.Children.Clear();
            foreach (var child in sortedChildren)
            {
                folder.Children.Add(child);
            }
        }

        private void ExpandToItem(FileSystemItem item)
        {
            var parent = item.Parent as ProjectFolder;
            while (parent != null)
            {
                parent.IsExpanded = true;
                parent = parent.Parent as ProjectFolder;
            }
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
                LoadProjectStructure(rootPath);

                if (!string.IsNullOrEmpty(_currentProjectUrl))
                {
                    _projectAssociationService.AddOrUpdateProjectData(_currentProjectUrl, rootPath, ArtifactManager.Artifacts.ToList());
                }
            }
        }

        public void HandleProjectChanged(string projectUrl)
        {
            _currentProjectUrl = projectUrl;

            // Clear existing project structure
            RootProjectFolder = null;
            OnPropertyChanged(nameof(RootProjectFolder));

            var projectData = _projectAssociationService.GetProjectDataByUrl(projectUrl);

            if (projectData != null)
            {
                LoadProjectStructure(projectData.ProjectAssociation.LocalFolderPath);
                UpdateArtifactStatus();
                ApplyFilters();
            }
            else
            {
                SetProjectRoot();
            }
        }

        private void LoadProjectStructure(string rootPath)
        {
            if (RootProjectFolder == null || RootProjectFolder.FullPath != rootPath)
            {
                RootProjectFolder = new ProjectFolder(Path.GetFileName(rootPath), rootPath);
            }
            UpdateProjectStructureRecursive(RootProjectFolder);
            ApplyFilters();
            OnPropertyChanged(nameof(RootProjectFolder));
        }

        private void ApplyFilters()
        {
            if (RootProjectFolder != null)
            {
                var files = EnumerateProjectFiles().ToList();
                var tracked = files.Where(f => f.IsTrackedAsArtifact).ToList();
                var newer = tracked.Where(f => f.IsLocalNewer).ToList();

                RootProjectFolder.ApplyFilter(CurrentFilterMode);
                OnPropertyChanged(nameof(RootProjectFolder));
            }
        }

        IEnumerable<ProjectFile> EnumerateProjectFiles(ProjectFolder projectFolder = null)
        {
            if (projectFolder == null)
                projectFolder = RootProjectFolder;

            foreach (var child in projectFolder.Children)
            {
                switch (child)
                {
                    case ProjectFile file:
                        yield return file;
                        break;
                    case ProjectFolder folder:
                        foreach (var descendant in EnumerateProjectFiles(folder))
                            yield return descendant;
                        break;
                }
            }
        }

        private void UpdateArtifactStatus()
        {
            if (RootProjectFolder != null && ArtifactManager.Artifacts != null)
            {
                UpdateArtifactStatusRecursive(RootProjectFolder);
                if (!string.IsNullOrEmpty(_currentProjectUrl))
                    _projectAssociationService.UpdateArtifacts(_currentProjectUrl, ArtifactManager.Artifacts.ToList());
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
                    ExpandToItem(newFile);

                    MessageBox.Show($"File saved successfully: {fullPath}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error saving file: {ex.Message}");
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
                ProjectRootPath = RootProjectFolder?.FullPath,
                CurrentFilterMode = CurrentFilterMode
            };

            string json = JsonConvert.SerializeObject(state);
            File.WriteAllText("appstate.json", json);

            // Save current project data
            if (!string.IsNullOrEmpty(_currentProjectUrl) && RootProjectFolder != null)
            {
                _projectAssociationService.AddOrUpdateProjectData(_currentProjectUrl, RootProjectFolder.FullPath, ArtifactManager.Artifacts.ToList());
            }
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
                CurrentFilterMode = state.CurrentFilterMode;

                if (!string.IsNullOrEmpty(state.ProjectRootPath))
                {
                    LoadProjectStructure(state.ProjectRootPath);
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