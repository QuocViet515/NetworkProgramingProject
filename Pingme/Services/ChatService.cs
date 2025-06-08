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
            string privateKeyPath = KeyManager.LoadPrivateKeyPath(currentUserId);
            string privateKey = File.ReadAllText(privateKeyPath);

            foreach (var msg in encryptedMessages)
            {
                if (msg.SessionKeyEncrypted != null && msg.SessionKeyEncrypted.ContainsKey(currentUserId))
                {
                    try
                    {
                        string encryptedSessionKey = msg.SessionKeyEncrypted[currentUserId];
                        string aesKey = _rsaService.Decrypt(encryptedSessionKey, privateKey);
                        msg.Content = _aesService.DecryptMessage(msg.Content, aesKey);
                    }
                    catch (Exception ex)
                    {
                        msg.Content = "[Không thể giải mã]";
                        Console.WriteLine("Decrypt error: " + ex.Message);
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
                // Sinh khóa AES tạm thời và mã hóa nội dung
                string aesKey = _aesService.GenerateAesKey();
                string encryptedContent = _aesService.EncryptMessage(plainContent, aesKey);

                // Lấy public key của người gửi và người nhận
                var sender = await _firebaseService.GetUserByIdAsync(senderId);
                var receiver = await _firebaseService.GetUserByIdAsync(receiverId);

                // ✅ Kiểm tra xem đã có public key chưa
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

                // Mã hóa khóa AES bằng RSA
                string encryptedKeyForSender = _rsaService.Encrypt(aesKey, sender.PublicKey);
                string encryptedKeyForReceiver = _rsaService.Encrypt(aesKey, receiver.PublicKey);

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

        public void ListenForMessages(string senderId, string receiverId)
        {
            string chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);

            _firebaseService.SubscribeToIncomingMessages(chatRoomId, senderId, async message =>
            {
                string privateKeyPath = KeyManager.LoadPrivateKeyPath(senderId);
                string privateKey = File.ReadAllText(privateKeyPath);

                if (message.SessionKeyEncrypted != null && message.SessionKeyEncrypted.ContainsKey(senderId))
                {
                    try
                    {
                        string encryptedKey = message.SessionKeyEncrypted[senderId];
                        string aesKey = _rsaService.Decrypt(encryptedKey, privateKey);
                        message.Content = _aesService.DecryptMessage(message.Content, aesKey);
                    }
                    catch
                    {
                        message.Content = "[Không thể giải mã]";
                    }
                }
                else
                {
                    message.Content = "[Không có khóa giải mã]";
                }

                message.FromSelf = message.SenderId == senderId;

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
