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

namespace Pingme.Views.Dialogs
{
    public partial class KickMemberDialog : Window
    {
        private readonly string _groupId;
        private ChatGroup _group;
        private readonly FirebaseService _firebase = new FirebaseService();

        public KickMemberDialog(string groupId)
        {
            InitializeComponent();
            _groupId = groupId;

            // Lấy thông tin nhóm từ Session
            if (!SessionManager.CurrentGroups.TryGetValue(_groupId, out _group))
            {
                MessageBox.Show("❌ Không tìm thấy nhóm.");
                Close();
                return;
            }

            LoadGroupInfo();
            LoadMemberListAsync();
        }

        private void LoadGroupInfo()
        {
            GroupNameText.Text = _group.Name ?? "Nhóm";
            GroupAvatar.Source = new BitmapImage(
                new Uri(_group.AvatarUrl ?? "/Assets/Icons/avatar-default.png", UriKind.RelativeOrAbsolute));
        }

        private async Task LoadMemberListAsync()
        {
            MemberListPanel.Children.Clear();

            foreach (var memberId in _group.Members.ToList())
            {
                // Không cho xóa chính mình
                if (memberId == SessionManager.UID)
                    continue;

                if (!SessionManager.CurrentUserMap.TryGetValue(memberId, out var user))
                    continue;

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
                    Source = new BitmapImage(new Uri(user.AvatarUrl ?? "/Assets/Icons/avatar-default.png", UriKind.RelativeOrAbsolute)),
                    Width = 40,
                    Height = 40,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(0, 0, 10, 0),
                    ClipToBounds = true
                };

                var nameBlock = new TextBlock
                {
                    Text = user.FullName ?? user.UserName,
                    FontWeight = FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var kickBtn = new Button
                {
                    Content = "❌ Xóa",
                    Background = Brushes.IndianRed,
                    Foreground = Brushes.White,
                    Padding = new Thickness(10, 4, 10, 4),
                    Margin = new Thickness(10, 0, 0, 0),
                    BorderBrush = null,
                    BorderThickness = new Thickness(0),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 12,
                    Tag = user.Id
                };
                kickBtn.Click += KickBtn_Click;

                DockPanel.SetDock(avatar, Dock.Left);
                DockPanel.SetDock(kickBtn, Dock.Right);
                panel.Children.Add(avatar);
                panel.Children.Add(nameBlock);
                panel.Children.Add(kickBtn);

                border.Child = panel;
                MemberListPanel.Children.Add(border);
            }

            if (MemberListPanel.Children.Count == 0)
            {
                MemberListPanel.Children.Add(new TextBlock
                {
                    Text = "Không có thành viên nào để xóa.",
                    Foreground = Brushes.Gray,
                    FontStyle = FontStyles.Italic,
                    Margin = new Thickness(10)
                });
            }
        }

        private async void KickBtn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string userId)
            {
                var confirm = MessageBox.Show($"Bạn có chắc muốn xóa thành viên này?", "Xác nhận xóa", MessageBoxButton.YesNo);
                if (confirm != MessageBoxResult.Yes) return;

                btn.IsEnabled = false;
                btn.Content = "⏳ Đang xóa...";

                try
                {
                    await _firebase.RemoveUserFromGroupAsync(_groupId, userId);

                    // Cập nhật cache
                    _group.Members.Remove(userId);
                    SessionManager.CurrentGroups[_groupId] = _group;

                    // Reload lại danh sách
                    await LoadMemberListAsync();
                }
                catch (Exception ex)
                {
                    btn.Content = "❌ Lỗi";
                    MessageBox.Show("Không thể xóa: " + ex.Message);
                }
            }
        }
    }
}
