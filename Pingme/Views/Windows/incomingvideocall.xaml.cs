using Pingme.Models;
using Pingme.Services;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pingme.Views.Windows
{
    public partial class incomingvideocall : Window
    {
        private readonly DispatcherTimer _timeoutTimer;
        private readonly CallRequest _request;
        private readonly DateTime _startTime;

        public incomingvideocall(CallRequest request)
        {
            InitializeComponent();
            _request = request;
            _startTime = DateTime.UtcNow;

            // Hiển thị tên người gọi (sẽ cần sửa lại nếu bạn lấy được FullName từ Id)
            CallerName.Text = $"Cuộc gọi từ {_request.FromUserId}";

            // Hiển thị avatar nếu có
            if (!string.IsNullOrEmpty(_request.CallerAvatarUrl))
            {
                AvatarEllipse.Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(_request.CallerAvatarUrl)),
                    Stretch = Stretch.UniformToFill
                };
            }
            else
            {
                AvatarEllipse.Fill = new SolidColorBrush(Colors.Gray);
            }

            // Bắt đầu đếm thời gian timeout
            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(40)
            };
            _timeoutTimer.Tick += TimeoutTimer_Tick;
            _timeoutTimer.Start();
        }

        private async void AcceptCall_Click(object sender, RoutedEventArgs e)
        {
            _timeoutTimer.Stop();

            DateTime callStartTime = DateTime.UtcNow;

            // Mở cửa sổ gọi, truyền vào request và thời gian bắt đầu
            var callWindow = new videoCallWindows(_request, callStartTime);
            callWindow.Show();

            this.Close();
        }

        private async void DeclineCall_Click(object sender, RoutedEventArgs e)
        {
            _timeoutTimer.Stop();
            await SendCallStatusAsync("declined", DateTime.UtcNow);
            this.Close();
        }

        private async void TimeoutTimer_Tick(object sender, EventArgs e)
        {
            _timeoutTimer.Stop();
            await SendCallStatusAsync("missed", DateTime.UtcNow);
            this.Close();
        }

        private async Task SendCallStatusAsync(string status, DateTime time)
        {
            var firebaseService = new FirebaseService();
            await firebaseService.SendCallStatusMessageAsync(
                _request.FromUserId,
                _request.ToUserId,
                _request.PushId,
                status,
                time
            );
        }
    }
}
