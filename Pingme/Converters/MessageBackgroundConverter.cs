using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace Pingme.Converters
{
    public class MessageBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool fromSelf = (bool)value;
            return fromSelf ? new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))  // Green
                            : new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x30)); // Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
