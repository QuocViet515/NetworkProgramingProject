using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
using Pingme.Helpers;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for ForgotPasswordPage.xaml
    /// </summary>
    public partial class ForgotPasswordPage : Page
    {
        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/");
        public ForgotPasswordPage()
        {
            InitializeComponent();
        }

        private async void SendResetLink_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("Please enter your email.");
                return;
            }

            var payload = new
            {
                requestType = "PASSWORD_RESET",
                email = email
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using (var client = new HttpClient())
            {
                try
                {
                    var response = await client.PostAsync(
                        $"https://identitytoolkit.googleapis.com/v1/accounts:sendOobCode?key={SessionManager.FirebaseApiKey}",
                        content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Reset link has been sent to your email.");
                    }
                    else
                    {
                        string errorMsg = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Failed to send reset link.\n" + errorMsg);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error occurred:\n" + ex.Message);
                }
            }
        }


        private void BackToLogin_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.NavigationService.Navigate(new LoginPage());
        }
    }
}
