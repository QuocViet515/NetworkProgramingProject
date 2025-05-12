using Firebase.Database;
using Pingme.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Pingme.Services
{
    public class ChatService
    {
        private readonly FirebaseService _firebaseService = new FirebaseService();

        public Action<Message> OnNewMessageReceived { get; set; }

        public async Task<List<Message>> LoadChatHistory(string currentUserId, string targetUserId)
        {
            string roomId = FirebaseService.GetChatRoomId(currentUserId, targetUserId);
            return await _firebaseService.LoadMessagesAsync(roomId);
        }


        public Task SendMessageAsync(string senderId, string receiverId, string content)
        {
            return _firebaseService.SendMessageAsync(senderId, receiverId, content);
        }

        public void ListenForMessages(string senderId, string receiverId)
        {
            // Lấy chatRoomId chuẩn hóa giữa 2 user
            var chatRoomId = FirebaseService.GetChatRoomId(senderId, receiverId);

            // Lắng nghe tin nhắn đến từ phía người dùng khác
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
