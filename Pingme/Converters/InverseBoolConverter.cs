﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Pingme.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => !(value is bool b) || !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => !(value is bool b) || !b;
    }
}
