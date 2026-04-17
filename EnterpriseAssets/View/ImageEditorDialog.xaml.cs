using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace EnterpriseAssets.View
{
    public partial class ImageEditorDialog : Window
    {
        private byte[] _originalImageBytes;
        private byte[] _currentImageBytes;
        private BitmapImage _currentBitmap;

        // Для перемещения (из второго варианта - через Grid)
        private bool _isDragging = false;
        private Point _dragStart;
        private double _originalLeft;
        private double _originalTop;

        // Для изменения размера (из первого варианта)
        private bool _isResizing = false;
        private double _originalSize;

        // Параметры квадрата
        private double _cropLeft = 0;
        private double _cropTop = 0;
        private double _cropSize = 150;

        public byte[] EditedImageBytes { get; private set; }

        public ImageEditorDialog(byte[] imageBytes)
        {
            InitializeComponent();
            _originalImageBytes = imageBytes;
            _currentImageBytes = imageBytes;
            Loaded += ImageEditorDialog_Loaded;
        }

        private void ImageEditorDialog_Loaded(object sender, RoutedEventArgs e)
        {
            LoadImage();
        }

        private void LoadImage()
        {
            try
            {
                using (var stream = new MemoryStream(_currentImageBytes))
                {
                    _currentBitmap = new BitmapImage();
                    _currentBitmap.BeginInit();
                    _currentBitmap.StreamSource = stream;
                    _currentBitmap.CacheOption = BitmapCacheOption.OnLoad;
                    _currentBitmap.EndInit();
                    _currentBitmap.Freeze();
                    PreviewImage.Source = _currentBitmap;
                }

                PreviewImage.Loaded += (s, e) => UpdateCropBorder();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void UpdateCropBorder()
        {
            if (PreviewImage.ActualWidth <= 0) return;

            double maxSize = Math.Min(PreviewImage.ActualWidth, PreviewImage.ActualHeight);
            _cropSize = Math.Min(_cropSize, maxSize);
            _cropSize = Math.Max(50, _cropSize);

            _cropLeft = Math.Max(0, Math.Min(_cropLeft, PreviewImage.ActualWidth - _cropSize));
            _cropTop = Math.Max(0, Math.Min(_cropTop, PreviewImage.ActualHeight - _cropSize));

            Canvas.SetLeft(CropBorder, _cropLeft);
            Canvas.SetTop(CropBorder, _cropTop);
            CropBorder.Width = _cropSize;
            CropBorder.Height = _cropSize;
        }

        // ========== ПЕРЕМЕЩЕНИЕ (из второго варианта) ==========
        private void CropBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStart = e.GetPosition(PreviewImage);
            _originalLeft = _cropLeft;
            _originalTop = _cropTop;
            CropBorder.CaptureMouse();
        }

        private void CropBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var current = e.GetPosition(PreviewImage);
                double deltaX = current.X - _dragStart.X;
                double deltaY = current.Y - _dragStart.Y;

                double newLeft = _originalLeft + deltaX;
                double newTop = _originalTop + deltaY;

                newLeft = Math.Max(0, Math.Min(newLeft, PreviewImage.ActualWidth - _cropSize));
                newTop = Math.Max(0, Math.Min(newTop, PreviewImage.ActualHeight - _cropSize));

                _cropLeft = newLeft;
                _cropTop = newTop;

                Canvas.SetLeft(CropBorder, _cropLeft);
                Canvas.SetTop(CropBorder, _cropTop);
            }
            else if (_isResizing)
            {
                // ========== ИЗМЕНЕНИЕ РАЗМЕРА (из первого варианта) ==========
                var current = e.GetPosition(PreviewImage);
                double delta = current.X - _dragStart.X;

                double newSize = _originalSize + delta;
                newSize = Math.Max(50, Math.Min(newSize, PreviewImage.ActualWidth - _cropLeft));
                newSize = Math.Min(newSize, PreviewImage.ActualHeight - _cropTop);

                _cropSize = newSize;
                CropBorder.Width = _cropSize;
                CropBorder.Height = _cropSize;
            }
        }

        private void CropBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            CropBorder.ReleaseMouseCapture();
        }

        // ========== ИЗМЕНЕНИЕ РАЗМЕРА ЗА УГОЛОК ==========
        private void ResizeHandle_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _isResizing = true;
            _dragStart = e.GetPosition(PreviewImage);
            _originalSize = _cropSize;
            ResizeHandle.CaptureMouse();
            e.Handled = true;
        }

        private void ResizeHandle_MouseMove(object sender, MouseEventArgs e) { }

        private void ResizeHandle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            _isResizing = false;
            ResizeHandle.ReleaseMouseCapture();
        }

        // ========== КНОПКИ (из первого варианта) ==========
        private void SizeDown_Click(object sender, RoutedEventArgs e)
        {
            _cropSize -= 20;
            UpdateCropBorder();
        }

        private void SizeUp_Click(object sender, RoutedEventArgs e)
        {
            _cropSize += 20;
            UpdateCropBorder();
        }

        private void Center_Click(object sender, RoutedEventArgs e)
        {
            _cropLeft = (PreviewImage.ActualWidth - _cropSize) / 2;
            _cropTop = (PreviewImage.ActualHeight - _cropSize) / 2;
            UpdateCropBorder();
        }

        // ========== ПОВОРОТЫ ==========
        private void RotateLeft_Click(object sender, RoutedEventArgs e)
        {
            RotateImage(-90);
        }

        private void RotateRight_Click(object sender, RoutedEventArgs e)
        {
            RotateImage(90);
        }

        private void RotateImage(double angle)
        {
            try
            {
                var transform = new RotateTransform(angle);
                transform.CenterX = _currentBitmap.Width / 2;
                transform.CenterY = _currentBitmap.Height / 2;

                var rotated = new TransformedBitmap(_currentBitmap, transform);
                rotated.Freeze();

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(rotated));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    _currentImageBytes = stream.ToArray();
                }

                LoadImage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        // ========== СБРОС ==========
        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _currentImageBytes = _originalImageBytes;
            _cropSize = 150;
            LoadImage();
        }

        // ========== СОХРАНЕНИЕ ==========
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double imageWidth = _currentBitmap.PixelWidth;
                double imageHeight = _currentBitmap.PixelHeight;
                double displayWidth = PreviewImage.ActualWidth;
                double displayHeight = PreviewImage.ActualHeight;

                int cropX = (int)((_cropLeft / displayWidth) * imageWidth);
                int cropY = (int)((_cropTop / displayHeight) * imageHeight);
                int cropSizePixels = (int)((_cropSize / displayWidth) * imageWidth);

                cropX = Math.Max(0, Math.Min(cropX, (int)imageWidth - cropSizePixels));
                cropY = Math.Max(0, Math.Min(cropY, (int)imageHeight - cropSizePixels));
                cropSizePixels = Math.Min(cropSizePixels, (int)Math.Min(imageWidth - cropX, imageHeight - cropY));

                var cropped = new CroppedBitmap(_currentBitmap, new Int32Rect(cropX, cropY, cropSizePixels, cropSizePixels));
                cropped.Freeze();

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(cropped));

                using (var stream = new MemoryStream())
                {
                    encoder.Save(stream);
                    EditedImageBytes = stream.ToArray();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}