using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    internal class CallLog
    {
        public string Id { get; set; }
        public string CallerId { get; set; }
        public string ReceiverId { get; set; }
        public bool IsGroupCall { get; set; }
        public string GroupId { get; set; }
        public string Type { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; } 
        public string Status { get; set; }
    }
}
