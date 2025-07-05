using Firebase.Database;
using Firebase.Database.Query;
using Newtonsoft.Json;
using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using Pingme.ViewModels;
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
        private async Task<FirebaseAuthResponse> SignInWithEmailPassword(string email, string password)
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
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={SessionManager.FirebaseApiKey}",
                    content);

                if (response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<FirebaseAuthResponse>(body);
                }
                else
                {
                    //string error = await response.Content.ReadAsStringAsync();
                    throw new Exception("Login failed please login again later !!!");
                }
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
            string password = PasswordBox.Password;
            string email = await ResolveEmailIfUsername(input);

            if (string.IsNullOrEmpty(email))
            {
                UsernameTextBoxError.Text = "Account does not exist!";
                UsernameTextBoxError.Visibility = Visibility.Visible;
                return;
            }

            try
            {
                UsernameTextBoxError.Visibility = Visibility.Collapsed;
                PasswordTextBoxError.Visibility = Visibility.Collapsed;

                var auth = await SignInWithEmailPassword(email, password);

                SessionManager.UID = auth.localId;
                SessionManager.IdToken = auth.idToken;
                SessionManager.RefreshToken = auth.refreshToken;
                int expiresInSeconds = int.Parse(auth.expiresIn);
                SessionManager.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSeconds);

                var firebase = new FirebaseClient(
                    "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                    new FirebaseOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken)
                    });

                var user = await firebase.Child("users").Child(SessionManager.UID).OnceSingleAsync<User>();
                if (user == null)
                {
                    MessageBox.Show("User information not found.");
                    return;
                }

                var rsa = new RSAService();
                try
                {
                    rsa.EnsureUserKeyExists(SessionManager.UID);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("❌ Lỗi khi tạo khóa RSA: " + ex.Message);
                    return;
                }

                // Upload public key nếu chưa có
                if (string.IsNullOrWhiteSpace(user.PublicKey))
                {
                    string pubKeyPath = KeyManager.GetPublicKeyPath(SessionManager.UID);
                    if (!System.IO.File.Exists(pubKeyPath))
                    {
                        MessageBox.Show("❌ Không tìm thấy public key. Vui lòng đăng nhập lại.");
                        return;
                    }
                    string pubKeyXml = System.IO.File.ReadAllText(pubKeyPath);
                    user.PublicKey = pubKeyXml;
                }


                // Ensure user is in employees node
                //var employeeRef = await firebase.Child("employees").Child(SessionManager.UID).OnceSingleAsync<Employee>();
                //if (employeeRef == null)
                //{
                //    await firebase.Child("employees").Child(SessionManager.UID).PutAsync(new Employee
                //    {
                //        Position = "Engineer", // bạn có thể tùy chỉnh vai trò
                //        AddedAt = DateTime.UtcNow.ToString("o")
                //    });
                //    Console.WriteLine("✅ Added current user to employees node");
                //}

                // Thêm user hiện tại vào node employees nếu chưa có
                await firebase.Child("employees").Child(SessionManager.UID).PutAsync(new Pingme.ViewModels.EmployeeModel
                {
                    UserId = SessionManager.UID,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = "Engineer",
                    Department = "IT",
                    JoinDate = DateTime.UtcNow,
                    IsActive = true
                });

                // Lấy danh sách nhân viên từ Firebase (node employees)
                //var employees = await firebase.Child("employees").OnceAsync<EmployeeModel>();
                var employees = await firebase.Child("employees").OnceAsync<Pingme.ViewModels.EmployeeModel>();

                var employeeList = employees.Select(emp => emp.Key).ToList(); // Giả sử Key = userId

                // Nếu chưa có chữ ký public key → ký và lưu
                if (string.IsNullOrWhiteSpace(user.PublicKeySignature))
                {
                    try
                    {
                        var signer = new IdentifyPublicKeyService("C:\\Apache24\\conf\\ssl\\ec-private-key.pem", employeeList);
                        var signResult = signer.SignPublicKey(SessionManager.UID, user.PublicKey);

                        if (signResult.Success)
                        {
                            user.PublicKeySignature = signResult.SignatureBase64;
                            Console.WriteLine("✅ Public key signed successfully.");
                        }
                        else
                        {
                            MessageBox.Show("❌ Lỗi khi ký public key: " + signResult.Error);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ Lỗi khi xử lý domain signature: " + ex.Message);
                        return;
                    }
                }

                // Lưu user (có public key + signature) vào Firebase
                await firebase.Child("users").Child(SessionManager.UID).PutAsync(user);
                Console.WriteLine("✅ Uploaded public key + signature to Firebase");

                SessionManager.CurrentUser = user;

                MessageBox.Show($"Hi {user.FullName}!");
                var firebaseService = new FirebaseService();
                var currentUserdb = await firebaseService.GetUserByUsernameAsync(SessionManager.CurrentUser.UserName);
                var firebaseNotificationService = new FirebaseNotificationService();
                firebaseNotificationService.StartListeningForCalls(currentUserdb.Id);

                this.NavigationService.Navigate(new ProfilePage());

                if (RememberMeCheckBox.IsChecked == true)
                {
                    Properties.Settings.Default.RememberMe = true;
                    Properties.Settings.Default.SavedUsername = input;
                    Properties.Settings.Default.SavedPassword = password;
                }
                else
                {
                    Properties.Settings.Default.RememberMe = false;
                    Properties.Settings.Default.SavedUsername = "";
                    Properties.Settings.Default.SavedPassword = "";
                }
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                PasswordTextBoxError.Text = "Please enter correct password";
                PasswordTextBoxError.Visibility = Visibility.Visible;
            }
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
