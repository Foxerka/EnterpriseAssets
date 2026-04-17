using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnterpriseAssets
{
    public class ByteArrayToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            byte[] bytes = value as byte[];
            return (bytes != null && bytes.Length > 0) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}