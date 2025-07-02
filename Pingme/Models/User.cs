using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class User
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
        public string PublicKey { get; set; } //RSA Public Key dùng để mã hóa AES key

        public string PublicKeySignature { get; set; } // Chữ ký của PublicKey, dùng để xác thực tính toàn vẹn của PublicKey
        //public string id { get; set; }
        //public string userName { get; set; }
        //public string password { get; set; }
        //public string email { get; set; }
        //public string phoneNumber { get; set; }
        //public string fullName { get; set; }
        //public string avatarUrl { get; set; }
        //public bool isOnline { get; set; }
        //public string PublicKey { get; set; }
        //public DateTime createAt { get; set; }
        //public DateTime lastActive { get; set; }
    }
}
