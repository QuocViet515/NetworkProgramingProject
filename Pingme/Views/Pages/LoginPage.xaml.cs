using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for LoginPage.xaml
    /// </summary>
    public partial class LoginPage : Page
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult("haBViPv4zOuLMSuBt2mnaD2SYokqsRpbLIt5PcFL")
            });

        public LoginPage()
        {
            InitializeComponent();

            if (Properties.Settings.Default.RememberMe)
            {
                UsernameTextBox.Text = Properties.Settings.Default.SavedUsername;
                PasswordBox.Password = Properties.Settings.Default.SavedPassword;
                RememberMeCheckBox.IsChecked = true;
            }
        }
        private async Task<string> SignInAndGetUID(string email, string password)
        {
            var payload = new
            {
                email = email,
                password = password,
                returnSecureToken = true
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key=AIzaSyDC_fCjmDw4IkAqhLjqWCzG02LRXmvKgB0",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(body);
                    return result.localId;
                }
                else return null;
            }
        }
        private async Task<string> ResolveEmailIfUsername(string input)
        {
            if (input.Contains("@")) return input; // là email rồi

            var users = await _firebase.Child("users").OnceAsync<User>();
            var matched = users.FirstOrDefault(u => u.Object.UserName == input);
            return matched?.Object?.Email;
        }

        private async void Login_Click(object sender, RoutedEventArgs e)
        {
            string input = UsernameTextBox.Text.Trim();
            string email = await ResolveEmailIfUsername(input);
            string password = PasswordBox.Password;
            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Không tìm thấy người dùng.");
                return;
            }

            string uid = await SignInAndGetUID(email, password);
            if (uid == null)
            {
                MessageBox.Show("Đăng nhập thất bại. Vui lòng kiểm tra email và mật khẩu.");
                return;
            }

            // Lấy thông tin user từ database
            var user = await _firebase.Child("users").Child(uid).OnceSingleAsync<User>();
            if (user != null)
            {
                MessageBox.Show($"Chào {user.FullName}!");
                // this.NavigationService.Navigate(new MainPage());
            }
            else
            {
                MessageBox.Show("Không tìm thấy thông tin người dùng.");
            }


            if (RememberMeCheckBox.IsChecked == true)
            {
                Properties.Settings.Default.RememberMe = true;
                Properties.Settings.Default.SavedUsername = UsernameTextBox.Text.Trim();
                Properties.Settings.Default.SavedPassword = PasswordBox.Password.Trim();
            }
            else
            {
                Properties.Settings.Default.RememberMe = false;
                Properties.Settings.Default.SavedUsername = string.Empty;
                Properties.Settings.Default.SavedPassword = string.Empty;
            }

            // Rất quan trọng: Save lại Settings
            Properties.Settings.Default.Save();

        }
        private bool isPasswordVisible = false;

        private void TogglePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isPasswordVisible = !isPasswordVisible;

            if (isPasswordVisible)
            {
                PasswordVisibleBox.Text = PasswordBox.Password;
                PasswordBox.Visibility = Visibility.Collapsed;
                PasswordVisibleBox.Visibility = Visibility.Visible;
                TogglePasswordButton.Content = "🙈";
            }
            else
            {
                PasswordBox.Password = PasswordVisibleBox.Text;
                PasswordBox.Visibility = Visibility.Visible;
                PasswordVisibleBox.Visibility = Visibility.Collapsed;
                TogglePasswordButton.Content = "👁";
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!isPasswordVisible)
                PasswordVisibleBox.Text = PasswordBox.Password;
        }

        private void PasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isPasswordVisible)
                PasswordBox.Password = PasswordVisibleBox.Text;

            PasswordPlaceholder.Visibility = string.IsNullOrEmpty(PasswordVisibleBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void ForgotPassword_Click(object sender, MouseButtonEventArgs e)
        {
            // Điều hướng sang ForgotPasswordPage
            this.NavigationService.Navigate(new ForgotPasswordPage());
        }

        private void SignUp_Click(object sender, MouseButtonEventArgs e)
        {
            // Điều hướng sang RegisterPage
            this.NavigationService.Navigate(new RegisterPage());
        }
    }
}
