using Agora.Rtc;
using Pingme.Helpers;
using Pingme.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Pingme.Models;
namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for MessageList.xaml
    /// </summary>
    public partial class MessageList : UserControl
    {
        public MessageList()
        {
            InitializeComponent();
        }
        private async void DownloadFile_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Message msg)
            {
                string fileId = msg.FileId;

                Console.WriteLine($"[DEBUG] fileId từ Message.Content: {fileId}");

                if (fileId.StartsWith("[") || string.IsNullOrWhiteSpace(fileId))
                {
                    MessageBox.Show($"❌ File ID không hợp lệ:\n\nID: {fileId}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string fileName = msg?.FileName ?? "unknown_file.dat";
                var dialog = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = fileName,
                    Title = "Chọn nơi lưu file"
                };

                if (dialog.ShowDialog() == true)
                {
                    string receiverId = AuthService.CurrentUser.Id;
                    string privateKeyPath = Pingme.Helpers.KeyManager.GetPrivateKeyPath(receiverId);
                    string savePath = System.IO.Path.GetDirectoryName(dialog.FileName);

                    var fileService = new FirebaseFileService();
                    await fileService.DownloadAndDecryptFileAsync(fileId, privateKeyPath, dialog.FileName);

                    MessageBox.Show($"✅ File đã được tải và giải mã thành công!\n\n{dialog.FileName}", "Thành công");
                }
            }
        }
    }
}
