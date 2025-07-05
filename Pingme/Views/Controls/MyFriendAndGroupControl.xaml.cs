using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using Pingme.Views.Dialogs;
using Pingme.Views.Pages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pingme.Views.Controls
{
    public partial class MyFriendAndGroupControl : UserControl
    {
        private readonly FirebaseService _firebase = new FirebaseService();

        public MyFriendAndGroupControl()
        {
            InitializeComponent();
            LoadData();
        }
        private async void LoadData()
        {
            var currentUserId = SessionManager.UID;

            // === Load bạn bè ===
            var allFriends = await _firebase.GetAllFriendsAsync();
            var myAcceptedFriends = allFriends
                .Where(f =>
                    f.Status == "accept" &&
                    (f.User1 == currentUserId || f.User2 == currentUserId))
                .Select(f => f.User1 == currentUserId ? f.User2 : f.User1)
                .Distinct()
                .ToList();

            var allUsers = await _firebase.GetAllUsersAsync();
            var friendUsers = allUsers.Where(u => myAcceptedFriends.Contains(u.Id)).ToList();

            FriendPanel.Children.Clear();
            if (friendUsers.Any())
            {
                foreach (var friend in friendUsers)
                    FriendPanel.Children.Add(CreateUserItem(friend.FullName, friend.AvatarUrl));
            }
            else
            {
                FriendPanel.Children.Add(new TextBlock
                {
                    Text = "Bạn chưa có bạn bè nào.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10)
                });
            }

            // === Load nhóm ===
            var allGroups = await _firebase.GetAllGroupsAsync();
            var myGroups = allGroups
                .Where(g =>
                    (g.Members != null && g.Members.Contains(currentUserId)) ||
                    g.CreatedBy == currentUserId)
                .ToList();

            GroupPanel.Children.Clear();
            if (myGroups.Any())
            {
                foreach (var group in myGroups)
                    GroupPanel.Children.Add(CreateUserItem(group.Name, group.AvatarUrl));
            }
            else
            {
                GroupPanel.Children.Add(new TextBlock
                {
                    Text = "Bạn chưa tham gia nhóm nào.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10)
                });
            }
        }
        private StackPanel CreateUserItem(string name, string avatarUrl)
        {
            var panel = new StackPanel
            {
                Margin = new Thickness(10),
                Width = 120
            };

            var avatar = new Ellipse
            {
                Width = 50,
                Height = 50,
                Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(
                        string.IsNullOrWhiteSpace(avatarUrl)
                            ? "pack://application:,,,/Assets/Icons/avatar-default.png"
                            : avatarUrl,
                        UriKind.RelativeOrAbsolute))
                }
            };

            panel.Children.Add(avatar);
            panel.Children.Add(new TextBlock
            {
                Text = name,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            return panel;
        }
        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            // Điều hướng sang ProfilePage tab friendgroup
            var mainWindow = Application.Current.MainWindow as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.MainFrame.Navigate(new ProfilePage("friendgroup"));
            }
        }

        private async void AddGroup_Click(object sender, RoutedEventArgs e)
        {
            // Tạo và mở dialog để nhập tên nhóm và chọn bạn bè
            var dialog = new CreateGroupDialog();
            var result = dialog.ShowDialog();
            if (result == true)
            {
                var groupId = Guid.NewGuid().ToString();
                var selectedFriends = dialog.SelectedUserIds;

                var group = new ChatGroup
                {
                    Id = groupId,
                    Name = dialog.GroupName,
                    AvatarUrl = dialog.AvatarUrl,
                    CreatedBy = SessionManager.UID,
                    Admin = new List<string> { SessionManager.UID },
                    Members = new List<string>(selectedFriends) { SessionManager.UID },
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _firebase.AddGroupAsync(group);

                // Gửi thông báo đến người tạo
                await _firebase.AddNotificationAsync(new Notification
                {
                    Id = Guid.NewGuid().ToString(),
                    ReceiverId = SessionManager.UID,
                    Type = "group_created",
                    Data = new Dictionary<string, string>
            {
                { "groupId", group.Id },
                { "groupName", group.Name },
                { "groupAvatar", group.AvatarUrl }
            },
                    IsRead = false,
                    CreatedAt = DateTime.UtcNow
                });

                // Gửi thông báo mời vào nhóm cho các thành viên
                foreach (var uid in selectedFriends)
                {
                    await _firebase.AddNotificationAsync(new Notification
                    {
                        Id = Guid.NewGuid().ToString(),
                        ReceiverId = uid,
                        Type = "added_to_group",
                        Data = new Dictionary<string, string>
                {
                    { "groupId", group.Id },
                    { "groupName", group.Name },
                    { "groupAvatar", group.AvatarUrl },
                    { "inviter", SessionManager.CurrentUser?.FullName ?? "Người dùng" }
                },
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                MessageBox.Show("Đã tạo nhóm thành công.", "Thông báo");
                LoadData();
            }
        }
    }
}
