using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class FileMetadata
    {
        public string fileName { get; set; }
        public string storagePath { get; set; }
        //public string encryptedAESKeyIV { get; set; }
        public string encryptedAESKey { get; set; }
        public string encryptedIV { get; set; }
        public string encryptedTag { get; set; }
        public long timestamp { get; set; }
        public string senderId { get; set; }
        public string receiverId { get; set; }
        public string hash { get; set; }  // Thêm hash
        public string encryptedAESKeyForSender { get; set; }
        public string encryptedIVForSender { get; set; }
        public string encryptedTagForSender { get; set; }

    }
}
