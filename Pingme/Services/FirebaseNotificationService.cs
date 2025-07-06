using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json;
using Pingme.Helpers;
using Pingme.Models;
using Pingme.Views.Pages;
using Pingme.Views.Windows;
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
            client = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");
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
        public async Task SendCallRequest(string fromUserId, string toUserId, string type)
        {
            string channel = $"call_{fromUserId}_{toUserId}";

            // 🔍 Lấy profile người gọi
            var fromUser = await client
                .Child("users")
                .Child(fromUserId)
                .OnceSingleAsync<User>();

            // 🔍 Lấy profile người nhận
            var toUser = await client
                .Child("users")
                .Child(toUserId)
                .OnceSingleAsync<User>();

            var callRequest = new CallRequest
            {
                FromUserId = fromUserId,
                ToUserId = toUserId,
                ChannelName = channel,
                CallerAvatarUrl = fromUser.AvatarUrl,
                ReceiverAvatarUrl = toUser.AvatarUrl,
                Type = type,
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            try
            {
                await client
                    .Child("calls")
                    .Child(toUserId)
                    .PostAsync(callRequest);

                Console.WriteLine("✅ Đã gửi tín hiệu gọi " + type);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi gửi cuộc gọi: " + ex.Message);
            }
        }




        // Hàm xử lý khi có cuộc gọi đến
        private async void OnCallRequestReceived(CallRequest request)
        {
            var firebaseService = new FirebaseService();
            // ✅ Chỉ hiển thị nếu người nhận là chính mình (đã lắng nghe đúng path)
            // ❌ Tránh hiển thị nếu người gọi lại bắt được chính event của mình (do logic sai)
            var currentUser = SessionManager.CurrentUser;
            var currentUserDb = await firebaseService.GetUserByUsernameAsync(currentUser.UserName);
            string currentUserId = currentUserDb.Id;
            // Nếu currentUserId KHÁC FromUserId → đây là người nhận → hợp lệ
            if (currentUserId == null || currentUserId == request.FromUserId)
            {
                Console.WriteLine("⚠️ Bỏ qua vì là người gửi.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                Console.WriteLine($"📞 Có cuộc gọi từ: {request.FromUserId} - Channel: {request.ChannelName}");

                var incomingWindow = new IncomingCallWindow(request);
                incomingWindow.Show();
            });
        }



        // Lắng nghe cuộc gọi đến (từ Firebase realtime)
        public async void StartListeningForCalls(string userId)
        {
            StopListening();

            var firebaseService = new FirebaseService();
            var currentUser = await firebaseService.GetUserByUsernameAsync(SessionManager.CurrentUser.UserName);

            Console.WriteLine($"📡 Listening for calls on: {userId}");
            Console.WriteLine("🔒 Người đang đăng nhập: " + currentUser.Id);

            _callSubscription = client
                .Child("calls")
                .Child(userId)
                .AsObservable<CallRequest>()
                .Where(f => f.EventType == FirebaseEventType.InsertOrUpdate)
                .Subscribe(async call =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (call.Object == null) return;

                        Console.WriteLine("📥 Nhận được cuộc gọi:");
                        Console.WriteLine($"📞 Từ: {call.Object.FromUserId} - Channel: {call.Object.ChannelName}");

                        // ⚠️ Kiểm tra nếu người gửi là chính mình thì bỏ qua
                        if (call.Object.FromUserId == currentUser.Id)
                        {
                            Console.WriteLine("⚠️ Bỏ qua vì là người gửi.");
                            return;
                        }

                        // ✅ Gọi cửa sổ IncomingCall
                        if (call.Object.Type == "video")
                        {
                            var incomingVideo = new incomingvideocall(call.Object);
                            incomingVideo.Show();
                        }
                        else
                        {
                            var incomingAudio = new IncomingCallWindow(call.Object);
                            incomingAudio.Show();
                        }


                        // ✅ Sau khi xử lý, xóa cuộc gọi khỏi Firebase
                        await client
                            .Child("calls")
                            .Child(userId)
                            .Child(call.Key)
                            .DeleteAsync();

                        Console.WriteLine("🗑️ Đã xóa CallRequest sau khi xử lý.");
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
