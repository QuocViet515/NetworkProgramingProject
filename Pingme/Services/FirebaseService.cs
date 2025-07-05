using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Services
{
    class FirebaseService
    {
        private readonly FirebaseClient _client;

        public FirebaseService()
        {
            _client = new FirebaseClient(
                "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult("haBViPv4zOuLMSuBt2mnaD2SYokqsRpbLIt5PcFL")
                });
        }

        // ---------- USER ----------
        public Task AddUserAsync(User user) =>
            _client.Child("users").Child(user.Id).PutAsync(user);

        public async Task<List<User>> GetAllUsersAsync()
        {
            var result = await _client.Child("users").OnceAsync<User>();
            return result.Select(x =>
            {
                x.Object.Id = x.Key;
                return x.Object;
            }).ToList();
        }
        public async Task<User> GetUserByIdAsync(string id)
        {
            var snapshot = await _client.Child("users").Child(id).OnceSingleAsync<User>();
            snapshot.Id = id;
            return snapshot;
        }
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            var allUsers = await _client.Child("users").OnceAsync<User>();

            var matched = allUsers
                .FirstOrDefault(u => string.Equals(u.Object.UserName, username, StringComparison.OrdinalIgnoreCase));

            if (matched != null)
            {
                matched.Object.Id = matched.Key; // Gán Id từ Firebase key
                return matched.Object;
            }

            return null;
        }


        public async Task<List<User>> GetAllUsersExceptCurrentAsync(string currentUserId)
        {
            var allUsers = await _client.Child("users").OnceAsync<User>();
            return allUsers.Select(u => {
                u.Object.Id = u.Key;
                return u.Object;
            }).Where(u => u.Id != currentUserId).ToList();
        }
        public async Task<string> GetPublicKeyAsync(string userId)
        {
            var user = await _client.Child("users").Child(userId).OnceSingleAsync<User>();
            return user?.PublicKey;
        }
        public async Task<string> GetPrivateKeyAsync(string userId)
        {
            return await _client.Child("users").Child(userId).Child("privateKey").OnceSingleAsync<string>();
        }

        /// <summary>
        /// Truy vấn chữ ký ECDSA của public key người dùng từ Firebase.
        /// </summary>
        /// <param name="uid">ID người dùng</param>
        /// <returns>Chữ ký dạng Base64 nếu có, hoặc null</returns>
        public async Task<string> GetPublicKeySignatureAsync(string uid)
        {
            try
            {
                var result = await _client
                    .Child("users")
                    .Child(uid)
                    .Child("PublicKeySignature")
                    .OnceSingleAsync<string>();

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FirebaseService] Không thể lấy chữ ký ECDSA của user {uid}: {ex.Message}");
                return null;
            }
        }

        // ---------- FRIEND ----------
        public Task AddFriendAsync(Friend friend) =>
            _client.Child("friends").Child(friend.Id).PutAsync(friend);

        public async Task<List<Friend>> GetAllFriendsAsync() =>
            (await _client.Child("friends").OnceAsync<Friend>()).Select(x => x.Object).ToList();
        public async Task UpdateFriendStatus(string user1, string user2, string newStatus)
        {
            var all = await _client.Child("friends").OnceAsync<Friend>();
            var match = all.FirstOrDefault(f =>
                (f.Object.User1 == user1 && f.Object.User2 == user2) ||
                (f.Object.User1 == user2 && f.Object.User2 == user1));

            if (match != null)
            {
                match.Object.Status = newStatus;
                match.Object.AcceptedAt = newStatus == "accept" ? DateTime.UtcNow : (DateTime?)null;
                await _client.Child("friends").Child(match.Key).PutAsync(match.Object);
            }
        }
        //    public async Task<string> GetPublicKeyAsync(string userId)
        //    {
        //        var user = await _firebaseClient
        //            .Child("users")
        //            .Child(userId)
        //            .OnceSingleAsync<dynamic>();

        //        // Đọc trường publicKey
        //        return user?.publicKey;
        //    }
        // ---------- CHAT ----------
        public Task AddChatAsync(Chat chat) =>
            _client.Child("chats").Child(chat.Id).PutAsync(chat);

        // ---------- GROUP ----------
        public Task AddGroupAsync(ChatGroup group) =>
            _client.Child("chatGroups").Child(group.Id).PutAsync(group);
        public async Task<List<ChatGroup>> GetAllGroupsAsync()
        {
            var result = await _client.Child("chatGroups").OnceAsync<ChatGroup>();
            return result.Select(x => x.Object).ToList();
        }

        // ---------- MESSAGE ----------
        public Task AddMessageAsync(Message msg) =>
            _client.Child("messages").Child(msg.Id).PutAsync(msg);
        // ⚙️ Hàm sinh ID phòng chat cố định giữa 2 người dùng (order-independent)
        public static string GetChatRoomId(string userId1, string userId2)
        {
            var ids = new[] { userId1, userId2 };
            Array.Sort(ids); // đảm bảo cùng ID dù gửi/nhận
            return $"room_{ids[0]}_{ids[1]}";
        }
        public async Task<List<Message>> LoadMessagesAsync(string chatRoomId)
        {
            var messages = await _client
                .Child("messages")
                .Child(chatRoomId)
                .OnceAsync<Message>();

            return messages.Select(m => m.Object).ToList();
        }

        // ---------- FILE ----------
        public Task AddFileAsync(File file) =>
            _client.Child("files").Child(file.Id).PutAsync(file);

        // ---------- NOTIFICATION ----------
        public Task AddNotificationAsync(Notification notification) =>
            _client.Child("notifications").Child(notification.Id).PutAsync(notification);
        public async Task<List<Notification>> GetNotificationsByUserAsync(string userId)
        {
            var all = await _client.Child("notifications").OnceAsync<Notification>();
            return all
                .Where(n => n.Object.ReceiverId == userId)
                .Select(n =>
                {
                    n.Object.Id = n.Key;
                    return n.Object;
                })
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }
        public async Task DeleteNotificationAsync(string notificationId)
        {
            await _client.Child("notifications").Child(notificationId).DeleteAsync();
        }
        public async Task UpdateNotificationAsync(Notification notification)
        {
            await _client.Child("notifications").Child(notification.Id).PutAsync(notification);
        }

        // ---------- CALL LOG ----------
        public Task AddCallLogAsync(CallLog callLog) =>
            _client.Child("callLogs").Child(callLog.Id).PutAsync(callLog);
        //

        private IDisposable _currentListener;
        private DateTime _subscriptionStartTime = DateTime.UtcNow;

        public void SubscribeToIncomingMessages(string chatRoomId, string currentUserId, Action<Message> onNewMessage)
        {
            _currentListener?.Dispose();

            _subscriptionStartTime = DateTime.UtcNow;

            _client
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
                        msg.SentAt > _subscriptionStartTime)
                    {
                        onNewMessage?.Invoke(msg);
                    }
                });
        }
        public void UnsubscribeAll()
        {
            _currentListener?.Dispose();
            _currentListener = null;
        }
        public async Task MarkMessagesAsReadAsync(string chatRoomId, string currentUserId)
        {
            var messages = await _client
                .Child("messages")
                .Child(chatRoomId)
                .OnceAsync<Message>();

            foreach (var m in messages)
            {
                if (m.Object.ReceiverId == currentUserId && !m.Object.IsRead)
                {
                    await _client
                        .Child("messages")
                        .Child(chatRoomId)
                        .Child(m.Key)
                        .PatchAsync(new { isRead = true });
                }
            }
        }
        public async Task SendEncryptedMessageAsync(string roomId, Message message)
        {
            await _client
                .Child("messages")
                .Child(roomId)
                .PostAsync(message);

            var metadata = new
            {
                participants = new[] { message.SenderId, message.ReceiverId },
                lastMessage = "[Tin nhắn đã mã hóa]",
                lastUpdated = DateTime.UtcNow
            };

            await _client
                .Child("chat_rooms")
                .Child(roomId)
                .PutAsync(metadata);
        }
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var result = await _client.Child("test_connection").OnceSingleAsync<string>();
                Console.WriteLine("✅ Đã kết nối Firebase: " + result);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi kết nối Firebase: " + ex.Message);
                return false;
            }
        }
        public void SubscribeToAllMessages(string chatRoomId, Action<Message> onMessageUpdate)
        {
            _client
                .Child("messages")
                .Child(chatRoomId)
                .AsObservable<Message>()
                .Where(f => !string.IsNullOrEmpty(f.Key))
                .Subscribe(f =>
                {
                    if (f.EventType == FirebaseEventType.InsertOrUpdate)
                    {
                        onMessageUpdate?.Invoke(f.Object);
                    }
                });
        }
        public async Task SendCallSummaryMessageAsync(string _senderId, string _receiverId, string _callType, int durationSeconds, DateTime endTime)
        {
            var message = new
            {
                senderId = _senderId,
                receiverId = _receiverId,
                type = "call_log",
                callType = _callType, // "video" hoặc "audio"
                duration = durationSeconds,
                endedAt = endTime.ToString("o"), // ISO 8601
                content = $"Cuộc gọi {_callType} kết thúc. Thời lượng: {durationSeconds / 60} phút {durationSeconds % 60} giây.",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            string chatId = GetChatRoomId(_senderId, _receiverId);

            await _client.Child("messages")
                .Child(chatId)
                .PostAsync(message);
        }

        public async Task SendCallStatusMessageAsync(string callerId, string receiverId, string status, DateTime time)
        {
            var message = new Message
            {
                ChatId = GetChatRoomId(callerId, receiverId),
                SenderId = receiverId,
                ReceiverId = callerId,
                Type = "status",
                Content = status == "missed"
                    ? $"📵 Cuộc gọi nhỡ lúc {time:HH:mm:ss}"
                    : $"❌ {receiverId} đã từ chối cuộc gọi lúc {time:HH:mm:ss}",
                SentAt = time,
                IsRead = false
            };

            await _client.Child("messages").PostAsync(message);
        }

    }

    //public class FirebaseService
    //{
    //    private readonly FirebaseClient _firebaseClient;
    //    public event Action<Message> OnNewMessageReceived;
    //    private IDisposable _currentListener;
    //    private DateTime _subscriptionStartTime = DateTime.UtcNow;
    //    public FirebaseService()
    //    {
    //        _firebaseClient = new FirebaseClient("https://fir-36ac0-default-rtdb.firebaseio.com/");
    //    }

    //    // Sinh ID phòng chat cố định giữa 2 user
    //    public static string GetChatRoomId(string userId1, string userId2)
    //    {
    //        var ids = new[] { userId1, userId2 };
    //        Array.Sort(ids);
    //        return $"room_{ids[0]}_{ids[1]}";
    //    }

    //    // Lắng nghe tất cả tin nhắn trong phòng (ví dụ để load lịch sử hoặc debug)
    //    public void SubscribeToAllMessages(string chatRoomId)
    //    {
    //        _firebaseClient
    //            .Child("messages")
    //            .Child(chatRoomId)
    //            .AsObservable<Message>()
    //            .Where(f => !string.IsNullOrEmpty(f.Key))
    //            .Subscribe(f =>
    //            {
    //                if (f.EventType == FirebaseEventType.InsertOrUpdate)
    //                {
    //                    OnNewMessageReceived?.Invoke(f.Object);
    //                }
    //            });
    //    }

    //    // Lắng nghe chỉ tin nhắn nhận (ví dụ phía người dùng A đang nhận tin từ B)
    //    public void SubscribeToIncomingMessages(string chatRoomId, string currentUserId, Action<Message> onNewMessage)
    //    {
    //        _currentListener?.Dispose(); // hủy listener cũ

    //        _subscriptionStartTime = DateTime.UtcNow; // ghi lại thời điểm bắt đầu

    //        _firebaseClient
    //            .Child("messages")
    //            .Child(chatRoomId)
    //            .AsObservable<Message>()
    //            .Where(f => !string.IsNullOrEmpty(f.Key))
    //            .Subscribe(d =>
    //            {
    //                var msg = d.Object;

    //                if (msg != null &&
    //                    msg.ReceiverId == currentUserId &&
    //                    !msg.IsRead &&
    //                    msg.Timestamp > _subscriptionStartTime)
    //                {
    //                    onNewMessage?.Invoke(msg);
    //                }
    //            });
    //    }

    //    // Đánh dấu đã đọc
    //    public async Task MarkMessagesAsReadAsync(string chatRoomId, string currentUserId)
    //    {
    //        var messages = await _firebaseClient
    //            .Child("messages")
    //            .Child(chatRoomId)
    //            .OnceAsync<Message>();

    //        foreach (var m in messages)
    //        {
    //            if (m.Object.ReceiverId == currentUserId && !m.Object.IsRead)
    //            {
    //                await _firebaseClient
    //                    .Child("messages")
    //                    .Child(chatRoomId)
    //                    .Child(m.Key)
    //                    .PatchAsync(new { isRead = true });
    //            }
    //        }
    //    }
    //    public void UnsubscribeAll()
    //    {
    //        _currentListener?.Dispose();
    //        _currentListener = null;
    //    }
    //    // Lấy tất cả user trừ người hiện tại
    //    public async Task<List<User>> GetAllUsersExceptCurrentAsync(string currentUserId)
    //    {
    //        var allUsers = await _firebaseClient
    //            .Child("users")
    //            .OnceAsync<User>();

    //        return allUsers
    //            .Select(u => u.Object)
    //            .Where(u => u.id != currentUserId)
    //            .ToList();
    //    }
    //    public async Task<bool> TestConnectionAsync()
    //    {
    //        try
    //        {
    //            var result = await _firebaseClient
    //                .Child("test_connection")
    //                .OnceSingleAsync<string>();

    //            Console.WriteLine("✅ Đã kết nối Firebase: " + result);
    //            return true;
    //        }
    //        catch (Exception ex)
    //        {
    //            Console.WriteLine("❌ Lỗi kết nối Firebase: " + ex.Message);
    //            return false;
    //        }
    //    }
    //    public async Task<List<Message>> LoadMessagesAsync(string chatRoomId)
    //    {
    //        var messages = await _firebaseClient
    //            .Child("messages")
    //            .Child(chatRoomId)
    //            .OnceAsync<Message>();

    //        return messages.Select(m => m.Object).ToList();
    //    }
    //    public async Task<User> GetUserByIdAsync(string userId)
    //    {
    //        var userSnapshot = await _firebaseClient
    //            .Child("users")
    //            .Child(userId)
    //            .OnceSingleAsync<User>();

    //        return userSnapshot;
    //    }
    //    public async Task<string> GetPrivateKeyAsync(string userId)
    //    {
    //        var privateKey = await _firebaseClient
    //            .Child("users")
    //            .Child(userId)
    //            .Child("privateKey")
    //            .OnceSingleAsync<string>();

    //        return privateKey;
    //    }
    //    public async Task SendEncryptedMessageAsync(string roomId, Message message)
    //    {
    //        await _firebaseClient
    //            .Child("messages")
    //            .Child(roomId)
    //            .PostAsync(message);

    //        var metadata = new
    //        {
    //            participants = new[] { message.SenderId, message.ReceiverId },
    //            lastMessage = "[Tin nhắn đã mã hóa]",
    //            lastUpdated = DateTime.UtcNow
    //        };

    //        await _firebaseClient
    //            .Child("chat_rooms")
    //            .Child(roomId)
    //            .PutAsync(metadata);
    //    }
    //    public async Task<string> GetPublicKeyAsync(string userId)
    //    {
    //        var user = await _firebaseClient
    //            .Child("users")
    //            .Child(userId)
    //            .OnceSingleAsync<dynamic>();

    //        // Đọc trường publicKey
    //        return user?.publicKey;
    //    }
    //}
}
