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
using System.Windows.Input;
using System.Windows.Media;

namespace Pingme.Views.Controls
{
    public partial class ChatListControl : UserControl
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken) });

        public ObservableCollection<Chat> PersonalChats { get; set; } = new ObservableCollection<Chat>();
        public ObservableCollection<ChatGroup> GroupChats { get; set; } = new ObservableCollection<ChatGroup>();

        private Dictionary<string, User> _allUsers = new Dictionary<string, User>();
        private List<Chat> _allPersonalChats = new List<Chat>();
        private List<ChatGroup> _allGroupChats = new List<ChatGroup>();
        private string _selectedChatId;
        public ChatListControl()
        {
            InitializeComponent();
            DataContext = this;
            LoadChats();
        }

        private async void LoadChats()
        {
            await SessionManager.EnsureValidTokenAsync();
            var uid = SessionManager.UID;

            // Tải users và lưu vào session
            _allUsers = (await _firebase.Child("users").OnceAsync<User>())
                .Select(u => { u.Object.Id = u.Key; return u.Object; })
                .ToDictionary(u => u.Id, u => u);

            SessionManager.CurrentUserMap = _allUsers;

            // Chat cá nhân
            var chatSnaps = await _firebase.Child("chats").OnceAsync<Chat>();
            _allPersonalChats = chatSnaps
                .Select(c => { c.Object.Id = c.Key; return c.Object; })
                .Where(chat => chat.User1 == uid || chat.User2 == uid)
                .ToList();

            SessionManager.CurrentChats = _allPersonalChats.ToDictionary(c => c.Id);

            // Nhóm
            var groupSnaps = await _firebase.Child("chatGroups").OnceAsync<ChatGroup>();
            _allGroupChats = groupSnaps
                .Select(g => { g.Object.Id = g.Key; return g.Object; })
                .Where(group => group.Members.Contains(uid))
                .ToList();

            SessionManager.CurrentGroups = _allGroupChats.ToDictionary(g => g.Id);

            // (Tuỳ chọn) tải tin nhắn cuối
            SessionManager.LastMessages = new Dictionary<string, Message>();
            foreach (var chat in _allPersonalChats)
            {
                if (!string.IsNullOrEmpty(chat.LastMessageId))
                {
                    var msg = await _firebase.Child("messages").Child(chat.LastMessageId).OnceSingleAsync<Message>();
                    if (msg != null)
                        SessionManager.LastMessages[chat.LastMessageId] = msg;
                }
            }

            foreach (var group in _allGroupChats)
            {
                if (!string.IsNullOrEmpty(group.LastMessageId))
                {
                    var msg = await _firebase.Child("messages").Child(group.LastMessageId).OnceSingleAsync<Message>();
                    if (msg != null)
                        SessionManager.LastMessages[group.LastMessageId] = msg;
                }
            }

            FilterChats();
        }
        private void FilterChats()
        {
            string keyword = Search.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(keyword) || keyword == "tìm kiếm")
            {
                PersonalChats.Clear();
                foreach (var chat in _allPersonalChats)
                    PersonalChats.Add(chat);

                GroupChats.Clear();
                foreach (var group in _allGroupChats)
                    GroupChats.Add(group);

                return;
            }

            PersonalChats.Clear();
            foreach (var chat in _allPersonalChats)
            {
                var otherId = chat.User1 == SessionManager.UID ? chat.User2 : chat.User1;
                if (_allUsers.TryGetValue(otherId, out var user))
                {
                    var displayName = user.FullName ?? user.UserName ?? "";
                    if (displayName.ToLower().Contains(keyword))
                        PersonalChats.Add(chat);
                }
                chat.IsSelected = chat.Id == _selectedChatId;
            }

            GroupChats.Clear();
            foreach (var group in _allGroupChats)
            {
                var groupName = group.Name ?? "";
                if (groupName.ToLower().Contains(keyword))
                    GroupChats.Add(group);
                group.IsSelected = group.Id == _selectedChatId;
            }
        }
        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilterChats();
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
        public event EventHandler<object> ChatSelected;
        private void OnChatItemClicked(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext != null)
            {
                // Reset tất cả chat trước
                foreach (var chat in _allPersonalChats)
                    chat.IsSelected = false;
                foreach (var group in _allGroupChats)
                    group.IsSelected = false;

                // Đặt IsSelected cho mục được chọn
                if (fe.DataContext is Chat selectedChat)
                {
                    selectedChat.IsSelected = true;
                    _selectedChatId = selectedChat.Id;
                }
                else if (fe.DataContext is ChatGroup selectedGroup)
                {
                    selectedGroup.IsSelected = true;
                    _selectedChatId = selectedGroup.Id;
                }

                // Gọi sự kiện và cập nhật giao diện
                ChatSelected?.Invoke(this, fe.DataContext);
            }
        }
    }
}
