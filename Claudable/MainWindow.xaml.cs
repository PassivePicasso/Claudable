﻿using Claudable.Models;
using Claudable.Services;
using Claudable.Utilities;
using Claudable.ViewModels;
using Claudable.Windows;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Claudable;

public partial class MainWindow : Window
{
    public static readonly MainViewModel MainViewModel = new MainViewModel();
    private readonly WebViewManager _webViewManager;
    private readonly WindowStateManager _windowStateManager;
    private readonly IDialogService _dialogService;
    private Point _startPoint;
    private DragAdorner dragAdorner = new DragAdorner();

    public WebViewManager WebViewManager => _webViewManager;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = MainViewModel;

        _dialogService = new DialogService();
        _webViewManager = new WebViewManager(ClaudeWebView, "https://claude.ai");
        _webViewManager.DocsReceived += _webViewManager_DocsReceived;
        _webViewManager.ProjectChanged += _webViewManager_ProjectChanged;
        _webViewManager.ArtifactDeleted += _webViewManager_ArtifactDeleted;

        _windowStateManager = new WindowStateManager(this, MainViewModel, _webViewManager);
        MainViewModel.WebViewManager = _webViewManager;

        MouseHook.MouseMoved += OnMouseMoved;
        MouseHook.SetHook();
        this.Closed += (s, e) =>
        {
            MouseHook.Unhook();
            dragAdorner.Close();
        };

        Application.Current.MainWindow = this;
        InitializeAsync();
    }

    private void OnMouseMoved(Point cursorPosition)
    {
        dragAdorner.UpdatePosition(cursorPosition);
    }
    private void TreeView_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Find the TreeViewItem that was clicked
        var treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);
        if (treeViewItem != null)
        {
            var fileSystemItem = treeViewItem.DataContext as FileSystemItem;
            if (fileSystemItem != null)
            {
                // Select the item that was right-clicked
                treeViewItem.IsSelected = true;
                e.Handled = true;

                // Show the context menu
                var position = e.GetPosition(this);
                position = PointToScreen(position);
                ShellContextMenuHandler.ShowContextMenu(fileSystemItem.FullPath, this, position);
            }
        }
    }
    private void _webViewManager_ArtifactDeleted(object? sender, string e)
    {
        if (string.IsNullOrEmpty(e)) return;
        var matched = MainViewModel.ArtifactManager.Artifacts.FirstOrDefault(a => a.Uuid == e);
        if (matched == null) return;
        var projectFile = MainViewModel.ArtifactManager.RootProjectFolder.GetAllProjectFiles().FirstOrDefault(pf => pf.AssociatedArtifact == matched);
        if (projectFile == null) return;
        projectFile.AssociatedArtifact = null;
        projectFile.ArtifactLastModified = DateTime.MinValue;
    }

    private void _webViewManager_DocsReceived(object? sender, string e)
    {
        if (string.IsNullOrEmpty(e)) return;
        MainViewModel.ArtifactManager.LoadArtifacts(e);
    }
    private void _webViewManager_ProjectChanged(object? sender, string e)
    {
        if (string.IsNullOrEmpty(e)) return;
        MainViewModel.HandleProjectChanged(e);
    }

    private async void InitializeAsync()
    {
        await WebViewManager.InitializeAsync();
        _windowStateManager.LoadState();
        MainViewModel.LoadStateCommand.Execute(null);

        if (MainViewModel.IsPanelsSwapped)
        {
            SwapPanels(false);
        }

        // Initialize DownloadManager with WebView2
        MainViewModel.DownloadManager.Initialize(ClaudeWebView.CoreWebView2);
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
            MainViewModel.IsPanelsSwapped = !MainViewModel.IsPanelsSwapped;
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
            MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,0 H10 V10 H0 V0 M0,3");
        }
        else
        {
            WindowState = WindowState.Maximized;
            MaximizeRestoreButtonPath.Data = Geometry.Parse("M0,3 H7 V10 H0 V3 M3,3 V0 H10 V7 H7");
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
        MainViewModel.SaveStateCommand.Execute(null);
    }

    #region Drag and Drop
    private void DownloadItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item)
        {
            _startPoint = e.GetPosition(null);
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

                    // Create custom adorner for drag and drop visual
                    dragAdorner.DataContext = downloadItem;
                    dragAdorner.Show();

                    DataObject dragData = new DataObject("DownloadItem", downloadItem);
                    DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);

                    dragAdorner.Hide();
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
                    MainViewModel.DropSvgArtifactCommand.Execute(dropInfo);
                }
            }
        }
        else if (e.Data.GetDataPresent("DownloadItem"))
        {
            DownloadItem downloadItem = e.Data.GetData("DownloadItem") as DownloadItem;
            TreeViewItem treeViewItem = FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource);

            FileSystemItem targetItem = null;
            if (treeViewItem != null)
            {
                targetItem = treeViewItem.DataContext as FileSystemItem;
            }

            if (targetItem == null)
            {
                targetItem = MainViewModel.RootProjectFolder;
            }

            ProjectFolder targetFolder = targetItem as ProjectFolder;
            if (targetItem is ProjectFile)
            {
                targetFolder = targetItem.Parent as ProjectFolder;
            }

            if (targetFolder != null && downloadItem != null)
            {
                string sourceFilePath = downloadItem.Path;
                string destinationFilePath = Path.Combine(targetFolder.FullPath, Path.GetFileName(sourceFilePath));

                try
                {
                    File.Move(sourceFilePath, destinationFilePath);
                    MainViewModel.DownloadManager.Downloads.Remove(downloadItem);

                    // Add the new file to the target folder's children
                    ProjectFile newFile = new ProjectFile(Path.GetFileName(destinationFilePath), destinationFilePath);
                    targetFolder.AddChild(newFile);

                    // Optionally, you can sort the children after adding the new file
                    SortFolderChildren(targetFolder);

                    // Refresh the TreeView
                    ProjectStructureTreeView.Items.Refresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error moving file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    private void FilterModeButton_Click(object sender, RoutedEventArgs e)
    {
        MainViewModel.CurrentFilterMode = (FilterMode)(((int)MainViewModel.CurrentFilterMode + 1) % 3);
        ((Button)sender).Content = MainViewModel.CurrentFilterMode.ToString();
        ProjectStructureTreeView?.Items.Refresh();
    }
}