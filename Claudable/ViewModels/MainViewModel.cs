using Claudable.Extensions;
using Claudable.Models;
using Claudable.Services;
using Claudable.Utilities;
using Claudable.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, FileSystemItem> _pathCache = new();
        private readonly object _updateLock = new object();
        private bool _isUpdating;
        private bool _isRefreshing;

        public bool IsRefreshing
        {
            get => _isRefreshing;
            set
            {
                _isRefreshing = value;
                OnPropertyChanged();
            }
        }
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
        public ICommand RefreshArtifactsCommand { get; private set; }
        public ICommand ViewArtifactCommand { get; private set; }
        public ICommand ViewProjectFileCommand { get; private set; }
        public ICommand CompareFilesCommand { get; }
        public ICommand CopyFileNameCommand { get; private set; }

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
            RefreshArtifactsCommand = new RelayCommand<ProjectFolder>(async pf => await RefreshFolderArtifacts(pf), pf => pf is ProjectFolder);
            ViewArtifactCommand = new RelayCommand<ArtifactViewModel>(ViewArtifact);
            ViewProjectFileCommand = new RelayCommand<ProjectFile>(ViewProjectFile);
            CompareFilesCommand = new RelayCommand<ProjectFile>(async pf => await DiffViewer.ShowDiffDialog(pf));
            CopyFileNameCommand = new RelayCommand<ProjectFile>(pf => Clipboard.SetText(pf.Name));
        }

        private void ViewArtifact(ArtifactViewModel artifact)
        {
            if (artifact == null) return;
            ShowViewer($"Artifact Viewer ({artifact.FileName})", artifact.Content);
        }
        private void ViewProjectFile(ProjectFile projectFile)
        {
            if (projectFile == null) return;
            ShowViewer($"Project File Viewer ({Path.GetFileName(projectFile.FullPath)})", 
                       File.ReadAllText(projectFile.FullPath));
        }
        private void ShowViewer(string title, string content)
        {
            var options = new ArtifactViewerOptions { Title = title, Content = content };
            var viewer = new ArtifactViewer(options)
            {
                Title = title,
                Owner = Application.Current.MainWindow
            };
            viewer.Show();
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
            if (_isUpdating)
                return;

            lock (_updateLock)
            {
                if (_isUpdating)
                    return;

                _isUpdating = true;
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var changes = UpdateProjectStructureRecursive(RootProjectFolder);
                        if (changes)
                        {
                            UpdateArtifactStatus();
                            ArtifactManager.UpdateAllArtifactsStatus();
                            ApplyFilters();
                            OnPropertyChanged(nameof(RootProjectFolder));
                        }
                    });
                }
                finally
                {
                    _isUpdating = false;
                }
            }
        }

        private bool UpdateProjectStructureRecursive(ProjectFolder folder)
        {
            bool ShouldExcludeEntry(Filter filter, string filePath)
            {
                var filterValue = filter.ShouldPrependProjectFolder
                    ? $"{RootProjectFolder.FullPath}{filter.Value}"
                    : filter.Value;

                return filePath.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            bool hasChanges = false;
            var currentItems = new HashSet<string>(folder.Children.Select(c => c.FullPath));

            var entries = Directory.EnumerateFileSystemEntries(folder.FullPath)
                                   .Where(filePath => !FilterViewModel.Filters.Any(filter => ShouldExcludeEntry(filter, filePath)));

            var directoryItems = new HashSet<string>(entries);

            // Remove items that no longer exist
            foreach (var itemPath in currentItems.Except(directoryItems))
            {
                var itemToRemove = folder.Children.FirstOrDefault(c => c.FullPath == itemPath);
                if (itemToRemove != null)
                {
                    folder.Children.Remove(itemToRemove);
                    _pathCache.TryRemove(itemPath, out _);
                    hasChanges = true;
                }
            }

            // Add new items
            foreach (var itemPath in directoryItems.Except(currentItems))
            {
                if (_pathCache.TryGetValue(itemPath, out var existingItem))
                {
                    folder.Children.Add(existingItem);
                    hasChanges = true;
                    continue;
                }

                var itemName = Path.GetFileName(itemPath);
                FileSystemItem newItem;

                if (Directory.Exists(itemPath))
                {
                    newItem = new ProjectFolder(itemName, itemPath, folder);
                    folder.Children.Add(newItem);
                    UpdateProjectStructureRecursive((ProjectFolder)newItem);
                }
                else
                {
                    newItem = new ProjectFile(itemName, itemPath, folder);
                    folder.Children.Add(newItem);
                }

                _pathCache.TryAdd(itemPath, newItem);
                hasChanges = true;

                //ExpandToItem(newItem);
            }

            // Recursively update existing subfolders
            foreach (var child in folder.Children.OfType<ProjectFolder>().ToList())
            {
                hasChanges |= UpdateProjectStructureRecursive(child);
            }

            // Sort children only if there were changes
            if (hasChanges)
            {
                var sortedChildren = folder.Children
                    .OrderBy(c => c is ProjectFile)
                    .ThenBy(c => c.Name)
                    .ToList();

                folder.Children.Clear();
                foreach (var child in sortedChildren)
                {
                    folder.Children.Add(child);
                }
            }

            return hasChanges;
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

        public async Task RefreshFolderArtifacts(ProjectFolder folder)
        {
            if (IsRefreshing || folder == null)
                return;

            IsRefreshing = true;
            folder.RefreshInProgress = true;

            try
            {
                var outdatedFiles = folder.GetAllProjectFiles()
                    .Where(file => file.IsTrackedAsArtifact && file.IsLocalNewer)
                    .ToList();

                int totalFiles = outdatedFiles.Count;
                int processedFiles = 0;

                foreach (var file in outdatedFiles)
                {
                    try
                    {
                        file.UntrackArtifact();
                        var content = await File.ReadAllTextAsync(file.FullPath);
                        var artifact = await WebViewManager.CreateArtifact(file.Name, content);

                        if (artifact != null)
                            file.UpdateFromArtifact(artifact);

                        processedFiles++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error refreshing artifact for {file.Name}: {ex.Message}");
                    }
                }

                // Update UI after all files are processed
                ApplyFilters();
                UpdateArtifactStatus();
            }
            finally
            {
                IsRefreshing = false;
                folder.RefreshInProgress = false;
                folder.NotifyFileStatusChanged();
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
                Filters = FilterViewModel.Filters.Select(f => f.Value).ToArray(),
                ProjectRootPath = RootProjectFolder?.FullPath,
                CurrentFilterMode = CurrentFilterMode,
                ExpandedFolders = RootProjectFolder?.GetExpandedPaths() ?? Array.Empty<string>(),
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
                FilterViewModel.Filters = new ObservableCollection<Filter>(state.Filters.Any() ? state.Filters.Select(f => new Filter(f)) : Array.Empty<Filter>());
                CurrentFilterMode = state.CurrentFilterMode;

                if (!string.IsNullOrEmpty(state.ProjectRootPath))
                {
                    LoadProjectStructure(state.ProjectRootPath);

                    if (RootProjectFolder != null)
                        RootProjectFolder.RestoreExpandedState(state.ExpandedFolders);
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