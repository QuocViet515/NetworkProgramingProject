using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Input;

namespace Pingme.Views.Controls
{
    public partial class NotificationControl : UserControl
    {
        private FirebaseService _firebaseService = new FirebaseService();

        public NotificationControl()
        {
            InitializeComponent();
            LoadNotifications();
        }

        private async void LoadNotifications()
        {
            try
            {
                NotificationsPanel.Children.Clear(); // Clear cũ

                var userId = SessionManager.UID;
                var allNotifications = await _firebaseService.GetNotificationsByUserAsync(userId);

                int unreadCount = allNotifications.Count(n => !n.IsRead);
                NotificationHeader.Text = $"Notification ({unreadCount})";

                var grouped = allNotifications
                    .GroupBy(n => n.Type)
                    .ToDictionary(g => g.Key, g => g.ToList());

                ShowGrouped(grouped, "new_message", "📩 Tin nhắn mới");
                ShowGrouped(grouped, "friend_request", "👥 Yêu cầu kết bạn");
                ShowGrouped(grouped, "call_active", "📞 Cuộc gọi đang diễn ra");
                ShowGrouped(grouped, "call_missed", "❌ Cuộc gọi nhỡ");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thông báo: " + ex.Message);
            }
        }

        private void ShowGrouped(Dictionary<string, List<Notification>> grouped, string typeKey, string header)
        {
            if (!grouped.ContainsKey(typeKey)) return;

            var section = new StackPanel { Margin = new Thickness(10) };
            section.Children.Add(new TextBlock
            {
                Text = header,
                FontWeight = FontWeights.Bold,
                FontSize = 16,
                Margin = new Thickness(0, 10, 0, 5)
            });

            foreach (var noti in grouped[typeKey])
            {
                // 🔄 DÙ LÀ friend_request hay không, dùng RenderNotificationCard
                var card = RenderNotificationCard(noti);
                section.Children.Add(card);
            }

            NotificationsPanel.Children.Add(section);
        }

        private Border RenderNotificationCard(Notification noti)
        {
            var border = new Border
            {
                Background = Brushes.White,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 5, 0, 5),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Opacity = noti.IsRead ? 0.5 : 1.0
            };

            string senderName = noti.Data.TryGetValue("fromName", out var fromName) ? fromName : "Ping me";
            string avatarUrl = noti.Data.TryGetValue("fromAvatar", out var fromAvatar) ? fromAvatar : "/Assets/Icons/logo-app.jpg";
            string message = "Bạn có thông báo mới.";
            Brush messageColor = Brushes.Black;

            switch (noti.Type)
            {
                case "friend_request":
                    message = "đã gửi lời mời kết bạn.";
                    messageColor = Brushes.SteelBlue;
                    break;
                case "call_missed":
                    message = "đã gọi cho bạn nhưng bạn đã bỏ lỡ.";
                    messageColor = Brushes.IndianRed;
                    break;
                case "call_active":
                    message = "đang gọi cho bạn.";
                    messageColor = Brushes.ForestGreen;
                    break;
                case "new_message":
                    message = "đã gửi một tin nhắn.";
                    messageColor = Brushes.ForestGreen;
                    break;
            }

            var avatar = new Ellipse
            {
                Width = 48,
                Height = 48,
                Margin = new Thickness(0, 0, 12, 0),
                //Fill = new ImageBrush(new BitmapImage(new Uri(avatarUrl, UriKind.RelativeOrAbsolute)))
                Fill = new ImageBrush(new BitmapImage(new Uri(
                    string.IsNullOrWhiteSpace(avatarUrl) ? "../../Assets/Icons/logo-app.jpg" : avatarUrl,
                    UriKind.RelativeOrAbsolute)))

            };

            var nameBlock = new TextBlock
            {
                Text = senderName,
                FontWeight = FontWeights.Bold,
                FontSize = 14
            };

            var messageBox = new Border
            {
                Background = (Brush)new BrushConverter().ConvertFrom("#EFEFEF"),
                CornerRadius = new CornerRadius(5),
                Padding = new Thickness(5),
                Margin = new Thickness(0, 4, 0, 0),
                Child = new TextBlock
                {
                    Text = message,
                    Foreground = messageColor,
                    FontSize = 13
                }
            };

            var textStack = new StackPanel();
            textStack.Children.Add(nameBlock);
            textStack.Children.Add(messageBox);

            var detailBtn = new Button
            {
                Content = "Chi tiết",
                Background = Brushes.LightGreen,
                Margin = new Thickness(0, 0, 0, 5),
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = Cursors.Hand
            };
            detailBtn.Click += async (s, e) =>
            {
                try
                {
                    noti.IsRead = true;
                    noti.ReadAt = DateTime.UtcNow;
                    await _firebaseService.UpdateNotificationAsync(noti);

                    if (noti.Type == "friend_request" && noti.Data.TryGetValue("from", out var fromId))
                    {
                        var senderUser = await _firebaseService.GetUserByIdAsync(fromId);

                        var dialog = new Window
                        {
                            Title = "Yêu cầu kết bạn",
                            Width = 300,
                            Height = 250,
                            Content = CreateFriendRequestDialog(senderUser, noti.Id),
                            WindowStartupLocation = WindowStartupLocation.CenterScreen,
                            ResizeMode = ResizeMode.NoResize
                        };
                        dialog.ShowDialog();

                        NotificationsPanel.Children.Clear();
                        LoadNotifications();
                    }
                    else if (noti.Data.TryGetValue("chatId", out var chatId))
                    {
                        MessageBox.Show($"Đi đến Chat với ID: {chatId}");
                    }
                    else
                    {
                        MessageBox.Show("Không tìm thấy thông tin chi tiết.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi khi xử lý thông báo: " + ex.Message);
                }
            };

            var deleteBtn = new Button
            {
                Content = "Xóa",
                Background = Brushes.IndianRed,
                Foreground = Brushes.White,
                Padding = new Thickness(8, 4, 8, 4),
                Cursor = Cursors.Hand
            };
            deleteBtn.Click += async (s, e) =>
            {
                await _firebaseService.DeleteNotificationAsync(noti.Id);
                NotificationsPanel.Children.Clear();
                LoadNotifications();
            };

            var buttonStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            buttonStack.Children.Add(detailBtn);
            buttonStack.Children.Add(deleteBtn);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(60) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });

            grid.Children.Add(avatar);
            Grid.SetColumn(avatar, 0);

            grid.Children.Add(textStack);
            Grid.SetColumn(textStack, 1);

            grid.Children.Add(buttonStack);
            Grid.SetColumn(buttonStack, 2);

            border.Child = grid;
            return border;
        }

        private StackPanel CreateFriendRequestDialog(User senderUser, string notificationId)
        {
            var panel = new StackPanel { Margin = new Thickness(10) };
            panel.Children.Add(new TextBlock { Text = $"Tên: {senderUser.FullName}" });
            panel.Children.Add(new TextBlock { Text = $"Email: {senderUser.Email}" });
            panel.Children.Add(new TextBlock { Text = $"Username: {senderUser.UserName}" });

            var acceptBtn = new Button { Content = "Chấp nhận", Margin = new Thickness(5) };
            var rejectBtn = new Button { Content = "Từ chối", Margin = new Thickness(5) };

            acceptBtn.Click += async (s, e) =>
            {
                await _firebaseService.UpdateFriendStatus(SessionManager.UID, senderUser.Id, "accept");
                await _firebaseService.AddChatAsync(new Chat
                {
                    Id = Guid.NewGuid().ToString(),
                    User1 = senderUser.Id,
                    User2 = SessionManager.UID,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });

                MessageBox.Show("Đã chấp nhận kết bạn.");
                Window.GetWindow((FrameworkElement)s)?.Close();
            };

            rejectBtn.Click += async (s, e) =>
            {
                await _firebaseService.UpdateFriendStatus(SessionManager.UID, senderUser.Id, "delete");
                MessageBox.Show("Đã từ chối yêu cầu.");
                Window.GetWindow((FrameworkElement)s)?.Close();
            };

            var btnRow = new StackPanel { Orientation = Orientation.Horizontal };
            btnRow.Children.Add(acceptBtn);
            btnRow.Children.Add(rejectBtn);
            panel.Children.Add(btnRow);

            return panel;
        }
    }
}
