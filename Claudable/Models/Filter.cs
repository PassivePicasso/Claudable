namespace Claudable.Models
{
    using System.Text.Json.Serialization;
    using ViewModels;

    public class Filter
    {
        public Filter(string filterValue)
        {
            Value = filterValue;
        }

        public string Value { get; set; }
    
        [JsonIgnore]
        public bool ShouldPrependProjectFolder => FilterViewModel.FolderChar.Contains(Value[0]);
    }
}