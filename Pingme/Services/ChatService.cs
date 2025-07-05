using Firebase.Database;
using Pingme.Helpers;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

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
                        string aesKey = _rsaService.Decrypt(encryptedSessionKey, userId);

                        //var (decryptedText, isValid) = _aesService.DecryptMessageWithHashCheck(msg.Ciphertext, aesKey, msg.Hash ?? "");
                        var (decryptedText, isValid) = _aesService.DecryptMessageWithHashCheck(
                            msg.Ciphertext,
                            aesKey,
                            msg.IV,
                            msg.Tag,
                            msg.Hash ?? ""
                        );


                        msg.Content = decryptedText + (isValid ? "" : "\n⚠️ Tin nhắn có thể đã bị thay đổi!");
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

                // Sử dụng hàm mới: EncryptMessageWithHash
                var (encryptedContent, iv, tag, contentHash) = _aesService.EncryptMessageWithHash(plainContent, aesKey);


                var sender = await _firebaseService.GetUserByIdAsync(senderId);
                var receiver = await _firebaseService.GetUserByIdAsync(receiverId);

                if (string.IsNullOrWhiteSpace(sender.PublicKey) || string.IsNullOrWhiteSpace(receiver.PublicKey))
                {
                    MessageBox.Show("Người gửi hoặc người nhận chưa có public key. Không thể gửi tin nhắn.");
                    return;
                }

                string encryptedKeyForSender = _rsaService.EncryptWithXml(aesKey, sender.PublicKey);
                string encryptedKeyForReceiver = _rsaService.EncryptWithXml(aesKey, receiver.PublicKey);

                string roomId = FirebaseService.GetChatRoomId(senderId, receiverId);
                var message = new Message
                {
                    SenderId = senderId,
                    ReceiverId = receiverId,
                    Ciphertext = encryptedContent,
                    IV = iv,
                    Tag = tag,
                    SentAt = DateTime.UtcNow,
                    IsRead = false,
                    Hash = contentHash,
                    SessionKeyEncrypted = new Dictionary<string, string>
                    {
                        { senderId, encryptedKeyForSender },
                        { receiverId, encryptedKeyForReceiver }
                    },
                    IsGroup = false,
                    IsDeleted = false,
                    IsEdited = false
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

                if (message.Type == "file")
                {
                    // Bỏ qua file
                }
                else if (!System.IO.File.Exists(privPath))
                {
                    message.Content = "[Không tìm thấy khóa giải mã]";
                }
                else if (message.SessionKeyEncrypted == null || !message.SessionKeyEncrypted.ContainsKey(userId))
                {
                    message.Content = "[Không có khóa giải mã]";
                }
                else
                {
                    try
                    {
                        string encryptedKey = message.SessionKeyEncrypted[userId];
                        string aesKey = _rsaService.Decrypt(encryptedKey, userId);

                        //var (decryptedText, isValid) = _aesService.DecryptMessageWithHashCheck(message.Ciphertext, aesKey, message.Hash ?? "");
                        var (decryptedText, isValid) = _aesService.DecryptMessageWithHashCheck(
                             message.Ciphertext,
                             aesKey,
                             message.IV,
                             message.Tag,
                             message.Hash ?? ""
                         );

                        message.Content = decryptedText + (isValid ? "" : "\n⚠️ Tin nhắn có thể đã bị thay đổi!");
                    }
                    catch (Exception ex)
                    {
                        message.Content = "[Không thể giải mã]";
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
