using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    internal class Chat
    {
        public string Id { get; set; }
        public string User1 { get; set; }
        public string User2 { get; set; }
        public string LastMessageId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
