using Pingme.Models;
using Pingme.Views.Pages;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pingme.Views.Windows
{
    public partial class GroupDetailDialog : Window
    {
        private readonly string _groupId;

        public GroupDetailDialog(ChatGroup group, User creator)
        {
            InitializeComponent();
            _groupId = group.Id;

            // Set tên + người tạo
            GroupNameText.Text = group.Name;
            CreatedByText.Text = $"👤 Người tạo: {creator?.FullName ?? "Không rõ"}";

            // Set thông tin chi tiết
            GroupIdText.Text = $"🆔 Mã nhóm: {group.Id}";
            AdminCountText.Text = $"👑 Số admin: {group.Admin?.Count ?? 0} người";
            MemberCountText.Text = $"👥 Số thành viên: {group.Members?.Count ?? 0} người";
            CreatedAtText.Text = $"📅 Ngày tạo: {group.CreatedAt.ToLocalTime():dd/MM/yyyy HH:mm}";

            // Avatar nhóm
            GroupAvatarImage.ImageSource = new BitmapImage(new Uri(
                string.IsNullOrWhiteSpace(group.AvatarUrl)
                    ? "pack://application:,,,/Assets/Icons/logo-app.jpg"
                    : group.AvatarUrl,
                UriKind.RelativeOrAbsolute));
        }

        private void OpenChat_Click(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow?.MainFrame.Navigate(new ChatPage(_groupId, isGroup: true));
            this.Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
