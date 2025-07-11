using Pingme.Models;
using Pingme.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms; // ⚠️ cho Panel
using System.Windows.Forms.Integration; // ⚠️ cho WindowsFormsHost
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
        private DispatcherTimer _statusTimer;

        public videoCallWindows(CallRequest request, DateTime callStartTime)
        {
            InitializeComponent(); // ⬅️ PHẢI gọi dòng này đầu tiên để UI controls được khởi tạo

            _request = request;
            _callStartTime = callStartTime;

            // Tạo panel WinForms chứa video local
            _localVideoPanel = new Panel
            {
                BackColor = System.Drawing.Color.Black,
                Dock = DockStyle.Fill
            };
            _localVideoHost.Child = _localVideoPanel;

            // ✅ LÚC NÀY RemoteVideoContainer đã KHÔNG còn null
            _videoService = new AgoraVideoService(_localVideoPanel.Handle, RemoteVideoContainer);
            _videoService.InitializeAgora(_request.AppId, _request.ChannelName);

            Loaded += CallWindow_Loaded;
            Closed += CallWindow_Closed;
        }


        private async void CallWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var firebase = new FirebaseService();
                // Hiển thị tên người nhận nếu có
                var receiver =await firebase.GetUserByIdAsync(_request.ToUserId);
                string receiverName = receiver?.UserName ?? "Người nhận";
                // Hiển thị tên người nhận nếu có
                if (!string.IsNullOrEmpty(receiverName))
                {
                    remoteUser.Text = receiverName;
                }

                Console.WriteLine(RemoteVideoContainer == null ? "[DEBUG] RemoteVideoContainer is null" : "[DEBUG] RemoteVideoContainer OK");
                // 1. Khởi tạo Agora
                _videoService.InitializeAgora(_request.AppId, _request.ChannelName);

                // 2. Bật camera + mic ban đầu
                _videoService.SetLocalVideoEnabled(_cameraOn);
                _videoService.SetLocalAudioEnabled(_micOn);

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

                // 6. Bắt đầu kiểm tra trạng thái cuộc gọi mỗi 1s
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
                                CallStatusText.Text = "📞 Cuộc gọi đã kết thúc ";
                                CallStatusBanner.Visibility = Visibility.Visible;

                                // Tự ẩn sau 1 giây (nếu muốn)
                                await Task.Delay(1000);

                                _statusTimer.Stop();
                                this.Close();
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
                System.Windows.MessageBox.Show("❌ Lỗi khi khởi tạo cuộc gọi: " + ex.Message);
            }
            UpdateAvatarVisibility();
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
            UpdateAvatarVisibility();

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

            var firebase = new FirebaseService();

            // Gửi trạng thái kết thúc nếu có PushId
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

            // Gửi thống kê cuộc gọi
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
        private void UpdateAvatarVisibility()
        {
            // Xử lý local video/camera
            if (_cameraOn)
            {
                _localVideoGrid.Visibility = Visibility.Visible;   // Sửa tại đây
                _localVideoHost.Visibility = Visibility.Visible;
                LocalAvatar.Visibility = Visibility.Collapsed;
            }
            else
            {
                _localVideoGrid.Visibility = Visibility.Collapsed; // Sửa tại đây
                _localVideoHost.Visibility = Visibility.Collapsed;
                LocalAvatar.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
                {
                    LocalAvatar.Source = new BitmapImage(new Uri(_request.CallerAvatarUrl));
                }
            }


            // Xử lý remote video (nếu chưa nhận video thì hiển thị avatar)
            if (RemoteVideoContainer.Visibility == Visibility.Visible)
            {
                RemoteAvatar.Visibility = Visibility.Collapsed;
            }
            else
            {
                RemoteAvatar.Visibility = Visibility.Visible;

                if (!string.IsNullOrEmpty(_request.ReceiverAvatarUrl))
                {
                    RemoteAvatar.Source = new BitmapImage(new Uri(_request.ReceiverAvatarUrl));
                }
            }
        }

    }
}
