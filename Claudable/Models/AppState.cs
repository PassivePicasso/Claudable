namespace Claudable.Models;

public class AppState
{
    public bool IsPanelsSwapped { get; set; }
    public int SelectedTabIndex { get; set; }
    public string[] Filters { get; set; }
    public string[] ExpandedFolders { get; set; }
    public string ProjectRootPath { get; set; }
    public FilterMode CurrentFilterMode { get; internal set; }
}