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
        public string SenderId { get; set; }
        public string Content { get; set; }
        public DateTime Timestamp { get; set; }
        public string ReceiverId { get; set; }
        public bool IsRead { get; set; }
        public string FileName { get; set; }  

        public string Type { get; set; }
        public Dictionary<string, string> SessionKeyEncrypted { get; set; }
        [JsonIgnore]
        public bool FromSelf { get; set; }
        public string DisplayText => Type == "file" ? $"📎 {FileName}" : Content;
        public string FileId { get; set; }

    }
}
