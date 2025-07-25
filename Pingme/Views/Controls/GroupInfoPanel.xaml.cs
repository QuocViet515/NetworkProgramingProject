﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Pingme.Helpers;
using Pingme.Views.Windows;
using Pingme.Models;
using Pingme.Services;
using Pingme.ViewModels;
using System.Collections.ObjectModel;
using Pingme.Views.Dialogs;

namespace Pingme.Views.Controls
{
    public partial class GroupInfoPanel : UserControl
    {

        private AgoraVideoService _videoService;
        private bool isMicOn = true;
        private bool isCamOn = true;
        private CallWindow callWindow;

        public ObservableCollection<Message> AllMessages { get; set; } = new ObservableCollection<Message>();
        public event Action<Message> MessageClicked;
        public bool IsGroupChat { get; set; } = true;
        public string SelectedChatId { get; set; } // cần set từ ChatPage
        public User PeerUser { get; set; }  // chính là "other"

        public GroupInfoPanel()
        {
            InitializeComponent();
        }

        private async void CallButton_Click(object sender, RoutedEventArgs e)
        {
            if (PeerUser == null)
            {
                MessageBox.Show("❗ Chưa chọn người để gọi.", "Thông báo");
                return;
            }

            var firebaseService = new FirebaseService();
            var peerUserFromDb = await firebaseService.GetUserByUsernameAsync(PeerUser.UserName);
            if (peerUserFromDb == null)
            {
                MessageBox.Show("❌ Không tìm thấy người dùng.");
                return;
            }

            var currentUser = SessionManager.CurrentUser;
            var currentUserDb = await firebaseService.GetUserByUsernameAsync(currentUser.UserName);

            var firebase = new FirebaseNotificationService();
            var (request, pushId) = await firebase.SendCallRequest(currentUserDb.Id, peerUserFromDb.Id, "audio");

            if (request == null || string.IsNullOrEmpty(pushId))
            {
                MessageBox.Show("❌ Gửi cuộc gọi thất bại.");
                return;
            }

            request.PushId = pushId;
            request.ChatId = SelectedChatId;     // ✅ Gán đúng ChatId đang mở
            request.IsGroup = IsGroupChat;       // ✅ Gán kiểu nhóm/hay 1-1
            //MessageBox.Show("pushId: " + request.PushId);
            await firebaseService.SendCallStatusMessageAsync(
                request.FromUserId,
                request.ToUserId,
                request.PushId,
                "waiting",
                DateTime.UtcNow
            );

            var waitingWindow = new WaitingCallWindow(request);
            waitingWindow.Show();
        }




        private async void VideoCallButton_Click(object sender, RoutedEventArgs e)
        {
            if (PeerUser == null)
            {
                MessageBox.Show("❗ Chưa chọn người để gọi.", "Thông báo");
                return;
            }

            var firebaseService = new FirebaseService();
            var peerUserFromDb = await firebaseService.GetUserByUsernameAsync(PeerUser.UserName);
            if (peerUserFromDb == null)
            {
                MessageBox.Show("❌ Không tìm thấy người dùng.");
                return;
            }

            var currentUser = SessionManager.CurrentUser;
            var currentUserDb = await firebaseService.GetUserByUsernameAsync(currentUser.UserName);

            var firebase = new FirebaseNotificationService();
            var (request, pushId) = await firebase.SendCallRequest(currentUserDb.Id, peerUserFromDb.Id, "video");

            if (request == null || string.IsNullOrEmpty(pushId))
            {
                MessageBox.Show("❌ Gửi cuộc gọi thất bại.");
                return;
            }

            request.PushId = pushId;
            request.ChatId = SelectedChatId;     // ✅ Gán đúng ChatId đang mở
            request.IsGroup = IsGroupChat;       // ✅ Gán kiểu nhóm/hay 1-1
            //MessageBox.Show ("pushId: " + pushId);
            await firebaseService.SendCallStatusMessageAsync(
                request.FromUserId,
                request.ToUserId,
                request.PushId,
                "waiting",
                DateTime.UtcNow
            );

            var waitingWindow = new WaitingCallWindow(request);
            waitingWindow.Show();
        }

        private void UserInfoButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsGroupChat && !string.IsNullOrEmpty(SelectedChatId))
            {
                SetupUserProfile(SelectedChatId);
            }
        }

        private void PinMessageButton_Click(object sender, RoutedEventArgs e)
        {
            InfoContent.Content = new TextBlock
            {
                Text = "📌 Các tin nhắn đã ghim sẽ hiển thị ở đây.",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(10)
            };
        }

        private void MemberGroupButton_Click(object sender, RoutedEventArgs e)
        {
            SetupGroupInfo();
        }

        public void SetupUserProfile(string chatId)
        {
            if (!SessionManager.CurrentChats.TryGetValue(chatId, out var chat))
                return;

            string currentUserId = SessionManager.UID;
            string otherUserId = chat.User1 == currentUserId ? chat.User2 : chat.User1;

            if (!SessionManager.CurrentUserMap.TryGetValue(otherUserId, out var user))
                return;

            InfoContent.Content = new UserProfileCard
            {
                FullName = user.FullName,
                UserName = user.UserName,
                Email = user.Email,
                Phone = user.Phone,
                Birthday = user.Birthday,
                Address = user.Address,
                Status = user.Status
            };
        }

        private void SetupGroupInfo()
        {
            var stack = new StackPanel();

            //stack.Children.Add(new Border
            //{
            //    Background = Brushes.LightGray,
            //    CornerRadius = new CornerRadius(10),
            //    Padding = new Thickness(10),
            //    Margin = new Thickness(0, 0, 0, 10),
            //    Child = new TextBlock
            //    {
            //        Text = "Thành viên",
            //        FontWeight = FontWeights.Bold,
            //        FontSize = 14,
            //        HorizontalAlignment = HorizontalAlignment.Center
            //    }
            //});
            stack.Children.Add(new Border
            {
                Background = Brushes.LightGray,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 10),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "Thành viên",
                            FontWeight = FontWeights.Bold,
                            FontSize = 14,
                            VerticalAlignment = VerticalAlignment.Center
                        },
                        //new Button
                        //{
                        //    Content = new TextBlock
                        //    {
                        //        Text = "⋮",
                        //        FontSize = 18,
                        //        Foreground = Brushes.Black,
                        //        HorizontalAlignment = HorizontalAlignment.Center,
                        //        VerticalAlignment = VerticalAlignment.Center
                        //    },
                        //    Width = 30,
                        //    Height = 30,
                        //    Background = Brushes.Transparent,
                        //    BorderBrush = Brushes.Gray,
                        //    BorderThickness = new Thickness(1),
                        //    Cursor = System.Windows.Input.Cursors.Hand,
                        //    Margin = new Thickness(10, 0, 0, 0),
                        //    Padding = new Thickness(0),
                        //    Tag = "MenuButton",
                        //    HorizontalAlignment = HorizontalAlignment.Center,
                        //    VerticalAlignment = VerticalAlignment.Center,
                        //    Style = (Style)Application.Current.FindResource("RoundedIconButtonStyle")
                        //}
                        new Button
                        {
                            Content = new TextBlock
                            {
                                Text = "⋮",
                                FontSize = 18,
                                Foreground = Brushes.Black,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                VerticalAlignment = VerticalAlignment.Center
                            },
                            Width = 30,
                            Height = 30,
                            Background = Brushes.White,
                            BorderBrush = Brushes.Gray,
                            BorderThickness = new Thickness(1),
                            Cursor = System.Windows.Input.Cursors.Hand,
                            Margin = new Thickness(10, 0, 0, 0),
                            Padding = new Thickness(0),
                            Tag = "MenuButton",
                            HorizontalAlignment = HorizontalAlignment.Center,
                            VerticalAlignment = VerticalAlignment.Center
                        }

                    }
                }
            });

            //var plusButton = ((stack.Children[0] as Border).Child as StackPanel).Children[1] as Button;
            //plusButton.Click += (s, e) =>
            //{
            //    var inviteWindow = new InviteFriendToGroupWindow(SelectedChatId);
            //    inviteWindow.ShowDialog();
            //};

            var menuButton = ((stack.Children[0] as Border).Child as StackPanel).Children[1] as Button;
            menuButton.Click += (s, e) =>
            {
                var contextMenu = new ContextMenu
                {
                    Background = Brushes.White,
                    BorderThickness = new Thickness(1),
                    BorderBrush = Brushes.LightGray,
                    Padding = new Thickness(8),
                    HasDropShadow = true
                };

                // ➕ Mời thành viên
                var inviteItem = CreateStyledMenuItem("➕ Mời thành viên", "#4CAF50", async () =>
                {
                    var inviteWindow = new InviteFriendToGroupWindow(SelectedChatId);
                    inviteWindow.ShowDialog();
                });
                contextMenu.Items.Add(inviteItem);

                // 🚪 Rời nhóm
                var leaveItem = CreateStyledMenuItem("🚪 Rời nhóm", "#f44336", async () =>
                {
                    var result = MessageBox.Show("Bạn có chắc muốn rời nhóm không?", "Xác nhận", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.Yes)
                    {
                        var firebase = new FirebaseService();
                        await firebase.RemoveUserFromGroupAsync(SelectedChatId, SessionManager.UID);
                        MessageBox.Show("✅ Bạn đã rời khỏi nhóm.");
                    }
                });
                contextMenu.Items.Add(leaveItem);

                // ❌ Xóa thành viên (chỉ admin)
                if (SessionManager.CurrentGroups.TryGetValue(SelectedChatId, out var currentGroup)
                    && currentGroup.Admin != null
                    && currentGroup.Admin.Contains(SessionManager.UID))
                {
                    var kickItem = CreateStyledMenuItem("❌ Xóa thành viên", "#2196F3", () =>
                    {
                        var kickDialog = new KickMemberDialog(SelectedChatId);
                        kickDialog.ShowDialog();
                    });
                    contextMenu.Items.Add(kickItem);
                }

                // Gán menu
                contextMenu.PlacementTarget = menuButton;
                contextMenu.IsOpen = true;
            };

            if (SessionManager.CurrentGroups.TryGetValue(SelectedChatId, out var group))
            {
                foreach (var memberId in group.Members)
                {
                    if (SessionManager.CurrentUserMap.TryGetValue(memberId, out var user))
                    {
                        stack.Children.Add(new GroupMember
                        {
                            ContactName = user.FullName ?? user.UserName,
                            AvatarPath = user.AvatarUrl ?? "/Assets/Icons/avatar-default.png"
                        });
                    }
                }
            }

            InfoContent.Content = stack;
        }

        public void UpdateUIForChatType()
        {
            // Reset trước
            UserInfoButton.Click -= UserInfoButton_Click;
            UserInfoButton.Click -= MemberGroupButton_Click;

            if (IsGroupChat)
            {
                UserInfoButton.Tag = "/Assets/Icons/contacticon.png";
                UserInfoButton.Click += MemberGroupButton_Click;
            }
            else
            {
                UserInfoButton.Tag = "/Assets/Icons/profileicon.png";
                UserInfoButton.Click += UserInfoButton_Click;
            }

            UserInfoButton.Background = new ImageBrush(new BitmapImage(new Uri((string)UserInfoButton.Tag, UriKind.RelativeOrAbsolute)));
        }

        public void ShowSearchPanel()
        {
            var searchPanel = new SearchMessagePanel
            {
                AllMessages = this.AllMessages
            };

            searchPanel.MessageClicked += (msg) =>
            {
                MessageClicked?.Invoke(msg); // Gửi tiếp ra ngoài
            };

            InfoContent.Content = searchPanel;
        }
        private MenuItem CreateStyledMenuItem(string text, string colorHex, Action action)
        {
            var item = new MenuItem
            {
                Header = text,
                Background = (SolidColorBrush)new BrushConverter().ConvertFromString(colorHex),
                Foreground = Brushes.White,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Padding = new Thickness(8, 4, 8, 4),
                Margin = new Thickness(0, 4, 0, 4),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            item.Click += (s, e) => action?.Invoke();
            return item;
        }
    }
}
