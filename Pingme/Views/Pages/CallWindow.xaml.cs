using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using Pingme.Services;

namespace Pingme.Views.Pages
{
    public partial class CallWindow : Window
    {
        private readonly string _appId;
        private readonly string _channelName;
        private readonly AgoraVideoService _videoService;

        public CallWindow(string appId, string channel)
        {
            InitializeComponent();

            _appId = appId;
            _channelName = channel;

            _videoService = new AgoraVideoService(LocalVideoContainer, RemoteVideoContainer);

            Loaded += CallWindow_Loaded;
            Closed += CallWindow_Closed;
        }

        private void CallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _videoService.InitializeAgora(_appId, _channelName);
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi khởi tạo cuộc gọi: " + ex.Message);
            }
        }

        private void CallWindow_Closed(object sender, EventArgs e)
        {
            _videoService.LeaveChannel();
        }

        private void BtnToggleCamera_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            bool cameraOn = btn.IsChecked == true;
            btn.Content = cameraOn ? "📷" : "🚫";

            _videoService.SetLocalVideoEnabled(cameraOn);

            // Chuyển đổi giữa video và avatar
            LocalVideoContainer.Visibility = cameraOn ? Visibility.Visible : Visibility.Collapsed;
            LocalAvatar.Visibility = cameraOn ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnToggleMic_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as ToggleButton;
            bool micOn = btn.IsChecked == true;
            btn.Content = micOn ? "🎤" : "🔇";

            _videoService.SetLocalAudioEnabled(micOn);
        }

        private void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            _videoService.LeaveChannel();
            this.Close();
        }
    }
}
