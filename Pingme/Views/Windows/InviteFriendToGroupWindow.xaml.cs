using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pingme.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Pingme.Services;
using System;
using Pingme.Helpers;

namespace Pingme.Views.Windows
{
    public partial class InviteFriendToGroupWindow : Window
    {
        private readonly string _groupId;
        private readonly ChatGroup _group;
        private List<User> _friendList;

        public InviteFriendToGroupWindow(string groupId)
        {
            InitializeComponent();
            _groupId = groupId;

            // Lấy thông tin nhóm từ cache
            if (!SessionManager.CurrentGroups.TryGetValue(_groupId, out _group))
            {
                MessageBox.Show("❌ Không tìm thấy thông tin nhóm.");
                Close();
                return;
            }

            LoadGroupInfo();
            LoadFriendListAsync();
        }

        private void LoadGroupInfo()
        {
            GroupNameText.Text = _group.Name ?? "Nhóm";
            GroupAvatar.Source = new BitmapImage(
                new Uri(_group.AvatarUrl ?? "/Assets/Icons/avatar-default.png", UriKind.RelativeOrAbsolute));
        }

        private async Task LoadFriendListAsync()
        {
            var firebaseService = new FirebaseService();
            string currentUserId = SessionManager.UID;

            var allFriends = await firebaseService.GetAllFriendsAsync();
            var acceptedFriendIds = allFriends
                .Where(f =>
                    f.Status == "accept" &&
                    (f.User1 == currentUserId || f.User2 == currentUserId))
                .Select(f => f.User1 == currentUserId ? f.User2 : f.User1)
                .Distinct()
                .ToList();

            var allUsers = await firebaseService.GetAllUsersAsync();
            _friendList = allUsers.Where(u => acceptedFriendIds.Contains(u.Id)).ToList();

            FriendListPanel.Children.Clear();

            foreach (var friend in _friendList)
            {
                var border = new Border
                {
                    Margin = new Thickness(4),
                    Background = Brushes.White,
                    CornerRadius = new CornerRadius(8),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(220, 220, 220)),
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(10)
                };

                var panel = new DockPanel();

                var avatar = new Image
                {
                    Source = new BitmapImage(new Uri(friend.AvatarUrl ?? "/Assets/Icons/avatar-default.png", UriKind.RelativeOrAbsolute)),
                    Width = 40,
                    Height = 40,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(0, 0, 10, 0),
                    ClipToBounds = true
                };

                var nameBlock = new TextBlock
                {
                    Text = friend.FullName ?? friend.UserName,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var isAlreadyInGroup = _group.Members?.Contains(friend.Id) ?? false;
                Button inviteBtn;

                if (isAlreadyInGroup)
                {
                    inviteBtn = new Button
                    {
                        Content = "✓ Đã tham gia",
                        Background = new SolidColorBrush(Color.FromRgb(200, 200, 200)),
                        Foreground = Brushes.Black,
                        Padding = new Thickness(10, 4, 10, 4),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        IsEnabled = false,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12
                    };
                }
                else
                {
                    inviteBtn = new Button
                    {
                        Content = "Mời",
                        Background = new SolidColorBrush(Color.FromRgb(64, 158, 255)),
                        Foreground = Brushes.White,
                        Padding = new Thickness(10, 4, 10, 4),
                        BorderBrush = null,
                        BorderThickness = new Thickness(0),
                        Margin = new Thickness(10, 0, 0, 0),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Center,
                        FontSize = 12,
                        Tag = friend.Id
                    };
                    inviteBtn.Click += InviteBtn_Click;
                }

                DockPanel.SetDock(avatar, Dock.Left);
                DockPanel.SetDock(inviteBtn, Dock.Right);
                panel.Children.Add(avatar);
                panel.Children.Add(nameBlock);
                panel.Children.Add(inviteBtn);

                border.Child = panel;
                FriendListPanel.Children.Add(border);
            }

            if (_friendList.Count == 0)
            {
                FriendListPanel.Children.Add(new TextBlock
                {
                    Text = "Bạn chưa có bạn bè nào để mời.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10)
                });
            }
        }
        private async void InviteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string userId)
            {
                btn.IsEnabled = false;
                btn.Content = "⏳ Mời...";
                try
                {
                    var firebase = new FirebaseService();

                    // ✅ Mời vào nhóm + tạo notification đều nằm trong 1 hàm
                    await firebase.InviteUserToGroupAsync(_groupId, userId);

                    btn.Content = "✅ Đã mời";
                }
                catch
                {
                    btn.Content = "❌ Thất bại";
                }
            }
        }
    }
}
