using Claudable.Models;
using Claudable.Services;
using Claudable.ViewModels;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Claudable
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly FileWatcherService _fileWatcherService;
        private readonly WebViewManager _webViewManager;
        private readonly WindowStateManager _windowStateManager;
        private readonly IDialogService _dialogService;
        private Point _startPoint;
        private bool _isLeftPanelExpanded = true;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _dialogService = new DialogService();
            _webViewManager = new WebViewManager(ClaudeWebView, "https://claude.ai");
            _windowStateManager = new WindowStateManager(this, _viewModel, _webViewManager);
            _fileWatcherService = new FileWatcherService(
                OnFileCreated,
                OnFileChanged,
                OnFileRenamed,
                OnFileDeleted
            );

            InitializeAsync();
        }

        private async void InitializeAsync()
        {
            await _webViewManager.InitializeAsync();
            _windowStateManager.LoadState();
            _viewModel.LoadStateCommand.Execute(null);
            InitializeFileWatchers();
            ApplyViewModelState();

            // Initialize DownloadManager with WebView2
            _viewModel.DownloadManager.Initialize(ClaudeWebView.CoreWebView2);
        }

        private void InitializeFileWatchers()
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            _fileWatcherService.InitializeDownloadWatcher(downloadsPath);
            _fileWatcherService.InitializeChangeWatcher(_viewModel.FileChangeMonitorViewModel.SelectedFolder);
        }

        private void ApplyViewModelState()
        {
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

        private void SwapPanels(bool updateViewModel)
        {
            SwapPanelContents();
            SwapColumnWidths();

            if (updateViewModel)
            {
                _viewModel.IsPanelsSwapped = !_viewModel.IsPanelsSwapped;
            }

            _windowStateManager.SaveState();
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

        private void BrowseMonitoredFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog
            {
                Title = "Select Folder to Monitor"
            };

            if (dialog.ShowDialog() == true)
            {
                _viewModel.FileChangeMonitorViewModel.SelectedFolder = dialog.FolderName;
                _fileWatcherService.InitializeChangeWatcher(_viewModel.FileChangeMonitorViewModel.SelectedFolder);
                _viewModel.FileChangeMonitorViewModel.Clear();
            }
        }

        private void FileListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                _viewModel.FileChangeMonitorViewModel.DeleteSelectedFilesCommand.Execute(null);
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

        private void OpenFile(string filePath)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                _dialogService.ShowError($"Error opening file: {ex.Message}");
            }
        }

        private void OnFileCreated(string filePath)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.FileChangeMonitorViewModel.AddFile(filePath);
            });
        }

        private void OnFileChanged(string filePath)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.FileChangeMonitorViewModel.UpdateFile(filePath);
            });
        }

        private void OnFileRenamed(string oldPath, string newPath)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.FileChangeMonitorViewModel.RemoveFile(oldPath);
                _viewModel.FileChangeMonitorViewModel.AddFile(newPath);
            });
        }

        private void OnFileDeleted(string filePath)
        {
            Dispatcher.Invoke(() =>
            {
                _viewModel.FileChangeMonitorViewModel.RemoveFile(filePath);
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _windowStateManager.SaveState();
            _viewModel.SaveStateCommand.Execute(null);
            _fileWatcherService.Dispose();
        }

        private void DownloadItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void DownloadItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    ListBox listBox = sender as ListBox;
                    ListBoxItem listBoxItem = FindAncestor<ListBoxItem>((DependencyObject)e.OriginalSource);

                    if (listBoxItem != null)
                    {
                        DownloadItem downloadItem = (DownloadItem)listBox.ItemContainerGenerator.ItemFromContainer(listBoxItem);

                        DataObject dragData = new DataObject("DownloadItem", downloadItem);
                        DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);
                    }
                }
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T)
                {
                    return (T)current;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);
            return null;
        }

        private void ProjectFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("DownloadItem"))
            {
                DownloadItem downloadItem = e.Data.GetData("DownloadItem") as DownloadItem;
                TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (treeViewItem != null)
                {
                    ProjectFolder targetFolder = treeViewItem.DataContext as ProjectFolder;

                    if (targetFolder != null && downloadItem != null)
                    {
                        string sourceFilePath = downloadItem.FileName;
                        string destinationFilePath = Path.Combine(targetFolder.FullPath, Path.GetFileName(sourceFilePath));

                        try
                        {
                            File.Move(sourceFilePath, destinationFilePath);
                            _viewModel.DownloadManager.Downloads.Remove(downloadItem);
                            targetFolder.Files.Add(Path.GetFileName(destinationFilePath));
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowError($"Error moving file: {ex.Message}");
                        }
                    }
                }
            }
        }
    }
}