using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class User
    {
        public int id { get; set; }
        public string userName { get; set; }
        public string password { get; set; }
        public string email { get; set; }
        public string phoneNumber { get; set; }
        public string fullName { get; set; }
        public string avatarUrl { get; set; }
        public bool isOnline { get; set; }
        public DateTime createAt { get; set; }
        public DateTime lastActive { get; set; }
    }
}
