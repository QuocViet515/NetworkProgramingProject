using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace Pingme.Helpers
{
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush SelectedBrush { get; set; } = new LinearGradientBrush(
            new GradientStopCollection
            {
            new GradientStop((Color)ColorConverter.ConvertFromString("#C7FADF"), 0),
            new GradientStop((Color)ColorConverter.ConvertFromString("#94B9FF"), 1)
            },
            new Point(0, 0),
            new Point(1, 0)
        );

        public Brush DefaultBrush { get; set; } = Brushes.White;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool b && b) ? SelectedBrush : DefaultBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

}
