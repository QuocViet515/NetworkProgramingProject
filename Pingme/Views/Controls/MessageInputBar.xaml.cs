using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Pingme.Services;
using Pingme.Models;
using Pingme.ViewModels;
using Pingme.Views.Pages;

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

        private void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                MessageBox.Show($"📁 Gửi file: {System.IO.Path.GetFileName(filePath)}");
                // TODO: gửi file lên ViewModel
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

        private void Call_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ChatViewModel;
            if (viewModel == null || viewModel.SelectedUser == null)
            {
                MessageBox.Show("❗ Chưa chọn người để gọi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            MessageBox.Show($"MessageInputBar.SelectedUser = {viewModel?.SelectedUser?.fullName ?? "null"}");
            string appId = "c94888a36cee4d71a2d36eb0e2cc6f9b";
            string currentUserId = AuthService.CurrentUser.id;
            string peerUserId = viewModel.SelectedUser.id;
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
        }
    }
}
