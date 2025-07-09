using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Newtonsoft.Json;
using Pingme.Helpers;
using Pingme.Models;
using Pingme.Views.Pages;
using Pingme.Views.Windows;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Linq;

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
        public async Task<(CallRequest callRequest, string pushId)> SendCallRequest(string fromUserId, string toUserId, string type)
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
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                status = "waiting"
            };

            try
            {
                // ✅ Đẩy lên Firebase, nhận lại pushId
                var result = await client
                    .Child("calls")
                    //.Child(toUserId)
                    .PostAsync(callRequest);

                string pushId = result.Key;

                // ✅ Cập nhật lại PushId trong Firebase và object
                callRequest.PushId = pushId;
                await client
                    .Child("calls")
                    //.Child(toUserId)
                    .Child(pushId)
                    .PatchAsync(new { PushId = pushId });

                Console.WriteLine("✅ Đã gửi tín hiệu gọi " + type + " với pushId: " + pushId);
                return (callRequest, pushId);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi khi gửi cuộc gọi: " + ex.Message);
                return (null, null);
            }
        }






        // Hàm xử lý khi có cuộc gọi đến
        //public async void OnCallRequestReceived(CallRequest request)
        //{
        //    var firebaseService = new FirebaseService();
        //    // ✅ Chỉ hiển thị nếu người nhận là chính mình (đã lắng nghe đúng path)
        //    // ❌ Tránh hiển thị nếu người gọi lại bắt được chính event của mình (do logic sai)
        //    var currentUser = SessionManager.CurrentUser;
        //    var currentUserDb = await firebaseService.GetUserByUsernameAsync(currentUser.UserName);
        //    string currentUserId = currentUserDb.Id;
        //    // Nếu currentUserId KHÁC FromUserId → đây là người nhận → hợp lệ
        //    if (currentUserId == null || currentUserId == request.FromUserId)
        //    {
        //        Console.WriteLine("⚠️ Bỏ qua vì là người gửi.");
        //        return;
        //    }

        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        Console.WriteLine($"📞 Có cuộc gọi từ: {request.FromUserId} - Channel: {request.ChannelName}");

        //        var incomingWindow = new IncomingCallWindow(request);
        //        incomingWindow.Show();
        //    });
        //}



        // Lắng nghe cuộc gọi đến (từ Firebase realtime)
        private static HashSet<string> _handledPushIds = new HashSet<string>();

        public async void StartListeningForCalls(string userId)
        {
            StopListening(); // Dừng lắng nghe cũ nếu có

            var firebaseService = new FirebaseService();
            var currentUser = await firebaseService.GetUserByUsernameAsync(SessionManager.CurrentUser.UserName);
            Console.WriteLine($"📡 Listening for incoming calls targeting user: {userId}");

            var activeWindows = new Dictionary<string, Window>();

            _callSubscription = client
                .Child("calls")
                .AsObservable<CallRequest>()
                .Where(f =>
                    f.EventType == FirebaseEventType.InsertOrUpdate &&
                    f.Object != null &&
                    f.Object.ToUserId == userId &&
                    !_handledPushIds.Contains(f.Object.PushId))
                .Subscribe(async call =>
                {
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        var request = call.Object;
                        if (request == null || string.IsNullOrWhiteSpace(request.PushId))
                            return;

                        if (request.FromUserId == currentUser.Id)
                        {
                            Console.WriteLine("⚠️ Bỏ qua vì là người gọi.");
                            return;
                        }

                        if (request.status != "waiting")
                            return;

                        if (activeWindows.ContainsKey(request.PushId))
                        {
                            var win = activeWindows[request.PushId];
                            if (win.IsVisible)
                            {
                                Console.WriteLine("⚠️ Cửa sổ đã mở rồi.");
                                return;
                            }
                            else
                            {
                                activeWindows.Remove(request.PushId);
                            }
                        }

                        _handledPushIds.Add(request.PushId);

                        Window incomingWindow;
                        if (request.Type == "video")
                        {
                            incomingWindow = new incomingvideocall(request);
                        }
                        else if (request.Type == "audio")
                        {
                            incomingWindow = new IncomingCallWindow(request);
                        }
                        else
                        {
                            Console.WriteLine("❌ Loại cuộc gọi không hợp lệ: " + request.Type);
                            return;
                        }

                        incomingWindow.Tag = request.PushId;
                        activeWindows[request.PushId] = incomingWindow;

                        incomingWindow.Closed += (s, e) =>
                        {
                            _handledPushIds.Remove(request.PushId);
                            activeWindows.Remove(request.PushId);
                            Console.WriteLine($"❌ Cửa sổ cuộc gọi đã đóng: {request.PushId}");
                        };

                        incomingWindow.Show();
                    });
                },
                error => Console.WriteLine("❌ Lỗi khi lắng nghe Firebase: " + error.Message));

            // 🕒 Poll mỗi 1 giây để kiểm tra status từ xa
            var pollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };

            pollTimer.Tick += async (s, e) =>
            {
                var toCheck = activeWindows.Keys.ToList();

                foreach (var pushId in toCheck)
                {
                    try
                    {
                        var call = await client.Child("calls").Child(pushId).OnceSingleAsync<CallRequest>();
                        if (call != null)
                        {
                            Console.WriteLine($"🕵️ Poll: PushId={pushId}, Status={call.status}");

                            if (call.status == "ended")
                            {
                                Console.WriteLine($"⛔ Cuộc gọi {pushId} đã kết thúc từ xa.");

                                if (activeWindows.TryGetValue(pushId, out var win))
                                {
                                    // Đảm bảo gọi trên UI thread
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        if (win.IsVisible)
                                        {
                                            win.Close();
                                            Console.WriteLine($"✅ Đã đóng cửa sổ gọi: {pushId}");
                                        }
                                    });

                                    activeWindows.Remove(pushId);
                                    _handledPushIds.Remove(pushId);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("⚠️ Poll lỗi: " + ex.Message);
                    }
                }
            };

            pollTimer.Start();
        }





        // Ngắt lắng nghe (ví dụ khi đăng xuất)
        public void StopListening()
        {
            _callSubscription?.Dispose();
            _callSubscription = null;
            _handledPushIds.Clear();
        }
    }
}
