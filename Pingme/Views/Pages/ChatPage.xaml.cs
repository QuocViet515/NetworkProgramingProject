using Pingme.Models;
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
using Pingme.Views.Controls;

namespace Pingme.Views.Pages
{
    /// <summary>
    /// Interaction logic for ChatPage.xaml
    /// </summary>
    public partial class ChatPage : Page
    {
        public ChatPage()
        {
            InitializeComponent();

            ChatList.ChatSelected += OnChatSelected;

            GroupInforPanel.IsGroupChat = false;  // neu la nhom la true con ca nhan la false
        }
        private async void OnChatSelected(object sender, object chatItem)
        {
            if (chatItem is Chat chat)
            {
                GroupInforPanel.IsGroupChat = false;
                GroupInforPanel.SelectedChatId = chat.Id;
                GroupInforPanel.UpdateUIForChatType();

                await ChatDetail.LoadChat(chat.Id, false);

                GroupInforPanel.PeerUser = ChatDetail.other;
            }
            else if (chatItem is ChatGroup group)
            {
                GroupInforPanel.IsGroupChat = true;
                GroupInforPanel.SelectedChatId = group.Id;
                GroupInforPanel.UpdateUIForChatType();

                await ChatDetail.LoadChat(group.Id, true);
            }
        }


        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProfilePage("friendgroup"));
        }

        private void NotificationButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProfilePage("notification")); // Truyền chế độ "notification"
        }

        private void SettingButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProfilePage("setting")); // Truyền chế độ "setting"
        }

        private void ProfileButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new ProfilePage()); // Chế độ mặc định: thông tin cá nhân
        }
    }
}
