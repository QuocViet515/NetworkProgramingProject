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
        public string encryptedAESKeyIV { get; set; }
        public long timestamp { get; set; }
        public string senderId { get; set; }
        public string receiverId { get; set; }
        public string hash { get; set; }  // ✅ Thêm dòng này

    }
}
