using Microsoft.Web.WebView2.Core;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace Claudable.Models;

public class DownloadManager : INotifyPropertyChanged
{
    private ObservableCollection<DownloadItem> _downloads;
    public ObservableCollection<DownloadItem> Downloads
    {
        get => _downloads;
        set
        {
            _downloads = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public DownloadManager()
    {
        Downloads = new ObservableCollection<DownloadItem>();
    }

    public void Initialize(CoreWebView2 webView)
    {
        webView.DownloadStarting += WebView_DownloadStarting;
    }

    private void WebView_DownloadStarting(object sender, CoreWebView2DownloadStartingEventArgs e)
    {
        e.Handled = true;

        var download = new DownloadItem
        {
            Path = e.ResultFilePath,
            Status = DownloadStatus.InProgress
        };

        Application.Current.Dispatcher.Invoke(() =>
        {
            Downloads.Add(download);
        });

        e.DownloadOperation.StateChanged += (s, args) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                switch (e.DownloadOperation.State)
                {
                    case CoreWebView2DownloadState.Completed:
                        download.Status = DownloadStatus.Completed;
                        break;
                    case CoreWebView2DownloadState.Interrupted:
                        download.Status = DownloadStatus.Failed;
                        break;
                }
            });
        };

        e.DownloadOperation.BytesReceivedChanged += (s, args) =>
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                download.BytesReceived = (ulong)e.DownloadOperation.BytesReceived;
                if (e.DownloadOperation.TotalBytesToReceive.HasValue)
                    download.TotalBytes = e.DownloadOperation.TotalBytesToReceive.Value;
            });
        };
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class DownloadItem : INotifyPropertyChanged
{
    private string _path;
    private DownloadStatus _status;
    private ulong _bytesReceived;
    private ulong _totalBytes;

    public string Path
    {
        get => _path;
        set
        {
            _path = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(FileName));
            OnPropertyChanged(nameof(Directory));
        }
    }

    public DownloadStatus Status
    {
        get => _status;
        set
        {
            _status = value;
            OnPropertyChanged();
            if (value == DownloadStatus.Completed)
            {
                BytesReceived = TotalBytes;

                OnPropertyChanged(nameof(Path));
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(Directory));
            }
        }
    }

    public ulong BytesReceived
    {
        get => _bytesReceived;
        set
        {
            _bytesReceived = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Progress));
        }
    }

    public ulong TotalBytes
    {
        get => _totalBytes;
        set
        {
            _totalBytes = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Progress));
        }
    }

    public string FileName => System.IO.Path.GetFileName(_path);
    public string Directory => System.IO.Path.GetDirectoryName(_path);

    public int Progress => TotalBytes > 0 ? (int)((double)BytesReceived / TotalBytes * 100) : 100;

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum DownloadStatus
{
    InProgress,
    Completed,
    Failed
}