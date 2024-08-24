using System.Windows;
using Microsoft.Win32;

namespace Claudable.Services
{
    public interface IDialogService
    {
        void ShowInformation(string message, string title = "Information");
        void ShowWarning(string message, string title = "Warning");
        void ShowError(string message, string title = "Error");
        bool ShowConfirmation(string message, string title = "Confirm");
        string ShowOpenFileDialog(string title, string filter);
        string ShowSaveFileDialog(string title, string filter, string defaultExt);
        string ShowFolderBrowserDialog(string description);
    }

    public class DialogService : IDialogService
    {
        public void ShowInformation(string message, string title = "Information")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "Warning")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "Error")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool ShowConfirmation(string message, string title = "Confirm")
        {
            var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        }

        public string ShowOpenFileDialog(string title, string filter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter
            };

            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
            }

            return null;
        }

        public string ShowSaveFileDialog(string title, string filter, string defaultExt)
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = title,
                Filter = filter,
                DefaultExt = defaultExt
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                return saveFileDialog.FileName;
            }

            return null;
        }

        public string ShowFolderBrowserDialog(string description)
        {
            var folderBrowserDialog = new OpenFolderDialog
            {
                Title = description
            };

            if (folderBrowserDialog.ShowDialog() == true)
            {
                return folderBrowserDialog.FolderName;
            }

            return null;
        }
    }
}
