using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    internal class Notification
    {
        public string Id { get; set; }
        public string ReceiverId { get; set; }
        public string Type { get; set; } // e.g., new_message, added_to_group
        public Dictionary<string, string> Data { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}
