using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json;
using Pingme.Models;
using Pingme.Views.Pages;
using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace Pingme.Services
{
    public class FirebaseNotificationService
    {
        private const string APP_ID = "c94888a36cee4d71a2d36eb0e2cc6f9b";
        private readonly FirebaseClient client;
        private IDisposable _callSubscription;

        public FirebaseNotificationService()
        {
            client = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
        }

        // Lắng nghe tin nhắn đến (có thể dùng nếu muốn hiện thông báo)
        public void StartListeningMessages(string userId)
        {
            client
                .Child("messages")
                .AsObservable<Message>()
                .Subscribe(d =>
                {
                    if (d.EventType == FirebaseEventType.InsertOrUpdate && d.Object.ReceiverId == userId)
                    {
                        ShowLocalNotification(d.Object);
                    }
                });
        }

        private void ShowLocalNotification(Message msg)
        {
            Console.WriteLine($"📨 Tin nhắn mới từ {msg.SenderId}: {msg.Content}", "Thông báo");
        }

        // Gửi yêu cầu gọi đến Firebase
        public async Task SendCallRequest(string fromUserId, string toUserId)
        {
            string channel = $"call_{fromUserId}_{toUserId}";

            var callRequest = new CallRequest
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                ChannelName = channel,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            try
            {
                await client
                    .Child("calls")
                    .Child(toUserId)
                    .PostAsync(callRequest); // ✅ Ghi thêm, không ghi đè

                Console.WriteLine("✅ Đã gửi tín hiệu gọi (ghi thêm) qua Firebase!");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi gửi cuộc gọi: " + ex.Message);
            }
        }

        // Hàm xử lý khi có cuộc gọi đến
        private void OnCallRequestReceived(CallRequest request)
        {
            if (request.ToUserId != AuthService.CurrentUser.id) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"📞 Mở CallWindow: {request.ChannelName}");
                new CallWindow(APP_ID, request.ChannelName).Show();
            });
        }

        // Lắng nghe cuộc gọi đến (từ Firebase realtime)
        public void StartListeningForCalls(string userId)

        {
            StopListening();
            Console.WriteLine($"📡 Listening for calls on: {userId}");
            Console.WriteLine("Bạn là: " + AuthService.CurrentUser.id);
            string firebasePath = $"calls/{userId}";
            Console.WriteLine("Đang lắng nghe path: " + firebasePath);
            Console.WriteLine("AuthService.CurrentUser.id: " + AuthService.CurrentUser.id);


            _callSubscription = client
                .Child("calls")
                .Child(userId)
                .AsObservable<CallRequest>()
                .Where(f => f.EventType == FirebaseEventType.InsertOrUpdate)
                .Subscribe(call =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Console.WriteLine("📥 Firebase sự kiện nhận được");

                        if (call.Object != null)
                        {
                            Console.WriteLine($"📞 Có cuộc gọi từ: {call.Object.FromUserId}");
                            OnCallRequestReceived(call.Object);
                        }
                        else
                        {
                            Console.WriteLine("⚠️ call.Object là null");
                        }
                    });
                },
                error =>
                {
                    Console.WriteLine("❌ Lỗi Firebase: " + error.Message);
                });
        }

        // Ngắt lắng nghe (ví dụ khi đăng xuất)
        public void StopListening()
        {
            _callSubscription?.Dispose();
        }
    }
}
