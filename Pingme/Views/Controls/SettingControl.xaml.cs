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
using Pingme.Views.Pages;

namespace Pingme.Views.Controls
{
    public partial class SettingControl : UserControl
    {
        public SettingControl()
        {
            InitializeComponent();
            UpdateStatusVisual();
        }

        private void StatusToggle_Click(object sender, RoutedEventArgs e)
        {
            UpdateStatusVisual();
        }

        private void UpdateStatusVisual()
        {
            if (StatusToggle.IsChecked == true)
            {
                StatusToggle.Content = "Bật";
                StatusToggle.Background = Brushes.LightGreen;
                StatusToggle.Foreground = Brushes.Black;
            }
            else
            {
                StatusToggle.Content = "Tắt";
                StatusToggle.Background = Brushes.IndianRed;
                StatusToggle.Foreground = Brushes.White;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đã lưu cài đặt.");
            // TODO: bạn có thể lưu vào Firebase hoặc local config
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult confirm = MessageBox.Show(
                "Bạn có chắc chắn muốn đăng xuất?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    // Xóa token/tài khoản lưu trong SessionManager nếu bạn có
                    SessionManager.IdToken = null;
                    SessionManager.UID = null;

                    // Điều hướng về HomePage
                    var home = new HomePage();
                    NavigationService.GetNavigationService(this)?.Navigate(home);
                }
                catch
                {
                    MessageBox.Show("Lỗi khi đăng xuất.");
                }
            }
        }
    }
}
