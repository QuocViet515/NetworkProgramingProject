using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Reactive.Disposables;
namespace Pingme.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebaseClient;
        public event Action<Message> OnNewMessageReceived;
        private IDisposable _currentListener;
        private DateTime _subscriptionStartTime = DateTime.UtcNow;
        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
        }

        // Sinh ID phòng chat cố định giữa 2 user
        public static string GetChatRoomId(string userId1, string userId2)
        {
            var ids = new[] { userId1, userId2 };
            Array.Sort(ids);
            return $"room_{ids[0]}_{ids[1]}";
        }

        // Lắng nghe tất cả tin nhắn trong phòng (ví dụ để load lịch sử hoặc debug)
        public void SubscribeToAllMessages(string chatRoomId)
        {
            _firebaseClient
                .Child("messages")
                .Child(chatRoomId)
                .AsObservable<Message>()
                .Where(f => !string.IsNullOrEmpty(f.Key))
                .Subscribe(f =>
                {
                    if (f.EventType == FirebaseEventType.InsertOrUpdate)
                    {
                        OnNewMessageReceived?.Invoke(f.Object);
                    }
                });
        }

        // Lắng nghe chỉ tin nhắn nhận (ví dụ phía người dùng A đang nhận tin từ B)
        public void SubscribeToIncomingMessages(string chatRoomId, string currentUserId, Action<Message> onNewMessage)
        {
            _currentListener?.Dispose(); // hủy listener cũ

            _subscriptionStartTime = DateTime.UtcNow; // ghi lại thời điểm bắt đầu

            _firebaseClient
                .Child("messages")
                .Child(chatRoomId)
                .AsObservable<Message>()
                .Where(f => !string.IsNullOrEmpty(f.Key))
                .Subscribe(d =>
                {
                    var msg = d.Object;

                    if (msg != null &&
                        msg.ReceiverId == currentUserId &&
                        !msg.IsRead &&
                        msg.Timestamp > _subscriptionStartTime)
                    {
                        onNewMessage?.Invoke(msg);
                    }
                });
        }

        // Đánh dấu đã đọc
        public async Task MarkMessagesAsReadAsync(string chatRoomId, string currentUserId)
        {
            var messages = await _firebaseClient
                .Child("messages")
                .Child(chatRoomId)
                .OnceAsync<Message>();

            foreach (var m in messages)
            {
                if (m.Object.ReceiverId == currentUserId && !m.Object.IsRead)
                {
                    await _firebaseClient
                        .Child("messages")
                        .Child(chatRoomId)
                        .Child(m.Key)
                        .PatchAsync(new { isRead = true });
                }
            }
        }
        public void UnsubscribeAll()
        {
            _currentListener?.Dispose();
            _currentListener = null;
        }
        // Lấy tất cả user trừ người hiện tại
        public async Task<List<User>> GetAllUsersExceptCurrentAsync(string currentUserId)
        {
            var allUsers = await _firebaseClient
                .Child("users")
                .OnceAsync<User>();

            return allUsers
                .Select(u => u.Object)
                .Where(u => u.id != currentUserId)
                .ToList();
        }
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var result = await _firebaseClient
                    .Child("test_connection")
                    .OnceSingleAsync<string>();

                Console.WriteLine("✅ Đã kết nối Firebase: " + result);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi kết nối Firebase: " + ex.Message);
                return false;
            }
        }
        public async Task<List<Message>> LoadMessagesAsync(string chatRoomId)
        {
            var messages = await _firebaseClient
                .Child("messages")
                .Child(chatRoomId)
                .OnceAsync<Message>();

            return messages.Select(m => m.Object).ToList();
        }
        public async Task<User> GetUserByIdAsync(string userId)
        {
            var userSnapshot = await _firebaseClient
                .Child("users")
                .Child(userId)
                .OnceSingleAsync<User>();

            return userSnapshot;
        }
        public async Task<string> GetPrivateKeyAsync(string userId)
        {
            var privateKey = await _firebaseClient
                .Child("users")
                .Child(userId)
                .Child("privateKey")
                .OnceSingleAsync<string>();

            return privateKey;
        }
        public async Task SendEncryptedMessageAsync(string roomId, Message message)
        {
            await _firebaseClient
                .Child("messages")
                .Child(roomId)
                .PostAsync(message);

            var metadata = new
            {
                participants = new[] { message.SenderId, message.ReceiverId },
                lastMessage = "[Tin nhắn đã mã hóa]",
                lastUpdated = DateTime.UtcNow
            };

            await _firebaseClient
                .Child("chat_rooms")
                .Child(roomId)
                .PutAsync(metadata);
        }
    }
}
