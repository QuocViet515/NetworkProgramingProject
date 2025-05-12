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
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var firebase = new FirebaseService();
            bool isConnected = await firebase.TestConnectionAsync();

            if (!isConnected)
                MessageBox.Show("Không thể kết nối Firebase", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }

    }
}
