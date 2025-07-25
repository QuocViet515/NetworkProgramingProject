﻿using Pingme.Models;
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
using Pingme.Services;
using System.Windows.Media.Animation;
using System.Collections.ObjectModel;

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
            GroupInforPanel.MessageClicked += OnMessageClicked;
            GroupInforPanel.IsGroupChat = false;  // neu la nhom la true con ca nhan la false
        }

        public ChatPage(string chatId, bool isGroup)
        {
            InitializeComponent();

            GroupInforPanel.IsGroupChat = isGroup;
            GroupInforPanel.SelectedChatId = chatId;
            GroupInforPanel.UpdateUIForChatType();

            ChatList.ChatSelected += OnChatSelected;

            _ = ChatDetail.LoadChat(chatId, isGroup);
        }

        private async void OnChatSelected(object sender, object chatItem)
        {
            if (chatItem is Chat chat)
            {
                GroupInforPanel.IsGroupChat = false;
                GroupInforPanel.SelectedChatId = chat.Id;
                GroupInforPanel.UpdateUIForChatType();
                //FirebaseService.curentChatId = ChatDetailControl.current
                await ChatDetail.LoadChat(chat.Id, false);
                GroupInforPanel.AllMessages = new ObservableCollection<Message>(ChatDetail.CurrentMessages);

                GroupInforPanel.PeerUser = ChatDetail.other;
            }
            else if (chatItem is ChatGroup group)
            {
                GroupInforPanel.IsGroupChat = true;
                GroupInforPanel.SelectedChatId = group.Id;
                GroupInforPanel.UpdateUIForChatType();

                await ChatDetail.LoadChat(group.Id, true);
                GroupInforPanel.AllMessages = new ObservableCollection<Message>(ChatDetail.CurrentMessages);
            }
        }

        private async void OnMessageClicked(Message msg)
        {
            var index = ChatDetail.CurrentMessages.FindIndex(m => m.Id == msg.Id);

            if (index >= 0 && index < ChatDetail.ChatPanelRef.Children.Count)
            {
                var target = ChatDetail.ChatPanelRef.Children[index] as FrameworkElement;
                target?.BringIntoView();

                var bg = new SolidColorBrush(Colors.Transparent);
                var anim = new ColorAnimation
                {
                    From = Colors.Yellow,
                    To = Colors.Transparent,
                    Duration = TimeSpan.FromSeconds(2)
                };

                if (target is Control ctrl)
                    ctrl.Background = bg;

                bg.BeginAnimation(SolidColorBrush.ColorProperty, anim);
            }
            else
            {
                MessageBox.Show("❌ Không tìm thấy tin nhắn trong khung chat!");
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
