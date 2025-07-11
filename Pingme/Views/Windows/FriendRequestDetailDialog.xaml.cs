using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Linq;
using System.Windows;

namespace Pingme.Views.Dialogs
{
    public partial class FriendRequestDetailDialog : Window
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly User _senderUser;
        private readonly bool _isRead;

        //public FriendRequestDetailDialog(User senderUser, bool isRead)
        //{
        //    InitializeComponent();
        //    _senderUser = senderUser;
        //    _isRead = isRead;

        //    // Gán dữ liệu vào DataContext để binding
        //    this.DataContext = new
        //    {
        //        senderUser.FullName,
        //        senderUser.Email,
        //        senderUser.UserName,
        //        AvatarUrl = string.IsNullOrWhiteSpace(senderUser.AvatarUrl)
        //            ? "/Assets/Icons/avatar-default.png"
        //            : senderUser.AvatarUrl,
        //        IsRead = isRead
        //    };
        //}
        public FriendRequestDetailDialog(User senderUser)
        {
            InitializeComponent();
            _senderUser = senderUser;

            // Gán DataContext tạm thời
            this.DataContext = new
            {
                senderUser.FullName,
                senderUser.Email,
                senderUser.UserName,
                AvatarUrl = string.IsNullOrWhiteSpace(senderUser.AvatarUrl)
                    ? "/Assets/Icons/avatar-default.png"
                    : senderUser.AvatarUrl,
                IsFriend = false // tạm thời false
            };

            // Load thông tin bạn bè thật sự
            Loaded += async (s, e) =>
            {
                var allFriends = await _firebaseService.GetAllFriendsAsync();
                var isFriend = allFriends.Any(f =>
                    f.Status == "accept" &&
                    ((f.User1 == SessionManager.UID && f.User2 == senderUser.Id) ||
                     (f.User2 == SessionManager.UID && f.User1 == senderUser.Id)));

                this.DataContext = new
                {
                    senderUser.FullName,
                    senderUser.Email,
                    senderUser.UserName,
                    AvatarUrl = string.IsNullOrWhiteSpace(senderUser.AvatarUrl)
                        ? "/Assets/Icons/avatar-default.png"
                        : senderUser.AvatarUrl,
                    IsFriend = isFriend
                };
            };
        }

        private async void Accept_Click(object sender, RoutedEventArgs e)
        {
            await _firebaseService.UpdateFriendStatus(SessionManager.UID, _senderUser.Id, "accept");

            // Tạo chat mới
            await _firebaseService.AddChatAsync(new Chat
            {
                Id = Guid.NewGuid().ToString(),
                User1 = SessionManager.UID,
                User2 = _senderUser.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });

            MessageBox.Show("✅ Đã chấp nhận kết bạn.");
            this.Close();
        }

        private async void Reject_Click(object sender, RoutedEventArgs e)
        {
            await _firebaseService.UpdateFriendStatus(SessionManager.UID, _senderUser.Id, "delete");
            MessageBox.Show("⛔ Đã từ chối yêu cầu.");
            this.Close();
        }
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
