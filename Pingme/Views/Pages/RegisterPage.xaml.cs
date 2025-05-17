using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
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
using Firebase.Database;
using Firebase.Database.Query;
using Pingme.Models;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using Newtonsoft.Json.Linq;
using Pingme.Helpers;


namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for RegisterPage.xaml
    /// </summary>
    public partial class RegisterPage : Page
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult("haBViPv4zOuLMSuBt2mnaD2SYokqsRpbLIt5PcFL")
            });
        public RegisterPage()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(EmailTextBox.Text);
                if (addr.Address != EmailTextBox.Text)
                {
                    MessageBox.Show("Invalid email!");
                    return;
                }
            }
            catch
            {
                MessageBox.Show("Invalid email!");
                return;
            }

            if (string.IsNullOrWhiteSpace(EmailTextBox.Text) || PasswordBox.Password != ConfirmPasswordBox.Password)
            {
                MessageBox.Show("Please check email or confirm password.");
                return;
            }

            Step1Panel.Visibility = Visibility.Collapsed;
            Step2Panel.Visibility = Visibility.Visible;
        }

        private void GoToLogin_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new LoginPage());
        }

        // Tạo tài khoản Firebase Authentication
        private async Task<FirebaseAuthResponse> CreateFirebaseUser(string email, string password)
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
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={SessionManager.FirebaseApiKey}",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(responseBody);
                }

                return null;
            }
        }


        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();
            string password = PasswordBox.Password;
            string username = UsernameTextBox.Text.Trim();
            string fullName = FullNameTextBox.Text.Trim();
            string birthday = BirthdayTextBox.Text.Trim();
            string phone = PhoneTextBox.Text.Trim();

            try
            {
                var authResponse = await CreateFirebaseUser(email, password);
                if (authResponse == null)
                {
                    MessageBox.Show("Firebase Authentication registration failed.");
                    return;
                }
                string uid = authResponse.localId;
                // Lưu thông tin token vào SessionManager
                SessionManager.UID = uid;
                SessionManager.IdToken = authResponse.idToken;
                SessionManager.RefreshToken = authResponse.refreshToken;
                SessionManager.TokenExpiresAt = DateTime.Now.AddSeconds(int.Parse(authResponse.expiresIn));

                // Kiểm tra trùng username
                var existingUsers = await _firebase.Child("users").OnceAsync<User>();
                bool usernameExists = existingUsers.Any(u => u.Object.UserName == username);

                if (usernameExists)
                {
                    MessageBox.Show("Username already exists. Please choose another Username");
                    return;
                }

                if (!DateTime.TryParseExact(birthday, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedBirthday))
                {
                    MessageBox.Show("Date of birth is not in dd/MM/yyyy format!");
                    return;
                }

                // Tạo đối tượng user
                var newUser = new User
                {
                    UserName = username,
                    FullName = fullName,
                    Email = email,
                    Phone = phone,
                    Birthday = parsedBirthday,
                    CreateAt = DateTime.Now
                };

                // Gửi lên Firebase
                await _firebase.Child("users").Child(uid).PutAsync(newUser);

                MessageBox.Show("Registered successfully!");
                this.NavigationService.Navigate(new LoginPage());
            }
            catch (Exception)
            {
                MessageBox.Show("Registration failed, please register again later.");
            }
        }


        private bool isPasswordVisible = false;
        private bool isConfirmPasswordVisible = false;

        // Password
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

        // Confirm Password
        private void ToggleConfirmPasswordButton_Click(object sender, RoutedEventArgs e)
        {
            isConfirmPasswordVisible = !isConfirmPasswordVisible;

            if (isConfirmPasswordVisible)
            {
                ConfirmPasswordVisibleBox.Text = ConfirmPasswordBox.Password;
                ConfirmPasswordBox.Visibility = Visibility.Collapsed;
                ConfirmPasswordVisibleBox.Visibility = Visibility.Visible;
                ToggleConfirmPasswordButton.Content = "🙈";
            }
            else
            {
                ConfirmPasswordBox.Password = ConfirmPasswordVisibleBox.Text;
                ConfirmPasswordBox.Visibility = Visibility.Visible;
                ConfirmPasswordVisibleBox.Visibility = Visibility.Collapsed;
                ToggleConfirmPasswordButton.Content = "👁";
            }
        }

        private void ConfirmPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPasswordBox.Password)
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!isConfirmPasswordVisible)
                ConfirmPasswordVisibleBox.Text = ConfirmPasswordBox.Password;
        }

        private void ConfirmPasswordVisibleBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (isConfirmPasswordVisible)
                ConfirmPasswordBox.Password = ConfirmPasswordVisibleBox.Text;

            ConfirmPasswordPlaceholder.Visibility = string.IsNullOrEmpty(ConfirmPasswordVisibleBox.Text)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}
