using System;
using System.Globalization;
using System.Windows.Data;

namespace Pingme.Helpers
{
    public class SoundIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            return value.ToString() == parameter.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value && int.TryParse(parameter.ToString(), out int index))
                return index;

            return Binding.DoNothing;
        }
    }
}
