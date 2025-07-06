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
        private bool _cameraOn = false;
        private bool _micOn = true;

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
                _videoService.SetLocalVideoEnabled(_cameraOn);
                _videoService.SetLocalAudioEnabled(_micOn);
                LocalVideoContainer.Visibility = Visibility.Collapsed;
                LocalAvatar.Visibility = Visibility.Visible;

                // Avatar người gọi
                if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
                    LocalAvatar.Source = new BitmapImage(new Uri(_request.CallerAvatarUrl));

                // Avatar người nhận
                if (!string.IsNullOrEmpty(_request.ReceiverAvatarUrl))
                    RemoteAvatar.Source = new BitmapImage(new Uri(_request.ReceiverAvatarUrl));

                // Hiện avatar nếu video chưa có
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
            _cameraOn = !_cameraOn;
            var btn = sender as ToggleButton;
            btn.Content = _cameraOn ? "📷" : "🚫";

            _videoService.SetLocalVideoEnabled(_cameraOn);
            LocalVideoContainer.Visibility = _cameraOn ? Visibility.Visible : Visibility.Collapsed;
            LocalAvatar.Visibility = _cameraOn ? Visibility.Collapsed : Visibility.Visible;
        }

        private void BtnToggleMic_Click(object sender, RoutedEventArgs e)
        {
            _micOn = !_micOn;
            var btn = sender as ToggleButton;
            btn.Content = _micOn ? "🎤" : "🔇";

            _videoService.SetLocalAudioEnabled(_micOn);
        }

        private async void BtnEndCall_Click(object sender, RoutedEventArgs e)
        {
            string callType = _cameraOn ? "video" : "audio";
            var callDuration = (DateTime.UtcNow - _callStartTime).TotalSeconds;

            var firebaseService = new FirebaseService();
            await firebaseService.SendCallSummaryMessageAsync(
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
