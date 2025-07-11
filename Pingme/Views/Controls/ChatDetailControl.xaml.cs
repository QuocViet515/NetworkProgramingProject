using Pingme.Models;
using Pingme.Helpers;
using Pingme.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Firebase.Database;
using System.Threading.Tasks;
using Firebase.Database.Query;
using Pingme.Services;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Pingme.Views.Pages;
using System.Windows.Media;

namespace Pingme.Views.Controls
{
    public partial class ChatDetailControl : UserControl
    {
        public bool isGroup { get; set; } = true;
        private string currentChatId;
        private FirebaseClient firebase;
        private readonly AESService _aesService = new AESService();
        private readonly RSAService _rsaService = new RSAService();

        public User other { get; private set; }
        public string otherId => other?.Id;

        public ChatDetailControl()
        {
            InitializeComponent();
        }

        public List<Message> CurrentMessages { get; private set; } = new List<Message>();
        public async Task LoadChat(string chatId, bool isGroup)
        {
            ChatPanel.Children.Clear();
            this.isGroup = isGroup;
            currentChatId = chatId;

            firebase = new FirebaseClient("https://pingmeapp-1691-1703-1784-default-rtdb.asia-southeast1.firebasedatabase.app/",
                new FirebaseOptions { AuthTokenAsyncFactory = () => Task.FromResult(SessionManager.IdToken) });

            // Load chat/group info
            if (isGroup)
            {
                var group = await firebase.Child("chatGroups").Child(chatId).OnceSingleAsync<ChatGroup>();
                ChatHeader.ContactName = group.Name ?? "Nhóm Chat";
                ChatHeader.AvatarPath = group.AvatarUrl ?? "/Assets/Icons/avatar-default.png";
            }
            else
            {
                var chat = await firebase.Child("chats").Child(chatId).OnceSingleAsync<Chat>();
                string resolvedId = chat.User1 == SessionManager.UID ? chat.User2 : chat.User1;

                // ✅ Gán peer user vào property `other`
                other = await firebase.Child("users").Child(resolvedId).OnceSingleAsync<User>();

                ChatHeader.ContactName = other.FullName ?? other.UserName ?? "Người dùng";
                ChatHeader.AvatarPath = other.AvatarUrl ?? "/Assets/Icons/avatar-default.png";
            }

            await LoadMessagesAsync();
        }

        private async Task LoadMessagesAsync()
        {

            var messages = await firebase.Child("messages").OnceAsync<Message>();
            //var messages = await firebase
            //    .Child("messages")
            //    .Child(currentChatId)
            //    .OnceAsync<Message>();
            var sortedMessages = messages
                .Select(m => m.Object)
                .Where(m => m.ChatId == currentChatId)
                .OrderBy(m => m.SentAt)
                .ToList();
            CurrentMessages = sortedMessages;
            //foreach (var item in messages)
            foreach (var msg in sortedMessages)
            {
                //var msg = item.Object;

                //if (msg.ChatId != currentChatId) continue;

                if (msg.Type == "file" && !string.IsNullOrEmpty(msg.FileId))
                {
                    // Tạo nút tải file
                    var button = new Button
                    {
                        Content = $"📥 Tải xuống: {msg.FileName}",
                        Tag = msg.FileId,
                        Margin = new Thickness(5),
                        Padding = new Thickness(10)
                    };
                    button.Click += async (s, e) =>
                    {
                        var dialog = new SaveFileDialog { FileName = msg.FileName };
                        if (dialog.ShowDialog() == true)
                        {
                            try
                            {
                                string privateKeyPath = KeyManager.GetPrivateKeyPath(SessionManager.UID);
                                await new FirebaseFileService().DownloadAndDecryptFileAsync(msg.FileId, privateKeyPath, dialog.FileName);
                                MessageBox.Show("✅ Tải và giải mã thành công!");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("❌ Lỗi: " + ex.Message);
                            }
                        }
                    };

                    ChatPanel.Children.Add(button);
                }
                else
                {
                    string decryptedText = "[Không thể giải mã]";
                    if (msg.SessionKeyEncrypted != null &&
                        msg.SessionKeyEncrypted.TryGetValue(SessionManager.UID, out string encryptedKey))
                    {
                        string aesKey = _rsaService.Decrypt(encryptedKey, SessionManager.UID);
                        if (!string.IsNullOrEmpty(aesKey))
                        {
                            //var (plain, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.Hash);
                            var (plain, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.IV, msg.Tag, msg.Hash);
                            decryptedText = isValid ? plain : "[Sai hash – nội dung đã bị thay đổi]";
                            msg.Content = decryptedText;
                            msg.Plaintext = decryptedText;
                            msg.FromSelf = msg.SenderId == SessionManager.UID;
                            msg.SenderName = msg.FromSelf ? "Bạn" : await GetUserNameById(msg.SenderId);
                        }
                    }

                    if (msg.SenderId == SessionManager.UID)
                        ChatPanel.Children.Add(new OutgoingMessageControl(decryptedText));
                    else
                        ChatPanel.Children.Add(new IncomingMessageControl(decryptedText));
                }

                await Task.Delay(100);
                ScrollToBottom();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var plainText = MessageInput.Text.Trim();

            if (string.IsNullOrEmpty(plainText)) return;

            if (string.IsNullOrEmpty(currentChatId))
            {
                MessageBox.Show("⚠️ Chưa chọn cuộc trò chuyện nào.");
                return;
            }

            if (string.IsNullOrEmpty(plainText) || string.Equals(plainText, "", StringComparison.OrdinalIgnoreCase))
                return;

            if (SessionManager.CurrentUser == null || string.IsNullOrEmpty(SessionManager.CurrentUser.PublicKey))
            {
                MessageBox.Show("⚠️ Không tìm thấy khoá công khai của bạn.");
                return;
            }

            var chat = await firebase.Child(isGroup ? "chatGroups" : "chats").Child(currentChatId).OnceSingleAsync<Chat>();
            string receiverId = isGroup ? null : (chat.User1 == SessionManager.UID ? chat.User2 : chat.User1);

            if (!isGroup && string.IsNullOrEmpty(receiverId))
            {
                MessageBox.Show("⚠️ Không tìm thấy người nhận.");
                return;
            }

            // 1. Tạo AES key
            string aesKey = _aesService.GenerateAesKey();

            // 2. Mã hóa tin nhắn
            var (cipher, iv, tag, hash) = _aesService.EncryptMessageWithHash(plainText, aesKey);

            // 3. Mã hóa AES key với public key người nhận
            Dictionary<string, string> encryptedKeys = new Dictionary<string, string>();
            if (!isGroup)
            {
                //var receiverPublicKey = await new FirebaseService().GetPublicKeyAsync(receiverId);
                //if (string.IsNullOrEmpty(receiverPublicKey))
                //{
                //    MessageBox.Show("❌ Không tìm thấy khoá công khai của người nhận.");
                //    return;
                //}

                ////encryptedKeys[receiverId] = _rsaService.Encrypt(aesKey, receiverPublicKey);
                //encryptedKeys[receiverId] = _rsaService.EncryptWithXml(aesKey, receiverPublicKey);
                var firebaseService = new FirebaseService();

                // Lấy public key và chữ ký của người nhận
                string receiverPublicKey = await firebaseService.GetPublicKeyAsync(receiverId);
                string receiverSignature = await firebaseService.GetPublicKeySignatureAsync(receiverId);

                if (string.IsNullOrEmpty(receiverPublicKey) || string.IsNullOrEmpty(receiverSignature))
                {
                    MessageBox.Show("❌ Không thể xác thực khóa công khai của người nhận.");
                    return;
                }

                // Tải public key của domain (CA)
                string domainPubKeyPath = "C:\\Apache24\\conf\\ssl\\ec-public-key.pem";
                string domainPem = System.IO.File.ReadAllText(domainPubKeyPath);

                // Xác minh chữ ký ECDSA
                var verifier = new ECDsaVerifier();
                bool isValid = verifier.Verify(receiverPublicKey, receiverSignature, domainPem);
                // Normalize PublicKey giống với lúc ký
                //string normalizedPubKey = receiverPublicKey.Replace("\r", "").Replace("\n", "").Replace("  ", "").Trim();

                //bool isValid = verifier.Verify(normalizedPubKey, receiverSignature, domainPem);

                if (!isValid)
                {
                    MessageBox.Show("⚠️ Public key của người nhận không hợp lệ hoặc đã bị giả mạo.");
                    return;
                }

                // Mã hóa AES key bằng public key người nhận
                encryptedKeys[receiverId] = _rsaService.EncryptWithXml(aesKey, receiverPublicKey);
            }
            encryptedKeys[SessionManager.UID] = _rsaService.EncryptWithXml(aesKey, SessionManager.CurrentUser.PublicKey);

            var msg = new Message
            {
                Id = Guid.NewGuid().ToString(),
                ChatId = currentChatId,
                SenderId = SessionManager.UID,
                ReceiverId = receiverId,
                Ciphertext = cipher,
                IV = iv,
                Tag = tag,  // THÊM tag vào message
                Hash = hash,
                IsGroup = isGroup,
                Type = "text",
                SentAt = DateTime.UtcNow,
                SessionKeyEncrypted = encryptedKeys
            };


            await firebase.Child("messages").Child(msg.Id).PutAsync(msg);
            ChatPanel.Children.Add(new OutgoingMessageControl(plainText));
            MessageInput.Text = "";
            ScrollToBottom();
        }

        private async void AttachFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                string filePath = openFileDialog.FileName;
                string fileName = Path.GetFileName(filePath);
                MessageBox.Show($"📁 Đang gửi file: {fileName}");

                string senderId = SessionManager.UID;
                string receiverId;

                if (isGroup)
                {
                    MessageBox.Show("❗ Chưa hỗ trợ gửi file trong nhóm.");
                    return;
                }

                var chat = await firebase.Child("chats").Child(currentChatId).OnceSingleAsync<Chat>();
                receiverId = chat.User1 == senderId ? chat.User2 : chat.User1;

                if (string.IsNullOrEmpty(receiverId))
                {
                    MessageBox.Show("❌ Không tìm thấy người nhận!");
                    return;
                }

                try
                {
                    var firebaseService = new FirebaseService();
                    string receiverPublicKeyXml = await firebaseService.GetPublicKeyAsync(receiverId);

                    var fileService = new FirebaseFileService();
                    //await fileService.UploadEncryptedFileAsync(filePath, receiverPublicKeyXml, senderId, receiverId);
                    MessageBox.Show($"✅ File \"{fileName}\" đã gửi thành công!");
                    var fileId = await fileService.UploadEncryptedFileAsync(filePath, receiverPublicKeyXml, senderId, receiverId);                    // hoặc bạn lấy ID thực từ FirebaseStorage nếu có
                    var msg = new Message
                    {
                        Id = Guid.NewGuid().ToString(),
                        ChatId = currentChatId,
                        SenderId = senderId,
                        ReceiverId = receiverId,
                        IsGroup = isGroup,
                        Type = "file",
                        FileId = fileId,
                        FileName = fileName,
                        SentAt = DateTime.UtcNow
                    };
                    await firebase.Child("messages").Child(msg.Id).PutAsync(msg);

                    // Hiển thị luôn file trong giao diện
                    var button = new Button
                    {
                        Content = $"📥 Tải xuống: {fileName}",
                        Tag = fileId,
                        Margin = new Thickness(5),
                        Padding = new Thickness(10)
                    };
                    button.Click += async (s, args) =>
                    {
                        var dialog = new SaveFileDialog { FileName = fileName };
                        if (dialog.ShowDialog() == true)
                        {
                            try
                            {
                                string privateKeyPath = KeyManager.GetPrivateKeyPath(senderId);
                                await new FirebaseFileService().DownloadAndDecryptFileAsync(fileId, privateKeyPath, dialog.FileName);
                                MessageBox.Show("✅ Tải và giải mã thành công!");
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("❌ Lỗi: " + ex.Message);
                            }
                        }
                    };
                    ChatPanel.Children.Add(button);
                    ScrollToBottom();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"❌ Lỗi khi gửi file: {ex.Message}");
                }
            }

        }
        private void ScrollToBottom()
        {
            ChatScrollViewer?.ScrollToEnd();
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;
            else
                return FindParent<T>(parentObject);
        }

        private void ChatHeader_SearchClicked(object sender, EventArgs e)
        {
            var chatPage = FindParent<ChatPage>(this);
            if (chatPage != null)
            {
                chatPage.GroupInforPanel.ShowSearchPanel(); // Hàm này ta sẽ thêm ở bước sau
            }
        }

        private async Task<string> GetUserNameById(string userId)
        {
            try
            {
                var user = await firebase.Child("users").Child(userId).OnceSingleAsync<User>();
                return user.FullName ?? user.UserName ?? "Không rõ";
            }
            catch
            {
                return "Không rõ";
            }
        }

        private void EmojiButton_Click(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = !EmojiPopup.IsOpen;
        }

        private void EmojiPopup_EmojiSelected(object sender, string emoji)
        {
            MessageInput.Text += emoji;
            EmojiPopup.IsOpen = false;
        }


        public StackPanel ChatPanelRef => this.ChatPanel;
    }
}
