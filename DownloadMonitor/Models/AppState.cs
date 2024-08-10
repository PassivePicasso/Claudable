namespace DownloadMonitor.Models
{
    public class AppState
    {
        public FileTrackingState DownloadMonitorState { get; set; }
        public FileTrackingState FileChangeMonitorState { get; set; }
        public bool IsPanelsSwapped { get; set; }
    }
}