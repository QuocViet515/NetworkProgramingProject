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
            return result.Select(x => x.Object).ToList();
        }

        // ---------- FRIEND ----------
        public Task AddFriendAsync(Friend friend) =>
            _client.Child("friends").Child(friend.Id).PutAsync(friend);

        public async Task<List<Friend>> GetAllFriendsAsync() =>
            (await _client.Child("friends").OnceAsync<Friend>()).Select(x => x.Object).ToList();

        // ---------- CHAT ----------
        public Task AddChatAsync(Chat chat) =>
            _client.Child("chats").Child(chat.Id).PutAsync(chat);

        // ---------- GROUP ----------
        public Task AddGroupAsync(ChatGroup group) =>
            _client.Child("chatGroups").Child(group.Id).PutAsync(group);

        // ---------- MESSAGE ----------
        public Task AddMessageAsync(Message msg) =>
            _client.Child("messages").Child(msg.Id).PutAsync(msg);

        // ---------- FILE ----------
        public Task AddFileAsync(File file) =>
            _client.Child("files").Child(file.Id).PutAsync(file);

        // ---------- NOTIFICATION ----------
        public Task AddNotificationAsync(Notification notification) =>
            _client.Child("notifications").Child(notification.Id).PutAsync(notification);

        // ---------- CALL LOG ----------
        public Task AddCallLogAsync(CallLog callLog) =>
            _client.Child("callLogs").Child(callLog.Id).PutAsync(callLog);
    }
}
