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
        private MainViewModel _viewModel;
        private FileSystemWatcher _watcher;
        private FileSystemWatcher _changeWatcher;
        private Point _startPoint;
        private bool _isDragging = false;

        public FileTrackingViewModel DownloadMonitorViewModel => _viewModel.DownloadMonitorViewModel;
        public FileTrackingViewModel FileChangeMonitorViewModel => _viewModel.FileChangeMonitorViewModel;
        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
            InitializeFileWatcher();
            InitializeWebView2();
            _viewModel.LoadStateCommand.Execute(null);
            // Load window size
            LoadWindowSize();
            ApplySwapState();
        }
        private void ApplySwapState()
        {
            if (_viewModel.IsPanelsSwapped)
            {
                SwapPanels(false);
            }
        }
        private void SwapPanels_Click(object sender, RoutedEventArgs e)
        {
            SwapPanels(true);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            // Save window size
            SaveWindowSize();
            _viewModel.SaveStateCommand.Execute(null);
        }
        private void SaveWindowSize()
        {
            var settings = new WindowSettings
            {
                Width = this.Width,
                Height = this.Height,
                Left = this.Left,
                Top = this.Top
            };

            string json = System.Text.Json.JsonSerializer.Serialize(settings);
            File.WriteAllText("windowsettings.json", json);
        }
        private void LoadWindowSize()
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
                }
            }
        }

        private async void InitializeWebView2()
        {
            var userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FileMonitor", "WebView2Data");
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, userDataFolder);

            await ClaudeWebView.EnsureCoreWebView2Async(webView2Environment);
            ClaudeWebView.Source = new Uri("https://claude.ai");

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
        #region FileWatcher
        private void InitializeFileWatcher()
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _watcher = new FileSystemWatcher(downloadsPath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
                Filter = "*.*"
            };

            _watcher.Created += OnFileCreated;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Deleted += OnFileDeleted;
            _watcher.EnableRaisingEvents = true;
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

        #region File Events
        private void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (!IsFileHidden(e.FullPath))
                {
                    DownloadMonitorViewModel.AddFile(e.FullPath);
                }
            });
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (sender == _watcher)
                {
                    DownloadMonitorViewModel.RemoveFile(e.OldFullPath);
                    if (!IsFileHidden(e.FullPath))
                    {
                        DownloadMonitorViewModel.AddFile(e.FullPath);
                    }
                }
                else if (sender == _changeWatcher)
                {
                    FileChangeMonitorViewModel.RemoveFile(e.OldFullPath);
                    if (!IsFileHidden(e.FullPath))
                    {
                        FileChangeMonitorViewModel.AddFile(e.FullPath);
                    }
                }
            });
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
                if (File.Exists(e.FullPath) && !IsFileHidden(e.FullPath) && !IsTempFile(e.FullPath))
                {
                    FileChangeMonitorViewModel.UpdateFile(e.FullPath);
                }
            });
        }

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
        #endregion

        #region UI Events

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
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(selectedFile.FullPath) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error opening file: {ex.Message}");
                }
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
                foreach (var file in selectedFiles)
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
        }

        private void StartDrag(ListBox listBox)
        {
            var selectedFiles = listBox.SelectedItems.Cast<TrackedFile>().ToList();

            if (selectedFiles.Count > 0)
            {
                var fileList = selectedFiles.Select(f => f.FullPath).ToList();

                if (fileList.Count > 0)
                {
                    _isDragging = true;
                    DataObject data = new DataObject(DataFormats.FileDrop, fileList.ToArray());
                    DragDrop.DoDragDrop(listBox, data, DragDropEffects.Copy);
                    _isDragging = false;

                    // Remove the dragged items from the ListBox and ViewModel
                    foreach (var file in selectedFiles)
                    {
                        FileChangeMonitorViewModel.RemoveFile(file.FullPath);
                    }
                }
            }
        }
        private void SwapPanels(bool updateViewModel)
        {
            // Swap the contents of the panels
            var leftContent = LeftPanel.Children[0];
            var rightContent = RightPanel.Children[0];

            LeftPanel.Children.Clear();
            RightPanel.Children.Clear();

            LeftPanel.Children.Add(rightContent);
            RightPanel.Children.Add(leftContent);

            // Swap the Width and MinWidth of the ColumnDefinitions
            var leftColumn = MainGrid.ColumnDefinitions[0];
            var rightColumn = MainGrid.ColumnDefinitions[2];

            var tempWidth = leftColumn.Width;
            var tempMinWidth = leftColumn.MinWidth;

            leftColumn.Width = rightColumn.Width;
            leftColumn.MinWidth = rightColumn.MinWidth;

            rightColumn.Width = tempWidth;
            rightColumn.MinWidth = tempMinWidth;

            if (updateViewModel)
            {
                _viewModel.IsPanelsSwapped = !_viewModel.IsPanelsSwapped;
            }
        }
        #endregion


    }
}