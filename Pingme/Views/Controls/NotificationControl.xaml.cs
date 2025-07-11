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
using Pingme.Views.Dialogs;
using Pingme.Views.Windows;

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

            //string senderName = noti.Data.TryGetValue("fromName", out var fromName) ? fromName : "Ping me";
            string senderName = "Ping me";

            switch (noti.Type)
            {
                case "friend_request":
                case "new_message":
                case "call_missed":
                case "call_active":
                    noti.Data.TryGetValue("fromName", out senderName);
                    break;

                case "added_to_group":
                case "group_created":
                    noti.Data.TryGetValue("groupName", out senderName);
                    break;
            }

            //string avatarUrl = noti.Data.TryGetValue("fromAvatar", out var fromAvatar) ? fromAvatar : "/Assets/Icons/logo-app.jpg";
            string avatarUrl = "/Assets/Icons/logo-app.jpg";

            switch (noti.Type)
            {
                case "friend_request":
                case "new_message":
                case "call_missed":
                case "call_active":
                    if (noti.Data.TryGetValue("fromAvatar", out var fromAvt))
                        avatarUrl = fromAvt;
                    break;

                case "added_to_group":
                case "group_created":
                    if (noti.Data.TryGetValue("groupAvatar", out var groupAvt))
                        avatarUrl = groupAvt;
                    break;
            }

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

            //var nameBlock = new TextBlock
            //{
            //    Text = senderName,
            //    FontWeight = FontWeights.Bold,
            //    FontSize = 14
            //};
            var nameBlock = new TextBlock
            {
                Text = $"{GetPrefixEmoji(noti.Type)} {senderName}",
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

                        //var dialog = new Window
                        //{
                        //    Title = "Yêu cầu kết bạn",
                        //    Width = 350,
                        //    Height = 300,
                        //    Background = Brushes.White,
                        //    Content = CreateFriendRequestDialog(senderUser, noti.Id, noti.IsRead),
                        //    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                        //    ResizeMode = ResizeMode.NoResize
                        //};
                        //dialog.ShowDialog();

                        new FriendRequestDetailDialog(senderUser).ShowDialog();

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
        private string GetPrefixEmoji(string type)
        {
            switch (type)
            {
                case "friend_request": return "👤";
                case "added_to_group": return "👪";
                case "group_created": return "🆕";
                case "new_message": return "📩";
                case "call_missed": return "❌";
                case "call_active": return "📞";
                default: return "🔔";
            }
        }

        //private StackPanel CreateFriendRequestDialog(User senderUser, string notificationId, bool isRead)
        //{
        //    var panel = new StackPanel
        //    {
        //        Margin = new Thickness(15),
        //        Background = Brushes.White
        //    };

        //    panel.Children.Add(new TextBlock
        //    {
        //        Text = "Yêu cầu kết bạn",
        //        FontWeight = FontWeights.Bold,
        //        FontSize = 16,
        //        Margin = new Thickness(0, 0, 0, 10),
        //        Foreground = Brushes.DarkSlateBlue
        //    });

        //    panel.Children.Add(new TextBlock { Text = $"👤 Tên: {senderUser.FullName}", Margin = new Thickness(0, 2, 0, 2) });
        //    panel.Children.Add(new TextBlock { Text = $"📧 Email: {senderUser.Email}", Margin = new Thickness(0, 2, 0, 2) });
        //    panel.Children.Add(new TextBlock { Text = $"🆔 Username: {senderUser.UserName}", Margin = new Thickness(0, 2, 0, 10) });

        //    var btnRow = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };

        //    var acceptBtn = new Button
        //    {
        //        Content = "✔ Chấp nhận",
        //        Margin = new Thickness(5),
        //        Background = Brushes.LightGreen,
        //        Padding = new Thickness(8, 4, 8, 4),
        //        IsEnabled = !isRead
        //    };

        //    var rejectBtn = new Button
        //    {
        //        Content = "❌ Từ chối",
        //        Margin = new Thickness(5),
        //        Background = Brushes.IndianRed,
        //        Foreground = Brushes.White,
        //        Padding = new Thickness(8, 4, 8, 4),
        //        IsEnabled = !isRead
        //    };

        //    if (isRead)
        //    {
        //        panel.Children.Add(new TextBlock
        //        {
        //            Text = "🔒 Yêu cầu này đã được xử lý trước đó.",
        //            FontStyle = FontStyles.Italic,
        //            Foreground = Brushes.Gray,
        //            Margin = new Thickness(0, 5, 0, 0)
        //        });
        //    }

        //    acceptBtn.Click += async (s, e) =>
        //    {
        //        await _firebaseService.UpdateFriendStatus(SessionManager.UID, senderUser.Id, "accept");
        //        await _firebaseService.AddChatAsync(new Chat
        //        {
        //            Id = Guid.NewGuid().ToString(),
        //            User1 = senderUser.Id,
        //            User2 = SessionManager.UID,
        //            CreatedAt = DateTime.UtcNow,
        //            UpdatedAt = DateTime.UtcNow
        //        });

        //        MessageBox.Show("Đã chấp nhận kết bạn.");
        //        Window.GetWindow((FrameworkElement)s)?.Close();
        //    };

        //    rejectBtn.Click += async (s, e) =>
        //    {
        //        await _firebaseService.UpdateFriendStatus(SessionManager.UID, senderUser.Id, "delete");
        //        MessageBox.Show("Đã từ chối yêu cầu.");
        //        Window.GetWindow((FrameworkElement)s)?.Close();
        //    };

        //    btnRow.Children.Add(acceptBtn);
        //    btnRow.Children.Add(rejectBtn);
        //    panel.Children.Add(btnRow);

        //    return panel;
        //}

    }
}
