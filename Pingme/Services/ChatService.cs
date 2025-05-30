using Firebase.Database;
using Pingme.Models;
using System;
using System.Collections.Generic;
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
            var privateKey = await _firebaseService.GetPrivateKeyAsync(currentUserId);

            foreach (var msg in encryptedMessages)
            {
                if (msg.SessionKeyEncrypted != null && msg.SessionKeyEncrypted.ContainsKey(currentUserId))
                {
                    try
                    {
                        // Giải mã AES key đã được RSA mã hóa
                        string encryptedSessionKey = msg.SessionKeyEncrypted[currentUserId];
                        string aesKey = _rsaService.Decrypt(encryptedSessionKey, privateKey);

                        // Giải mã nội dung tin nhắn
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

                // Mã hóa khóa AES bằng RSA cho cả hai phía
                string encryptedKeyForSender = _rsaService.Encrypt(aesKey, sender.PublicKey);
                string encryptedKeyForReceiver = _rsaService.Encrypt(aesKey, receiver.PublicKey);

                // Tạo tin nhắn
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

                // Gửi lên Firebase
                await _firebaseService.SendEncryptedMessageAsync(roomId, message);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi gửi tin nhắn: " + ex.Message);
            }
        }

        public void ListenForMessages(string senderId, string receiverId)
        {
            var chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);

            _firebaseService.SubscribeToIncomingMessages(chatRoomId, senderId, message =>
            {
                OnNewMessageReceived?.Invoke(message);
            });
        }

        public Task MarkMessagesAsReadAsync(string senderId, string receiverId)
        {
            var chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);
            return _firebaseService.MarkMessagesAsReadAsync(chatRoomId, senderId);
        }
    }
}
