using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Pingme.Views.Dialogs
{
    public partial class FindFriendDialog : Window
    {
        private readonly FirebaseService _firebase = new FirebaseService();
        private System.Collections.Generic.List<User> allUsers = new System.Collections.Generic.List<User>();

        public FindFriendDialog()
        {
            InitializeComponent();
            LoadUsers();
        }

        private async void LoadUsers()
        {
            allUsers = await _firebase.GetAllUsersAsync();
        }

        private async void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchBox.Text.Trim().ToLower();
            if (keyword.Length < 2)
            {
                ResultPanel.Children.Clear();
                StatusText.Text = "";
                return;
            }

            StatusText.Text = "🔄 Đang tìm...";

            await Task.Delay(300); // debounce

            var results = allUsers
                .Where(u =>
                    u.Id != SessionManager.UID &&
                    (u.FullName?.ToLower().Contains(keyword) == true ||
                     u.UserName?.ToLower().Contains(keyword) == true ||
                     u.Email?.ToLower().Contains(keyword) == true ||
                     u.Phone?.ToLower().Contains(keyword) == true))
                .ToList();

            await RenderResults(results);
        }

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "🔄 Đang tìm...";
            await Task.Delay(200);
            SearchBox_TextChanged(null, null);
        }

        private async Task RenderResults(List<User> users)
        {
            ResultPanel.Children.Clear();

            if (users.Count == 0)
            {
                StatusText.Text = "❌ Không tìm thấy người dùng nào.";
                return;
            }

            var allFriends = await _firebase.GetAllFriendsAsync();

            foreach (var user in users)
            {
                var row = new DockPanel
                {
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    LastChildFill = false
                };

                // Avatar
                var avatar = new Ellipse
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(5),
                    Fill = new ImageBrush(new BitmapImage(new Uri(
                        string.IsNullOrWhiteSpace(user.AvatarUrl)
                            ? "pack://application:,,,/Assets/Icons/avatar-default.png"
                            : user.AvatarUrl,
                        UriKind.RelativeOrAbsolute)))
                };
                DockPanel.SetDock(avatar, Dock.Left);
                row.Children.Add(avatar);

                // Info giữa
                var info = new StackPanel { Margin = new Thickness(5), Width = 220 };
                info.Children.Add(new TextBlock
                {
                    Text = $"{user.FullName} ({user.UserName})",
                    FontWeight = FontWeights.Bold
                });
                info.Children.Add(new TextBlock
                {
                    Text = user.Email,
                    FontSize = 12,
                    Foreground = Brushes.Gray
                });
                row.Children.Add(info);

                // Nút bên phải
                var buttonStack = new StackPanel { Margin = new Thickness(5), VerticalAlignment = VerticalAlignment.Center };

                bool isFriend = allFriends.Any(f =>
                    f.Status == "accept" &&
                    ((f.User1 == SessionManager.UID && f.User2 == user.Id) ||
                     (f.User2 == SessionManager.UID && f.User1 == user.Id)));

                if (isFriend)
                {
                    buttonStack.Children.Add(new TextBlock
                    {
                        Text = "✅ Đã là bạn",
                        Foreground = Brushes.Green,
                        FontWeight = FontWeights.SemiBold,
                        Margin = new Thickness(0, 5, 0, 0)
                    });
                }
                else
                {
                    var addBtn = new Button
                    {
                        Content = "➕ Kết bạn",
                        Width = 90,
                        Margin = new Thickness(0, 0, 0, 5),
                        Tag = user
                    };
                    addBtn.Click += async (s, e) =>
                    {
                        var target = (User)((Button)s).Tag;

                        await _firebase.AddFriendAsync(new Friend
                        {
                            Id = Guid.NewGuid().ToString(),
                            User1 = SessionManager.UID,
                            User2 = target.Id,
                            Status = "waiting",
                            CreatedAt = DateTime.UtcNow
                        });

                        await _firebase.AddNotificationAsync(new Notification
                        {
                            Id = Guid.NewGuid().ToString(),
                            ReceiverId = target.Id,
                            Type = "friend_request",
                            Data = new Dictionary<string, string>
                            {
                                { "from", SessionManager.UID },
                                { "fromName", SessionManager.CurrentUser?.FullName ?? "Người dùng" },
                                { "fromAvatar", SessionManager.CurrentUser?.AvatarUrl ?? "" }
                            },
                            IsRead = false,
                            CreatedAt = DateTime.UtcNow
                        });

                        MessageBox.Show("✅ Đã gửi yêu cầu kết bạn.");
                        this.Close();
                    };
                    buttonStack.Children.Add(addBtn);
                }

                var detailBtn = new Button
                {
                    Content = "👁 Chi tiết",
                    Width = 90,
                    Tag = user
                };
                detailBtn.Click += (s, e) =>
                {
                    var u = (User)((Button)s).Tag;
                    MessageBox.Show($"👤 {u.FullName}\n📧 {u.Email}\n📱 {u.Phone}");
                };

                buttonStack.Children.Add(detailBtn);

                DockPanel.SetDock(buttonStack, Dock.Right);
                row.Children.Add(buttonStack);

                ResultPanel.Children.Add(row);
            }

            StatusText.Text = $"🔍 Tìm thấy {users.Count} kết quả.";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
