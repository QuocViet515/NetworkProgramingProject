using Firebase.Database;
using Firebase.Database.Query;
using Firebase.Storage;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32; // Hộp thoại chọn file
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.WebRequestMethods;
using Pingme.Views.Pages;


namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for ProfileInfoControl.xaml
    /// </summary>
    public partial class ProfileInfoControl : UserControl
    {

        private readonly FirebaseClient _firebase = new FirebaseClient(
            "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
            new FirebaseOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken)
            });

        private User currentUser;
        public ProfileInfoControl()
        {
            InitializeComponent();
            LoadUserData();
        }
        private async void LoadUserData()
        {
            try
            {
                await SessionManager.EnsureValidTokenAsync(); // Kiểm tra và làm mới token nếu cần

                string uid = SessionManager.UID;
                if (string.IsNullOrEmpty(uid))
                {
                    MessageBox.Show("Không tìm thấy UID đăng nhập.");
                    return;
                }

                var userNode = await _firebase
                    .Child("users")
                    .Child(uid)
                    .OnceSingleAsync<User>();

                if (userNode != null)
                {
                    currentUser = userNode;

                    DisplayUserName.Text = currentUser.UserName;
                    UserNameText.Text = currentUser.UserName;
                    FullNameText.Text = currentUser.FullName;
                    EmailText.Text = currentUser.Email;
                    PhoneText.Text = currentUser.Phone;
                    BirthdayText.Text = currentUser.Birthday.ToString("dd/MM/yyyy");
                    AddressText.Text = currentUser.Address;
                    if (!string.IsNullOrEmpty(currentUser.AvatarUrl))
                    {
                        AvatarBrush.ImageSource = new BitmapImage(new Uri(currentUser.AvatarUrl));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi tải thông tin người dùng:\n" + ex.Message);
            }
        }
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            // Hiện các TextBox để chỉnh sửa
            SetEditMode(true);
        }
        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SessionManager.EnsureValidTokenAsync(); // Kiểm tra và làm mới token nếu cần
                // Cập nhật giá trị
                currentUser.FullName = FullNameEdit.Text;
                currentUser.Email = EmailEdit.Text;
                currentUser.Phone = PhoneEdit.Text;
                currentUser.Birthday = DateTime.ParseExact(BirthdayEdit.Text, "dd/MM/yyyy", null);
                currentUser.Address = AddressEdit.Text;

                // Ghi lên Firebase
                await _firebase
                    .Child("users")
                    .Child(SessionManager.UID)
                    .PutAsync(currentUser);

                // Hiện lại TextBlock
                LoadUserData();
                SetEditMode(false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi lưu thông tin:\n" + ex.Message);
            }
        }
        private async void ChangeAvatar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await SessionManager.EnsureValidTokenAsync(); // Kiểm tra và làm mới token nếu cần
                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Image files (*.jpg;*.png)|*.jpg;*.png"
                };

                if (dialog.ShowDialog() == true)
                {
                    string localFilePath = dialog.FileName;
                    string uid = SessionManager.UID;
                    string fileName = $"{uid}_{System.IO.Path.GetFileName(localFilePath)}";

                    using (var stream = System.IO.File.Open(localFilePath, FileMode.Open))
                    {
                        // Upload lên Firebase Storage
                        var uploadTask = new FirebaseStorage("pingmeapp-1691-1703-1784.firebasestorage.app",
                                            new FirebaseStorageOptions
                                            {
                                                AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken), // ✅ dùng token
                                                ThrowOnCancel = true
                                            })
                                            .Child("avatars")
                                            .Child(fileName)
                                            .PutAsync(stream);

                        string downloadUrl = await uploadTask;

                        // Kiểm tra URL trả về
                        if (string.IsNullOrWhiteSpace(downloadUrl))
                        {
                            MessageBox.Show("Lỗi: Không lấy được đường dẫn ảnh tải lên.");
                            return;
                        }

                        // Cập nhật Firebase
                        currentUser.AvatarUrl = downloadUrl;
                        await _firebase.Child("users").Child(uid).PutAsync(currentUser);

                        AvatarBrush.ImageSource = new BitmapImage(new Uri(downloadUrl));
                        MessageBox.Show("Cập nhật ảnh đại diện thành công!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đổi ảnh:\n" + ex.Message + "\n" + ex.InnerException?.Message);
            }
        }

        private void SetEditMode(bool isEdit)
        {
            // Toggle TextBlock vs TextBox
            FullNameText.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            FullNameEdit.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            FullNameEdit.Text = currentUser.FullName;

            EmailText.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            EmailEdit.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            EmailEdit.Text = currentUser.Email;

            PhoneText.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            PhoneEdit.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            PhoneEdit.Text = currentUser.Phone;

            BirthdayText.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            BirthdayEdit.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            BirthdayEdit.Text = currentUser.Birthday.ToString("dd/MM/yyyy");

            AddressText.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            AddressEdit.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            AddressEdit.Text = currentUser.Address;

            // Toggle nút
            EditButton.Visibility = isEdit ? Visibility.Collapsed : Visibility.Visible;
            SaveButton.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
            ChangeAvatar.Visibility = isEdit ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
