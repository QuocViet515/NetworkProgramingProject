using Pingme.Models;
using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Pingme.Views.Windows
{
    public partial class UserDetailDialog : Window
    {
        public UserDetailDialog(User user)
        {
            InitializeComponent();
            LoadUserInfo(user);
        }

        private void LoadUserInfo(User user)
        {
            FullNameText.Text = user.FullName;
            UsernameText.Text = $"({user.UserName})";
            EmailText.Text = $"📧 {user.Email}";
            PhoneText.Text = $"📱 {user.Phone}";
            BirthdayText.Text = $"🎂 {user.Birthday:dd/MM/yyyy}";
            AddressText.Text = $"📍 {user.Address}";

            AvatarImage.ImageSource = new BitmapImage(new Uri(
                string.IsNullOrWhiteSpace(user.AvatarUrl)
                    ? "pack://application:,,,/Assets/Icons/avatar-default.png"
                    : user.AvatarUrl,
                UriKind.RelativeOrAbsolute));
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

}
