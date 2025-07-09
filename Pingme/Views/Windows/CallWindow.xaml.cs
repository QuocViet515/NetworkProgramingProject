using Pingme.Models;
using Pingme.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pingme.Views.Windows
{
    public partial class CallWindow : Window
    {
        private readonly CallRequest _request;
        private readonly AgoraVideoService _videoService;
        private DateTime _callStartTime;
        private bool _cameraOn = false;
        private bool _micOn = true;
        private DispatcherTimer _statusTimer;

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

                RemoteVideoContainer.Visibility = Visibility.Collapsed;
                RemoteAvatar.Visibility = Visibility.Visible;

                // 🎯 Bắt đầu kiểm tra trạng thái cuộc gọi mỗi 1s
                if (!string.IsNullOrEmpty(_request.PushId))
                {
                    _statusTimer = new DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(1)
                    };
                    _statusTimer.Tick += async (s, args) =>
                    {
                        try
                        {
                            var call = await new FirebaseService().GetCallRequestByIdAsync(_request.PushId);
                            if (call != null && call.status == "ended")
                            {
                                CallStatusText.Text = "📞 Cuộc gọi đã kết thúc";
                                CallStatusBanner.Visibility = Visibility.Visible;
                                await Task.Delay(3000);
                                this.Close();

                                _statusTimer.Stop();
                                //this.Close();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("⚠️ Lỗi kiểm tra trạng thái cuộc gọi: " + ex.Message);
                        }
                    };
                    _statusTimer.Start();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi khởi tạo cuộc gọi: " + ex.Message);
            }
        }

        private void CallWindow_Closed(object sender, EventArgs e)
        {
            _videoService.LeaveChannel();
            _statusTimer?.Stop();
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
            var firebase = new FirebaseService();
            string callType = _cameraOn ? "video" : "audio";
            var callDuration = (DateTime.UtcNow - _callStartTime).TotalSeconds;

            if (!string.IsNullOrEmpty(_request.PushId))
            {
                await firebase.SendCallStatusMessageAsync(
                    _request.FromUserId,
                    _request.ToUserId,
                    _request.PushId,
                    "ended",
                    DateTime.UtcNow
                );
            }

            await firebase.SendCallSummaryMessageAsync(
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
