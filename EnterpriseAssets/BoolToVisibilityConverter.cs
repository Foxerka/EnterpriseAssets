using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EnterpriseAssets
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string strValue)
            {
                // Для строк: если не пустая - Visible, иначе Collapsed
                return string.IsNullOrEmpty(strValue) ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (value is bool boolValue)
            {
                // Для bool: если true - Visible, иначе Collapsed
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}