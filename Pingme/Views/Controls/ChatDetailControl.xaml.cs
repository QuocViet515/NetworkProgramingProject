using Pingme.Models;
using Pingme.Helpers;
using Pingme.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using Firebase.Database;
using System.Threading.Tasks;
using Firebase.Database.Query;
using Microsoft.Win32;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Pingme.Views.Pages;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Pingme.Views.Controls
{
    public partial class ChatDetailControl : UserControl
    {
        public bool isGroup { get; set; } = true;
        private string currentChatId;
        private FirebaseClient firebase;
        private readonly AESService _aesService = new AESService();
        private readonly RSAService _rsaService = new RSAService();
        private const string FirebaseStorageBucket = "pingmeapp-1691-1703-1784.firebasestorage.app";
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

            //var messages = await firebase.Child("messages").OnceAsync<Message>();
            string msgPath = isGroup ? $"messages/group_{currentChatId}" : $"messages/chat_{currentChatId}";
            var messages = await firebase.Child(msgPath).OnceAsync<Message>();
            //var messages = await firebase
            //    .Child("messages")
            //    .Child(currentChatId)
            //    .OnceAsync<Message>();
            //var sortedMessages = messages
            //    .Select(m => m.Object)
            //    .Where(m => m.ChatId == currentChatId)
            //    .OrderBy(m => m.SentAt)
            //    .ToList();
            var sortedMessages = messages
                .Select(m =>
                {
                    m.Object.Id = m.Key;
                    m.Object.ChatId = currentChatId; // GÁN LẠI CHAT ID ĐÃ MẤT
                    return m.Object;
                })
                .OrderBy(m => m.SentAt)
                .ToList();
            CurrentMessages = sortedMessages;
            //foreach (var item in messages)
            foreach (var msg in sortedMessages)
            {
                if (msg.Type == "call_log")
                {
                    string callTypeText = msg.CallType?.ToUpper() == "VIDEO" ? "📹 Cuộc gọi VIDEO" : "📞 Cuộc gọi AUDIO";
                    string startTime = msg.SentAt.ToLocalTime().ToString("HH:mm:ss");
                    string endTime = msg.CallEndedAt?.ToLocalTime().ToString("HH:mm:ss") ?? "Không rõ";

                    string durationText = msg.CallDuration.HasValue
                        ? $" (kéo dài {msg.CallDuration.Value / 60} phút {msg.CallDuration.Value % 60} giây)"
                        : "";

                    var label = new TextBlock
                    {
                        Text = $"{callTypeText} bắt đầu lúc {startTime} và kết thúc lúc {endTime}{durationText}",
                        Foreground = Brushes.Gray,
                        FontStyle = FontStyles.Italic,
                        Margin = new Thickness(10, 5, 10, 5),
                        TextWrapping = TextWrapping.Wrap,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };

                    ChatPanel.Children.Add(label);
                    continue; // ⛔ bỏ qua xử lý tiếp theo vì đã hiển thị rồi
                }
                //var msg = item.Object;

                //if (msg.ChatId != currentChatId) continue;

                //if (msg.Type == "file" && !string.IsNullOrEmpty(msg.FileId))
                //{
                //    // Tạo nút tải file
                //    var button = new Button
                //    {
                //        Content = $"📥 Tải xuống: {msg.FileName}",
                //        Tag = msg.FileId,
                //        Margin = new Thickness(5),
                //        Padding = new Thickness(10)
                //    };
                //    button.Click += async (s, e) =>
                //    {
                //        var dialog = new SaveFileDialog { FileName = msg.FileName };
                //        if (dialog.ShowDialog() == true)
                //        {
                //            try
                //            {
                //                string privateKeyPath = KeyManager.GetPrivateKeyPath(SessionManager.UID);
                //                await new FirebaseFileService().DownloadAndDecryptFileAsync(msg.FileId, privateKeyPath, dialog.FileName);
                //                MessageBox.Show("✅ Tải và giải mã thành công!");
                //            }
                //            catch (Exception ex)
                //            {
                //                MessageBox.Show("❌ Lỗi: " + ex.Message);
                //            }
                //        }
                //    };

                //    ChatPanel.Children.Add(button);
                //}
                if (msg.Type == "file" && !string.IsNullOrEmpty(msg.FileId))
                {
                    FrameworkElement element = CreateFileElement(msg);

                    if (msg.SenderId == SessionManager.UID)
                        ChatPanel.Children.Add(new OutgoingMessageControl(element));
                    else
                        ChatPanel.Children.Add(new IncomingMessageControl(element));

                    await Task.Delay(100);
                    ScrollToBottom();
                    continue;
                }
                else
                {
                    //string decryptedText = "[Không thể giải mã]";
                    //if (msg.SessionKeyEncrypted != null &&
                    //    msg.SessionKeyEncrypted.TryGetValue(SessionManager.UID, out string encryptedKey))
                    //{
                    //    string aesKey = _rsaService.Decrypt(encryptedKey, SessionManager.UID);
                    //    if (!string.IsNullOrEmpty(aesKey))
                    //    {
                    //        //var (plain, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.Hash);
                    //        var (plain, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.IV, msg.Tag, msg.Hash);
                    //        decryptedText = isValid ? plain : "[Sai hash – nội dung đã bị thay đổi]";
                    //        msg.Content = decryptedText;
                    //        msg.Plaintext = decryptedText;
                    //        msg.FromSelf = msg.SenderId == SessionManager.UID;
                    //        msg.SenderName = msg.FromSelf ? "Bạn" : await GetUserNameById(msg.SenderId);
                    //    }
                    //}

                    //if (msg.SenderId == SessionManager.UID)
                    //    ChatPanel.Children.Add(new OutgoingMessageControl(decryptedText));
                    //else
                    //    ChatPanel.Children.Add(new IncomingMessageControl(decryptedText));

                    string content = isGroup
                        ? msg.RawText
                        : "[Không thể giải mã]";


                    Console.WriteLine($"[DEBUG] Bạn đang thay: \"{content}\"");

                    if (!isGroup)
                    {
                        string decryptedText = "[Không thể giải mã]";
                        if (msg.SessionKeyEncrypted != null &&
                            msg.SessionKeyEncrypted.TryGetValue(SessionManager.UID, out string encryptedKey))
                        {
                            string aesKey = _rsaService.Decrypt(encryptedKey, SessionManager.UID);
                            if (!string.IsNullOrEmpty(aesKey))
                            {
                                var (plain, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.IV, msg.Tag, msg.Hash);
                                decryptedText = isValid ? plain : "[Sai hash – nội dung đã bị thay đổi]";
                                content = decryptedText;
                            }
                        }
                    }
                    else
                    {

                    }

                    if (msg.SenderId == SessionManager.UID)
                        ChatPanel.Children.Add(new OutgoingMessageControl(content));
                    else
                        ChatPanel.Children.Add(new IncomingMessageControl(content));
                }

                await Task.Delay(100);
                ScrollToBottom();
            }
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            var plainText = MessageInput.Text.Trim();

            Console.WriteLine($"[DEBUG] Bạn đang gửi: \"{plainText}\"");

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
            if (isGroup)
            {
                var groupMsg = new Message
                {
                    Id = Guid.NewGuid().ToString(),
                    ChatId = currentChatId,
                    SenderId = SessionManager.UID,
                    ReceiverId = null,
                    IsGroup = true,
                    Type = "text",
                    RawText = plainText,
                    SentAt = DateTime.UtcNow
                };

                //await firebase.Child("messages").Child(groupMsg.Id).PutAsync(groupMsg);
                string groupMsgPath = $"messages/group_{currentChatId}";
                await firebase.Child(groupMsgPath).Child(groupMsg.Id).PutAsync(groupMsg);

                var groupRef = firebase.Child("chatGroups").Child(currentChatId);
                var rawGroup = await groupRef.OnceSingleAsync<object>();
                if (rawGroup == null)
                {
                    var newGroup = new ChatGroup
                    {
                        Id = currentChatId,
                        LastMessageId = groupMsg.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Members = new List<string> { groupMsg.SenderId },
                        Admin = new List<string> { groupMsg.SenderId },
                        Name = "Nhóm mới",
                        AvatarUrl = ""
                    };
                    await groupRef.PutAsync(newGroup);
                }
                else
                {
                    await groupRef.PatchAsync(new
                    {
                        LastMessageId = groupMsg.Id,
                        UpdatedAt = DateTime.UtcNow
                    });
                }

                ChatPanel.Children.Add(new OutgoingMessageControl(plainText));
                MessageInput.Text = "";
                ScrollToBottom();
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
                //string domainPubKeyPath = "C:\\Apache24\\conf\\ssl\\ec-public-key.pem";

                //string domainPubKeyPath = @"..\..\..\..\ssl\ec-public-key.pem";
                //string domainPem = System.IO.File.ReadAllText(domainPubKeyPath);

                string domainPubKeyPath = PemPathResolver.GetSslPath("ec-public-key.pem");
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


            //await firebase.Child("messages").Child(msg.Id).PutAsync(msg);
            string msgPath = isGroup ? $"messages/group_{currentChatId}" : $"messages/chat_{currentChatId}";
            await firebase.Child(msgPath).Child(msg.Id).PutAsync(msg);

            if (!isGroup)
            {
                var chatRef = firebase.Child("chats").Child(currentChatId);
                //var existingChat = await chatRef.OnceSingleAsync<Chat>();

                //if (existingChat == null)
                //{
                //    // Chat chưa tồn tại, khởi tạo mới
                //    var newChat = new Chat
                //    {
                //        Id = currentChatId,
                //        User1 = msg.SenderId,
                //        User2 = receiverId,
                //        LastMessageId = msg.Id,
                //        CreatedAt = DateTime.UtcNow,
                //        UpdatedAt = DateTime.UtcNow
                //    };

                //    await chatRef.PutAsync(newChat);
                //}
                //else
                //{
                //    // Chat đã có, chỉ cập nhật
                //    await chatRef.Child("LastMessageId").PutAsync(msg.Id);
                //    await chatRef.Child("UpdatedAt").PutAsync(DateTime.UtcNow);
                //}
                var raw = await chatRef.OnceSingleAsync<object>();

                if (raw == null)
                {
                    var newChat = new Chat
                    {
                        Id = currentChatId,
                        User1 = msg.SenderId,
                        User2 = receiverId,
                        LastMessageId = msg.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await chatRef.PutAsync(newChat);
                }
                else
                {
                    //await chatRef.Child("LastMessageId").PutAsync(msg.Id);
                    //await chatRef.Child("UpdatedAt").PutAsync(DateTime.UtcNow);
                    await chatRef.PatchAsync(new
                    {
                        LastMessageId = msg.Id,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            else
            {
                var groupRef = firebase.Child("chatGroups").Child(currentChatId);
                //var existingGroup = await groupRef.OnceSingleAsync<ChatGroup>();

                //if (existingGroup == null)
                //{
                //    // Phòng nhóm chưa tồn tại (hiếm gặp)
                //    var newGroup = new ChatGroup
                //    {
                //        Id = currentChatId,
                //        LastMessageId = msg.Id,
                //        CreatedAt = DateTime.UtcNow,
                //        UpdatedAt = DateTime.UtcNow,
                //        Members = new List<string> { msg.SenderId },
                //        Admin = new List<string> { msg.SenderId },
                //        Name = "Nhóm mới", // hoặc để trống nếu không có
                //        AvatarUrl = ""
                //    };

                //    await groupRef.PutAsync(newGroup);
                //}
                //else
                //{
                //    // Cập nhật bình thường
                //    await groupRef.Child("LastMessageId").PutAsync(msg.Id);
                //    await groupRef.Child("UpdatedAt").PutAsync(DateTime.UtcNow);
                //}
                var rawGroup = await groupRef.OnceSingleAsync<object>();

                if (rawGroup == null)
                {
                    var newGroup = new ChatGroup
                    {
                        Id = currentChatId,
                        LastMessageId = msg.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        Members = new List<string> { msg.SenderId },
                        Admin = new List<string> { msg.SenderId },
                        Name = "Nhóm mới", // hoặc để trống nếu không có
                        AvatarUrl = ""
                    };

                    await groupRef.PutAsync(newGroup);
                }
                else
                {
                    await groupRef.PatchAsync(new
                    {
                        LastMessageId = msg.Id,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

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
                    //MessageBox.Show("❗ Chưa hỗ trợ gửi file trong nhóm.");
                    //return;
                    try
                    {
                        // Upload file KHÔNG mã hóa
                        var fileService = new FirebaseFileService();
                        var fileId = await fileService.UploadPlainFileAsync(filePath, currentChatId, senderId);

                        var msg = new Message
                        {
                            Id = Guid.NewGuid().ToString(),
                            ChatId = currentChatId,
                            SenderId = senderId,
                            ReceiverId = null,
                            IsGroup = true,
                            Type = "file",
                            FileId = fileId,
                            FileName = fileName,
                            SentAt = DateTime.UtcNow
                        };

                        //await firebase.Child("messages").Child(msg.Id).PutAsync(msg);
                        string msgPath = $"messages/group_{currentChatId}";
                        await firebase.Child(msgPath).Child(msg.Id).PutAsync(msg);

                        // Hiển thị luôn
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
                                    // Tải file thô, không giải mã
                                    await new FirebaseFileService().DownloadPlainFileAsync(fileId, dialog.FileName);
                                    MessageBox.Show("✅ Tải thành công!");
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show("❌ Lỗi tải: " + ex.Message);
                                }
                            }
                        };
                        ChatPanel.Children.Add(button);
                        ScrollToBottom();
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("❌ Gửi file nhóm thất bại: " + ex.Message);
                        return;
                    }
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

                    //var fileId = await fileService.UploadEncryptedFileAsync(filePath, receiverPublicKeyXml, senderId, receiverId, currentChatId);                    // hoặc bạn lấy ID thực từ FirebaseStorage nếu có
                    //var msg = new Message
                    //{
                    //    Id = Guid.NewGuid().ToString(),
                    //    ChatId = currentChatId,
                    //    SenderId = senderId,
                    //    ReceiverId = receiverId,
                    //    IsGroup = isGroup,
                    //    Type = "file",
                    //    FileId = fileId,
                    //    FileName = fileName,
                    //    SentAt = DateTime.UtcNow
                    //};

                    ////await firebase.Child("messages").Child(msg.Id).PutAsync(msg);
                    //string msgPath = $"messages/chat_{currentChatId}";
                    //await firebase.Child(msgPath).Child(msg.Id).PutAsync(msg);

                    Message sentMsg = await fileService.UploadEncryptedFileAsync(filePath, receiverPublicKeyXml, senderId, receiverId, currentChatId);

                    // Hiển thị luôn file trong giao diện
                    //var button = new Button
                    //{
                    //    Content = $"📥 Tải xuống: {fileName}",
                    //    Tag = fileId,
                    //    Margin = new Thickness(5),
                    //    Padding = new Thickness(10)
                    //};
                    //button.Click += async (s, args) =>
                    //{
                    //    var dialog = new SaveFileDialog { FileName = fileName };
                    //    if (dialog.ShowDialog() == true)
                    //    {
                    //        try
                    //        {
                    //            string privateKeyPath = KeyManager.GetPrivateKeyPath(senderId);
                    //            await new FirebaseFileService().DownloadAndDecryptFileAsync(fileId, privateKeyPath, dialog.FileName);
                    //            MessageBox.Show("✅ Tải và giải mã thành công!");
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            MessageBox.Show("❌ Lỗi: " + ex.Message);
                    //        }
                    //    }
                    //};
                    //ChatPanel.Children.Add(button);
                    //ScrollToBottom();

                    //var button = new Button
                    //{
                    //    Content = $"📥 Tải xuống: {sentMsg.FileName}",
                    //    Tag = sentMsg.FileId,
                    //    Margin = new Thickness(5),
                    //    Padding = new Thickness(10)
                    //};
                    //button.Click += async (s, args) =>
                    //{
                    //    var dialog = new SaveFileDialog { FileName = sentMsg.FileName };
                    //    if (dialog.ShowDialog() == true)
                    //    {
                    //        try
                    //        {
                    //            string privateKeyPath = KeyManager.GetPrivateKeyPath(senderId);
                    //            await new FirebaseFileService().DownloadAndDecryptFileAsync(sentMsg.FileId, privateKeyPath, dialog.FileName);
                    //            MessageBox.Show("✅ Tải và giải mã thành công!");
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            MessageBox.Show("❌ Lỗi: " + ex.Message);
                    //        }
                    //    }
                    //};
                    //ChatPanel.Children.Add(button);

                    FrameworkElement element = CreateFileElement(sentMsg);
                    ChatPanel.Children.Add(new OutgoingMessageControl(element));
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
        private FrameworkElement CreateFileElement(Message msg)
        {
            string fileName = msg.FileName?.ToLower() ?? "";
            bool isImage = fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".png") || fileName.EndsWith(".gif") || fileName.EndsWith(".webp");
            bool isVideo = fileName.EndsWith(".mp4") || fileName.EndsWith(".webm");
            bool isEncrypted = !msg.IsGroup;

            if (isImage)
            {
                var image = new Image
                {
                    Width = 200,
                    Height = 200,
                    Margin = new Thickness(5),
                    Stretch = Stretch.UniformToFill,
                };

                _ = Task.Run(async () =>
                {
                    try
                    {
                        string path = await new FirebaseFileService().DownloadToTempFileAsync(msg.FileId, isEncrypted);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            image.Source = new BitmapImage(new Uri(path, UriKind.Absolute));

                            image.MouseLeftButtonUp += (s, e) =>
                            {
                                new Pingme.Views.Windows.ImageViewerWindow(path).ShowDialog();
                            };
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"❌ Không tải được ảnh: {ex.Message}");
                    }
                });

                return WrapFileElement(image);
            }
            else if (isVideo)
            {
                string videoPath = null;
                MediaElement thumbnail = new MediaElement
                {
                    Width = 240,
                    Height = 160,
                    LoadedBehavior = MediaState.Manual,
                    UnloadedBehavior = MediaState.Manual,
                    Stretch = Stretch.UniformToFill,
                    Volume = 0,
                    IsMuted = true
                };

                Border videoCard = new Border
                {
                    Background = Brushes.Black,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(4),
                    Margin = new Thickness(5),
                    Child = thumbnail,
                    Cursor = System.Windows.Input.Cursors.Hand
                };

                // Tải video nền (ngầm)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        videoPath = await new FirebaseFileService().DownloadToTempFileAsync(msg.FileId, isEncrypted);
                        if (!System.IO.File.Exists(videoPath)) throw new FileNotFoundException("Video không tồn tại.", videoPath);

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            thumbnail.Source = new Uri(videoPath);
                            thumbnail.MediaOpened += (s, e) =>
                            {
                                thumbnail.Position = TimeSpan.FromSeconds(0.1);
                                thumbnail.Play();
                            };
                        });
                    }
                    catch (Exception ex)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            videoCard.Child = new TextBlock
                            {
                                Text = $"❌ Không tải được video: {ex.Message}",
                                Foreground = Brushes.Red,
                                Margin = new Thickness(8)
                            };
                        });
                    }
                });

                // Khi nhấn vào thì DỪNG lại và mở cửa sổ xem lớn
                videoCard.MouseLeftButtonUp += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(videoPath) && System.IO.File.Exists(videoPath))
                    {
                        thumbnail.Stop(); // Dừng phát nhỏ
                        new Pingme.Views.Windows.VideoViewerWindow(videoPath).ShowDialog();
                    }
                    else
                    {
                        MessageBox.Show("❌ Video chưa được tải hoàn tất!");
                    }
                };

                return WrapFileElement(videoCard);
            }
            else
            {
                var button = new Button
                {
                    Content = $"📥 Tải xuống: {msg.FileName}",
                    Margin = new Thickness(5),
                    Padding = new Thickness(8, 4, 8, 4),
                    Background = Brushes.WhiteSmoke,
                    Foreground = Brushes.Black
                };

                button.Click += async (s, e) =>
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog
                    {
                        FileName = msg.FileName
                    };

                    if (dialog.ShowDialog() == true)
                    {
                        try
                        {
                            if (isEncrypted)
                            {
                                string privateKeyPath = KeyManager.GetPrivateKeyPath(SessionManager.UID);
                                await new FirebaseFileService().DownloadAndDecryptFileAsync(msg.FileId, privateKeyPath, dialog.FileName);
                            }
                            else
                            {
                                // Nếu là nhóm thì tải trực tiếp theo FileId
                                if (!msg.IsGroup)
                                {
                                    var metadata = await new FirebaseService().GetFileMetadataAsync(msg.FileId);
                                    await new FirebaseFileService().DownloadPlainFileAsync(metadata.storagePath, dialog.FileName);
                                }
                                else
                                {
                                    await new FirebaseFileService().DownloadPlainFileAsync(msg.FileId, dialog.FileName);
                                }
                            }

                            MessageBox.Show("✅ Tải file thành công!");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("❌ Lỗi khi tải file: " + ex.Message);
                        }
                    }
                };

                return WrapFileElement(button);
            }
        }
        private Border WrapFileElement(UIElement child)
        {
            return new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Background = Brushes.White,
                Padding = new Thickness(6),
                Margin = new Thickness(4),
                Child = child
            };
        }
    }
}
