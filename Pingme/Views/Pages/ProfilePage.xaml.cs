using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using Pingme.Views.Controls;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        private UserService _userService = new UserService();
        private FirebaseService _firebaseService = new FirebaseService();
        private List<User> _searchResults = new List<User>();
        public ProfilePage()
        {
            InitializeComponent();
        }
        public ProfilePage(string section = "profile")
        {
            InitializeComponent();

            Loaded += (s, e) =>
            {
                switch (section)
                {
                    case "notification":
                        LeftPanelContent.Content = new NotificationControl();
                        break;
                    case "setting":
                        LeftPanelContent.Content = new SettingControl();
                        break;
                    case "friendgroup":
                        LeftPanelContent.Content = new MyFriendAndGroupControl();
                        break;
                    default:
                        LeftPanelContent.Content = new SettingControl(); // Mặc định
                        break;
                }
            };
        }
        private void GoToChat_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ChatPage());
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new SettingControl(); // Mặc định là setting
        }
        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new SettingControl();
        }

        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new NotificationControl();
        }

        private void BtnFriendGroup_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new MyFriendAndGroupControl();
        }
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string keyword = SearchTextBox.Text.Trim().ToLower();
            if (keyword.Length < 2)
            {
                SearchPopup.IsOpen = false;
                return;
            }

            var allUsers = await _userService.GetAllUsersAsync();
            _searchResults = allUsers
                .Where(u => u.FullName.ToLower().Contains(keyword) && u.Id != SessionManager.UID)
                .ToList();

            RenderSearchResults();
        }

        private async /*void*/ Task RenderSearchResults()
        {
            SearchResultsPanel.Items.Clear();

            var allFriends = await _firebaseService.GetAllFriendsAsync();

            foreach (var user in _searchResults)
            {
                // Avatar
                BitmapImage avatarImage = null;
                try
                {
                    avatarImage = new BitmapImage(new Uri(user.AvatarUrl ?? "", UriKind.RelativeOrAbsolute));
                }
                catch { avatarImage = null; }

                var avatar = new Image
                {
                    Width = 40,
                    Height = 40,
                    Margin = new Thickness(5),
                    //Source = /*new BitmapImage(new Uri(user.AvatarUrl, UriKind.RelativeOrAbsolute))*/
                    Source = avatarImage
                };

                var info = new StackPanel { Margin = new Thickness(5) };
                info.Children.Add(new TextBlock { Text = $"@{user.UserName}" });
                info.Children.Add(new TextBlock { Text = user.FullName });
                info.Children.Add(new TextBlock { Text = user.Email, FontStyle = FontStyles.Italic });

                //var allFriends = await _firebaseService.GetAllFriendsAsync(); // THÊM DÒNG NÀY NẾU CHƯA CÓ
                // Check friendship status
                var isFriend = allFriends.Any(f =>
                    f.Status == "accept" &&
                    ((f.User1 == SessionManager.UID && f.User2 == user.Id) ||
                     (f.User2 == SessionManager.UID && f.User1 == user.Id)));

                var isWaiting = allFriends.Any(f =>
                    f.Status == "waiting" &&
                    ((f.User1 == SessionManager.UID && f.User2 == user.Id) ||
                     (f.User2 == SessionManager.UID && f.User1 == user.Id)));

                // Action button
                Button actionBtn;
                if (isFriend)
                {
                    actionBtn = new Button
                    {
                        Content = "Chat",
                        Background = Brushes.LightGreen,
                        Margin = new Thickness(5),
                        Padding = new Thickness(5)
                    };
                    actionBtn.Click += (s, e) =>
                    {
                        this.NavigationService?.Navigate(new ChatPage());
                    };
                }
                else if (isWaiting)
                {
                    actionBtn = new Button
                    {
                        Content = "Đang chờ",
                        IsEnabled = false,
                        Margin = new Thickness(5),
                        Padding = new Thickness(5)
                    };
                }
                else
                {
                    actionBtn = new Button
                    {
                        Content = "Kết bạn",
                        Background = Brushes.LightSkyBlue,
                        Margin = new Thickness(5),
                        Padding = new Thickness(5)
                    };
                    actionBtn.Click += async (s, e) => await SendFriendRequest(user);
                }

                // Wrap and add to panel
                var wrap = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(5) };
                wrap.Children.Add(avatar);
                wrap.Children.Add(info);
                wrap.Children.Add(actionBtn);

                SearchResultsPanel.Items.Add(wrap);
            }

            SearchPopup.IsOpen = _searchResults.Count > 0;
        }
        private async Task SendFriendRequest(User targetUser)
        {
            var allFriends = await _firebaseService.GetAllFriendsAsync();
            var exists = allFriends.Any(f =>
                (f.User1 == SessionManager.UID && f.User2 == targetUser.Id) ||
                (f.User2 == SessionManager.UID && f.User1 == targetUser.Id));

            if (exists)
            {
                MessageBox.Show("Yêu cầu đã tồn tại hoặc đã là bạn.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var friend = new Friend
            {
                Id = Guid.NewGuid().ToString(),
                User1 = SessionManager.UID,
                User2 = targetUser.Id,
                Status = "waiting",
                CreatedAt = DateTime.UtcNow
            };

            var notification = new Notification
            {
                Id = Guid.NewGuid().ToString(),
                ReceiverId = targetUser.Id,
                Type = "friend_request",
                Data = new Dictionary<string, string>
        {
            { "from", SessionManager.UID },
            { "fromName", SessionManager.CurrentUser?.FullName ?? "Người dùng" },
            { "fromAvatar", SessionManager.CurrentUser?.AvatarUrl ?? "" }
        },
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _firebaseService.AddFriendAsync(friend);
            await _firebaseService.AddNotificationAsync(notification);
            MessageBox.Show("Yêu cầu kết bạn đã được gửi.", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            // Có thể trigger mở Popup ở đây nếu cần
            if (_searchResults.Count > 0)
            {
                SearchPopup.IsOpen = true;
            }
        }

    }
}
