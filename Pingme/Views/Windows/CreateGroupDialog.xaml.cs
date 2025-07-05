using Pingme.Helpers;
using Pingme.Models;
using Pingme.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System.IO;
using Pingme.Views.Windows;
using Firebase.Storage;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;


namespace Pingme.Views.Dialogs
{
    public partial class CreateGroupDialog : Window
    {
        private readonly FirebaseService _firebase = new FirebaseService();
        public string GroupName => GroupNameBox.Text.Trim();
        private string avatarDownloadUrl = null;
        public string AvatarUrl => avatarDownloadUrl;
        public List<string> SelectedUserIds { get; private set; } = new List<string>();

        public CreateGroupDialog()
        {
            InitializeComponent();
            LoadFriends();
        }
        private async void ChooseAvatar_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "Image files (*.jpg;*.png)|*.jpg;*.png"
            };

            if (dialog.ShowDialog() == true)
            {
                var cropWindow = new CropImageWindow(dialog.FileName);
                var cropResult = cropWindow.ShowDialog();
                if (cropResult == true && cropWindow.CroppedResult != null)
                {
                    string tempPath = System.IO.Path.GetTempFileName(); // <-- chỉ khai báo ở đây

                    using (var fs = new FileStream(tempPath, FileMode.Create))
                    {
                        var encoder = new PngBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(cropWindow.CroppedResult));
                        encoder.Save(fs);
                    }

                    await SessionManager.EnsureValidTokenAsync(); // Đảm bảo token hợp lệ

                    string fileName = $"{SessionManager.UID}_{DateTime.UtcNow.Ticks}.png";

                    using (var stream = System.IO.File.Open(tempPath, FileMode.Open))
                    {
                        try
                        {
                            var uploadTask = new FirebaseStorage("pingmeapp-1691-1703-1784.firebasestorage.app",
                                new FirebaseStorageOptions
                                {
                                    AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken),
                                    ThrowOnCancel = true
                                })
                                .Child("groupAvatars")
                                .Child(fileName)
                                .PutAsync(stream);

                            avatarDownloadUrl = await uploadTask;
                            AvatarPreview.ImageSource = new BitmapImage(new Uri(avatarDownloadUrl));
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi tải ảnh lên Firebase:\n" + ex.Message);
                        }
                    }
                }

            }
        }

        private async void LoadFriends()
        {
            var allFriends = await _firebase.GetAllFriendsAsync();
            var myId = SessionManager.UID;
            var friendIds = allFriends
                .Where(f => f.Status == "accept" && (f.User1 == myId || f.User2 == myId))
                .Select(f => f.User1 == myId ? f.User2 : f.User1)
                .Distinct()
                .ToList();

            var allUsers = await _firebase.GetAllUsersAsync();
            var friendUsers = allUsers.Where(u => friendIds.Contains(u.Id)).ToList();

            foreach (var user in friendUsers)
            {
                var cb = new CheckBox
                {
                    Content = $"{user.FullName} (@{user.UserName})",
                    Tag = user.Id,
                    Margin = new Thickness(5)
                };
                FriendList.Items.Add(cb);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(GroupName))
            {
                MessageBox.Show("Vui lòng nhập tên nhóm.", "Thiếu thông tin", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SelectedUserIds = FriendList.Items
                .OfType<CheckBox>()
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag.ToString())
                .ToList();

            DialogResult = true;
            Close();
        }
    }
}
