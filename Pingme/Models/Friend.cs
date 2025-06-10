using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class Friend
    {
        public string Id { get; set; }
        public string User1 { get; set; }
        public string User2 { get; set; }
        public string Status { get; set; } // accept, watting, delete
        public DateTime CreatedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
    }
}
