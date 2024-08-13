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

namespace Claudable
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
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
            _webViewManager.DocsReceived += _webViewManager_DocsReceived;
            _windowStateManager = new WindowStateManager(this, _viewModel, _webViewManager);

            InitializeAsync();
        }

        private void _webViewManager_DocsReceived(object? sender, string e)
        {
            _viewModel.ArtifactManager.LoadArtifacts(e);
        }

        private async void InitializeAsync()
        {
            await _webViewManager.InitializeAsync();
            _windowStateManager.LoadState();
            _viewModel.LoadStateCommand.Execute(null);
            ApplyViewModelState();

            // Initialize DownloadManager with WebView2
            _viewModel.DownloadManager.Initialize(ClaudeWebView.CoreWebView2);
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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _windowStateManager.SaveState();
            _viewModel.SaveStateCommand.Execute(null);
        }

        private void DownloadItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                DragDrop.DoDragDrop(item, item.DataContext, DragDropEffects.Move);
            }
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

        private void ProjectFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(DownloadItem)))
            {
                DownloadItem downloadItem = e.Data.GetData(typeof(DownloadItem)) as DownloadItem;
                TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (treeViewItem != null)
                {
                    FileSystemItem targetItem = treeViewItem.DataContext as FileSystemItem;

                    if (targetItem != null && targetItem.IsFolder && downloadItem != null)
                    {
                        ProjectFolder targetFolder = targetItem as ProjectFolder;
                        string sourceFilePath = downloadItem.Path;
                        string destinationFilePath = Path.Combine(targetFolder.FullPath, Path.GetFileName(sourceFilePath));

                        try
                        {
                            File.Move(sourceFilePath, destinationFilePath);
                            _viewModel.DownloadManager.Downloads.Remove(downloadItem);

                            // Add the new file to the target folder's children
                            ProjectFile newFile = new ProjectFile(Path.GetFileName(destinationFilePath), destinationFilePath);
                            targetFolder.Children.Add(newFile);

                            // Optionally, you can sort the children after adding the new file
                            SortFolderChildren(targetFolder);
                        }
                        catch (Exception ex)
                        {
                            _dialogService.ShowError($"Error moving file: {ex.Message}");
                        }
                    }
                }
            }
        }
        private void TreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _startPoint = e.GetPosition(null);
        }

        private void TreeView_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point mousePos = e.GetPosition(null);
                Vector diff = _startPoint - mousePos;

                if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    TreeView treeView = sender as TreeView;
                    TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                    if (treeViewItem != null)
                    {
                        FileSystemItem fileSystemItem = treeViewItem.DataContext as FileSystemItem;
                        if (fileSystemItem != null && !fileSystemItem.IsFolder)
                        {
                            string[] files = { fileSystemItem.FullPath };
                            DataObject dragData = new DataObject(DataFormats.FileDrop, files);
                            DragDrop.DoDragDrop(treeViewItem, dragData, DragDropEffects.Copy);
                        }
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


        private void SortFolderChildren(ProjectFolder folder)
        {
            var sortedChildren = folder.Children.OrderBy(c => !c.IsFolder).ThenBy(c => c.Name).ToList();
            folder.Children.Clear();
            foreach (var child in sortedChildren)
            {
                folder.Children.Add(child);
            }
        }
    }
}