using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class User
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AvatarUrl { get; set; }
        public DateTime Birthday { get; set; }
        public string Address { get; set; }
        public string Status { get; set; } // online/offline
        public DateTime CreateAt { get; set; }
        public DateTime LastActiveAt { get; set; }
        //public string Password { get; set; }
    }
}
