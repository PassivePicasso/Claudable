using DownloadMonitor.Models;
using DownloadMonitor.ViewModels;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DownloadMonitor
{
    public partial class MainWindow : Window
    {
        #region Fields
        private MainViewModel _viewModel;
        private FileSystemWatcher _downloadWatcher;
        private FileSystemWatcher _changeWatcher;
        private Point _startPoint;
        private bool _isDragging = false;
        private string _lastVisitedUrl = "https://claude.ai";
        #endregion

        #region Properties
        public FileTrackingViewModel DownloadMonitorViewModel => _viewModel.DownloadMonitorViewModel;
        public FileTrackingViewModel FileChangeMonitorViewModel => _viewModel.FileChangeMonitorViewModel;
        #endregion

        #region Constructor
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            _viewModel.LoadStateCommand.Execute(null);
            LoadWindowSettings();
            InitializeFileWatcher();
            InitializeWebView2();
            ApplySwapState();
        }
        #endregion

        #region Initialization Methods
        private async void InitializeWebView2()
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileMonitor", "WebView2Data");
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            await ClaudeWebView.EnsureCoreWebView2Async(webView2Environment);
            ClaudeWebView.Source = new Uri(_lastVisitedUrl);

            ConfigureWebView2Settings();
            SetupWebViewEventHandlers();
        }

        private void ConfigureWebView2Settings()
        {
            // Enable caching
            ClaudeWebView.CoreWebView2.Settings.AreDefaultScriptDialogsEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsScriptEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.AreDevToolsEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;

            // Enable cookies and local storage
            ClaudeWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsPinchZoomEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsSwipeNavigationEnabled = true;

            // Persist user data
            ClaudeWebView.CoreWebView2.Settings.IsGeneralAutofillEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsPasswordAutosaveEnabled = true;
            ClaudeWebView.CoreWebView2.Settings.IsStatusBarEnabled = true;
        }

        private void SetupWebViewEventHandlers()
        {
            ClaudeWebView.SourceChanged += ClaudeWebView_SourceChanged;
        }

        private void ClaudeWebView_SourceChanged(object? sender, CoreWebView2SourceChangedEventArgs e)
        {
            _lastVisitedUrl = ClaudeWebView.Source.ToString();
            SaveWindowSettings();
        }

        private void InitializeFileWatcher()
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _downloadWatcher = new FileSystemWatcher(downloadsPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*"
            };

            _downloadWatcher.Created += OnFileCreated;
            _downloadWatcher.Renamed += OnFileRenamed;
            _downloadWatcher.Deleted += OnFileDeleted;
            _downloadWatcher.EnableRaisingEvents = true;
        }

        private void InitializeChangeWatcher(string path)
        {
            if (_changeWatcher != null)
            {
                _changeWatcher.Dispose();
            }

            _changeWatcher = new FileSystemWatcher(path)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*",
                IncludeSubdirectories = true
            };

            _changeWatcher.Changed += OnFileChanged;
            _changeWatcher.Created += OnFileChanged;
            _changeWatcher.Renamed += OnFileRenamed;
            _changeWatcher.EnableRaisingEvents = true;
        }
        #endregion

        #region Window State Management
        private void ApplySwapState()
        {
            if (_viewModel.IsPanelsSwapped)
            {
                SwapPanels(false);
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            SaveWindowSettings();
            _viewModel.SaveStateCommand.Execute(null);
        }

        private void SaveWindowSettings()
        {
            double totalWidth = LeftColumn.Width.Value + RightColumn.Width.Value;
            double leftRatio = LeftColumn.Width.Value / totalWidth;

            var settings = new WindowSettings
            {
                Width = this.Width,
                Height = this.Height,
                Left = this.Left,
                Top = this.Top,
                LeftColumnRatio = leftRatio,
                IsPanelsSwapped = _viewModel.IsPanelsSwapped,
                LastVisitedUrl = _lastVisitedUrl
            };

            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(settings);
                File.WriteAllText("windowsettings.json", json);
            }
            catch (Exception ex)
            {
            }
        }

        private void LoadWindowSettings()
        {
            if (File.Exists("windowsettings.json"))
            {
                string json = File.ReadAllText("windowsettings.json");
                var settings = System.Text.Json.JsonSerializer.Deserialize<WindowSettings>(json);

                if (settings != null)
                {
                    this.Width = settings.Width;
                    this.Height = settings.Height;
                    this.Left = settings.Left;
                    this.Top = settings.Top;

                    _lastVisitedUrl = settings.LastVisitedUrl ?? "https://claude.ai";

                    if (settings.IsPanelsSwapped != _viewModel.IsPanelsSwapped)
                    {
                        SwapPanels(false);
                    }

                    ApplyColumnRatios(settings.LeftColumnRatio);
                }
            }
        }

        private void ApplyColumnRatios(double leftRatio)
        {
            LeftColumn.Width = new GridLength(leftRatio, GridUnitType.Star);
            RightColumn.Width = new GridLength(1 - leftRatio, GridUnitType.Star);
        }

        #endregion

        #region File Event Handlers
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsFileHidden(e.FullPath) && !IsUnderDotFolder(e.FullPath))
                {
                    DownloadMonitorViewModel.AddFile(e.FullPath);
                }
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (sender == _downloadWatcher)
                {
                    HandleDownloadFileRename(e);
                }
                else if (sender == _changeWatcher)
                {
                    HandleChangeFileRename(e);
                }
            });
        }

        private void HandleDownloadFileRename(RenamedEventArgs e)
        {
            DownloadMonitorViewModel.RemoveFile(e.OldFullPath);
            if (!IsFileHidden(e.FullPath) && !IsUnderDotFolder(e.FullPath))
            {
                DownloadMonitorViewModel.AddFile(e.FullPath);
            }
        }

        private void HandleChangeFileRename(RenamedEventArgs e)
        {
            FileChangeMonitorViewModel.RemoveFile(e.OldFullPath);
            if (!IsFileHidden(e.FullPath) && !IsUnderDotFolder(e.FullPath))
            {
                FileChangeMonitorViewModel.AddFile(e.FullPath);
            }
        }

        private void OnFileDeleted(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                DownloadMonitorViewModel.RemoveFile(e.FullPath);
            });
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (File.Exists(e.FullPath) && !IsFileHidden(e.FullPath) && !IsTempFile(e.FullPath) && !IsUnderDotFolder(e.FullPath))
                {
                    FileChangeMonitorViewModel.UpdateFile(e.FullPath);
                }
            });
        }
        #endregion

        #region UI Event Handlers
        private void SwapPanels_Click(object sender, RoutedEventArgs e)
        {
            SwapPanels(true);
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (Button)sender;
            var trackedFile = (TrackedFile)button.DataContext;

            if (DownloadMonitorViewModel.TrackedFiles.Any(f => f.FullPath == trackedFile.FullPath))
            {
                DownloadMonitorViewModel.RemoveFile(trackedFile.FullPath);
            }
            else if (FileChangeMonitorViewModel.TrackedFiles.Any(f => f.FullPath == trackedFile.FullPath))
            {
                FileChangeMonitorViewModel.RemoveFile(trackedFile.FullPath);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Destination Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                DownloadMonitorViewModel.SelectedFolder = dialog.FolderName;
            }
        }

        private void BrowseMonitoredFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Monitor"
            };

            if (dialog.ShowDialog() == true)
            {
                FileChangeMonitorViewModel.SelectedFolder = dialog.FolderName;
                InitializeChangeWatcher(FileChangeMonitorViewModel.SelectedFolder);
                FileChangeMonitorViewModel.Clear();
            }
        }

        private void MoveAndRenameButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DownloadMonitorViewModel.SelectedFolder))
            {
                MessageBox.Show("Please select a destination folder first.");
                return;
            }

            MoveAndRenameFiles();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedFiles();
        }

        private void FileListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                DeleteSelectedFiles();
            }
        }

        private void ChangedFilesListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;
            var selectedFile = listBox.SelectedItem as TrackedFile;

            if (selectedFile != null)
            {
                OpenFile(selectedFile.FullPath);
            }
        }

        private void ChangedFilesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void ChangedFilesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && !_isDragging)
            {
                Point position = e.GetPosition(null);

                if (Math.Abs(position.X - _startPoint.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(position.Y - _startPoint.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    StartDrag(sender as ListBox);
                }
            }
        }
        #endregion

        #region Helper Methods
        private bool IsTempFile(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".TMP", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsFileHidden(string filePath)
        {
            try
            {
                var attr = File.GetAttributes(filePath);
                return (attr & FileAttributes.Hidden) == FileAttributes.Hidden;
            }
            catch
            {
                return true;
            }
        }

        private bool IsUnderDotFolder(string filePath)
        {
            string[] pathParts = filePath.Split(Path.DirectorySeparatorChar);
            return pathParts.Any(part => part.StartsWith(".") && part.Length > 1);
        }
        private void DeleteSelectedFiles()
        {
            var selectedFiles = FileListBox.SelectedItems.Cast<TrackedFile>().ToList();
            if (selectedFiles.Count == 0)
            {
                MessageBox.Show("Please select files to delete.");
                return;
            }

            var result = MessageBox.Show($"Are you sure you want to delete {selectedFiles.Count} file(s)?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                DeleteFiles(selectedFiles);
            }
        }

        private void DeleteFiles(List<TrackedFile> files)
        {
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file.FullPath);
                    DownloadMonitorViewModel.RemoveFile(file.FullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error deleting file {file.FileName}: {ex.Message}");
                }
            }
        }

        private void StartDrag(ListBox listBox)
        {
            var selectedFiles = listBox.SelectedItems.Cast<TrackedFile>().ToList();

            if (selectedFiles.Count > 0)
            {
                var fileList = selectedFiles.Select(f => f.FullPath).ToList();

                if (fileList.Count > 0)
                {
                    PerformDragDrop(listBox, fileList);
                }
            }
        }

        private void PerformDragDrop(ListBox listBox, List<string> fileList)
        {
            _isDragging = true;
            DataObject data = new DataObject(DataFormats.FileDrop, fileList.ToArray());
            DragDrop.DoDragDrop(listBox, data, DragDropEffects.Copy);
            _isDragging = false;

            RemoveDraggedFiles(fileList);
        }

        private void RemoveDraggedFiles(List<string> fileList)
        {
            foreach (var filePath in fileList)
            {
                FileChangeMonitorViewModel.RemoveFile(filePath);
            }
        }

        private void SwapPanels(bool updateViewModel)
        {
            SwapPanelContents();

            // Swap column ratios
            double leftRatio = LeftColumn.ActualWidth / (LeftColumn.ActualWidth + RightColumn.ActualWidth);
            if (!double.IsNaN(leftRatio))
                ApplyColumnRatios(1 - leftRatio);

            if (updateViewModel)
            {
                _viewModel.IsPanelsSwapped = !_viewModel.IsPanelsSwapped;
            }

            // Save the new state immediately
            SaveWindowSettings();
        }

        private void SwapPanelContents()
        {
            var leftContent = LeftPanel.Children[0];
            var rightContent = RightPanel.Children[0];

            LeftPanel.Children.Clear();
            RightPanel.Children.Clear();

            LeftPanel.Children.Add(rightContent);
            RightPanel.Children.Add(leftContent);
        }

        private void SwapColumnWidths()
        {
            var leftColumn = MainGrid.ColumnDefinitions[0];
            var rightColumn = MainGrid.ColumnDefinitions[2];

            var tempWidth = leftColumn.Width;
            leftColumn.Width = rightColumn.Width;
            rightColumn.Width = tempWidth;

            AdjustColumnWidths(leftColumn, rightColumn);
        }

        private void AdjustColumnWidths(ColumnDefinition leftColumn, ColumnDefinition rightColumn)
        {
            double totalWidth = leftColumn.ActualWidth + rightColumn.ActualWidth;
            double newLeftRatio = rightColumn.ActualWidth / totalWidth;
            if (double.IsNaN(newLeftRatio)) return;

            double newRightRatio = 1 - newLeftRatio;

            leftColumn.Width = new GridLength(newLeftRatio, GridUnitType.Star);
            rightColumn.Width = new GridLength(newRightRatio, GridUnitType.Star);

            EnsureMinimumWidths(leftColumn, rightColumn);
        }

        private void EnsureMinimumWidths(ColumnDefinition leftColumn, ColumnDefinition rightColumn)
        {
            if (leftColumn.ActualWidth < leftColumn.MinWidth)
            {
                leftColumn.Width = new GridLength(leftColumn.MinWidth, GridUnitType.Pixel);
                rightColumn.Width = new GridLength(1, GridUnitType.Star);
            }
            else if (rightColumn.ActualWidth < rightColumn.MinWidth)
            {
                rightColumn.Width = new GridLength(rightColumn.MinWidth, GridUnitType.Pixel);
                leftColumn.Width = new GridLength(1, GridUnitType.Star);
            }
        }

        private void MoveAndRenameFiles()
        {
            foreach (var file in DownloadMonitorViewModel.TrackedFiles.ToList())
            {
                string destinationPath = Path.Combine(DownloadMonitorViewModel.SelectedFolder, file.PascalCaseFileName);

                try
                {
                    File.Move(file.FullPath, destinationPath);
                    DownloadMonitorViewModel.RemoveFile(file.FullPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file {file.FileName}: {ex.Message}");
                }
            }

            MessageBox.Show("Files moved and renamed successfully!");
        }

        private void OpenFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening file: {ex.Message}");
            }
        }
        #endregion
    }
}