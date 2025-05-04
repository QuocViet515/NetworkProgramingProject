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
using Pingme.Views.Controls;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        public ProfilePage()
        {
            InitializeComponent();
        }
        private void GoToChat_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new ChatPage());
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new SettingControl(); // Mặc định là setting
        }
        private void BtnSetting_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new SettingControl();
        }

        private void BtnNotification_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new NotificationControl();
        }

        private void BtnFriendGroup_Click(object sender, RoutedEventArgs e)
        {
            LeftPanelContent.Content = new MyFriendAndGroupControl();
        }
        private void BtnSearch_Click(object sender, RoutedEventArgs e) 
        {

        }
    }
}
