using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pingme.Views.Windows
{
    public partial class ImageViewerWindow : Window
    {
        private readonly string _imagePath;
        private readonly string _originalFileName;

        public ImageViewerWindow(string imagePath)
        {
            InitializeComponent();
            _imagePath = imagePath;
            _originalFileName = EnsureExtension(imagePath);

            ImageDisplay.Source = new BitmapImage(new Uri(_imagePath, UriKind.Absolute));
        }

        private string EnsureExtension(string path)
        {
            string ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
                return Path.GetFileName(path);

            return Path.GetFileName(path) + ".png"; // fallback đuôi ảnh mặc định
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = _originalFileName,
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.webp;*.gif|All files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_imagePath, dialog.FileName, true);
                    MessageBox.Show("✅ Đã lưu ảnh.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Lỗi khi lưu ảnh: " + ex.Message);
                }
            }
        }
    }
}
