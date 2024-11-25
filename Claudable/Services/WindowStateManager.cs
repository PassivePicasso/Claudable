using Claudable.Models;
using Claudable.ViewModels;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Claudable.Services;

public class WindowStateManager
{
    private const string StateFileName = "windowsettings.json";
    private readonly Window _window;
    private readonly MainViewModel _viewModel;
    private readonly WebViewManager _webViewManager;

    public WindowStateManager(Window window, MainViewModel viewModel, WebViewManager webViewManager)
    {
        _window = window ?? throw new ArgumentNullException(nameof(window));
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _webViewManager = webViewManager ?? throw new ArgumentNullException(nameof(webViewManager));
    }

    public void SaveState()
    {
        var grid = _window.FindName("MainGrid") as System.Windows.Controls.Grid;
        var leftColumn = grid?.ColumnDefinitions[0];
        var rightColumn = grid?.ColumnDefinitions[2];

        if (leftColumn == null || rightColumn == null)
        {
            return;
        }

        double totalWidth = leftColumn.ActualWidth + rightColumn.ActualWidth;
        double leftRatio = leftColumn.ActualWidth / totalWidth;

        var settings = new WindowSettings
        {
            Width = _window.Width,
            Height = _window.Height,
            Left = _window.Left,
            Top = _window.Top,
            LeftColumnRatio = leftRatio,
            IsPanelsSwapped = _viewModel.IsPanelsSwapped,
            LastVisitedUrl = _webViewManager.LastVisitedUrl
        };

        try
        {
            string json = JsonSerializer.Serialize(settings);
            File.WriteAllText(StateFileName, json);
        }
        catch (Exception ex)
        {
            // Log the exception or show a message to the user
            Console.WriteLine($"Error saving window state: {ex.Message}");
        }
    }

    public void LoadState()
    {
        if (File.Exists(StateFileName))
        {
            try
            {
                string json = File.ReadAllText(StateFileName);
                var settings = JsonSerializer.Deserialize<WindowSettings>(json);

                if (settings != null)
                {
                    ApplyWindowSettings(settings);
                }
            }
            catch (Exception ex)
            {
                // Log the exception or show a message to the user
                Console.WriteLine($"Error loading window state: {ex.Message}");
            }
        }
    }

    private void ApplyWindowSettings(WindowSettings settings)
    {
        _window.Width = settings.Width;
        _window.Height = settings.Height;
        _window.Left = settings.Left;
        _window.Top = settings.Top;

        _viewModel.IsPanelsSwapped = settings.IsPanelsSwapped;

        if (!string.IsNullOrEmpty(settings.LastVisitedUrl))
        {
            _webViewManager.Navigate(settings.LastVisitedUrl);
        }
        else
        {
            _webViewManager.Navigate("https://claude.ai");
        }

        ApplyColumnRatios(settings.LeftColumnRatio);
    }

    private void ApplyColumnRatios(double leftRatio)
    {
        var grid = _window.FindName("MainGrid") as System.Windows.Controls.Grid;
        if (grid == null)
        {
            return;
        }

        var leftColumn = grid.ColumnDefinitions[0];
        var rightColumn = grid.ColumnDefinitions[2];

        leftColumn.Width = new GridLength(leftRatio, GridUnitType.Star);
        rightColumn.Width = new GridLength(1 - leftRatio, GridUnitType.Star);
    }
}