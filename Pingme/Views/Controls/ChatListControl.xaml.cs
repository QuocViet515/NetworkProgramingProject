using Firebase.Database;
using Firebase.Database.Query;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Pingme.Views.Controls
{
    public partial class ChatListControl : UserControl
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken) });

        public ObservableCollection<Chat> Chats { get; set; } = new ObservableCollection<Chat>();

        public ChatListControl()
        {
            InitializeComponent();
            DataContext = this;      // Gán DataContext để XAML binding được
            LoadChats();             // Tải dữ liệu khi khởi tạo
        }

        private async void LoadChats()
        {
            await SessionManager.EnsureValidTokenAsync();
            var uid = SessionManager.UID;

            var allUsers = await _firebase.Child("users").OnceAsync<User>();
            var otherUsers = allUsers.Select(u =>
            {
                u.Object.Id = u.Key;
                return u.Object;
            }).Where(u => u.Id != uid);

            var existingChats = await _firebase.Child("chats").OnceAsync<Chat>();

            Chats.Clear();
            foreach (var user in otherUsers)
            {
                var existing = existingChats.FirstOrDefault(c =>
                {
                    var obj = c.Object;
                    return (obj.User1 == uid && obj.User2 == user.Id) || (obj.User2 == uid && obj.User1 == user.Id);
                });

                Chat chat;
                if (existing == null)
                {
                    chat = new Chat
                    {
                        Id = Guid.NewGuid().ToString(),
                        User1 = uid,
                        User2 = user.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    await _firebase.Child("chats").Child(chat.Id).PutAsync(chat);
                }
                else
                {
                    chat = existing.Object;
                    chat.Id = existing.Key;
                }

                Chats.Add(chat);
            }
        }

        private void Search_GotFocus(object sender, RoutedEventArgs e)
        {
            if (Search.Text == "Tìm kiếm")
            {
                Search.Text = "";
                Search.Foreground = Brushes.Black;
                Search.HorizontalContentAlignment = HorizontalAlignment.Left;
            }
        }

        private void Search_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Search.Text))
            {
                Search.Text = "Tìm kiếm";
                Search.Foreground = Brushes.Black;
                Search.HorizontalContentAlignment = HorizontalAlignment.Center;
            }
        }
    }
}
