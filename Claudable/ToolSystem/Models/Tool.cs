using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Claudable.ToolSystem.Models;

public class Tool : INotifyPropertyChanged
{
    private string _id;
    private string _name;
    private string _content;
    private DateTime _createdAt;
    private DateTime _lastModified;
    private bool _isOpen;

    public string Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    public string Content
    {
        get => _content;
        set
        {
            if (_content != value)
            {
                _content = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt != value)
            {
                _createdAt = value;
                OnPropertyChanged();
            }
        }
    }

    public DateTime LastModified
    {
        get => _lastModified;
        set
        {
            if (_lastModified != value)
            {
                _lastModified = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsOpen
    {
        get => _isOpen;
        set
        {
            if (_isOpen != value)
            {
                _isOpen = value;
                OnPropertyChanged();
            }
        }
    }

    public string WorkingDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Claudable",
        "Tools",
        Id);

    public Tool() { }

    public Tool(string id, string name, string content)
    {
        Id = id;
        Name = name;
        Content = content;
        CreatedAt = DateTime.UtcNow;
        LastModified = DateTime.UtcNow;
        IsOpen = false;
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}