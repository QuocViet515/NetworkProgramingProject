using Firebase.Database;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace Pingme.Services
{
    public class ChatService
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();
        private readonly RSAService _rsaService = new RSAService();
        private readonly AESService _aesService = new AESService();

        public Action<Message> OnNewMessageReceived { get; set; }

        public async Task<List<Message>> LoadChatHistory(string currentUserId, string targetUserId)
        {
            string roomId = FirebaseService.GetChatRoomId(currentUserId, targetUserId);
            var encryptedMessages = await _firebaseService.LoadMessagesAsync(roomId);
            string userId = currentUserId;

            foreach (var msg in encryptedMessages)
            {
                if (msg.SessionKeyEncrypted != null && msg.SessionKeyEncrypted.ContainsKey(userId))
                {
                    try
                    {
                        string encryptedSessionKey = msg.SessionKeyEncrypted[userId];
                        Console.WriteLine($"🔐 AES Key mã hóa (Base64): {encryptedSessionKey}");

                        // ✅ Decrypt bằng userId
                        string aesKey = _rsaService.Decrypt(encryptedSessionKey, userId);

                        Console.WriteLine($"🔓 AES Key đã giải mã: {aesKey}");
                        Console.WriteLine($"🧾 Nội dung đã mã hóa: {msg.Content}");

                        msg.Content = _aesService.DecryptMessage(msg.Content, aesKey);
                        Console.WriteLine($"✅ Nội dung đã giải mã: {msg.Content}");
                    }
                    catch (Exception ex)
                    {
                        msg.Content = "[Không thể giải mã]";
                        Console.WriteLine("❌ Decrypt error: " + ex.Message);
                    }
                }
                else
                {
                    msg.Content = "[Không có khóa giải mã]";
                }
            }

            return encryptedMessages;
        }

        public async Task SendMessageAsync(string senderId, string receiverId, string plainContent)
        {
            try
            {
                string aesKey = _aesService.GenerateAesKey();
                string encryptedContent = _aesService.EncryptMessage(plainContent, aesKey);

                var sender = await _firebaseService.GetUserByIdAsync(senderId);
                var receiver = await _firebaseService.GetUserByIdAsync(receiverId);

                if (string.IsNullOrWhiteSpace(sender.PublicKey))
                {
                    MessageBox.Show("Người gửi chưa có public key. Không thể gửi tin nhắn.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(receiver.PublicKey))
                {
                    MessageBox.Show("Người nhận chưa có public key. Không thể gửi tin nhắn.");
                    return;
                }

                string encryptedKeyForSender = _rsaService.EncryptWithXml(aesKey, sender.PublicKey);
                string encryptedKeyForReceiver = _rsaService.EncryptWithXml(aesKey, receiver.PublicKey);


                Console.WriteLine($"📤 Gửi AES key đến: {receiver.userName}");
                Console.WriteLine($"🔐 Public key của người nhận (50 ký tự đầu): {receiver.PublicKey.Substring(0, 50)}...");
                Console.WriteLine($"🔐 AES key gốc: {aesKey}");
                Console.WriteLine($"🧊 AES key mã hóa (RSA): {encryptedKeyForReceiver}");

                string roomId = FirebaseService.GetChatRoomId(senderId, receiverId);
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Content = encryptedContent,
                    Timestamp = DateTime.UtcNow,
                    IsRead = false,
                    SessionKeyEncrypted = new Dictionary<string, string>
                    {
                        { senderId, encryptedKeyForSender },
                        { receiverId, encryptedKeyForReceiver }
                    }
                };

                await _firebaseService.SendEncryptedMessageAsync(roomId, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message);
            }
        }

        public void ListenForMessages(string currentUserId, string targetUserId)
        {
            string chatRoomId = FirebaseService.GetChatRoomId(currentUserId, targetUserId);
            string userId = currentUserId;

            _firebaseService.SubscribeToIncomingMessages(chatRoomId, currentUserId, async message =>
            {
                string privPath = KeyManager.GetPrivateKeyPath(userId);
                Console.WriteLine($"👂 Nghe tin nhắn mới từ room: {chatRoomId}");
                Console.WriteLine($"🔐 Đang kiểm tra private key tại: {privPath}");

                if (message.Type == "file")
                {
                    // Không giải mã nếu là tin nhắn file
                    Console.WriteLine($"📁 Tin nhắn file, content là fileId: {message.Content}");
                }
                else if (!File.Exists(privPath))
                {
                    message.Content = "[Không tìm thấy khóa giải mã]";
                    Console.WriteLine($"❌ Không tìm thấy private key tại: {privPath}");
                }
                else if (message.SessionKeyEncrypted == null || !message.SessionKeyEncrypted.ContainsKey(userId))
                {
                    message.Content = "[Không có khóa giải mã]";
                    Console.WriteLine($"⚠️ Không có khóa AES RSA cho user {userId} trong message.");

                    if (message.SessionKeyEncrypted != null)
                        Console.WriteLine("📦 SessionKeyEncrypted keys: " + string.Join(", ", message.SessionKeyEncrypted.Keys));
                    else
                        Console.WriteLine("📦 SessionKeyEncrypted is null.");
                }
                else
                {
                    try
                    {
                        string encryptedKey = message.SessionKeyEncrypted[userId];
                        Console.WriteLine($"🔐 Encrypted AES key: {encryptedKey}");

                        string aesKey = _rsaService.Decrypt(encryptedKey, userId);
                        Console.WriteLine($"🔓 AES key được giải mã: {aesKey}");

                        Console.WriteLine($"🧾 Encrypted content: {message.Content}");
                        message.Content = _aesService.DecryptMessage(message.Content, aesKey);
                        Console.WriteLine($"✅ Tin nhắn đã giải mã: {message.Content}");
                    }
                    catch (Exception ex)
                    {
                        message.Content = "[Không thể giải mã]";
                        Console.WriteLine($"❌ Lỗi giải mã với message từ {message.SenderId} lúc {message.Timestamp}: {ex.Message}");
                    }
                }

                message.FromSelf = message.SenderId == currentUserId;
                OnNewMessageReceived?.Invoke(message);
            });
        }


        public Task MarkMessagesAsReadAsync(string senderId, string receiverId)
        {
            string chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);
            return _firebaseService.MarkMessagesAsReadAsync(chatRoomId, senderId);
        }
    }
}
