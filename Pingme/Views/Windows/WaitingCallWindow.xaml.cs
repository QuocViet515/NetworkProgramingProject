﻿using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Pingme.Models;
using Pingme.Services;
using Pingme.Views.Windows;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Pingme.Views.Windows
{
    public partial class WaitingCallWindow : Window
    {
        private readonly CallRequest _request;
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly FirebaseClient _firebaseClient = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");
        private IDisposable _callStatusSubscription;
        private DispatcherTimer _pollingTimer;

        public WaitingCallWindow(CallRequest request)
        {
            InitializeComponent();
            _request = request;

            Console.WriteLine($"📥 Đang lắng nghe trạng thái tại: /calls/{_request.PushId}");

            ListenForCallStatus();
            LoadAvatar();
        }

        public async void ListenForCallStatus()
        {
            var pushId = _request.PushId;

            // Bước 1: Kiểm tra trạng thái hiện tại
            var snapshot = await _firebaseClient
                .Child("calls")
                .Child(pushId)
                .OnceSingleAsync<CallRequest>();

            if (snapshot != null && snapshot.status != "waiting")
            {
                Console.WriteLine("⚡ Trạng thái đã cập nhật trước khi lắng nghe. Xử lý ngay.");
                HandleStatus(snapshot);
                return;
            }

            // Bước 2: Lắng nghe sự kiện realtime
            _callStatusSubscription = _firebaseClient
                .Child("calls")
                .Child(pushId)
                .AsObservable<CallRequest>()
                .Where(f => f.EventType == FirebaseEventType.InsertOrUpdate &&
                            f.Object != null &&
                            f.Object.status != "waiting")
                .Subscribe(async status =>
                {
                    Console.WriteLine($"📡 Trạng thái mới từ listener: {status.Object.status}");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        HandleStatus(status.Object);
                    });
                },
                error => Console.WriteLine("❌ Lỗi khi lắng nghe trạng thái cuộc gọi: " + error.Message));

            // Bước 3: Bắt đầu polling mỗi 1 giây
            _pollingTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _pollingTimer.Tick += PollCallStatus;
            _pollingTimer.Start();
        }

        private async void PollCallStatus(object sender, EventArgs e)
        {
            try
            {
                var current = await _firebaseClient
                    .Child("calls")
                    .Child(_request.PushId)
                    .OnceSingleAsync<CallRequest>();

                if (current != null && current.status != "waiting")
                {
                    Console.WriteLine($"🔁 Poll phát hiện status mới: {current.status}");

                    _pollingTimer.Stop();
                    _callStatusSubscription?.Dispose();

                    HandleStatus(current);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("⚠️ Lỗi khi polling: " + ex.Message);
            }
        }

        private async Task HandleStatus(CallRequest updatedRequest)
        {
            switch (updatedRequest.status)
            {
                case "accepted":
                    //Console.WriteLine("✅ Trạng thái accepted → Mở videoCallWindows...");
                    Window callwindow;
                    if (updatedRequest.Type == "video")
                    {
                        callwindow = new videoCallWindows(updatedRequest, DateTime.UtcNow);
                    }
                    else
                    {
                        callwindow = new CallWindow(updatedRequest, DateTime.UtcNow);
                    }

                    // ✅ Hiển thị cửa sổ trước khi đóng cái cũ
                    callwindow.Show();
                    this.Close();
                    break;

                case "declined":
                    //MessageBox.Show("❌ Cuộc gọi đã bị từ chối.");
                    CallStatus.Text = "Người dùng từ chối cuộc gọi";
                    await Task.Delay(2000); // Hiển thị trong 2 giây
                    this.Close();
                    break;

                case "missed":
                    //MessageBox.Show("⚠️ Cuộc gọi bị nhỡ.");
                    CallStatus.Text = "Người nhận không nhấc máy";
                    await Task.Delay(2000); // Hiển thị trong 2 giây
                    this.Close();
                    break;

                case "canceled":
                    //MessageBox.Show("🛑 Cuộc gọi đã bị hủy.");
                    CallStatus.Text = "Cuộc gọi đã bị hủy";
                    await Task.Delay(2000); // Hiển thị trong 2 giây
                    this.Close();
                    break;
            }
        }
        private void LoadAvatar()
        {
            string avatarUrl = _request.ReceiverAvatarUrl;

            try
            {
                ImageBrush avatarBrush = new ImageBrush();

                if (!string.IsNullOrWhiteSpace(avatarUrl))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(avatarUrl, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    avatarBrush.ImageSource = bitmap;
                }
                else
                {
                    avatarBrush.ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Icons/avatar-default.png"));
                }

                avatarBrush.Stretch = Stretch.UniformToFill;
                ReceiverAvatarUrl.Fill = avatarBrush;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi tải ảnh đại diện: " + ex.Message);
                ReceiverAvatarUrl.Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/Assets/Icons/avatar-default.png")),
                    Stretch = Stretch.UniformToFill
                };
            }
        }

        private async void CancelCall_Click(object sender, RoutedEventArgs e)
        {
            await _firebaseService.SendCallStatusMessageAsync(_request.FromUserId, _request.ToUserId, _request.PushId, "canceled", DateTime.UtcNow);
            this.Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _callStatusSubscription?.Dispose();
            _pollingTimer?.Stop();
        }
    }
}
