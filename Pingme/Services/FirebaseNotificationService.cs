using Pingme.Models;
using Firebase.Database;
using Firebase.Database.Query;
using System;
using System.Reactive.Linq;
using System.Windows;
using Firebase.Database.Streaming;
using Pingme.Views.Pages;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace Pingme.Services
{
    class FirebaseNotificationService
    {
        private const string APP_ID = "c94888a36cee4d71a2d36eb0e2cc6f9b"; // hoặc AppSettings.AppId
        private FirebaseClient client;

        public FirebaseNotificationService()
        {
            client = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");

        }

        public void StartListening(string userId)
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
            // Cách đơn giản (hoặc dùng Toast Notification API của Windows 10)
            MessageBox.Show($"Tin nhắn mới từ {msg.SenderId}: {msg.Content}", "Thông báo");

        }
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

            await client
                .Child("calls")
                .Child(toUserId)
                .PutAsync(callRequest); // Put sẽ ghi đè hoặc tạo mới
        }


        void OnCallRequestReceived(CallRequest request)
        {
            if (request.ToUserId != AuthService.CurrentUser.id) return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MessageBox.Show($"{request.FromUserId} đang gọi bạn!", "Gọi đến", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                {
                    var callWindow = new CallWindow(APP_ID, request.ChannelName);
                    callWindow.Show();
                }

            });
        }
        public void StartListeningForCalls(string userId)
        {
            client
                .Child("calls")
                .Child(userId)
                .AsObservable<CallRequest>()
                .Where(f => f.EventType == FirebaseEventType.InsertOrUpdate)
                .Subscribe(call =>
                {
                    if (call.Object != null)
                    {
                        OnCallRequestReceived(call.Object);
                    }
                });
        }
       
    }
}
