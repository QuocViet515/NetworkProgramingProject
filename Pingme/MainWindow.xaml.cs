using System;
using System.Windows;
using Microsoft.Win32;
using Pingme.Services;
using Pingme.Views.Pages;

namespace Pingme
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
        }
        private FirebaseNotificationService _notificationService;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("User chưa đăng nhập.");
                return;
            }

            string currentUserId = AuthService.CurrentUser.id;

            _notificationService = new FirebaseNotificationService();
            _notificationService.StartListeningForCalls(currentUserId);
        }


    }
}
