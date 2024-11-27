using System.Globalization;
using System.Windows.Data;

namespace Claudable.ToolSystem.Converters;

public class OpenStateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool isOpen && isOpen ? "Focus" : "Open";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}