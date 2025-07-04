using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class Message
    {
        public string Id { get; set; }
        public string ChatId { get; set; }
        public bool IsGroup { get; set; }
        public string SenderId { get; set; }
        //public string Text { get; set; }

        //Dữ liệu mã hóa
        public string Ciphertext { get; set; }
        public string EncryptedAESKey { get; set; }
        public string IV { get; set; }
        public string Hash { get; set; }

        //public string FileId { get; set; }
        public string Type { get; set; }  // "text" hoặc "file"
        public bool IsRead { get; set; }
        public Dictionary<string, DateTime> SeenBy { get; set; }
        public string ReplyToMessageId { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public string Tag { get; set; }     
        //theme
        public string ReceiverId { get; set; }


        //public string SenderId { get; set; }
        //public string Content { get; set; }
        //public DateTime Timestamp { get; set; }
        //public string ReceiverId { get; set; }
        //public bool IsRead { get; set; }
        //public string Hash { get; set; }  // Thêm thuộc tính hash
        //public string Type { get; set; }
        public Dictionary<string, string> SessionKeyEncrypted { get; set; }
        //[JsonIgnore]
        //public bool FromSelf { get; set; }
        //public string DisplayText => Type == "file" ? $"📎 {FileName}" : Content;
        public string FileId { get; set; }
        public string FileName { get; set; }

        [JsonIgnore] public string Content { get; set; }
        [JsonIgnore] public bool FromSelf {  get; set; }
    }
}
