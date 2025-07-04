using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pingme.Views.Windows
{
    /// <summary>
    /// Interaction logic for CropImageWindow.xaml
    /// </summary>
    public partial class CropImageWindow : Window
    {
        private TranslateTransform imageTranslate = new TranslateTransform();
        private ScaleTransform imageScale = new ScaleTransform();
        private TransformGroup imageTransformGroup = new TransformGroup();
        private Point lastMousePosition;
        private bool isDragging = false;
        private double currentScale = 1.0;
        private BitmapImage originalImage;
        public CroppedBitmap CroppedResult { get; private set; }

        public CropImageWindow(string imagePath)
        {
            InitializeComponent();

            originalImage = new BitmapImage();
            originalImage.BeginInit();
            originalImage.CacheOption = BitmapCacheOption.OnLoad;
            originalImage.UriSource = new Uri(imagePath);
            originalImage.EndInit();

            this.Loaded += CropImageWindow_Loaded;
        }

        private void CropImageWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ImageToCrop.Source = originalImage;

            imageTransformGroup.Children.Add(imageScale);
            imageTransformGroup.Children.Add(imageTranslate);
            ImageToCrop.RenderTransform = imageTransformGroup;

            CanvasCrop.Dispatcher.InvokeAsync(() =>
            {
                InitializeCropLayout();

                // Gắn sự kiện chuột sau khi đã layout xong
                ImageToCrop.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                ImageToCrop.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                ImageToCrop.MouseMove += Image_MouseMove;
            });
        }
        private void InitializeCropLayout()
        {
            double canvasWidth = CanvasCrop.ActualWidth;
            double canvasHeight = CanvasCrop.ActualHeight;
            double cropSize = CropCircle.Width;

            // Scale tối thiểu để ảnh đủ lớn để crop
            double minScaleX = cropSize / originalImage.PixelWidth;
            double minScaleY = cropSize / originalImage.PixelHeight;
            double minScale = Math.Max(minScaleX, minScaleY);

            // Scale tối đa để ảnh đầy chiều rộng (chỉ nếu ảnh ngang < dọc)
            double maxScale = canvasWidth / originalImage.PixelWidth;

            // Nếu ảnh quá lớn thì cho phép zoom thêm chút
            if (maxScale < minScale) maxScale = minScale * 1.5;

            // Gán giới hạn cho Slider
            ZoomSlider.Minimum = minScale;
            ZoomSlider.Maximum = maxScale;
            ZoomSlider.SmallChange = (maxScale - minScale) / 100;
            ZoomSlider.LargeChange = (maxScale - minScale) / 10;
            ZoomSlider.TickFrequency = (maxScale - minScale) / 100;

            // Bắt đầu với scale nằm giữa min và max
            currentScale = (minScale + maxScale) / 2;
            ZoomSlider.Value = currentScale;

            imageScale.ScaleX = currentScale;
            imageScale.ScaleY = currentScale;

            double imgWidth = originalImage.PixelWidth * currentScale;
            double imgHeight = originalImage.PixelHeight * currentScale;

            imageTranslate.X = (canvasWidth - imgWidth) / 2;
            imageTranslate.Y = (canvasHeight - imgHeight) / 2;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            lastMousePosition = e.GetPosition(CanvasCrop);
            isDragging = true;
            ImageToCrop.CaptureMouse();
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            isDragging = false;
            ImageToCrop.ReleaseMouseCapture();
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point currentPos = e.GetPosition(CanvasCrop);
                Vector delta = currentPos - lastMousePosition;

                imageTranslate.X += delta.X;
                imageTranslate.Y += delta.Y;

                // Giới hạn kéo ảnh không vượt khỏi Canvas
                LimitImagePosition();

                lastMousePosition = currentPos;
            }
        }
        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || originalImage == null) return;

            double oldScale = currentScale;
            currentScale = e.NewValue;

            double scaleRatio = currentScale / oldScale;

            // Giữ tâm ảnh không thay đổi khi scale
            Point center = new Point(CanvasCrop.ActualWidth / 2, CanvasCrop.ActualHeight / 2);
            imageTranslate.X = (imageTranslate.X - center.X) * scaleRatio + center.X;
            imageTranslate.Y = (imageTranslate.Y - center.Y) * scaleRatio + center.Y;

            imageScale.ScaleX = currentScale;
            imageScale.ScaleY = currentScale;
        }
        private void LimitImagePosition()
        {
            double imgWidth = originalImage.PixelWidth * currentScale;
            double imgHeight = originalImage.PixelHeight * currentScale;

            double cropLeft = Canvas.GetLeft(CropCircle);
            double cropTop = Canvas.GetTop(CropCircle);
            double cropSize = CropCircle.Width;

            // Tính min/max theo crop square
            double minX = cropLeft + cropSize - imgWidth;
            double maxX = cropLeft;

            double minY = cropTop + cropSize - imgHeight;
            double maxY = cropTop;

            // Giới hạn X
            imageTranslate.X = Math.Min(maxX, Math.Max(minX, imageTranslate.X));
            // Giới hạn Y
            imageTranslate.Y = Math.Min(maxY, Math.Max(minY, imageTranslate.Y));
        }


        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Tính tỉ lệ ảnh gốc với canvas hiện tại
                double scaleX = originalImage.PixelWidth / (ImageToCrop.ActualWidth * currentScale);
                double scaleY = originalImage.PixelHeight / (ImageToCrop.ActualHeight * currentScale);

                // Vị trí crop circle trong canvas
                double circleLeft = Canvas.GetLeft(CropCircle);
                double circleTop = Canvas.GetTop(CropCircle);
                double circleSize = CropCircle.Width;

                // Tính tọa độ crop trong ảnh gốc
                double cropX = (circleLeft - imageTranslate.X) * scaleX;
                double cropY = (circleTop - imageTranslate.Y) * scaleY;
                double cropWidth = circleSize * scaleX;

                var rect = new Int32Rect(
                    (int)Math.Round(cropX),
                    (int)Math.Round(cropY),
                    (int)Math.Round(cropWidth),
                    (int)Math.Round(cropWidth)); // hình tròn → width = height

                // Kiểm tra crop hợp lệ
                if (rect.X < 0 || rect.Y < 0 ||
                    rect.X + rect.Width > originalImage.PixelWidth ||
                    rect.Y + rect.Height > originalImage.PixelHeight)
                {
                    MessageBox.Show("Ảnh hiện tại không đủ để cắt. Vui lòng zoom hoặc căn lại ảnh.");
                    return;
                }

                // Thực hiện crop
                CroppedResult = new CroppedBitmap(originalImage, rect);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi cắt ảnh: " + ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
