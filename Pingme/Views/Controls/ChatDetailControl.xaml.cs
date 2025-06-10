using Pingme.Models;
using Pingme.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using Firebase.Database;
using System.Threading.Tasks;
using Firebase.Database.Query;

namespace Pingme.Views.Controls
{
    public partial class ChatDetailControl : UserControl
    {
        public bool isGroup { get; set; } = true;
        private string currentChatId;
        private FirebaseClient firebase;

        public ChatDetailControl()
        {
            InitializeComponent();
        }

        public async void LoadChat(string chatId, bool isGroup)
        {
            ChatPanel.Children.Clear();
            this.isGroup = isGroup;
            currentChatId = chatId;

            firebase = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken) });

            // Load chat/group info
            if (isGroup)
            {
                var group = await firebase.Child("chatGroups").Child(chatId).OnceSingleAsync<ChatGroup>();
                ChatHeader.ContactName = group.Name ?? "Nhóm Chat";
                ChatHeader.AvatarPath = group.AvatarUrl ?? "/Assets/Icons/avatar-default.png";
            }
            else
            {
                var chat = await firebase.Child("chats").Child(chatId).OnceSingleAsync<Chat>();
                string otherId = chat.User1 == SessionManager.UID ? chat.User2 : chat.User1;

                var other = await firebase.Child("users").Child(otherId).OnceSingleAsync<User>();
                ChatHeader.ContactName = other.FullName ?? other.UserName ?? "Người dùng";
                ChatHeader.AvatarPath = other.AvatarUrl ?? "/Assets/Icons/avatar-default.png";
            }

            await LoadMessagesAsync();
        }

        private async Task LoadMessagesAsync()
        {
            var messages = await firebase.Child("messages").OnceAsync<Message>();

            foreach (var item in messages)
            {
                var msg = item.Object;

                if (msg.ChatId != currentChatId) continue;

                if (msg.SenderId == SessionManager.UID)
                    ChatPanel.Children.Add(new OutgoingMessageControl(msg.Text));
                else
                    ChatPanel.Children.Add(new IncomingMessageControl(msg.Text));
            }
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var text = MessageInput.Text.Trim();
            if (string.IsNullOrEmpty(text) || string.Equals(text, "Nhắn gì đó . . .", StringComparison.OrdinalIgnoreCase))
                return;

            SendMessageAsync(text);
            MessageInput.Text = "";
        }

        private async void SendMessageAsync(string text)
        {
            var msg = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ChatId = currentChatId,
                SenderId = SessionManager.UID,
                Text = text,
                IsGroup = isGroup,
                SentAt = DateTime.UtcNow
            };

            await firebase.Child("messages").Child(msg.Id).PutAsync(msg);

            if (isGroup)
                await firebase.Child("chatGroups").Child(currentChatId).Child("LastMessageId").PutAsync(msg.Id);
            else
                await firebase.Child("chats").Child(currentChatId).Child("LastMessageId").PutAsync(msg.Id);

            ChatPanel.Children.Add(new OutgoingMessageControl(text));
        }
    }
}
