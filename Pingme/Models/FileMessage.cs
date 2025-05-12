using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pingme.Models
{
    class FileMessage
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadDate { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public FileMessage(string fileName, string filePath, string fileType, long fileSize)
        {
            FileName = fileName;
            FilePath = filePath;
            FileType = fileType;
            FileSize = fileSize;
            UploadDate = DateTime.Now;
        }
    }
}
