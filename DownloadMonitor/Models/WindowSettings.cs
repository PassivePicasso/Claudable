namespace DownloadMonitor.Models
{
    public class WindowSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double Left { get; set; }
        public double Top { get; set; }
        public double LeftColumnRatio { get; set; }
        public bool IsPanelsSwapped { get; set; }
        public string LastVisitedUrl { get; set; }
    }
}