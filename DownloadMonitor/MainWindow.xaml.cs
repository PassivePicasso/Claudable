using Claudable.Models;
using Claudable.Services;
using Claudable.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _dialogService = new DialogService();
            _webViewManager = new WebViewManager(ClaudeWebView, "https://claude.ai");
            _webViewManager.DocsReceived += _webViewManager_DocsReceived;
            _webViewManager.ProjectChanged += _webViewManager_ProjectChanged;
            _windowStateManager = new WindowStateManager(this, _viewModel, _webViewManager);

            InitializeAsync();
        }

        private void _webViewManager_DocsReceived(object? sender, string e)
        {
            if (string.IsNullOrEmpty(e)) return;
            _viewModel.ArtifactManager.LoadArtifacts(e);
        }
        private void _webViewManager_ProjectChanged(object? sender, string e)
        {
            _viewModel.HandleProjectChanged(e);
        }

        private async void InitializeAsync()
        {
            await _webViewManager.InitializeAsync();
            _windowStateManager.LoadState();
            _viewModel.LoadStateCommand.Execute(null);

            if (_viewModel.IsPanelsSwapped)
            {
                SwapPanels(false);
            }

            // Initialize DownloadManager with WebView2
            _viewModel.DownloadManager.Initialize(ClaudeWebView.CoreWebView2);
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
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        private void MaximizeRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Normal;
                MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,0 H10 V10 H0 V0 M0,3 H7 V10 M3,0 V7");
            }
            else
            {
                WindowState = WindowState.Maximized;
                MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,3 H7 V10 H0 V3 M3,0 H10 V7 H3 V0");
            }
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);
            if (WindowState == WindowState.Maximized)
            {
                MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,3 H7 V10 H0 V3 M3,0 H10 V7 H3 V0");
            }
            else
            {
                MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,0 H10 V10 H0 V0 M0,3 H7 V10 M3,0 V7");
            }
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _windowStateManager.SaveState();
            _viewModel.SaveStateCommand.Execute(null);
        }

        #region Drag and Drop
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
        private void SvgArtifact_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is ListBoxItem item)
            {
                _startPoint = e.GetPosition(null);
            }
        }
        private void SvgArtifact_MouseMove(object sender, MouseEventArgs e)
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
                        SvgArtifactViewModel svgArtifact = listBoxItem.DataContext as SvgArtifactViewModel;

                        if (svgArtifact != null)
                        {
                            DataObject dragData = new DataObject("SvgArtifact", svgArtifact);
                            DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Copy);
                        }
                    }
                }
            }
        }
        private void ProjectFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent("SvgArtifact"))
            {
                SvgArtifactViewModel svgArtifact = e.Data.GetData("SvgArtifact") as SvgArtifactViewModel;
                TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (treeViewItem != null && svgArtifact != null)
                {
                    FileSystemItem targetItem = treeViewItem.DataContext as FileSystemItem;

                    if (targetItem != null)
                    {
                        string fileType = Keyboard.Modifiers == ModifierKeys.Control ? "ico" : "png";
                        var dropInfo = new Tuple<SvgArtifactViewModel, FileSystemItem, string>(svgArtifact, targetItem, fileType);
                        _viewModel.DropSvgArtifactCommand.Execute(dropInfo);
                    }
                }
            }
            else if (e.Data.GetDataPresent(typeof(DownloadItem)))
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
        #endregion

        private void SortFolderChildren(ProjectFolder folder)
        {
            var sortedChildren = folder.Children.OrderBy(c => !c.IsFolder).ThenBy(c => c.Name).ToList();
            folder.Children.Clear();
            foreach (var child in sortedChildren)
            {
                folder.Children.Add(child);
            }
        }

        private void ToggleButton_Click(object sender, EventArgs e)
        {
            var toggleButton = sender as ToggleButton;
            if (toggleButton != null)
                toggleButton.Content = toggleButton.IsChecked ?? false ? "Showing Tracked" : "Showing All";
            ProjectStructureTreeView?.Items.Refresh();
        }
    }
}