using Firebase.Database;
using Firebase.Database.Query;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
