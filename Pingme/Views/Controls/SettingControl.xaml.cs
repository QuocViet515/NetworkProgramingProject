using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Navigation;
using Pingme.Helpers;
using Pingme.ViewModels;
using Pingme.Views.Pages;

namespace Pingme.Views.Controls
{
    public partial class SettingControl : UserControl
    {
        private SettingViewModel ViewModel => (SettingViewModel)DataContext;
        public SettingControl()
        {
            InitializeComponent();
            this.DataContext = new SettingViewModel();
        }
        private void StatusToggle_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.StatusText == "Online")
            {
                ViewModel.StatusText = "Offline";
                StatusToggle.Background = Brushes.LightGray;
                StatusToggle.Content = "Offline";
            }
            else
            {
                ViewModel.StatusText = "Online";
                StatusToggle.Background = Brushes.LightGreen;
                StatusToggle.Content = "Online";
            }
        }
        private void Sound_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn && int.TryParse(btn.Tag?.ToString(), out int index))
            {
                ViewModel.SoundMode = index;
            }
        }

    }
}
