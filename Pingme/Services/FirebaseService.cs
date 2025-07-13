using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Database.Streaming;
using Pingme.Models;
using Pingme.Views.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
namespace Pingme.Services
{
    class FirebaseService
    {
        public readonly FirebaseClient _client;

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
        public async Task DeleteFriendAsync(string user1, string user2)
        {
            var allFriends = await GetAllFriendsAsync();
            var friend = allFriends.FirstOrDefault(f =>
                (f.User1 == user1 && f.User2 == user2) ||
                (f.User1 == user2 && f.User2 == user1));

            if (friend != null)
            {
                await _client.Child("friends").Child(friend.Id).DeleteAsync();
            }
        }

        // ---------- CHAT ----------
        public Task AddChatAsync(Chat chat) =>
            _client.Child("chats").Child(chat.Id).PutAsync(chat);

        public async Task<List<Chat>> GetAllChatsAsync()
        {
            var chats = await _client
                .Child("chats")
                .OnceAsync<Chat>();

            return chats.Select(item => item.Object).ToList();
        }
        public async Task DeleteChatAsync(string chatId)
        {
            await _client.Child("Chats").Child(chatId).DeleteAsync();
        }

        // ---------- GROUP ----------
        public Task AddGroupAsync(ChatGroup group) =>
            _client.Child("chatGroups").Child(group.Id).PutAsync(group);
        public async Task<List<ChatGroup>> GetAllGroupsAsync()
        {
            var result = await _client.Child("chatGroups").OnceAsync<ChatGroup>();
            return result.Select(x => x.Object).ToList();
        }
        public async Task<ChatGroup> GetGroupByIdAsync(string groupId)
        {
            try
            {
                var result = await _client.Child("chatGroups").Child(groupId).OnceSingleAsync<ChatGroup>();
                if (result != null)
                {
                    result.Id = groupId;
                }
                return result;
            }
            catch
            {
                return null;
            }
        }

        // ---------- MESSAGE ----------
        public async Task<List<Message>> GetAllMessagesAsync()
        {
            var snapshot = await _client.Child("messages").OnceAsync<Message>();

            var allMessages = new List<Message>();

            foreach (var item in snapshot)
            {
                // Trường hợp message lưu dưới chatId → trong đó mỗi message có Id riêng
                var chatId = item.Key;

                var msgList = await _client.Child("messages").Child(chatId).OnceAsync<Message>();
                allMessages.AddRange(msgList.Select(m =>
                {
                    m.Object.Id = m.Key;
                    m.Object.ChatId = chatId;
                    return m.Object;
                }));
            }

            return allMessages;
        }
        public Task AddMessageAsync(Message msg)
        {
            string path = msg.IsGroup ? $"group_{msg.ChatId}" : $"chat_{msg.ChatId}";
            return _client.Child("messages").Child(path).Child(msg.Id).PutAsync(msg);
        }

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
        public async Task DeleteMessagesByChatIdAsync(string chatId)
        {
            var allMessages = await GetAllMessagesAsync();
            var toDelete = allMessages.Where(m => m.ChatId == chatId).ToList();

            foreach (var msg in toDelete)
            {
                await _client.Child("Messages").Child(msg.Id).DeleteAsync();
            }
        }

        // ---------- FILE ----------
        public Task AddFileAsync(File file) =>
            _client.Child("files").Child(file.Id).PutAsync(file);

        public async Task<FileMetadata> GetFileMetadataAsync(string fileId)
        {
            try
            {
                var metadata = await _client
                    .Child("file_metadata")
                    .Child(fileId)
                    .OnceSingleAsync<FileMetadata>();

                return metadata;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Không lấy được metadata của file {fileId}: {ex.Message}");
                return null;
            }
        }

        // ---------- NOTIFICATION ----------
        //public Task AddNotificationAsync(Notification notification) =>
        //    _client.Child("notifications").Child(notification.Id).PutAsync(notification);
        public Task AddNotificationAsync(Notification notification)
        {
            if (notification.CreatedAt == default)
            {
                notification.CreatedAt = DateTime.UtcNow;
            }

            return _client.Child("notifications").Child(notification.Id).PutAsync(notification);
        }
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
                //.OrderByDescending(n => n.CreatedAt)
                .OrderByDescending(n => n.CreatedAt != default ? n.CreatedAt : DateTime.MinValue)
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

        public async Task LogCallMessageAsync(string fromUserId, string toUserId, string callType, int durationSeconds, DateTime startTime, string chatId, bool isGroup)
        {
            string msgPath = isGroup ? $"messages/group_{chatId}" : $"messages/chat_{chatId}";

            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ChatId = chatId,
                SenderId = fromUserId,
                ReceiverId = isGroup ? null : toUserId,
                IsGroup = isGroup,
                Type = "call_log",
                CallType = callType,
                CallDuration = durationSeconds,
                SentAt = startTime,
                CallEndedAt = DateTime.UtcNow
            };

            await _client.Child(msgPath).Child(message.Id).PutAsync(message);

            if (!isGroup)
            {
                await _client.Child("chats").Child(chatId).PatchAsync(new
                {
                    LastMessageId = message.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                await _client.Child("chatGroups").Child(chatId).PatchAsync(new
                {
                    LastMessageId = message.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }
        public async Task SaveCallLogMessageAsync(
            string senderId,
            string receiverId,
            string chatId,
            bool isGroup,
            string callType,
            int durationInSeconds,
            DateTime startTime,
            DateTime endTime)
        {
            var message = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ChatId = chatId,
                SenderId = senderId,
                ReceiverId = receiverId,
                IsGroup = isGroup,
                Type = "call_log",
                CallType = callType,
                CallDuration = durationInSeconds,
                SentAt = startTime,
                CallEndedAt = endTime
            };

            string msgPath = isGroup ? $"messages/group_{chatId}" : $"messages/chat_{chatId}";
            await _client.Child(msgPath).Child(message.Id).PutAsync(message);

            // Cập nhật LastMessageId
            if (isGroup)
            {
                await _client.Child("chatGroups").Child(chatId).PatchAsync(new
                {
                    LastMessageId = message.Id,
                    UpdatedAt = DateTime.UtcNow
                });
            }
            else
            {
                await _client.Child("chats").Child(chatId).PatchAsync(new
                {
                    LastMessageId = message.Id,
                    UpdatedAt = DateTime.UtcNow
                });
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

        //public async Task SendCallStatusMessageAsync(string channelName, string status, DateTime time)
        //{
        //    await _client
        //        .Child("calls")
        //        .Child(channelName)
        //        .PutAsync(new
        //        {
        //            status = status,
        //            timestamp = new DateTimeOffset(time).ToUnixTimeMilliseconds()
        //        });
        //}
        public async Task SendCallStatusMessageAsync(string callerId, string receiverId, string pushId, string status, DateTime time)
        {
            if (string.IsNullOrEmpty(pushId))
            {
                MessageBox.Show("PushId cannot be null or empty.");
            }

            string chatId = GetChatRoomId(callerId, receiverId);

            string content;
            if (status == "accepted")
                content = $"✅ Cuộc gọi được chấp nhận lúc {time:HH:mm:ss}";
            else if (status == "declined")
                content = $"❌ Cuộc gọi bị từ chối lúc {time:HH:mm:ss}";
            else if (status == "missed")
                content = $"📵 Cuộc gọi nhỡ lúc {time:HH:mm:ss}";
            else if (status == "canceled")
                content = $"❌ Cuộc gọi đã bị hủy lúc {time:HH:mm:ss}";
            else
                content = $"Trạng thái cuộc gọi: {status} lúc {time:HH:mm:ss}";

            var message = new Message
            {
                ChatId = chatId,
                SenderId = receiverId, // người gửi trạng thái
                ReceiverId = callerId, // người gọi
                Type = "status",
                Content = content,
                SentAt = time,
                IsRead = false
            };

            await _client.Child("messages").PostAsync(message);

            // ✅ Cập nhật trạng thái trong "calls/{pushId}"
            await _client
                .Child("calls")
                .Child(pushId)
                .PatchAsync(new
                {
                    status,
                    timestamp = new DateTimeOffset(time).ToUnixTimeMilliseconds()
                });
        }


        public void ListenForCallResponse(string pushId, Action<string, DateTime> onStatusReceived)
        {
            if (string.IsNullOrEmpty(pushId))
            {
                MessageBox.Show("❌ PushId bị thiếu khi lắng nghe trạng thái!");
                return;
            }

            var refNode = _client.Child("calls").Child(pushId);

            refNode.AsObservable<CallRequest>()
                .Where(ev => ev.Object != null && ev.EventType == Firebase.Database.Streaming.FirebaseEventType.InsertOrUpdate)
                .Subscribe(call =>
                {
                    var status = call.Object.status;
                    if (string.IsNullOrEmpty(status)) return;

                    var time = DateTimeOffset.FromUnixTimeMilliseconds(call.Object.Timestamp).DateTime;
                    //MessageBox.Show($"📡 Call status: {status} at {time:HH:mm:ss}");
                    onStatusReceived(status, time);
                },
                error =>
                {
                    Console.WriteLine("❌ Lỗi Firebase: " + error.Message);
                });
        }

        public async Task<CallRequest> GetCallRequestByIdAsync(string pushId)
        {
            try
            {
                return await _client.Child("calls").Child(pushId).OnceSingleAsync<CallRequest>();
            }
            catch
            {
                return null;
            }
        }
    }
}

