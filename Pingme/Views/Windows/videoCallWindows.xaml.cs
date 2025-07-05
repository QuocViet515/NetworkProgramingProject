using Pingme.Models;
using Pingme.Services;
using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Forms; // ⚠️ cho Panel
using System.Windows.Forms.Integration; // ⚠️ cho WindowsFormsHost

namespace Pingme.Views.Windows
{
    public partial class videoCallWindows : Window
    {
        private readonly CallRequest _request;
        private readonly AgoraVideoService _videoService;
        private readonly Panel _localVideoPanel;

        private DateTime _callStartTime;
        private bool _cameraOn = true;
        private bool _micOn = true;

        public videoCallWindows(CallRequest request, DateTime callStartTime)
        {
            InitializeComponent();

            _request = request;
            _callStartTime = callStartTime;

            // Tạo panel WinForms chứa video local
            _localVideoPanel = new Panel
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill
            };
            _localVideoHost.Child = _localVideoPanel; // Gán vào WindowsFormsHost

            // Truyền handle của panel cho Agora
            _videoService = new AgoraVideoService(_localVideoPanel.Handle, RemoteVideoContainer);

            Loaded += CallWindow_Loaded;
            Closed += CallWindow_Closed;
        }

        private void CallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Khởi tạo Agora
                _videoService.InitializeAgora(_request.AppId, _request.ChannelName);

                // 2. Bật camera + mic ban đầu
                _videoService.SetLocalVideoEnabled(true);
                _videoService.SetLocalAudioEnabled(true);

                // 3. Avatar của người gọi (hiển thị local nếu tắt cam)
                if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
                {
                    LocalAvatar.Source = new BitmapImage(new Uri(_request.CallerAvatarUrl));
                    LocalAvatar.Visibility = Visibility.Visible;
                }

                // 4. Avatar của người nhận (hiển thị remote nếu chưa có video)
                if (!string.IsNullOrEmpty(_request.ReceiverAvatarUrl))
                {
                    RemoteAvatar.Source = new BitmapImage(new Uri(_request.ReceiverAvatarUrl));
                    RemoteAvatar.Visibility = Visibility.Visible;
                }

                // 5. Hiện video local nếu camera đang bật
                _localVideoHost.Visibility = Visibility.Visible;
                LocalAvatar.Visibility = Visibility.Collapsed;
                RemoteVideoContainer.Visibility = Visibility.Visible;


            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("❌ Lỗi khi khởi tạo cuộc gọi: " + ex.Message);
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

            if (_cameraOn)
            {
                _localVideoHost.Visibility = Visibility.Visible;
                LocalAvatar.Visibility = Visibility.Collapsed;
            }
            else
            {
                _localVideoHost.Visibility = Visibility.Collapsed;
                LocalAvatar.Visibility = Visibility.Visible;
            }
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
