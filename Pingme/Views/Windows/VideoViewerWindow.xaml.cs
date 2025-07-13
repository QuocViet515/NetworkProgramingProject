using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Pingme.Views.Windows
{
    public partial class VideoViewerWindow : Window
    {
        private readonly string _videoPath;
        private readonly string _originalFileName;

        public VideoViewerWindow(string videoPath)
        {
            InitializeComponent();
            _videoPath = videoPath;

            // Gán lại tên file gốc nếu có đuôi
            _originalFileName = EnsureExtension(videoPath);

            VideoPlayer.Source = new Uri(_videoPath, UriKind.Absolute);
        }

        private string EnsureExtension(string path)
        {
            string ext = Path.GetExtension(path);
            if (!string.IsNullOrEmpty(ext))
                return Path.GetFileName(path);

            // Nếu không có đuôi -> thêm mặc định
            return Path.GetFileName(path) + ".mp4";
        }

        private void Play_Click(object sender, RoutedEventArgs e)
        {
            VideoPlayer.Play();
        }

        private void Download_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                FileName = _originalFileName,
                Filter = "Video files|*.mp4;*.webm;*.avi|All files|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    File.Copy(_videoPath, dialog.FileName, true);
                    MessageBox.Show("✅ Đã lưu video.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Lỗi khi lưu video: " + ex.Message);
                }
            }
        }

        private void VideoPlayer_MediaOpened(object sender, RoutedEventArgs e) { }

        private void VideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            MessageBox.Show("❌ Không phát được video: " + e.ErrorException?.Message);
        }
    }
}
