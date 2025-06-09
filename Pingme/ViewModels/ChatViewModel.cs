using Pingme.Models;
using Pingme.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace Pingme.ViewModels
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        private readonly ChatService _chatService = new ChatService();
        private readonly FirebaseService _firebaseService = new FirebaseService();

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();
        public ObservableCollection<User> UserList { get; set; } = new ObservableCollection<User>();

        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                    if (_selectedUser != null)
                        _ = LoadChatAsync(); // Load chat when selected
                }
            }
        }

        public ChatViewModel()
        {
            _chatService.OnNewMessageReceived = HandleNewMessage;
            LoadUsers(); // Load danh sách người dùng
        }

        private void HandleNewMessage(Message msg)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                msg.FromSelf = msg.SenderId == AuthService.CurrentUser.id;

                // 🔐 Kiểm tra tin nhắn đã tồn tại chưa
                if (!Messages.Any(m => m.Timestamp == msg.Timestamp && m.SenderId == msg.SenderId))
                {
                    Messages.Add(msg);
                }
            });
        }

        public async void LoadUsers()
        {
            try
            {
                var users = await _firebaseService.GetAllUsersExceptCurrentAsync(AuthService.CurrentUser.id);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UserList.Clear();
                    foreach (var user in users)
                        UserList.Add(user);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không tải được danh sách người dùng: " + ex.Message);
            }
        }

        public async Task LoadChatAsync()
        {
            try
            {
                Messages.Clear();

                var messages = await _chatService.LoadChatHistory(AuthService.CurrentUser.id, SelectedUser.id);
                foreach (var msg in messages)
                {
                    msg.FromSelf = msg.SenderId == AuthService.CurrentUser.id;
                    Messages.Add(msg);
                }

                _chatService.ListenForMessages(AuthService.CurrentUser.id, SelectedUser.id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi tải lịch sử chat: " + ex.Message);
            }
        }

        public async Task SendMessage(string content)
        {
            if (SelectedUser == null || string.IsNullOrWhiteSpace(content))
                return;

            await _chatService.SendMessageAsync(AuthService.CurrentUser.id, SelectedUser.id, content);

            Messages.Add(new Message
            {
                SenderId = AuthService.CurrentUser.id,
                ReceiverId = SelectedUser.id,
                Content = content,
                Timestamp = DateTime.UtcNow,
                FromSelf = true
            });
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
