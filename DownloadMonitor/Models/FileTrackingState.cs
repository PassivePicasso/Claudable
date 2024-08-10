using DownloadMonitor.ViewModels;

namespace DownloadMonitor.Models
{
    public class FileTrackingState
    {
        public List<TrackedFile> TrackedFiles { get; set; }
        public string SelectedFolder { get; set; }
    }
}
