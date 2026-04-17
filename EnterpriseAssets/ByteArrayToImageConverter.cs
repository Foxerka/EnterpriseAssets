using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace EnterpriseAssets
{
    public class ByteArrayToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                System.Diagnostics.Debug.WriteLine("ByteArrayToImageConverter: value is null");
                return null;
            }

            byte[] bytes = value as byte[];
            if (bytes == null || bytes.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine($"ByteArrayToImageConverter: bytes is null or empty. Type: {value.GetType()}");
                return null;
            }

            System.Diagnostics.Debug.WriteLine($"ByteArrayToImageConverter: Converting {bytes.Length} bytes");

            try
            {
                using (MemoryStream stream = new MemoryStream(bytes))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                    image.Freeze();

                    System.Diagnostics.Debug.WriteLine("ByteArrayToImageConverter: Successfully converted");
                    return image;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ByteArrayToImageConverter ERROR: {ex.Message}");
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}