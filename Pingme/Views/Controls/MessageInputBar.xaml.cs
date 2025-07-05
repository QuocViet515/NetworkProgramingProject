using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Pingme.Services;
using Pingme.Models;
using Pingme.ViewModels;
using Pingme.Views.Pages;
using Pingme.Views.Windows;

namespace Pingme.Views.Controls
{
    public partial class MessageInputBar : UserControl
    {
        private AgoraVideoService _videoService;
        private bool isMicOn = true;
        private bool isCamOn = true;
        private CallWindow callWindow;

        public MessageInputBar()
        {
            InitializeComponent();
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChatViewModel;
            if (viewModel != null && viewModel.SelectedUser != null)
            {
                string content = MessageInput.Text.Trim();
                if (!string.IsNullOrEmpty(content))
                {
                    await viewModel.SendMessage(content);
                    MessageInput.Text = string.Empty;
                }
            }
        }

        private async void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChatViewModel;
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                MessageBox.Show($"📁 Đang gửi file: {fileName}");

                string senderId = AuthService.CurrentUser.Id;
                string receiverId = viewModel.SelectedUser.Id;

                if (string.IsNullOrEmpty(receiverId))
                {
                    MessageBox.Show("❌ Chưa chọn người nhận!");
                    return;
                }

                try
                {
                    var firebaseService = new FirebaseService();
                    string receiverPublicKeyXml = await firebaseService.GetPublicKeyAsync(receiverId);

                    Console.WriteLine("🧾 filePath: " + filePath);
                    Console.WriteLine("📥 receiverId: " + receiverId);
                    Console.WriteLine("📤 senderId: " + senderId);
                    Console.WriteLine("🔐 receiverPublicKeyXml (50 ký tự đầu): " + receiverPublicKeyXml?.Substring(0, Math.Min(receiverPublicKeyXml.Length, 50)));

                    var fileService = new FirebaseFileService();
                    await fileService.UploadEncryptedFileAsync(
                        filePath,
                        receiverPublicKeyXml,
                        senderId,
                        receiverId
                    );

                    MessageBox.Show($"✅ File \"{fileName}\" đã gửi thành công!");
                }

                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Lỗi khi gửi file: {ex.Message}");
                }
            }
        }


        private void ToggleMic_Click(object sender, RoutedEventArgs e)
        {
            isMicOn = !isMicOn;
            _videoService?.ToggleMic(isMicOn);
            MicToggleBtn.Content = isMicOn ? "🎤" : "🚫🎤";
        }

        private void ToggleCam_Click(object sender, RoutedEventArgs e)
        {
            isCamOn = !isCamOn;
            _videoService?.ToggleCamera(isCamOn);
            CamToggleBtn.Content = isCamOn ? "🎥" : "🙈";
        }

        private async void Call_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChatViewModel;
            if (viewModel == null || viewModel.SelectedUser == null)
            {
                MessageBox.Show("❗ Chưa chọn người để gọi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show($"📞 Gọi tới: {viewModel.SelectedUser.FullName}");

            string appId = "c94888a36cee4d71a2d36eb0e2cc6f9b";
            string currentUserId = AuthService.CurrentUser.Id;
            string peerUserId = viewModel.SelectedUser.Id;
            string channel = $"call_{currentUserId}_{peerUserId}";

            if (callWindow == null || !callWindow.IsLoaded)
            {
                callWindow = new CallWindow(appId, channel);
                callWindow.Show();
            }
            else
            {
                callWindow.Activate();
            }

            // ✅ Gửi tín hiệu gọi qua Firebase
            var firebase = new FirebaseNotificationService();
            await firebase.SendCallRequest(currentUserId, peerUserId);
            MessageBox.Show("✅ Đã gửi tín hiệu gọi qua Firebase!");
        }

    }
}
