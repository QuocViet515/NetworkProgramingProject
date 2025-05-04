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
        private Point startPoint;
        private Rectangle rect;
        private BitmapImage originalImage;
        public CroppedBitmap CroppedResult { get; private set; }

        public CropImageWindow(string imagePath)
        {
            InitializeComponent();
            originalImage = new BitmapImage(new Uri(imagePath));
            ImageToCrop.Source = originalImage;

            CanvasCrop.MouseLeftButtonDown += Canvas_MouseLeftButtonDown;
            CanvasCrop.MouseMove += Canvas_MouseMove;
            CanvasCrop.MouseLeftButtonUp += Canvas_MouseLeftButtonUp;
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(CanvasCrop);
            CropRect.Width = 0;
            CropRect.Height = 0;
            Canvas.SetLeft(CropRect, startPoint.X);
            Canvas.SetTop(CropRect, startPoint.Y);
            CropRect.Visibility = Visibility.Visible;
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point pos = e.GetPosition(CanvasCrop);
                double width = Math.Abs(pos.X - startPoint.X);
                double height = Math.Abs(pos.Y - startPoint.Y);

                CropRect.Width = width;
                CropRect.Height = height;
                Canvas.SetLeft(CropRect, Math.Min(pos.X, startPoint.X));
                Canvas.SetTop(CropRect, Math.Min(pos.Y, startPoint.Y));
            }
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // Do nothing yet
        }

        private void CropConfirm_Click(object sender, RoutedEventArgs e)
        {
            double x = Canvas.GetLeft(CropRect) * originalImage.PixelWidth / ImageToCrop.ActualWidth;
            double y = Canvas.GetTop(CropRect) * originalImage.PixelHeight / ImageToCrop.ActualHeight;
            double w = CropRect.Width * originalImage.PixelWidth / ImageToCrop.ActualWidth;
            double h = CropRect.Height * originalImage.PixelHeight / ImageToCrop.ActualHeight;

            var rect = new Int32Rect((int)x, (int)y, (int)w, (int)h);
            CroppedResult = new CroppedBitmap(originalImage, rect);

            DialogResult = true;
            Close();
        }
    }
}
