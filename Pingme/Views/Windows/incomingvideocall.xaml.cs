﻿using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Reactive.Linq;
namespace Pingme.Views.Windows
{
    public partial class incomingvideocall : Window
    {
        private readonly DispatcherTimer _timeoutTimer;
        private readonly DispatcherTimer _countdownTimer;
        private int _remainingSeconds = 40;
        private readonly FirebaseClient _firebaseClient = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");
        private IDisposable _callStatusSubscription;
        private readonly CallRequest _request;
        private readonly DateTime _startTime;
        private readonly FirebaseService _firebaseService;
        private DispatcherTimer _pollingTimer;
        private bool _callHandled = false;

        public incomingvideocall(CallRequest request)
        {
            // 🔒 Ngăn trùng cửa sổ cùng PushId
            foreach (var win in Application.Current.Windows.OfType<IncomingCallWindow>().ToList())
            {
                if (win.Tag?.ToString() == request.PushId)
                {
                    win.Close(); // Đóng cửa sổ cũ
                }
            }
            InitializeComponent();
            _request = request;
            _startTime = DateTime.UtcNow;
            _firebaseService = new FirebaseService();
            this.Tag = _request.PushId;

            _ = ShowCallerInfoAsync();

            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(40)
            };
            _timeoutTimer.Tick += TimeoutTimer_Tick;
            _timeoutTimer.Start();

            // ⏱️ Bắt đầu đếm ngược
            _countdownTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _countdownTimer.Tick += CountdownTimer_Tick;
            _countdownTimer.Start();

            UpdateCountdownVisual(_remainingSeconds);
            ListenForCallStatus();
        }

        private async Task ShowCallerInfoAsync()
        {
            string displayName = _request.FromUserId;

            try
            {
                var user = await _firebaseService.GetUserByIdAsync(_request.FromUserId);
                if (user != null && !string.IsNullOrEmpty(user.FullName))
                {
                    displayName = user.FullName;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Lỗi khi lấy tên người dùng: " + ex.Message);
            }

            CallerName.Text = $"Cuộc gọi từ {displayName}";

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

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            _remainingSeconds--;
            CountdownText.Text = $"00:{_remainingSeconds:00}";
            UpdateCountdownVisual(_remainingSeconds);

            if (_remainingSeconds <= 0)
            {
                _countdownTimer.Stop();
                TimeoutTimer_Tick(null, null);
            }
        }

        private void UpdateCountdownVisual(int remainingSeconds)
        {
            // Path vòng tròn xanh là phần tử thứ 1 trong Grid (Ellipse là thứ 0)
            var path = ((Grid)CountdownText.Parent).Children.OfType<Path>().FirstOrDefault();
            if (path == null) return;

            double percentage = remainingSeconds / 40.0;
            double angle = 360 * percentage;

            path.Data = CreateArcGeometry(angle, 60, 60, 50);
        }

        private Geometry CreateArcGeometry(double angle, double centerX, double centerY, double radius)
        {
            if (angle <= 0) angle = 0.01; // tránh lỗi Path khi 0 độ

            double startAngle = -90; // bắt đầu từ đỉnh
            double endAngle = startAngle + angle;

            double startRadians = startAngle * Math.PI / 180;
            double endRadians = endAngle * Math.PI / 180;

            Point startPoint = new Point(
                centerX + radius * Math.Cos(startRadians),
                centerY + radius * Math.Sin(startRadians)
            );

            Point endPoint = new Point(
                centerX + radius * Math.Cos(endRadians),
                centerY + radius * Math.Sin(endRadians)
            );

            bool isLargeArc = angle > 180;

            var segment = new ArcSegment
            {
                Point = endPoint,
                Size = new Size(radius, radius),
                IsLargeArc = isLargeArc,
                SweepDirection = SweepDirection.Clockwise
            };

            var figure = new PathFigure
            {
                StartPoint = startPoint,
                IsClosed = false
            };
            figure.Segments.Add(segment);

            var geometry = new PathGeometry();
            geometry.Figures.Add(figure);
            return geometry;
        }

        private async void AcceptCall_Click(object sender, RoutedEventArgs e)
        {
            if (_callHandled) return; // 🔒 đã xử lý rồi
            _callHandled = true;

            _timeoutTimer.Stop();
            _countdownTimer.Stop();

            if (string.IsNullOrEmpty(_request.PushId))
            {
                MessageBox.Show("❌ Không thể chấp nhận cuộc gọi vì thiếu PushId.");
                return;
            }

            var callStartTime = DateTime.UtcNow;

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

                bool alreadyOpen = Application.Current.Windows
                    .OfType<videoCallWindows>()
                    .Any(w => w.Tag?.ToString() == _request.PushId);

                if (!alreadyOpen)
                {
                    var callWindow = new videoCallWindows(_request, callStartTime)
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
            _countdownTimer.Stop();

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
            _countdownTimer.Stop();

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
        

        public void ListenForCallStatus()
        {
            string pushId = _request.PushId;

            _callStatusSubscription = _firebaseClient
                .Child("calls")
                .Child(pushId)
                .AsObservable<CallRequest>()
                .Where(f => f.EventType == FirebaseEventType.InsertOrUpdate &&
                            f.Object != null && f.Object.status != "waiting")
                .Subscribe(async call =>
                {
                    Console.WriteLine($"📲 [Receiver] Trạng thái gọi: {call.Object.status}");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        HandleStatus(call.Object);
                    });
                });

            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += PollStatus;
            _pollingTimer.Start();
        }
        private async void PollStatus(object sender, EventArgs e)
        {
            var current = await _firebaseClient
                .Child("calls")
                .Child(_request.PushId)
                .OnceSingleAsync<CallRequest>();

            if (current != null && current.status != "waiting")
            {
                _pollingTimer.Stop();
                _callStatusSubscription?.Dispose();
                HandleStatus(current);
            }
        }

        private async Task HandleStatus(CallRequest updatedRequest)
        {
            if (_callHandled) return; // 🔒 không xử lý lại
            _callHandled = true;

            switch (updatedRequest.status)
            {
                case "accepted":
                    Console.WriteLine("📲 Cuộc gọi đã được chấp nhận từ phía người kia.");
                    bool alreadyOpen = Application.Current.Windows
                        .OfType<videoCallWindows>()
                        .Any(w => w.Tag?.ToString() == _request.PushId);

                    if (!alreadyOpen)
                    {
                        var callWindow = new videoCallWindows(_request, DateTime.UtcNow)
                        {
                            Tag = _request.PushId
                        };
                        callWindow.Show();
                    }
                    this.Close();
                    break;

                case "declined":
                    StatusText.Text = "Bạn đã từ chối cuộc gọi này.";
                    await Task.Delay(3000); // Hiển thị trong 3 giây
                    this.Close();
                    break;

                case "missed":
                    StatusText.Text = "Cuộc gọi bị nhỡ.";
                    await Task.Delay(3000); // Hiển thị trong 3 giây
                    this.Close();
                    break;

                case "canceled":
                    StatusText.Text = "Người gọi đã hủy cuộc gọi.";
                    await Task.Delay(3000); // Hiển thị trong 3 giây
                    this.Close();
                    break;
            }
        }


    }
}
