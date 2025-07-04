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
using Pingme.Models;
using Pingme.Helpers;
using Firebase.Database;
using Firebase.Database.Query;
using System.Collections.ObjectModel;

namespace Pingme.Views.Controls
{
    /// <summary>
    /// Interaction logic for ChatTagController.xaml
    /// </summary>
    public partial class ChatTagController : UserControl
    {
        private readonly FirebaseClient _firebase;
        public ObservableCollection<ChatTag> ChatTags { get; set; } = new ObservableCollection<ChatTag>();
        public ChatTagController()
        {
            InitializeComponent();
            _firebase = new FirebaseClient(
                "https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions
                {
                    AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken)
                });

            DataContext = this;
            LoadChatTags();
        }

        private async void LoadChatTags()
        {
            try
            {
                await SessionManager.EnsureValidTokenAsync();

                string userId = SessionManager.UID;
                if (string.IsNullOrEmpty(userId))
                {
                    MessageBox.Show("Chưa đăng nhập.");
                    return;
                }

                var allTags = await _firebase.Child("chatTags").OnceAsync<ChatTag>();

                var userTags = allTags
                    .Where(t => t.Object.ChatIds != null && t.Object.ChatIds.Any()) // Lọc nếu muốn theo điều kiện khác thì sửa ở đây
                    .Select(t =>
                    {
                        var tag = t.Object;
                        tag.Id = t.Key;
                        return tag;
                    });

                ChatTags.Clear();
                foreach (var tag in userTags)
                {
                    ChatTags.Add(tag);
                }

                ChatTagList.ItemsSource = ChatTags;
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Lỗi tải tag: " + ex.Message);
            }
        }
        private void ChatTagButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is ChatTag tag)
            {
                // Xử lý khi chọn tag (ví dụ load chat nhóm tag này)
                MessageBox.Show($"Chọn tag: {tag.Name}");
            }
        }
        private void AddChatTag_Click(object sender, RoutedEventArgs e)
        {
            // Hiện popup hoặc trang tạo tag mới
            MessageBox.Show("Thêm Chat Tag mới");
        }
    }
}
