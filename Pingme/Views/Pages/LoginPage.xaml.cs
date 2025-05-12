using Pingme.Services;
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
using System.Windows.Shapes;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : UserControl
    {
        public LoginPage()
        {
            InitializeComponent();
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameBox.Text.Trim();
            string password = PasswordBox.Password.Trim();

            bool success = await AuthService.LoginAsync(username, password);
            if (success)
            {
                // ✅ Mở trang chat nếu đăng nhập thành công
                var chatPage = new ChatPage();
                Window parent = Window.GetWindow(this);
                parent.Content = chatPage;
            }
            else
            {
                ErrorText.Text = "Sai tên đăng nhập hoặc mật khẩu!";
            }
        }
    }
}
