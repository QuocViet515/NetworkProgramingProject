using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    public class File
    {
        public string Id { get; set; }
        public string MessageId { get; set; }
        public string UploaderId { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public string FileUrl { get; set; }
        public DateTime UploadedAt { get; set; }

        public string EncryptedAESKeyIV { get; set; }
        public string Hash { get; set; }
    }
}
