using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Claudable.ViewModels
{
    public class TrackedFile : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _fullPath;
        public string FullPath
        {
            get => _fullPath;
            set
            {
                _fullPath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileName));
                OnPropertyChanged(nameof(PascalCaseFileName));
            }
        }

        public string FileName => Path.GetFileName(FullPath);

        public string PascalCaseFileName => ToPascalCase(Path.GetFileNameWithoutExtension(FullPath)) + Path.GetExtension(FullPath);

        private DateTime _lastModified;
        public DateTime LastModified
        {
            get => _lastModified;
            set
            {
                _lastModified = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public TrackedFile(string fullPath)
        {
            FullPath = fullPath;
            LastModified = File.GetLastWriteTime(fullPath);
        }

        private string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Check if the input is already in PascalCase
            if (IsPascalCase(input))
                return input;

            // Replace underscores and hyphens with spaces
            input = input.Replace("_", " ").Replace("-", " ");

            // Split the input into words
            string[] words = input.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Convert each word to PascalCase
            for (int i = 0; i < words.Length; i++)
            {
                if (!string.IsNullOrEmpty(words[i]))
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
                }
            }

            // Join the words back together
            return string.Join("", words);
        }

        private bool IsPascalCase(string input)
        {
            // Check if the string starts with an uppercase letter
            if (!char.IsUpper(input[0]))
                return false;

            // Check if the string contains any spaces, underscores, or hyphens
            if (input.Contains(" ") || input.Contains("_") || input.Contains("-"))
                return false;

            // Check if the string follows PascalCase pattern (uppercase letters can be followed by lowercase letters)
            return Regex.IsMatch(input, @"^[A-Z][a-zA-Z0-9]*([A-Z][a-zA-Z0-9]*)*$");
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}