using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Pingme.Views.Windows;
using Pingme.Views.Pages;
using Pingme.Views.Controls;

namespace Pingme.Views.Dialogs
{
    public partial class FindFriendDialog : Window
    {
        private readonly FirebaseService _firebase = new FirebaseService();
        private List<User> allUsers = new List<User>();
        private List<Friend> allFriends = new List<Friend>();

        public FindFriendDialog()
        {
            InitializeComponent();
            Loaded += FindFriendDialog_Loaded;
        }

        private async void FindFriendDialog_Loaded(object sender, RoutedEventArgs e)
        {
            LoadingText.Visibility = Visibility.Visible;
            allUsers = await _firebase.GetAllUsersAsync();
            allFriends = await _firebase.GetAllFriendsAsync();
            LoadingText.Visibility = Visibility.Collapsed;
        }

        private async Task SearchUsersAsync(string keyword)
        {
            keyword = keyword.Trim().ToLower();
            if (string.IsNullOrWhiteSpace(keyword))
            {
                ResultsPanel.Children.Clear();
                ResultCountText.Text = "";
                return;
            }

            LoadingText.Visibility = Visibility.Visible;

            await Task.Delay(300); // debounce

            var currentUserId = SessionManager.UID;
            var matched = allUsers
                .Where(u =>
                    u.Id != currentUserId &&
                    (
                        u.FullName.ToLower().Contains(keyword) ||
                        u.Email.ToLower().Contains(keyword) ||
                        u.UserName.ToLower().Contains(keyword) ||
                        (u.Phone ?? "").ToLower().Contains(keyword)
                    )
                )
                .ToList();

            ResultsPanel.Children.Clear();

            //foreach (var user in matched)
            //{
            //    ResultsPanel.Children.Add(CreateUserResultItem(user));
            //}
            await RefreshFriendList(); // 👈 THÊM TRƯỚC khi hiển thị danh sách

            foreach (var user in matched)
            {
                ResultsPanel.Children.Add(await CreateUserResultItem(user));
            }

            ResultCountText.Text = matched.Any()
                ? $"🔍 Tìm thấy {matched.Count} kết quả."
                : "❌ Không tìm thấy người dùng nào.";

            LoadingText.Visibility = Visibility.Collapsed;
        }
        private async Task RefreshFriendList()
        {
            allFriends = await _firebase.GetAllFriendsAsync();
        }

        //private Border CreateUserResultItem(User user)
        //{
        //    var isFriend = allFriends.Any(f =>
        //        f.Status == "accept" &&
        //        ((f.User1 == SessionManager.UID && f.User2 == user.Id) ||
        //         (f.User2 == SessionManager.UID && f.User1 == user.Id)));

        //    var avatar = new Ellipse
        //    {
        //        Width = 50,
        //        Height = 50,
        //        Margin = new Thickness(10),
        //        Fill = new ImageBrush
        //        {
        //            ImageSource = new BitmapImage(new Uri(
        //                string.IsNullOrWhiteSpace(user.AvatarUrl)
        //                    ? "pack://application:,,,/Assets/Icons/avatar-default.png"
        //                    : user.AvatarUrl,
        //                UriKind.RelativeOrAbsolute))
        //        }
        //    };

        //    var nameText = new TextBlock
        //    {
        //        Text = $"{user.FullName} ({user.UserName})",
        //        FontWeight = FontWeights.Bold,
        //        FontSize = 14,
        //        Margin = new Thickness(0, 0, 0, 2)
        //    };

        //    var emailText = new TextBlock
        //    {
        //        Text = user.Email,
        //        FontSize = 12,
        //        Foreground = Brushes.Gray
        //    };

        //    var textStack = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };
        //    textStack.Children.Add(nameText);
        //    textStack.Children.Add(emailText);

        //    var buttonStack = new StackPanel
        //    {
        //        Orientation = Orientation.Vertical,
        //        VerticalAlignment = VerticalAlignment.Center
        //    };

        //    if (isFriend)
        //    {
        //        buttonStack.Children.Add(new Button
        //        {
        //            Content = "✔ Đã là bạn",
        //            IsEnabled = false,
        //            Foreground = Brushes.Green,
        //            BorderBrush = Brushes.LightGreen,
        //            Padding = new Thickness(4),
        //            Margin = new Thickness(0, 0, 0, 5)
        //        });
        //    }
        //    else
        //    {
        //        var addButton = new Button
        //        {
        //            Content = "➕ Kết bạn",
        //            Padding = new Thickness(4),
        //            Margin = new Thickness(0, 0, 0, 5)
        //        };
        //        addButton.Click += async (s, e) =>
        //        {
        //            await _firebase.AddFriendAsync(new Friend
        //            {
        //                Id = Guid.NewGuid().ToString(),
        //                User1 = SessionManager.UID,
        //                User2 = user.Id,
        //                Status = "pending",
        //                CreatedAt = DateTime.UtcNow
        //            });
        //            addButton.IsEnabled = false;
        //            addButton.Content = "⏳ Đã gửi yêu cầu";
        //        };
        //        buttonStack.Children.Add(addButton);
        //    }

        //    var detailButton = new Button
        //    {
        //        Content = "👁 Chi tiết",
        //        Padding = new Thickness(4)
        //    };
        //    detailButton.Click += (s, e) =>
        //    {
        //        var profile = new UserDetailDialog(user);
        //        profile.ShowDialog();
        //    };

        //    buttonStack.Children.Add(detailButton);

        //    var grid = new Grid();
        //    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        //    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        //    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        //    Grid.SetColumn(avatar, 0);
        //    Grid.SetColumn(textStack, 1);
        //    Grid.SetColumn(buttonStack, 2);

        //    grid.Children.Add(avatar);
        //    grid.Children.Add(textStack);
        //    grid.Children.Add(buttonStack);

        //    return new Border
        //    {
        //        Margin = new Thickness(10, 6, 10, 6),
        //        Background = Brushes.WhiteSmoke,
        //        CornerRadius = new CornerRadius(8),
        //        Child = grid,
        //        Effect = new System.Windows.Media.Effects.DropShadowEffect
        //        {
        //            Color = Colors.Gray,
        //            BlurRadius = 8,
        //            ShadowDepth = 2,
        //            Opacity = 0.3
        //        }
        //    };
        //}
        private async Task<Border> CreateUserResultItem(User user)
        {
            var isFriend = allFriends.Any(f =>
                f.Status == "accept" &&
                ((f.User1 == SessionManager.UID && f.User2 == user.Id) ||
                 (f.User2 == SessionManager.UID && f.User1 == user.Id)));

            var avatar = new Ellipse
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(10),
                Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri(
                        string.IsNullOrWhiteSpace(user.AvatarUrl)
                            ? "pack://application:,,,/Assets/Icons/avatar-default.png"
                            : user.AvatarUrl,
                        UriKind.RelativeOrAbsolute))
                }
            };

            var nameText = new TextBlock
            {
                Text = $"{user.FullName} ({user.UserName})",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 2)
            };

            var emailText = new TextBlock
            {
                Text = user.Email,
                FontSize = 12,
                Foreground = Brushes.Gray
            };

            var textStack = new StackPanel { Margin = new Thickness(0, 5, 0, 0) };
            textStack.Children.Add(nameText);
            textStack.Children.Add(emailText);

            var buttonStack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (isFriend)
            {
                // ===== Nút Hủy bạn =====
                var unfriendBtn = new Button
                {
                    Content = "❌ Hủy bạn",
                    Padding = new Thickness(6, 2, 6, 2),
                    Background = Brushes.IndianRed,
                    Foreground = Brushes.White,
                    BorderBrush = Brushes.DarkRed,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };

                unfriendBtn.Click += async (s, e) =>
                {
                    var result = MessageBox.Show("Bạn có chắc chắn muốn hủy kết bạn không?", "Xác nhận", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Tìm chat cá nhân
                        //var allChats = await _firebase.GetAllChatsAsync();
                        //var chat = allChats.FirstOrDefault(c =>
                        //    ((c.User1 == SessionManager.UID && c.User2 == user.Id) ||
                        //     (c.User2 == SessionManager.UID && c.User1 == user.Id)));

                        //if (chat != null)
                        //{
                        //    // Xóa tin nhắn
                        //    await _firebase.DeleteMessagesByChatIdAsync(chat.Id);

                        //    // Xóa chat
                        //    await _firebase.DeleteChatAsync(chat.Id);
                        //} -> giu lai doan chat

                        // Xóa friend
                        await _firebase.DeleteFriendAsync(SessionManager.UID, user.Id);

                        MessageBox.Show("Đã hủy kết bạn.");

                        await SearchUsersAsync(SearchBox.Text); // Refresh lại danh sách

                        var mainWindow = Application.Current.MainWindow as MainWindow;

                        if (mainWindow?.MainFrame.Content is ProfilePage profilePage)
                        {
                            if (profilePage.LeftPanelContent.Content is MyFriendAndGroupControl friendControl)
                            {
                                var loadMethod = typeof(MyFriendAndGroupControl)
                                    .GetMethod("LoadData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                                loadMethod?.Invoke(friendControl, null);
                            }
                        }

                    }
                };

                var unfriendBorder = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Child = unfriendBtn,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                buttonStack.Children.Add(unfriendBorder);
            }
            else
            {
                // ===== Nút Kết bạn =====
                var addButton = new Button
                {
                    Content = "➕ Kết bạn",
                    Padding = new Thickness(6, 2, 6, 2),
                    Background = Brushes.LightGreen,
                    Foreground = Brushes.Black,
                    BorderBrush = Brushes.Green,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand
                };

                addButton.Click += async (s, e) =>
                {
                    await _firebase.AddFriendAsync(new Friend
                    {
                        Id = Guid.NewGuid().ToString(),
                        User1 = SessionManager.UID,
                        User2 = user.Id,
                        Status = "pending",
                        CreatedAt = DateTime.UtcNow
                    });
                    addButton.IsEnabled = false;

                    // Gửi thông báo lời mời kết bạn
                    //await _firebase.AddNotificationAsync(new Notification
                    //{
                    //    Id = Guid.NewGuid().ToString(),
                    //    ReceiverId = user.Id,
                    //    Type = "friend_request",
                    //    Data = new Dictionary<string, string>
                    //    {
                    //        { "senderId", SessionManager.UID }
                    //    },
                    //    IsRead = false,
                    //    CreatedAt = DateTime.UtcNow
                    //});

                    var sender = await _firebase.GetUserByIdAsync(SessionManager.UID);

                    await _firebase.AddNotificationAsync(new Notification
                    {
                        Id = Guid.NewGuid().ToString(),
                        ReceiverId = user.Id,
                        Type = "friend_request",
                        Data = new Dictionary<string, string>
                        {
                            { "from", sender.Id },
                            { "fromName", sender.FullName },
                            { "fromAvatar", sender.AvatarUrl ?? "" }
                        },
                        IsRead = false,
                        CreatedAt = DateTime.UtcNow
                    });

                    addButton.Content = "⏳ Đã gửi yêu cầu";

                    //load lai:
                    var mainWindow = Application.Current.MainWindow as MainWindow;

                    if (mainWindow?.MainFrame.Content is ProfilePage profilePage)
                    {
                        if (profilePage.LeftPanelContent.Content is MyFriendAndGroupControl friendControl)
                        {
                            var loadMethod = typeof(MyFriendAndGroupControl)
                                .GetMethod("LoadData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                            loadMethod?.Invoke(friendControl, null);
                        }
                    }

                };

                var addBorder = new Border
                {
                    CornerRadius = new CornerRadius(6),
                    Child = addButton,
                    Margin = new Thickness(0, 0, 0, 5)
                };
                buttonStack.Children.Add(addBorder);
            }

            // ===== Nút Chi tiết =====
            var detailButton = new Button
            {
                Content = "👁 Chi tiết",
                Padding = new Thickness(6, 2, 6, 2),
                Background = Brushes.LightBlue,
                Foreground = Brushes.Black,
                BorderBrush = Brushes.SteelBlue,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };

            detailButton.Click += (s, e) =>
            {
                var profile = new UserDetailDialog(user);
                profile.ShowDialog();
            };

            var detailBorder = new Border
            {
                CornerRadius = new CornerRadius(6),
                Child = detailButton
            };
            buttonStack.Children.Add(detailBorder);

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            Grid.SetColumn(avatar, 0);
            Grid.SetColumn(textStack, 1);
            Grid.SetColumn(buttonStack, 2);

            grid.Children.Add(avatar);
            grid.Children.Add(textStack);
            grid.Children.Add(buttonStack);

            return new Border
            {
                Margin = new Thickness(10, 6, 10, 6),
                Background = Brushes.WhiteSmoke,
                CornerRadius = new CornerRadius(8),
                Child = grid,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = Colors.Gray,
                    BlurRadius = 8,
                    ShadowDepth = 2,
                    Opacity = 0.3
                }
            };
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _ = SearchUsersAsync(SearchBox.Text);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            _ = SearchUsersAsync(SearchBox.Text);
        }

    }
}
