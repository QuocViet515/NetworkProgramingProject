﻿using Pingme.Helpers;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Pingme.Views.Pages;
using System.Windows.Controls;
using System.Windows;
using Pingme;


namespace Pingme.ViewModels
{
    public class SettingViewModel : INotifyPropertyChanged
    {
        private bool _isDarkTheme;
        private bool _isNotificationOn;
        private string _statusText = "Online";
        private int _soundMode = 0; // 0: bell-on, 1: bell, 2: mute

        public bool IsDarkTheme
        {
            get => _isDarkTheme;
            set { _isDarkTheme = value; OnPropertyChanged(); }
        }

        public bool IsNotificationOn
        {
            get => _isNotificationOn;
            set { _isNotificationOn = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public int SoundMode
        {
            get => _soundMode;
            set { _soundMode = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand LogoutCommand { get; }

        public SettingViewModel()
        {
            SaveCommand = new RelayCommand(SaveSettings);
            LogoutCommand = new RelayCommand(Logout);
        }

        private void SaveSettings()
        {
            // TODO: Ghi cài đặt vào tệp cấu hình hoặc cloud
        }

        private void Logout()
        {
            // Xóa thông tin phiên người dùng
            SessionManager.Clear();

            // Truy cập MainWindow và điều hướng Frame
            if (Application.Current.MainWindow is MainWindow mainWindow)
            {
                mainWindow.MainFrame.Navigate(new LoginPage());
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
