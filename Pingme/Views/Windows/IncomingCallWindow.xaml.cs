using Pingme.Models;
using Pingme.Services;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pingme.Views.Windows
{
    public partial class IncomingCallWindow : Window
    {
        private readonly DispatcherTimer _timeoutTimer;
        private readonly CallRequest _request;
        private readonly DateTime _startTime;
        private readonly FirebaseService _firebaseService;

        public IncomingCallWindow(CallRequest request)
        {
            InitializeComponent();
            _request = request;
            _startTime = DateTime.UtcNow;
            _firebaseService = new FirebaseService();

            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(40)
            };
            _timeoutTimer.Tick += TimeoutTimer_Tick;
            _timeoutTimer.Start();
            this.Tag = _request.PushId;
            ShowCallerInfo();
        }

        private void ShowCallerInfo()
        {
            CallerName.Text = $"Cuộc gọi từ {_request.FromUserId}";

            if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
            {
                try
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.UriSource = new Uri(_request.CallerAvatarUrl, UriKind.Absolute);
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();

                    AvatarEllipse.Fill = new ImageBrush
                    {
                        ImageSource = image,
                        Stretch = Stretch.UniformToFill
                    };
                }
                catch (Exception ex)
                {
                    Console.WriteLine("❌ Lỗi avatar: " + ex.Message);
                    AvatarEllipse.Fill = new SolidColorBrush(Colors.Red);
                }
            }
            else
            {
                AvatarEllipse.Fill = new SolidColorBrush(Colors.Gray);
            }
        }

        private async void AcceptCall_Click(object sender, RoutedEventArgs e)
        {
            _timeoutTimer.Stop();

            var callStartTime = DateTime.UtcNow;

            if (string.IsNullOrEmpty(_request.PushId))
            {
                MessageBox.Show("❌ Không thể chấp nhận cuộc gọi vì thiếu PushId.");
                return;
            }

            try
            {
                await _firebaseService.SendCallStatusMessageAsync(
                    _request.FromUserId,
                    _request.ToUserId,
                    _request.PushId,
                    "accepted",
                    callStartTime
                );

                Console.WriteLine("✅ Đã gửi trạng thái 'accepted' với PushId: " + _request.PushId);

                // Ngăn mở lại nếu đã có cửa sổ CallWindow với cùng PushId
                bool alreadyOpen = Application.Current.Windows
                    .OfType<CallWindow>()
                    .Any(w => w.Tag?.ToString() == _request.PushId);

                if (!alreadyOpen)
                {
                    var callWindow = new CallWindow(_request, callStartTime)
                    {
                        Tag = _request.PushId
                    };
                    callWindow.Show();
                }

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi chấp nhận cuộc gọi: " + ex.Message);
            }
        }

        private async void DeclineCall_Click(object sender, RoutedEventArgs e)
        {
            _timeoutTimer.Stop();

            if (string.IsNullOrEmpty(_request.PushId))
            {
                MessageBox.Show("❌ Không thể từ chối cuộc gọi vì thiếu PushId.");
                return;
            }

            try
            {
                await _firebaseService.SendCallStatusMessageAsync(
                    _request.FromUserId,
                    _request.ToUserId,
                    _request.PushId,
                    "declined",
                    DateTime.UtcNow
                );

                Console.WriteLine("✅ Đã gửi trạng thái 'declined' với PushId: " + _request.PushId);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi từ chối cuộc gọi: " + ex.Message);
            }
        }

        private async void TimeoutTimer_Tick(object sender, EventArgs e)
        {
            _timeoutTimer.Stop();

            if (string.IsNullOrEmpty(_request.PushId))
            {
                Console.WriteLine("⚠️ Timeout nhưng thiếu PushId.");
                this.Close();
                return;
            }

            try
            {
                await _firebaseService.SendCallStatusMessageAsync(
                    _request.FromUserId,
                    _request.ToUserId,
                    _request.PushId,
                    "missed",
                    DateTime.UtcNow
                );

                Console.WriteLine("✅ Đã gửi trạng thái 'missed' với PushId: " + _request.PushId);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("❌ Lỗi khi xử lý timeout: " + ex.Message);
                this.Close();
            }
        }

    }
}
