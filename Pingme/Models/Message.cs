using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class Message
    {
        public string Id { get; set; }
        public string ChatId { get; set; }
        public bool IsGroup { get; set; }
        public string SenderId { get; set; }
        public string Text { get; set; }
        public string FileId { get; set; }
        public Dictionary<string, DateTime> SeenBy { get; set; }
        public string ReplyToMessageId { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }
}
