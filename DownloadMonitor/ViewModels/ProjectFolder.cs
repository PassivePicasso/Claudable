using System;
using System.Linq;
using Claudable.Models;

namespace Claudable.ViewModels
{
    public class ProjectFolder : FileSystemItem
    {
        public ProjectFolder(string name, string fullPath)
        {
            Name = name;
            FullPath = fullPath;
        }

        public void ApplyFilter(string[] filters)
        {
            bool shouldExclude = filters.Any(filter => Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);

            if (shouldExclude)
            {
                IsVisible = false;
                foreach (var child in Children)
                {
                    child.IsVisible = false;
                }
            }
            else
            {
                IsVisible = true;

                foreach (var child in Children)
                {
                    if (child is ProjectFolder folder)
                    {
                        folder.ApplyFilter(filters);
                    }
                    else
                    {
                        child.IsVisible = !filters.Any(filter => child.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0);
                    }
                }

                IsVisible = Children.Any(c => c.IsVisible);
            }
        }
    }
}