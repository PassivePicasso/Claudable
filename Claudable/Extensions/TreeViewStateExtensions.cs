namespace Claudable.Extensions;

using ViewModels;

public static class TreeViewStateExtensions
{
    public static string[] GetExpandedPaths(this ProjectFolder rootFolder)
    {
        var expandedPaths = new List<string>();
        CollectExpandedPaths(rootFolder, expandedPaths);
        return expandedPaths.ToArray();
    }

    private static void CollectExpandedPaths(ProjectFolder folder, List<string> expandedPaths)
    {
        if (folder.IsExpanded)
        {
            expandedPaths.Add(folder.FullPath);
        }

        foreach (var child in folder.Children)
        {
            if (child is ProjectFolder childFolder)
            {
                CollectExpandedPaths(childFolder, expandedPaths);
            }
        }
    }

    public static void RestoreExpandedState(this ProjectFolder rootFolder, string[] expandedPaths)
    {
        if (expandedPaths == null || expandedPaths.Length == 0)
        {
            return;
        }

        var expandedPathsSet = new HashSet<string>(expandedPaths);
        RestoreExpandedStateRecursive(rootFolder, expandedPathsSet);
    }

    private static void RestoreExpandedStateRecursive(ProjectFolder folder, HashSet<string> expandedPaths)
    {
        folder.IsExpanded = expandedPaths.Contains(folder.FullPath);

        foreach (var child in folder.Children)
        {
            if (child is ProjectFolder childFolder)
            {
                RestoreExpandedStateRecursive(childFolder, expandedPaths);
            }
        }
    }
}
