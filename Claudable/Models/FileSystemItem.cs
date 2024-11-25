using Claudable.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace Claudable.Models;

public class FileSystemItem : INotifyPropertyChanged
{
    private string _name;
    private string _fullPath;
    private string _editableName;
    private bool _isInRenameMode;
    private bool _isVisible = true;
    private ArtifactViewModel _associatedArtifact;
    private FileSystemItem? _parent;

    public string Name
    {
        get => _name;
        set { _name = value; OnPropertyChanged(); }
    }

    public string FullPath
    {
        get => _fullPath;
        set { _fullPath = value; OnPropertyChanged(); }
    }

    public bool IsVisible
    {
        get => _isVisible;
        set { _isVisible = value; OnPropertyChanged(); }
    }

    public ArtifactViewModel AssociatedArtifact
    {
        get => _associatedArtifact;
        set { _associatedArtifact = value; OnPropertyChanged(); }
    }

    public FileSystemItem? Parent
    {
        get => _parent;
        set { _parent = value; OnPropertyChanged(); }
    }
    public bool IsInRenameMode
    {
        get => _isInRenameMode;
        set
        {
            _isInRenameMode = value;
            OnPropertyChanged();
        }
    }

    public string EditableName
    {
        get => _editableName;
        set
        {
            _editableName = value;
            OnPropertyChanged();
        }
    }

    public ICommand BeginRenameCommand { get; }
    public ICommand EndRenameCommand { get; }
    public ICommand CancelRenameCommand { get; }
    public ICommand KeyDownCommand { get; }
    public ICommand LostFocusCommand { get; }
    public ObservableCollection<FileSystemItem> Children { get; } = new ObservableCollection<FileSystemItem>();

    public bool IsFolder => this is ProjectFolder;

    public event PropertyChangedEventHandler PropertyChanged;
    public FileSystemItem()
    {
        BeginRenameCommand = new RelayCommand(BeginRename);
        EndRenameCommand = new RelayCommand<string>(EndRename);
        CancelRenameCommand = new RelayCommand(CancelRename);
        KeyDownCommand = new RelayCommand<KeyEventArgs>(HandleKeyDown);
        LostFocusCommand = new RelayCommand<RoutedEventArgs>(HandleLostFocus);
    }

    private void BeginRename()
    {
        EditableName = Name;
        IsInRenameMode = true;
    }

    private void CancelRename()
    {
        IsInRenameMode = false;
        EditableName = Name;
    }

    private void HandleKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EndRename(EditableName);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CancelRename();
        }
    }

    private void HandleLostFocus(RoutedEventArgs e)
    {
        if (IsInRenameMode)
        {
            EndRename(EditableName);
        }
    }
    private void EndRename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName) || newName == Name || !IsValidFileName(newName))
        {
            CancelRename();
            return;
        }

        try
        {
            string newPath = Path.Combine(Path.GetDirectoryName(FullPath), newName);

            if (File.Exists(newPath) || Directory.Exists(newPath))
            {
                MessageBox.Show($"An item with name '{newName}' already exists.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                CancelRename();
                return;
            }

            if (IsFolder)
            {
                Directory.Move(FullPath, newPath);
            }
            else
            {
                File.Move(FullPath, newPath);
            }

            Name = newName;
            FullPath = newPath;
            IsInRenameMode = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error renaming item: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            CancelRename();
        }
    }

    private bool IsValidFileName(string fileName)
    {
        return fileName.IndexOfAny(Path.GetInvalidFileNameChars()) == -1;
    }
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}