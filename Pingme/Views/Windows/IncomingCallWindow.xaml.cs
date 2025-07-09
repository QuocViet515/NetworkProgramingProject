using Firebase.Database;
using Firebase.Database.Streaming;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Firebase.Database;
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
    public partial class IncomingCallWindow : Window
    {
        private readonly DispatcherTimer _timeoutTimer;
        private readonly CallRequest _request;
        private readonly DateTime _startTime;
        private readonly FirebaseService _firebaseService;
        private readonly FirebaseClient _firebaseClient = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");
        private IDisposable _callStatusSubscription;
        private DispatcherTimer _pollingTimer;
        private bool _callHandled = false;
        public IncomingCallWindow(CallRequest request)
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

            _timeoutTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(40)
            };
            _timeoutTimer.Tick += TimeoutTimer_Tick;
            _timeoutTimer.Start();

            ShowCallerInfo();
            LoadAvatar(_request.CallerAvatarUrl);
            StartCountdownRing();
            ListenForCallStatus();
        }

        private async void ShowCallerInfo()
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
        private DispatcherTimer _timer;
        private int _countdownSeconds = 40;

        private void StartCountdownRing()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            _timer.Tick += (s, e) =>
            {
                _countdownSeconds--;

                CountdownText.Text = $"{_countdownSeconds}s";
                ProgressArc.Angle = (_countdownSeconds / 40.0) * 360;

                if (_countdownSeconds <= 0)
                {
                    _timer.Stop();
                    this.Close(); // hoặc đóng cửa sổ/miss call
                }
            };

            _timer.Start();
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
        private void LoadAvatar(string avatarUrl)
        {
            try
            {
                var brush = new ImageBrush();

                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    var bitmap = new BitmapImage(new Uri(avatarUrl, UriKind.Absolute));
                    brush.ImageSource = bitmap;
                }
                else
                {
                    brush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Icons/avatar-default.png"));
                }

                brush.Stretch = Stretch.UniformToFill;
                AvatarEllipse.Fill = brush;
            }
            catch
            {
                AvatarEllipse.Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Icons/avatar-default.png")),
                    Stretch = Stretch.UniformToFill
                };
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
                        .OfType<CallWindow>()
                        .Any(w => w.Tag?.ToString() == _request.PushId);

                    if (!alreadyOpen)
                    {
                        var callWindow = new CallWindow(_request, DateTime.UtcNow)
                        {
                            Tag = _request.PushId
                        };
                        callWindow.Show();
                    }
                    this.Close();
                    break;

                case "declined":
                    StatusText.Text = "Bạn đã từ chối cuộc gọi này.";
                    await Task.Delay(1000); // Hiển thị trong 1 giây
                    this.Close();
                    break;

                case "missed":
                    StatusText.Text = "Cuộc gọi bị nhỡ.";
                    await Task.Delay(1000); // Hiển thị trong 1 giây
                    this.Close();
                    break;

                case "canceled":
                    StatusText.Text = "Người gọi đã hủy cuộc gọi.";
                    await Task.Delay(1000); // Hiển thị trong 1 giây
                    this.Close();
                    break;
            }
        }
    }
}
