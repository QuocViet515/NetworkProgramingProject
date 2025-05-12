using Pingme.Services;
using Pingme.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.ComponentModel;
using System.Windows.Input;
using Pingme.Helpers;

namespace Pingme.ViewModels
{
   public class ChatViewModel : INotifyPropertyChanged
    {
        public ICommand CallUserCommand { get; set; }

        public ChatViewModel()
        {
            CallUserCommand = new RelayCommand(CallUser);
            LoadUsers();
        }

        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();
        public ObservableCollection<User> UserList { get; set; } = new ObservableCollection<User>();
        private readonly FirebaseService firebaseService = new FirebaseService();
        private readonly ChatService chatService = new ChatService();

        private string currentUserId => AuthService.CurrentUser.id;
        private string targetUserId;
        private bool subscriptionStarted = false;

        public async void LoadUsers()
        {
            string currentUserId = AuthService.CurrentUser.id;
            var users = await firebaseService.GetAllUsersExceptCurrentAsync(currentUserId);

            Application.Current.Dispatcher.Invoke(() =>
            {
                UserList.Clear();
                foreach (var user in users)
                    UserList.Add(user);
            });
        }

        public async Task StartChatAsync(string receiverId)
        {
            if (targetUserId == receiverId && subscriptionStarted)
                return;

            targetUserId = receiverId;
            subscriptionStarted = true;

            Messages.Clear();

            chatService.OnNewMessageReceived += (msg) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Messages.Add(msg);
                });
            };

            chatService.ListenForMessages(currentUserId, targetUserId);

            var history = await chatService.LoadChatHistory(currentUserId, targetUserId);
            foreach (var msg in history)
                Messages.Add(msg);
        }

        public async Task SendMessage(string content)
        {
            var senderId = currentUserId;
            await chatService.SendMessageAsync(senderId, targetUserId, content);
        }

        // Selected user binding
        private User _selectedUser;
        public User SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged(nameof(SelectedUser));
            }
        }

        private void CallUser()
        {
            if (SelectedUser == null)
            {
                MessageBox.Show("❗ Chưa chọn người để gọi.");
                return;
            }

            MessageBox.Show($"📞 Đang gọi đến {SelectedUser.id}");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
