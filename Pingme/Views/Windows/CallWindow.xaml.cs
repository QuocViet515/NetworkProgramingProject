using Pingme.Models;
using Pingme.Services;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;

namespace Pingme.Views.Windows
{
    public partial class CallWindow : Window
    {
        private readonly CallRequest _request;
        private readonly AgoraVideoService _videoService;
        private DateTime _callStartTime;

        public CallWindow(CallRequest request, DateTime callStartTime)
        {
            InitializeComponent();
            _request = request;
            _callStartTime = callStartTime;
            _videoService = new AgoraVideoService(LocalVideoContainer, RemoteVideoContainer);

            Loaded += CallWindow_Loaded;
            Closed += CallWindow_Closed;
        }

        private void CallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Khởi tạo Agora
                _videoService.InitializeAgora(_request.AppId, _request.ChannelName);

                // Mặc định tắt camera
                _videoService.SetLocalVideoEnabled(false);
                LocalVideoContainer.Visibility = Visibility.Collapsed;
                LocalAvatar.Visibility = Visibility.Visible;

                // Load avatar người gọi
                if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
                {
                    LocalAvatar.Source = new BitmapImage(new Uri(_request.CallerAvatarUrl));
                }

                // Load avatar người nhận
                if (!string.IsNullOrEmpty(_request.ReceiverAvatarUrl))
                {
                    RemoteAvatar.Source = new BitmapImage(new Uri(_request.ReceiverAvatarUrl));
                }

                // Hiện avatar người nhận nếu video chưa có
                RemoteVideoContainer.Visibility = Visibility.Collapsed;
                RemoteAvatar.Visibility = Visibility.Visible;
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

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            string callType = "video"; // hoặc "audio"
            var callDuration = (DateTime.UtcNow - _callStartTime).TotalSeconds;

            var messageService = new FirebaseService();
            await messageService.SendCallSummaryMessageAsync(
                _request.FromUserId,
                _request.ToUserId,
                callType,
                (int)callDuration,
                DateTime.UtcNow
            );

            _videoService.LeaveChannel();
            this.Close();
        }
    }
}
