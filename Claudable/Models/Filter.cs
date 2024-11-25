namespace Claudable.Models;
using ViewModels;

public class Filter
{
    public Filter(string filterValue)
    {
        Value = filterValue;
    }

    public string Value { get; set; }

    public bool ShouldPrependProjectFolder => FilterViewModel.FolderChar.Contains(Value[0]);
}