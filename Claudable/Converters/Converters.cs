using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Claudable.Converters
{
    public class BoolToSolidColorBrushConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueColorProperty =
            DependencyProperty.Register("True", typeof(SolidColorBrush), typeof(BoolToSolidColorBrushConverter));

        public static readonly DependencyProperty FalseColorProperty =
            DependencyProperty.Register("False", typeof(SolidColorBrush), typeof(BoolToSolidColorBrushConverter));

        public SolidColorBrush True
        {
            get { return (SolidColorBrush)GetValue(TrueColorProperty); }
            set { SetValue(TrueColorProperty, value); }
        }

        public SolidColorBrush False
        {
            get { return (SolidColorBrush)GetValue(FalseColorProperty); }
            set { SetValue(FalseColorProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is bool boolValue && boolValue)
                    return True;

                return False;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class ChooseConverter : DependencyObject, IValueConverter
    {
        public static readonly DependencyProperty TrueProperty =
            DependencyProperty.Register(nameof(True), typeof(object), typeof(ChooseConverter), new PropertyMetadata(null));

        public static readonly DependencyProperty FalseProperty =
            DependencyProperty.Register(nameof(False), typeof(object), typeof(ChooseConverter), new PropertyMetadata(null));

        public object True
        {
            get { return GetValue(TrueProperty); }
            set { SetValue(TrueProperty, value); }
        }

        public object False
        {
            get { return GetValue(FalseProperty); }
            set { SetValue(FalseProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? True : False;
            }

            return False;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVersionTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? "Local Newer" : "Artifact Newer";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class FileSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long bytes)
            {
                string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
                int suffixIndex = 0;
                double size = bytes;

                while (size >= 1024 && suffixIndex < suffixes.Length - 1)
                {
                    size /= 1024;
                    suffixIndex++;
                }

                return $"{size:0.##} {suffixes[suffixIndex]}";
            }

            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double bytesPerSecond)
            {
                string[] suffixes = { "B/s", "KB/s", "MB/s", "GB/s" };
                int suffixIndex = 0;
                double speed = bytesPerSecond;

                while (speed >= 1024 && suffixIndex < suffixes.Length - 1)
                {
                    speed /= 1024;
                    suffixIndex++;
                }

                return $"{speed:0.##} {suffixes[suffixIndex]}";
            }

            return "0 B/s";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status.Equals("InProgress", StringComparison.OrdinalIgnoreCase)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
