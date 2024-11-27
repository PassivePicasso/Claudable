using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;

namespace Claudable.ToolSystem.Converters;

public class IsReactComponentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string fileName)
        {
            string extension = Path.GetExtension(fileName);
            if (string.Equals(extension, ".tsx", StringComparison.OrdinalIgnoreCase))
            {
                return Visibility.Visible;
            }
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}