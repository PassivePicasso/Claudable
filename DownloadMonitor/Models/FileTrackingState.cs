using Claudable.ViewModels;

namespace Claudable.Models
{
    public class FileTrackingState
    {
        public List<TrackedFile> TrackedFiles { get; set; }
        public string SelectedFolder { get; set; }
    }
}
